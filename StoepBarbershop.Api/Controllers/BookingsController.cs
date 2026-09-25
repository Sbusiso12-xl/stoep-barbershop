using Microsoft.AspNetCore.Mvc;
using StoepBarbershop.Api.Dtos;
using StoepBarbershop.Api.Exceptions;
using StoepBarbershop.Api.Services;

namespace StoepBarbershop.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookings;
    public BookingsController(IBookingService bookings) => _bookings = bookings;

    /// <summary>
    /// Creates a booking. Returns 409 Conflict — not a generic 500 — if the
    /// slot was taken by someone else between the customer loading the page
    /// and submitting, so the front end can re-fetch availability and ask
    /// them to pick another time instead of silently double-booking.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bookings.CreateBookingAsync(request, ct);
            return CreatedAtAction(nameof(GetByReference), new { reference = result.Reference }, result);
        }
        catch (BookingConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ValidationFailedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Public lookup so the confirmation/"find my booking" page can work without login.</summary>
    [HttpGet("{reference}")]
    public async Task<IActionResult> GetByReference(string reference, CancellationToken ct)
    {
        var result = await _bookings.GetByReferenceAsync(reference, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
