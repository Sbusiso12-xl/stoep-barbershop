namespace StoepBarbershop.Api.Models;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Customer-facing reference, e.g. "STOEP-M3K9QF2A"
    public string Reference { get; set; } = default!;

    public string ServiceId { get; set; } = default!;
    public Service Service { get; set; } = default!;

    public string BarberId { get; set; } = default!;
    public Barber Barber { get; set; } = default!;

    // Stored as "yyyy-MM-dd" to match the front end and keep date-only semantics
    // unambiguous regardless of the DB provider's date handling.
    public string Date { get; set; } = default!;

    public int StartMinutes { get; set; }
    public int EndMinutes { get; set; }

    public string CustomerName { get; set; } = default!;
    public string CustomerEmail { get; set; } = default!;
    public string CustomerPhone { get; set; } = default!;
    public string? Notes { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // One row per 15-minute slot this booking occupies. The unique index on
    // (BarberId, Date, SlotStart) is what actually prevents double-booking
    // at the database level — see BookedSlot / BookingService.
    public List<BookedSlot> Slots { get; set; } = new();
}
