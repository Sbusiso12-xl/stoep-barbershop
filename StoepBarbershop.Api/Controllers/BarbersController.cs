using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoepBarbershop.Api.Data;
using StoepBarbershop.Api.Dtos;

namespace StoepBarbershop.Api.Controllers;

[ApiController]
[Route("api/barbers")]
public class BarbersController : ControllerBase
{
    private readonly AppDbContext _db;
    public BarbersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<BarberDto>>> GetAll(CancellationToken ct)
    {
        var barbers = await _db.Barbers.Where(b => b.IsActive).ToListAsync(ct);
        return barbers.Select(BarberDto.From).ToList();
    }
}
