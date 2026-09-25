using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoepBarbershop.Api.Dtos;
using StoepBarbershop.Api.Exceptions;
using StoepBarbershop.Api.Models;
using StoepBarbershop.Api.Services;

namespace StoepBarbershop.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IBookingService _bookings;
    public DashboardController(IBookingService bookings) => _bookings = bookings;

    private string CurrentBarberId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsOwner => User.FindFirstValue("isOwner") == "true";

    /// <summary>
    /// Bookings for the dashboard calendar/list. A plain barber only ever
    /// sees their own bookings — the barberId query param is ignored for
    /// them and forced to their own id. The owner can see everyone's, or
    /// filter to one barber via ?barberId=.
    /// </summary>
    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings(
        [FromQuery] string? barberId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var effectiveBarberId = IsOwner ? barberId : CurrentBarberId;
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.Today);
        var toDate = to ?? fromDate.AddDays(7);

        var result = await _bookings.GetDashboardBookingsAsync(effectiveBarberId, fromDate, toDate, ct);
        return Ok(result);
    }

    /// <summary>Mark a booking Completed / Cancelled / NoShow. Barbers can only touch their own.</summary>
    [HttpPatch("bookings/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBookingStatusRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bookings.UpdateStatusAsync(id, request.Status, CurrentBarberId, IsOwner, ct);
            return Ok(result);
        }
        catch (ValidationFailedException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
