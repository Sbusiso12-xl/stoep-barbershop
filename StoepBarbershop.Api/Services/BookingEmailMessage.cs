namespace StoepBarbershop.Api.Services;

public sealed record BookingEmailMessage(
    string Reference,
    string CustomerName,
    string CustomerEmail,
    string ServiceName,
    string BarberName,
    string Date,
    int StartMinutes,
    int EndMinutes
);