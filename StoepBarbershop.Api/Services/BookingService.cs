using Microsoft.EntityFrameworkCore;
using StoepBarbershop.Api.Data;
using StoepBarbershop.Api.Dtos;
using StoepBarbershop.Api.Exceptions;
using StoepBarbershop.Api.Models;

namespace StoepBarbershop.Api.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BookingService> _logger;
    private readonly IEmailService _emailService;

    // A booking always touches (barberId, date) and nothing outside it, so a
    // lock keyed on that pair is exactly as coarse as it needs to be: two
    // customers booking different barbers, or the same barber on different
    // days, never wait on each other. This is a first, cheap line of
    // defence *within this process*. The real guarantee — the one that
    // holds even across multiple instances of this API behind a load
    // balancer — is the unique index on BookedSlot (see AppDbContext and
    // the catch block below). The lock just avoids paying for an avoidable
    // rollback/retry on the common single-instance case.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public BookingService(AppDbContext db, ILogger<BookingService> logger, IEmailService emailService)
    {
        _db = db;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<AvailabilityResponse> GetAvailabilityAsync(string? barberId, string date, string serviceId, CancellationToken ct = default)
    {
        var parsedDate = ParseDate(date);
        var service = await _db.Services.FindAsync(new object?[] { serviceId }, ct)
            ?? throw new ValidationFailedException($"Unknown service '{serviceId}'.");

        var hours = ShopSchedule.GetHours(parsedDate);
        if (hours is null)
        {
            return new AvailabilityResponse(date, barberId, new List<Dtos.TimeSlotDto>());
        }

        var (open, close) = hours.Value;
        var gridStarts = SlotCalculator.GenerateGridStarts(open, close, service.DurationMinutes);

        // Which 15-minute grid slots are already taken for this barber (or,
        // if no barber was specified, unioned across all barbers so "any
        // barber" availability doesn't show a slot only one of them has free).
        var occupiedQuery = _db.BookedSlots.Where(s => s.Date == date);
        occupiedQuery = barberId is null ? occupiedQuery : occupiedQuery.Where(s => s.BarberId == barberId);
        var occupied = (await occupiedQuery.Select(s => s.SlotStart).ToListAsync(ct)).ToHashSet();

        var now = DateTimeOffset.Now;
        var isToday = DateOnly.FromDateTime(now.DateTime) == parsedDate;
        var earliestMinutes = isToday ? now.Hour * 60 + now.Minute + ShopSchedule.MinNoticeMinutes : -1;

        var slots = gridStarts.Select(start =>
        {
            var neededSlots = SlotCalculator.SlotsFor(start, service.DurationMinutes);
            var blocked = start < earliestMinutes || neededSlots.Any(occupied.Contains);
            return new Dtos.TimeSlotDto(start, start + service.DurationMinutes, !blocked);
        }).ToList();

        return new AvailabilityResponse(date, barberId, slots);
    }

    public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        var service = await _db.Services.FindAsync(new object?[] { request.ServiceId }, ct)
            ?? throw new ValidationFailedException("Unknown service.");
        var barber = await _db.Barbers.FindAsync(new object?[] { request.BarberId }, ct)
            ?? throw new ValidationFailedException("Unknown barber.");

        var date = ParseDate(request.Date);
        var hours = ShopSchedule.GetHours(date)
            ?? throw new ValidationFailedException("The shop is closed that day.");

        var (open, close) = hours;
        var endMinutes = request.StartMinutes + service.DurationMinutes;

        if (request.StartMinutes < open || endMinutes > close)
            throw new ValidationFailedException("That time falls outside opening hours.");

        var now = DateTimeOffset.Now;
        var isToday = DateOnly.FromDateTime(now.DateTime) == date;
        if (isToday && request.StartMinutes < now.Hour * 60 + now.Minute + ShopSchedule.MinNoticeMinutes)
            throw new ValidationFailedException("That time is too soon — please choose a later slot.");

        var slotStarts = SlotCalculator.SlotsFor(request.StartMinutes, service.DurationMinutes);
        var lockKey = $"{request.BarberId}|{request.Date}";
        var gate = Locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(ct);
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            // Belt-and-braces check: even though the unique index is the real
            // guarantee, checking first lets us return a friendly 409 instead
            // of surfacing a raw DB exception in the common case.
            var alreadyTaken = await _db.BookedSlots
                .Where(s => s.BarberId == request.BarberId && s.Date == request.Date)
                .Select(s => s.SlotStart)
                .AnyAsync(s => slotStarts.Contains(s), ct);

            if (alreadyTaken)
                throw new BookingConflictException("That slot was just taken. Please pick another time.");

            var booking = new Booking
            {
                Reference = GenerateReference(),
                ServiceId = service.Id,
                BarberId = barber.Id,
                Date = request.Date,
                StartMinutes = request.StartMinutes,
                EndMinutes = endMinutes,
                CustomerName = request.CustomerName.Trim(),
                CustomerEmail = request.CustomerEmail.Trim(),
                CustomerPhone = request.CustomerPhone.Trim(),
                Notes = request.Notes?.Trim(),
                Status = BookingStatus.Confirmed,
            };

            foreach (var slotStart in slotStarts)
            {
                booking.Slots.Add(new BookedSlot
                {
                    BarberId = request.BarberId,
                    Date = request.Date,
                    SlotStart = slotStart
                });
            }

            _db.Bookings.Add(booking);

            try
            {
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // The unique index on BookedSlot fired: someone else committed
                // an overlapping booking in the tiny window between our check
                // above and this insert (e.g. two API instances racing).
                await tx.RollbackAsync(ct);
                _logger.LogInformation(ex, "Booking clash for {Barber} on {Date}", request.BarberId, request.Date);
                throw new BookingConflictException("That slot was just taken. Please pick another time.");
            }

            await _db.Entry(booking).Reference(b => b.Service).LoadAsync(ct);
            await _db.Entry(booking).Reference(b => b.Barber).LoadAsync(ct);
            await _emailService.SendBookingConfirmationAsync(booking, ct);
            return BookingResponse.From(booking);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<BookingResponse?> GetByReferenceAsync(string reference, CancellationToken ct = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.Service)
            .Include(b => b.Barber)
            .FirstOrDefaultAsync(b => b.Reference == reference, ct);
        return booking is null ? null : BookingResponse.From(booking);
    }

    public async Task<List<BookingResponse>> GetDashboardBookingsAsync(string? barberId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr = to.ToString("yyyy-MM-dd");

        var query = _db.Bookings
            .Include(b => b.Service)
            .Include(b => b.Barber)
            .Where(b => string.Compare(b.Date, fromStr) >= 0 && string.Compare(b.Date, toStr) <= 0);

        if (barberId is not null)
            query = query.Where(b => b.BarberId == barberId);

        var results = await query
            .OrderBy(b => b.Date).ThenBy(b => b.StartMinutes)
            .ToListAsync(ct);

        return results.Select(BookingResponse.From).ToList();
    }

    public async Task<BookingResponse> UpdateStatusAsync(Guid bookingId, BookingStatus status, string requestingBarberId, bool isOwner, CancellationToken ct = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.Service)
            .Include(b => b.Barber)
            .Include(b => b.Slots)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new ValidationFailedException("Booking not found.");

        if (!isOwner && booking.BarberId != requestingBarberId)
            throw new UnauthorizedAccessException("You can only update your own bookings.");

        booking.Status = status;

        // Free the slots back up when a booking is cancelled/no-show so that
        // time becomes bookable again; keep them reserved for Completed.
        if (status is BookingStatus.Cancelled or BookingStatus.NoShow)
        {
            _db.BookedSlots.RemoveRange(booking.Slots);
        }

        await _db.SaveChangesAsync(ct);
        return BookingResponse.From(booking);
    }

    private static DateOnly ParseDate(string date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsed))
            throw new ValidationFailedException("Date must be in yyyy-MM-dd format.");
        return parsed;
    }

    private static string GenerateReference()
    {
        var stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString("x").ToUpperInvariant();
        var rand = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"STOEP-{stamp}{rand}";
    }
}
