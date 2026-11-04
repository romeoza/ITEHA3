using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Vulindlela.Portal.Services;

/// <summary>
/// Copies the portal's cookie auth header onto outbound calls to the Booking API,
/// so the Booking API's own cookie middleware can authenticate the request.
/// Also propagates the API's Set-Cookie back to the browser, keeping the two
/// cookie jars in step for the lifetime of the session.
/// </summary>
public sealed class CookieForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _http;

    public CookieForwardingHandler(IHttpContextAccessor http) => _http = http;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _http.HttpContext;
        if (ctx is not null)
        {
            // Forward the portal's own auth cookie value to the Booking API.
            var portalCookie = await ctx.GetTokenAsync("vulindlela.booking") // see note below
                ?? await ctx.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!string.IsNullOrEmpty(portalCookie) && request.Headers.Authorization is null)
            {
                // The Booking API expects its own cookie name; set it explicitly.
                request.Headers.TryAddWithoutValidation("Cookie", $"vulindlela.booking={portalCookie}");
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If the Booking API refreshed its cookie, pass the Set-Cookie back to the browser.
        if (ctx is not null && response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var c in cookies)
                ctx.Response.Headers.Append("Set-Cookie", c);
        }

        return response;
    }
}