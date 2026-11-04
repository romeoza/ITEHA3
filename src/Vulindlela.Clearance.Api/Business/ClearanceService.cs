using Vulindlela.Clearance.Api.Data;
using Vulindlela.Clearance.Api.Data.Entities;
using Vulindlela.Shared;

namespace Vulindlela.Clearance.Api.Business;

public interface IClearanceService
{
    Task<ClearanceSubmissionDto> SubmitAsync(Guid citizenId, ClearanceSubmissionRequest request, CancellationToken ct);
    Task<ClearanceStatusDto?> StatusForAppointmentAsync(Guid appointmentId, CancellationToken ct);
    IReadOnlyList<string> EvaluateRequirements(string serviceType, IReadOnlyList<string> submitted);
}

public class ClearanceService : IClearanceService
{
    private readonly ISubmissionRepository _repo;
    private readonly ISubmissionQueue _queue;

    public ClearanceService(ISubmissionRepository repo, ISubmissionQueue queue)
    { _repo = repo; _queue = queue; }

    private static readonly Dictionary<string, string[]> Requirements = new()
    {
        [ServiceTypes.Passport] = new[] { "Certified ID copy", "Passport photograph" },
        [ServiceTypes.PassportUrgent] = new[] { "Certified ID copy", "Passport photograph" },
        [ServiceTypes.BirthCert] = new[] { "Parent ID copy" },
    };

    public IReadOnlyList<string> EvaluateRequirements(string serviceType, IReadOnlyList<string> submitted)
    {
        if (!Requirements.TryGetValue(serviceType, out var required)) return Array.Empty<string>();
        return required.Where(r => !submitted.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    public async Task<ClearanceSubmissionDto> SubmitAsync(Guid citizenId, ClearanceSubmissionRequest request, CancellationToken ct)
    {
        var missing = EvaluateRequirements(request.ServiceType, request.DocumentNames);
        var sub = new DocumentSubmission
        {
            AppointmentId = request.AppointmentId,
            CitizenId = citizenId,
            ServiceType = request.ServiceType,
            DocumentNamesCsv = string.Join(";", request.DocumentNames),
            MissingDocumentsCsv = string.Join(";", missing),
            Status = "Pending"
        };
        await _repo.AddAsync(sub, ct);
        await _repo.SaveAsync(ct);
        await _queue.EnqueueAsync(sub, ct);
        return new ClearanceSubmissionDto(sub.Id, sub.AppointmentId, sub.Status, missing, sub.UpdatedAtUtc);
    }

    public async Task<ClearanceStatusDto?> StatusForAppointmentAsync(Guid appointmentId, CancellationToken ct)
    {
        var sub = await _repo.ByAppointmentAsync(appointmentId, ct);
        if (sub is null) return null;
        var missing = string.IsNullOrEmpty(sub.MissingDocumentsCsv)
            ? Array.Empty<string>() : sub.MissingDocumentsCsv.Split(';');
        return new ClearanceStatusDto(appointmentId, sub.Status, missing);
    }
}