namespace StoepBarbershop.Api.Models;

public class Barber
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Role { get; set; } = default!;
    public string Specialty { get; set; } = default!;
    public int Years { get; set; }
    public string Bio { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public BarberAccount? Account { get; set; }
    public List<Booking> Bookings { get; set; } = new();
}
