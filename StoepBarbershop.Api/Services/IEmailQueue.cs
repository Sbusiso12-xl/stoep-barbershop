namespace StoepBarbershop.Api.Services;

public interface IEmailQueue
{
    ValueTask QueueAsync(
        BookingEmailMessage message,
        CancellationToken ct = default);
}