using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Portal.Services;

namespace Vulindlela.Portal.Controllers;

[Authorize]
public class AccessLogController : Controller
{
    private readonly IBookingApiClient _bookings;
    public AccessLogController(IBookingApiClient bookings) => _bookings = bookings;

    public async Task<IActionResult> Index()
    {
        try
        {
            var data = await _bookings.AccessLogAsync();
            return View(data);
        }
        catch
        {
            ViewBag.Degraded = "Access log is temporarily unavailable.";
            return View(new List<AccessLogRow>());
        }
    }

    public record AccessLogRow(Guid Reader, string Role, DateTime At, string Reason, string Resource);
}
