using System.Net.Http.Json;
using Vulindlela.Shared;

namespace Vulindlela.Portal.Services;

public interface IBookingApiClient
{
    Task<SignInResult?> SignInAsync(string user, string password);
    Task<List<AppointmentDto>> MyAppointmentsAsync(Guid citizenId);
    Task<AppointmentDto?> GetAppointmentAsync(Guid appointmentId);
    Task<bool> BookAsync(Guid citizenId, Guid slotId, string serviceType);
    Task<List<BranchDto>> BranchesAsync();
    Task<List<SlotDto>> SlotsAsync(Guid branchId);
}

public interface IClearanceApiClient
{
    Task<ClearanceStatusDto?> StatusAsync(Guid appointmentId);
    Task<ClearanceSubmissionDto?> SubmitAsync(ClearanceSubmissionRequest request);
}

public record SignInResult(Guid CitizenId, string Username, string Role);

public class BookingApiClient : IBookingApiClient
{
    private readonly HttpClient _http;
    public BookingApiClient(HttpClient http) => _http = http;

    public async Task<SignInResult?> SignInAsync(string user, string password)
    {
        var r = await _http.PostAsJsonAsync("/api/auth/signin", new { Username = user, Password = password });
        if (!r.IsSuccessStatusCode) return null;
        return await r.Content.ReadFromJsonAsync<SignInResult>();
    }
    public async Task<List<AppointmentDto>> MyAppointmentsAsync(Guid citizenId) =>
        await _http.GetFromJsonAsync<List<AppointmentDto>>("/api/appointments/mine") ?? new();
    public async Task<AppointmentDto?> GetAppointmentAsync(Guid appointmentId) =>
        await _http.GetFromJsonAsync<AppointmentDto>($"/api/appointments/{appointmentId}");
    public async Task<bool> BookAsync(Guid citizenId, Guid slotId, string serviceType)
    {
        var r = await _http.PostAsJsonAsync("/api/appointments", new BookRequest(slotId, serviceType));
        return r.IsSuccessStatusCode;
    }
    public async Task<List<BranchDto>> BranchesAsync() =>
        await _http.GetFromJsonAsync<List<BranchDto>>("/api/branches") ?? new();
    public async Task<List<SlotDto>> SlotsAsync(Guid branchId) =>
        await _http.GetFromJsonAsync<List<SlotDto>>($"/api/branches/{branchId}/slots") ?? new();
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