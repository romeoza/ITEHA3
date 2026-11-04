using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Portal.Services;
using Vulindlela.Shared;

namespace Vulindlela.Portal.Controllers;

[Authorize]
public class ClearanceController : Controller
{
    private readonly IClearanceApiClient _clearance;
    private readonly IBookingApiClient _bookings;
    public ClearanceController(IClearanceApiClient clearance, IBookingApiClient bookings)
    { _clearance = clearance; _bookings = bookings; }

    [HttpGet]
    public async Task<IActionResult> Submit(Guid appointmentId)
    {
        var appointment = await _bookings.GetAppointmentAsync(appointmentId);
        if (appointment is null) return NotFound();
        return View(new SubmitViewModel { Appointment = appointment, DocumentNames = new() });
    }

    [HttpPost]
    public async Task<IActionResult> Submit(Guid appointmentId, string serviceType, List<string> documentNames)
    {
        var id = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _clearance.SubmitAsync(new ClearanceSubmissionRequest(
            appointmentId, id, serviceType, documentNames ?? new()));
        TempData["ClearanceMessage"] = result is null
            ? "Clearance service is temporarily unavailable. Your appointment is still confirmed; try again later."
            : $"Submitted. Status: {result.Status}.";
        return RedirectToAction("Index", "MyAppointments");
    }

    public sealed class SubmitViewModel
    {
        public AppointmentDto Appointment { get; set; } = default!;
        public List<string> DocumentNames { get; set; } = new();
    }
}