using System.Net.Http.Json;
using Vulindlela.Shared;

namespace Vulindlela.Portal.Services;

public interface IBookingApiClient
{
    Task<SignInResult?> SignInAsync(string user, string password);
    Task<List<AppointmentDto>> MyAppointmentsAsync(Guid citizenId);
    Task<AppointmentDto?> GetAppointmentAsync(Guid appointmentId);
    /// <summary>Returns null on success, otherwise the reason the booking was refused.</summary>
    Task<string?> BookAsync(Guid citizenId, Guid slotId, string serviceType);
    Task<List<BranchDto>> BranchesAsync();
    Task<List<SlotDto>> SlotsAsync(Guid branchId);
    Task<List<Vulindlela.Portal.Controllers.AccessLogController.AccessLogRow>> AccessLogAsync();
    Task<List<OfficialAppointmentDto>> AllAppointmentsAsync();
}

public interface IClearanceApiClient
{
    Task<ClearanceStatusDto?> StatusAsync(Guid appointmentId);
    Task<ClearanceSubmissionDto?> SubmitAsync(ClearanceSubmissionRequest request);
}

/// <summary>BookingCookie is the Booking API's own "vulindlela.booking=..." cookie, kept by the portal for forwarding.</summary>
public record SignInResult(Guid CitizenId, string Username, string Role, string? BookingCookie = null);

public class BookingApiClient : IBookingApiClient
{
    private readonly HttpClient _http;
    public BookingApiClient(HttpClient http) => _http = http;

    public async Task<SignInResult?> SignInAsync(string user, string password)
    {
        var r = await _http.PostAsJsonAsync("/api/auth/signin", new { Username = user, Password = password });
        if (!r.IsSuccessStatusCode) return null;

        var body = await r.Content.ReadFromJsonAsync<SignInBody>();
        if (body is null) return null;

        // Keep only the name=value part of the Booking API's Set-Cookie header.
        string? cookie = null;
        if (r.Headers.TryGetValues("Set-Cookie", out var setCookies))
            cookie = setCookies.Select(c => c.Split(';')[0]).FirstOrDefault(c => c.StartsWith("vulindlela.booking="));

        return new SignInResult(body.CitizenId, body.Username, body.Role, cookie);
    }

    private sealed record SignInBody(Guid CitizenId, string Username, string Role);

    public async Task<List<AppointmentDto>> MyAppointmentsAsync(Guid citizenId) =>
        await _http.GetFromJsonAsync<List<AppointmentDto>>("/api/appointments/mine") ?? new();

    public async Task<AppointmentDto?> GetAppointmentAsync(Guid appointmentId)
    {
        var r = await _http.GetAsync($"/api/appointments/{appointmentId}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<AppointmentDto>() : null;
    }

    public async Task<string?> BookAsync(Guid citizenId, Guid slotId, string serviceType)
    {
        var r = await _http.PostAsJsonAsync("/api/appointments", new BookRequest(slotId, serviceType));
        if (r.IsSuccessStatusCode) return null;

        var problem = await r.Content.ReadFromJsonAsync<MessageBody>();
        return problem?.Message ?? "Booking could not be completed.";
    }

    private sealed record MessageBody(string? Message);

    public async Task<List<BranchDto>> BranchesAsync() =>
        await _http.GetFromJsonAsync<List<BranchDto>>("/api/branches") ?? new();

    public async Task<List<SlotDto>> SlotsAsync(Guid branchId) =>
        await _http.GetFromJsonAsync<List<SlotDto>>($"/api/branches/{branchId}/slots") ?? new();

    public async Task<List<Vulindlela.Portal.Controllers.AccessLogController.AccessLogRow>> AccessLogAsync()
    {
        var r = await _http.GetAsync("/api/access-log/mine");
        if (!r.IsSuccessStatusCode)
            throw new HttpRequestException($"Access log returned {(int)r.StatusCode}");
        return await r.Content.ReadFromJsonAsync<List<Vulindlela.Portal.Controllers.AccessLogController.AccessLogRow>>() ?? new();
    }

    public async Task<List<OfficialAppointmentDto>> AllAppointmentsAsync() =>
        await _http.GetFromJsonAsync<List<OfficialAppointmentDto>>("/api/appointments/all") ?? new();
}

public class ClearanceApiClient : IClearanceApiClient
{
    private readonly HttpClient _http;
    public ClearanceApiClient(HttpClient http) => _http = http;

    public async Task<ClearanceStatusDto?> StatusAsync(Guid appointmentId)
    {
        try
        {
            var r = await _http.GetAsync($"/api/clearance/status/{appointmentId}");
            // 404 means the service is up but nothing has been submitted yet: not the same as unavailable.
            if (r.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new ClearanceStatusDto(appointmentId, "NotSubmitted", Array.Empty<string>());
            return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<ClearanceStatusDto>() : null;
        }
        catch { return null; }
    }

    public async Task<ClearanceSubmissionDto?> SubmitAsync(ClearanceSubmissionRequest request)
    {
        try
        {
            var r = await _http.PostAsJsonAsync("/api/clearance/submissions", request);
            return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<ClearanceSubmissionDto>() : null;
        }
        catch { return null; }
    }
}