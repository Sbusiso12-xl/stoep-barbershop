using System.ComponentModel.DataAnnotations;
using StoepBarbershop.Api.Models;

namespace StoepBarbershop.Api.Dtos;

public class CreateBookingRequest
{
    [Required(ErrorMessage = "Please choose a service.")]
    public string ServiceId { get; set; } = default!;

    [Required(ErrorMessage = "Please choose a barber.")]
    public string BarberId { get; set; } = default!;

    // "yyyy-MM-dd"
    [Required(ErrorMessage = "Please choose a date.")]
    public string Date { get; set; } = default!;

    [Range(0, 1440, ErrorMessage = "Please choose a time.")]
    public int StartMinutes { get; set; }

    [Required(ErrorMessage = "Please enter your name.")]
    [MinLength(2, ErrorMessage = "Please enter your full name.")]
    [MaxLength(120)]
    public string CustomerName { get; set; } = default!;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    public string CustomerEmail { get; set; } = default!;

    [Required(ErrorMessage = "Please enter a phone number so we can reach you.")]
    public string CustomerPhone { get; set; } = default!;

    [MaxLength(500)] public string? Notes { get; set; }
}

public record BookingResponse(
    Guid Id,
    string Reference,
    string ServiceId,
    string ServiceName,
    int Price,
    string BarberId,
    string BarberName,
    string Date,
    int StartMinutes,
    int EndMinutes,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAt)
{
    public static BookingResponse From(Booking b) => new(
        b.Id, b.Reference, b.ServiceId, b.Service?.Name ?? "", b.Service?.Price ?? 0,
        b.BarberId, b.Barber?.Name ?? "", b.Date, b.StartMinutes, b.EndMinutes,
        b.CustomerName, b.CustomerEmail, b.CustomerPhone, b.Notes,
        b.Status.ToString(), b.CreatedAt);
}

public class UpdateBookingStatusRequest
{
    [Required] public BookingStatus Status { get; set; }
}
