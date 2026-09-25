using StoepBarbershop.Api.Models;

namespace StoepBarbershop.Api.Services;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateToken(BarberAccount account);
}
