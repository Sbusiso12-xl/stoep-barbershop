using System.Threading.Channels;

namespace StoepBarbershop.Api.Services;

public class EmailQueue : IEmailQueue
{
    private readonly Channel<BookingEmailMessage> _queue;

    public EmailQueue()
    {
        _queue = Channel.CreateUnbounded<BookingEmailMessage>();
    }

    public ValueTask QueueAsync(
        BookingEmailMessage message,
        CancellationToken ct = default)
    {
        return _queue.Writer.WriteAsync(message, ct);
    }

    public IAsyncEnumerable<BookingEmailMessage> ReadAllAsync(
        CancellationToken ct)
    {
        return _queue.Reader.ReadAllAsync(ct);
    }
}
