using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Data.Entities;

namespace Vulindlela.Booking.Api.Data;

public interface ISlotRepository
{
    Task<Slot?> GetAsync(Guid id, CancellationToken ct);
    Task<List<Slot>> ListForBranchAsync(Guid branchId, CancellationToken ct);
    Task<List<Slot>> ListTakenAsync(Guid citizenId, CancellationToken ct);
}

public interface IAppointmentRepository
{
    Task<Appointment?> GetAsync(Guid id, CancellationToken ct);
    Task<List<Appointment>> ListForCitizenAsync(Guid citizenId, CancellationToken ct);
    Task<List<Appointment>> ListAllAsync(CancellationToken ct);
    Task<bool> HasUpcomingForServiceAsync(Guid citizenId, string serviceType, DateTime nowUtc, CancellationToken ct);
    Task AddAsync(Appointment appointment, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public interface IBranchRepository { Task<List<Branch>> ListAsync(CancellationToken ct); }
public interface ICitizenRepository
{
    Task<Citizen?> ByUsernameAsync(string username, CancellationToken ct);
    Task<Citizen?> ByIdAsync(Guid id, CancellationToken ct);
}
public interface IAccessLogRepository
{
    Task AddAsync(AccessLog log, CancellationToken ct);
    Task<List<AccessLog>> ForSubjectAsync(Guid subjectId, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public class SlotRepository : ISlotRepository
{
    private readonly BookingDbContext _db;
    public SlotRepository(BookingDbContext db) => _db = db;
    public Task<Slot?> GetAsync(Guid id, CancellationToken ct) =>
        _db.Slots.Include(s => s.Branch).FirstOrDefaultAsync(s => s.Id == id, ct);
    public Task<List<Slot>> ListForBranchAsync(Guid branchId, CancellationToken ct) =>
        _db.Slots.Where(s => s.BranchId == branchId).OrderBy(s => s.StartsAtUtc).ToListAsync(ct);
    public Task<List<Slot>> ListTakenAsync(Guid citizenId, CancellationToken ct) =>
        (from a in _db.Appointments where a.CitizenId == citizenId select a.Slot).ToListAsync(ct);
}

public class AppointmentRepository : IAppointmentRepository
{
    private readonly BookingDbContext _db;
    public AppointmentRepository(BookingDbContext db) => _db = db;
    public Task<Appointment?> GetAsync(Guid id, CancellationToken ct) =>
        _db.Appointments.Include(a => a.Slot).FirstOrDefaultAsync(a => a.Id == id, ct);
    public Task<List<Appointment>> ListForCitizenAsync(Guid citizenId, CancellationToken ct) =>
        _db.Appointments.Include(a => a.Slot)
            .Where(a => a.CitizenId == citizenId).OrderByDescending(a => a.CreatedAtUtc).ToListAsync(ct);
    public Task<List<Appointment>> ListAllAsync(CancellationToken ct) =>
        _db.Appointments.Include(a => a.Slot).ThenInclude(s => s.Branch).Include(a => a.Citizen)
            .OrderBy(a => a.Slot.StartsAtUtc).ToListAsync(ct);
    public Task<bool> HasUpcomingForServiceAsync(Guid citizenId, string serviceType, DateTime nowUtc, CancellationToken ct) =>
        _db.Appointments.AnyAsync(a => a.CitizenId == citizenId && a.ServiceType == serviceType
            && a.Slot.StartsAtUtc >= nowUtc, ct);
    public async Task AddAsync(Appointment appointment, CancellationToken ct) =>
        await _db.Appointments.AddAsync(appointment, ct);
    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class BranchRepository : IBranchRepository
{
    private readonly BookingDbContext _db;
    public BranchRepository(BookingDbContext db) => _db = db;
    public Task<List<Branch>> ListAsync(CancellationToken ct) =>
        _db.Branches.OrderBy(b => b.Code).ToListAsync(ct);
}

public class CitizenRepository : ICitizenRepository
{
    private readonly BookingDbContext _db;
    public CitizenRepository(BookingDbContext db) => _db = db;
    public Task<Citizen?> ByUsernameAsync(string username, CancellationToken ct) =>
        _db.Citizens.FirstOrDefaultAsync(c => c.Username == username, ct);
    public Task<Citizen?> ByIdAsync(Guid id, CancellationToken ct) =>
        _db.Citizens.FirstOrDefaultAsync(c => c.Id == id, ct);
}

public class AccessLogRepository : IAccessLogRepository
{
    private readonly BookingDbContext _db;
    public AccessLogRepository(BookingDbContext db) => _db = db;
    public async Task AddAsync(AccessLog log, CancellationToken ct) => await _db.AccessLogs.AddAsync(log, ct);
    public Task<List<AccessLog>> ForSubjectAsync(Guid subjectId, CancellationToken ct) =>
        _db.AccessLogs.Where(l => l.SubjectCitizenId == subjectId)
            .OrderByDescending(l => l.TimestampUtc).ToListAsync(ct);
    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}