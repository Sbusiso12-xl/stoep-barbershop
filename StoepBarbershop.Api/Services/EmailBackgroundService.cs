namespace StoepBarbershop.Api.Services;

public class EmailBackgroundService : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(
        EmailQueue queue,
        IConfiguration config,
        ILogger<EmailBackgroundService> logger)
    {
        _queue = queue;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Stoep email background service started.");

        await foreach (
            var message in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendEmailAsync(message, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Background email failed for booking {Reference}",
                    message.Reference);
            }
        }

        _logger.LogInformation(
            "Stoep email background service stopped.");
    }

    private async Task SendEmailAsync(
        BookingEmailMessage booking,
        CancellationToken ct)
    {
        var host = _config["Email:Host"]
            ?? throw new InvalidOperationException("Email host is not configured.");

        var port = int.Parse(
            _config["Email:Port"]
            ?? throw new InvalidOperationException("Email port is not configured."));

        var username = _config["Email:Username"]
            ?? throw new InvalidOperationException("Email username is not configured.");

        var password = _config["Email:Password"]
            ?? throw new InvalidOperationException("Email password is not configured.");

        var fromAddress = _config["Email:FromAddress"]
            ?? throw new InvalidOperationException("Email from address is not configured.");

        var fromName = _config["Email:FromName"]
            ?? "Stoep Barbershop";

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(ct);

        timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

        using var client = new System.Net.Mail.SmtpClient(host, port)
        {
            Credentials = new System.Net.NetworkCredential(
                username,
                password),
            EnableSsl = true
        };

        using var mail = new System.Net.Mail.MailMessage
        {
            From = new System.Net.Mail.MailAddress(
                fromAddress,
                fromName),

            Subject = $"Booking confirmed — {booking.Reference}",

            Body = $"""
                Hi {booking.CustomerName},

                You're booked in at Stoep Barbershop.

                Reference: {booking.Reference}
                Service: {booking.ServiceName}
                Barber: {booking.BarberName}
                Date: {booking.Date}
                Time: {TimeSpan.FromMinutes(booking.StartMinutes):hh\:mm} – {TimeSpan.FromMinutes(booking.EndMinutes):hh\:mm}

                142 Duncan Street, Hatfield, Pretoria, 0028
                012 111 4402

                See you then.

                Stoep Barbershop
                """,

            IsBodyHtml = false
        };

        mail.To.Add(booking.CustomerEmail);

        await client.SendMailAsync(mail, timeoutCts.Token);

        _logger.LogInformation(
            "Booking confirmation email sent for {Reference}",
            booking.Reference);
    }
}