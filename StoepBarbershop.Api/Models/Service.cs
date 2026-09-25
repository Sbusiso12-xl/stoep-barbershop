namespace StoepBarbershop.Api.Models;

public class Service
{
    // Matches the ids used in the existing front-end (assets/js/data.js)
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}
