using System.Net;
using System.Net.Mail;

namespace StoepBarbershop.Api.Services;

public sealed class EmailBackgroundService : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(
        EmailQueue queue,
        IConfiguration configuration,
        ILogger<EmailBackgroundService> logger)
    {
        _queue = queue;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stoep email background service started.");

        await foreach (var booking in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendEmailAsync(booking, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Email background service is stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Background email failed for booking {Reference}",
                    booking.Reference);
            }
        }

        _logger.LogInformation("Stoep email background service stopped.");
    }

    private async Task SendEmailAsync(
        BookingEmailMessage booking,
        CancellationToken stoppingToken)
    {
        var host = _configuration["Email:Host"];
        var portValue = _configuration["Email:Port"];
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var fromAddress = _configuration["Email:FromAddress"];
        var fromName = _configuration["Email:FromName"];

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("Email:Host is not configured.");

        if (!int.TryParse(portValue, out var port))
            throw new InvalidOperationException("Email:Port is not configured correctly.");

        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Email:Username is not configured.");

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Email:Password is not configured.");

        if (string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("Email:FromAddress is not configured.");

        if (string.IsNullOrWhiteSpace(fromName))
            fromName = "Stoep Barbershop";

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = $"Booking confirmed — {booking.Reference}",
            Body = BuildBody(booking),
            IsBodyHtml = false
        };

        message.To.Add(booking.CustomerEmail);

        _logger.LogInformation(
            "Sending booking confirmation email for {Reference} to {Email}",
            booking.Reference,
            booking.CustomerEmail);

        // Do NOT use the booking request cancellation token here.
        // A booking should already be saved even if the HTTP request ends.
        await client.SendMailAsync(message);

        _logger.LogInformation(
            "Booking confirmation email sent successfully for {Reference}",
            booking.Reference);
    }

    private static string BuildBody(BookingEmailMessage booking)
    {
        var start = TimeSpan.FromMinutes(booking.StartMinutes);
        var end = TimeSpan.FromMinutes(booking.EndMinutes);

        return $"""
            Hi {booking.CustomerName},

            Your booking at Stoep Barbershop has been confirmed.

            Booking Reference: {booking.Reference}

            Service: {booking.ServiceName}
            Barber: {booking.BarberName}
            Date: {booking.Date}
            Time: {start:hh\:mm} – {end:hh\:mm}

            Address:
            142 Duncan Street
            Hatfield
            Pretoria
            0028

            Phone: 012 111 4402

            Please keep your booking reference for your records.

            See you then.

            Stoep Barbershop
            """;
    }
}