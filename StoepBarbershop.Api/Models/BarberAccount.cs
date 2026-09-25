namespace StoepBarbershop.Api.Models;

// Login credentials for a barber (or the shop owner) to access the dashboard.
public class BarberAccount
{
    public int Id { get; set; }
    public string BarberId { get; set; } = default!;
    public Barber Barber { get; set; } = default!;

    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    // Owner can see every barber's bookings; a plain barber only sees their own.
    public bool IsOwner { get; set; }
}
