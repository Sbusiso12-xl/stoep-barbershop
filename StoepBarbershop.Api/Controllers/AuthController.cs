using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoepBarbershop.Api.Data;
using StoepBarbershop.Api.Dtos;
using StoepBarbershop.Api.Services;

namespace StoepBarbershop.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private static readonly PasswordHasher<object> Hasher = new();

    public AuthController(AppDbContext db, IJwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    /// <summary>Barber/owner login for the dashboard. Returns a JWT to send as Authorization: Bearer.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var account = await _db.BarberAccounts
            .Include(a => a.Barber)
            .FirstOrDefaultAsync(a => a.Username == request.Username, ct);

        if (account is null)
            return Unauthorized(new { error = "Invalid username or password." });

        var verifyResult = Hasher.VerifyHashedPassword(null!, account.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Invalid username or password." });

        var (token, expiresAt) = _jwt.CreateToken(account);
        return Ok(new LoginResponse(token, expiresAt, account.BarberId, account.Barber.Name, account.IsOwner));
    }
}
