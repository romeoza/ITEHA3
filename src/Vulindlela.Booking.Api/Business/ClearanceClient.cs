using System.Net.Http.Json;
using Vulindlela.Shared;

namespace Vulindlela.Booking.Api.Business;

public interface IClearanceClient
{
    Task NotifyAppointmentCreatedAsync(AppointmentCreatedNotification notification, CancellationToken ct);
}

public class ClearanceClient : IClearanceClient
{
    private readonly HttpClient _http;
    public ClearanceClient(HttpClient http) => _http = http;

    public async Task NotifyAppointmentCreatedAsync(AppointmentCreatedNotification notification, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("/api/clearance/notify-appointment", notification, ct);
        response.EnsureSuccessStatusCode();
    }
}