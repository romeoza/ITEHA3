using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Portal.Services;
using Vulindlela.Shared;

namespace Vulindlela.Portal.Controllers;

[Authorize]
public class MyAppointmentsController : Controller
{
    private readonly IBookingApiClient _bookings;
    private readonly IClearanceApiClient _clearance;
    public MyAppointmentsController(IBookingApiClient bookings, IClearanceApiClient clearance)
    { _bookings = bookings; _clearance = clearance; }

    private Guid CallerId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    public async Task<IActionResult> Index()
    {
        // If Booking is down, show a plain message, never throw.
        try
        {
            var appts = await _bookings.MyAppointmentsAsync(CallerId);
            var rows = new List<(AppointmentDto A, ClearanceStatusDto? C, bool ClearanceAvailable)>();
            foreach (var a in appts)
            {
                var status = await _clearance.StatusAsync(a.Id); // already catches; null means unavailable
                rows.Add((a, status, status is not null));
            }
            return View(rows);
        }
        catch
        {
            ViewBag.Degraded = "Booking service is temporarily unavailable. Please try again shortly.";
            return View(new List<(AppointmentDto, ClearanceStatusDto?, bool)>());
        }
    }
}