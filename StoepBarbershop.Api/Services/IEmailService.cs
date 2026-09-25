// Services/IEmailService.cs
using StoepBarbershop.Api.Models;

public interface IEmailService
{
    Task SendBookingConfirmationAsync(Booking booking, CancellationToken ct = default);
}