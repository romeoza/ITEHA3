using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Portal.Services;
using Vulindlela.Shared;

namespace Vulindlela.Portal.Controllers;

/// <summary>Official overview: every booking across all branches. Each read is logged by the Booking API.</summary>
[Authorize(Roles = "Official")]
public class OfficialController : Controller
{
    private readonly IBookingApiClient _bookings;
    public OfficialController(IBookingApiClient bookings) => _bookings = bookings;

    public async Task<IActionResult> Index()
    {
        try
        {
            ViewBag.Branches = await _bookings.BranchesAsync();
            return View(await _bookings.AllAppointmentsAsync());
        }
        catch
        {
            ViewBag.Degraded = "Booking service is temporarily unavailable. Please try again shortly.";
            ViewBag.Branches = new List<BranchDto>();
            return View(new List<OfficialAppointmentDto>());
        }
    }
}
