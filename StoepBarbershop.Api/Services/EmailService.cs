// Services/EmailService.cs
using System.Net;
using StoepBarbershop.Api.Models;
using System.Net.Mail;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendBookingConfirmationAsync(Booking booking, CancellationToken ct = default)
    {
        try
        {
            var host = _config["Email:Host"]!;
            var port = int.Parse(_config["Email:Port"]!);
            var username = _config["Email:Username"]!;
            var password = _config["Email:Password"]!;
            var fromAddress = _config["Email:FromAddress"]!;
            var fromName = _config["Email:FromName"]!;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = $"Booking confirmed — {booking.Reference}",
                Body = BuildBody(booking),
                IsBodyHtml = false
            };
            message.To.Add(booking.CustomerEmail);

            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            // A failed email should never fail the booking — the booking is
            // already saved by the time this runs. Log it so it's visible,
            // but don't let it bubble up.
            _logger.LogError(ex, "Failed to send booking confirmation email for {Reference}", booking.Reference);
        }
    }

    private static string BuildBody(Booking booking)
    {
        var start = TimeSpan.FromMinutes(booking.StartMinutes);
        var end = TimeSpan.FromMinutes(booking.EndMinutes);
        return $"""
            Hi {booking.CustomerName},

            You're booked in at Stoep Barbershop.

            Reference: {booking.Reference}
            Service: {booking.Service?.Name}
            Barber: {booking.Barber?.Name}
            Date: {booking.Date}
            Time: {start:hh\:mm} – {end:hh\:mm}

            142 Duncan Street, Hatfield, Pretoria, 0028
            012 111 4402

            See you then.
            Stoep Barbershop
            """;
    }
}
