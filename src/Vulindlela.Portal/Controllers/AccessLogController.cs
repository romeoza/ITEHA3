using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Vulindlela.Portal.Controllers;

[Authorize]
public class AccessLogController : Controller
{
    private readonly IHttpClientFactory _http;
    public AccessLogController(IHttpClientFactory http) => _http = http;

    public async Task<IActionResult> Index()
    {
        var client = _http.CreateClient("booking");
        try
        {
            var data = await client.GetFromJsonAsync<List<AccessLogRow>>("/api/access-log/mine");
            return View(data ?? new());
        }
        catch
        {
            ViewBag.Degraded = "Access log is temporarily unavailable.";
            return View(new List<AccessLogRow>());
        }
    }

    public record AccessLogRow(Guid Reader, string Role, DateTime At, string Reason, string Resource);
}