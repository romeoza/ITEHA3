using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Vulindlela.Portal.Services;

/// <summary>
/// Sends the citizen's Booking API session cookie with each outbound call to the Booking API.
/// The cookie is captured at sign-in (see AccountController) and stored as a claim in the
/// portal's own encrypted auth cookie, so the browser never sees the Booking API cookie.
/// </summary>
public sealed class CookieForwardingHandler : DelegatingHandler
{
    public const string BookingCookieClaim = "booking_cookie";

    private readonly IHttpContextAccessor _http;

    public CookieForwardingHandler(IHttpContextAccessor http) => _http = http;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var bookingCookie = _http.HttpContext?.User.FindFirst(BookingCookieClaim)?.Value;
        if (!string.IsNullOrEmpty(bookingCookie))
            request.Headers.TryAddWithoutValidation("Cookie", bookingCookie);

        return base.SendAsync(request, cancellationToken);
    }
}
