using StoepBarbershop.Api.Models;

namespace StoepBarbershop.Api.Dtos;

public record ServiceDto(string Id, string Name, string Description, int Price, int DurationMinutes)
{
    public static ServiceDto From(Service s) => new(s.Id, s.Name, s.Description, s.Price, s.DurationMinutes);
}

public record BarberDto(string Id, string Name, string Role, string Specialty, int Years, string Bio)
{
    public static BarberDto From(Barber b) => new(b.Id, b.Name, b.Role, b.Specialty, b.Years, b.Bio);
}
