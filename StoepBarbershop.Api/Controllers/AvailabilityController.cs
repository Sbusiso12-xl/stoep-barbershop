using Microsoft.AspNetCore.Mvc;
using StoepBarbershop.Api.Exceptions;
using StoepBarbershop.Api.Services;

namespace StoepBarbershop.Api.Controllers;

[ApiController]
[Route("api/availability")]
public class AvailabilityController : ControllerBase
{
    private readonly IBookingService _bookings;
    public AvailabilityController(IBookingService bookings) => _bookings = bookings;

    /// <summary>
    /// Available time slots for a given date (+ service, to size the slots) and,
    /// optionally, a specific barber. Same grid/overlap logic the booking wizard
    /// used to run against localStorage — now backed by the real database so
    /// every customer sees the same, correct, up-to-the-second availability.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string date, [FromQuery] string serviceId, [FromQuery] string? barberId, CancellationToken ct)
    {
        try
        {
            var result = await _bookings.GetAvailabilityAsync(barberId, date, serviceId, ct);
            return Ok(result);
        }
        catch (ValidationFailedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
