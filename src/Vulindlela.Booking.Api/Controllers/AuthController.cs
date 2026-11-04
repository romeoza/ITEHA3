using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Booking.Api.Business;

namespace Vulindlela.Booking.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    public record SignInRequest(string Username, string Password);

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken ct)
    {
        var citizen = await _auth.VerifyAsync(request.Username, request.Password, ct);
        if (citizen is null) return Unauthorized(new { message = "Invalid credentials." });

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, citizen.Id.ToString()),
            new Claim(ClaimTypes.Name, citizen.Username),
            new Claim(ClaimTypes.Role, citizen.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Ok(new { username = citizen.Username, role = citizen.Role });
    }

    [HttpPost("signout")]
    public async Task<IActionResult> SignOutUser()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}