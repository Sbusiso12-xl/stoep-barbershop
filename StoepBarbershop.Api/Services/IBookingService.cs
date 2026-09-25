using StoepBarbershop.Api.Dtos;
using StoepBarbershop.Api.Models;

namespace StoepBarbershop.Api.Services;

public interface IBookingService
{
    Task<AvailabilityResponse> GetAvailabilityAsync(string? barberId, string date, string serviceId, CancellationToken ct = default);
    Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct = default);
    Task<BookingResponse?> GetByReferenceAsync(string reference, CancellationToken ct = default);
    Task<List<BookingResponse>> GetDashboardBookingsAsync(string? barberId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<BookingResponse> UpdateStatusAsync(Guid bookingId, BookingStatus status, string requestingBarberId, bool isOwner, CancellationToken ct = default);
}
