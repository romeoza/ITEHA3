using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Booking.Api.Business;
using Vulindlela.Shared;

namespace Vulindlela.Booking.Api.Controllers;

[ApiController, Route("api/appointments"), Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IBookingService _bookings;
    public AppointmentsController(IBookingService bookings) => _bookings = bookings;

    private Guid CallerId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private string CallerRole => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Citizen";

    [HttpPost]
    public async Task<IActionResult> Book(BookRequest request, CancellationToken ct)
    {
        var result = await _bookings.BookAsync(CallerId, request, ct);
        return result.Status switch
        {
            BookingStatus.Booked => CreatedAtAction(nameof(Get), new { id = result.Id }, new { id = result.Id }),
            BookingStatus.SlotTaken => Conflict(new { message = result.Message }),
            BookingStatus.Invalid => BadRequest(new { message = result.Message }),
            _ => StatusCode(500)
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var dto = await _bookings.GetAsync(CallerId, id, CallerRole, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct) =>
        Ok(await _bookings.ListMineAsync(CallerId, ct));
}