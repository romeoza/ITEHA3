using Microsoft.EntityFrameworkCore;
using Vulindlela.Clearance.Api.Data.Entities;

namespace Vulindlela.Clearance.Api.Data;

public interface ISubmissionRepository
{
    Task AddAsync(DocumentSubmission submission, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task<DocumentSubmission?> GetAsync(Guid id, CancellationToken ct);
    Task<DocumentSubmission?> ByAppointmentAsync(Guid appointmentId, CancellationToken ct);
    Task UpdateAsync(DocumentSubmission submission, CancellationToken ct);
}

public class SubmissionRepository : ISubmissionRepository
{
    private readonly ClearanceDbContext _db;
    public SubmissionRepository(ClearanceDbContext db) => _db = db;
    public async Task AddAsync(DocumentSubmission s, CancellationToken ct) => await _db.Submissions.AddAsync(s, ct);
    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    public Task<DocumentSubmission?> GetAsync(Guid id, CancellationToken ct) =>
        _db.Submissions.FirstOrDefaultAsync(s => s.Id == id, ct);
    public Task<DocumentSubmission?> ByAppointmentAsync(Guid appointmentId, CancellationToken ct) =>
        _db.Submissions.FirstOrDefaultAsync(s => s.AppointmentId == appointmentId, ct);
    public Task UpdateAsync(DocumentSubmission s, CancellationToken ct)
    {
        s.UpdatedAtUtc = DateTime.UtcNow;
        _db.Submissions.Update(s);
        return Task.CompletedTask;
    }
}