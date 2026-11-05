using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Portal.Services;
using Vulindlela.Shared;

namespace Vulindlela.Portal.Controllers;

[Authorize]
public class BookingsController : Controller
{
    private readonly IBookingApiClient _bookings;
    public BookingsController(IBookingApiClient bookings) => _bookings = bookings;

    public async Task<IActionResult> Branches() => View(await _bookings.BranchesAsync());

    public async Task<IActionResult> Slots(Guid branchId, string? serviceType)
    {
        var slots = await _bookings.SlotsAsync(branchId);
        ViewBag.BranchId = branchId;
        ViewBag.ServiceType = serviceType ?? ServiceTypes.IdCollection;
        ViewBag.ServiceTypes = ServiceTypes.BaseFees.Keys.ToList();
        return View(slots);
    }

    [HttpPost]
    public async Task<IActionResult> Book(Guid slotId, string serviceType)
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var error = await _bookings.BookAsync(Guid.Parse(id), slotId, serviceType);
        TempData["BookingMessage"] = error is null ? "Booking confirmed." : error;
        return RedirectToAction("Index", "MyAppointments");
    }
}