using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoepBarbershop.Api.Data;
using StoepBarbershop.Api.Dtos;

namespace StoepBarbershop.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ServicesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<ServiceDto>>> GetAll(CancellationToken ct)
    {
        var services = await _db.Services.Where(s => s.IsActive).ToListAsync(ct);
        return services.Select(ServiceDto.From).ToList();
    }
}
