using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Portal.Services;

namespace Vulindlela.Portal.Controllers;

public class AccountController : Controller
{
    private readonly IBookingApiClient _bookings;
    public AccountController(IBookingApiClient bookings) => _bookings = bookings;

    [HttpGet] public IActionResult SignIn() => View();

    [HttpPost]
    public async Task<IActionResult> SignIn(string username, string password)
    {
        var result = await _bookings.SignInAsync(username, password);
        if (result is null) { ViewBag.Error = "Invalid credentials."; return View(); }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.CitizenId.ToString()),
            new(ClaimTypes.Name, result.Username),
            new(ClaimTypes.Role, result.Role)
        };
        if (!string.IsNullOrEmpty(result.BookingCookie))
            claims.Add(new Claim(CookieForwardingHandler.BookingCookieClaim, result.BookingCookie));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return RedirectToAction("Index", "MyAppointments");
    }

    [HttpPost]
    public async Task<IActionResult> SignOut()
    { await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); return RedirectToAction("Index", "Home"); }
}