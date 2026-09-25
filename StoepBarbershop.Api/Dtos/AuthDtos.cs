using System.ComponentModel.DataAnnotations;

namespace StoepBarbershop.Api.Dtos;

public class LoginRequest
{
    [Required] public string Username { get; set; } = default!;
    [Required] public string Password { get; set; } = default!;
}

public record LoginResponse(string Token, DateTimeOffset ExpiresAt, string BarberId, string BarberName, bool IsOwner);
