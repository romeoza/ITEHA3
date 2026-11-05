using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Data;
using Vulindlela.Booking.Api.Data.Entities;
using Vulindlela.Shared;

namespace Vulindlela.Booking.Api.Business;

public enum BookingStatus { Booked, SlotTaken, Invalid }

public record BookingResult(BookingStatus Status, Guid? Id = null, string? Message = null)
{
    public static BookingResult Booked(Guid id) => new(BookingStatus.Booked, id);
    public static BookingResult SlotTaken() => new(BookingStatus.SlotTaken, Message: "That slot was just taken.");
    public static BookingResult Invalid(string message) => new(BookingStatus.Invalid, Message: message);
}

public interface IBookingService
{
    Task<BookingResult> BookAsync(Guid citizenId, BookRequest request, CancellationToken ct);
    Task<AppointmentDto?> GetAsync(Guid citizenId, Guid appointmentId, string role, CancellationToken ct);
    Task<List<AppointmentDto>> ListMineAsync(Guid citizenId, CancellationToken ct);
    Task<List<OfficialAppointmentDto>> ListAllForOfficialAsync(Guid readerId, string role, CancellationToken ct);
}

public class BookingService : IBookingService
{
    private readonly ISlotRepository _slots;
    private readonly IAppointmentRepository _appointments;
    private readonly IAccessLogRepository _logs;
    private readonly IClearanceClient _clearance;
    private readonly ILogger<BookingService> _log;

    public BookingService(ISlotRepository slots, IAppointmentRepository appointments,
        IAccessLogRepository logs, IClearanceClient clearance, ILogger<BookingService> log)
    {
        _slots = slots; _appointments = appointments; _logs = logs; _clearance = clearance; _log = log;
    }

    public async Task<BookingResult> BookAsync(Guid citizenId, BookRequest request, CancellationToken ct)
    {
        if (!ServiceTypes.IsKnown(request.ServiceType))
            return BookingResult.Invalid("Unknown service type.");

        var slot = await _slots.GetAsync(request.SlotId, ct);
        if (slot is null) return BookingResult.Invalid("Slot not found.");

        // Business rule: one live appointment per service type. Once it has passed, the citizen may book again.
        // Note: this is a check-then-act. Two simultaneous requests from the same citizen could both pass it.
        if (await _appointments.HasUpcomingForServiceAsync(citizenId, request.ServiceType, DateTime.UtcNow, ct))
            return BookingResult.Invalid(
                $"You already have an upcoming {request.ServiceType} appointment. You can book again once it has passed.");

        // ADR 0002: no pre-check. The unique index on SlotId is the guard.
        var appointment = new Appointment
        {
            SlotId = slot.Id,
            CitizenId = citizenId,
            ServiceType = request.ServiceType,
            Fee = ServiceTypes.FeeFor(request.ServiceType)
        };
        await _appointments.AddAsync(appointment, ct);
        try
        {
            await _appointments.SaveAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return BookingResult.SlotTaken();
        }

        await SafeNotifyClearanceAsync(appointment, ct);
        return BookingResult.Booked(appointment.Id);
    }

    public async Task<AppointmentDto?> GetAsync(Guid citizenId, Guid appointmentId, string role, CancellationToken ct)
    {
        var appt = await _appointments.GetAsync(appointmentId, ct);
        if (appt is null) return null;
        if (role != "Official" && appt.CitizenId != citizenId) return null;

        await _logs.AddAsync(new AccessLog
        {
            SubjectCitizenId = appt.CitizenId,
            ReaderCitizenId = citizenId,
            ReaderRole = role,
            Reason = "Appointment lookup",
            Resource = $"appointment:{appt.Id}"
        }, ct);
        await _logs.SaveAsync(ct);

        return new AppointmentDto(appt.Id, appt.SlotId, appt.Slot.BranchId,
            appt.ServiceType, appt.Fee, appt.CreatedAtUtc, "Booked");
    }

    public async Task<List<AppointmentDto>> ListMineAsync(Guid citizenId, CancellationToken ct)
    {
        var list = await _appointments.ListForCitizenAsync(citizenId, ct);
        return list.Select(a => new AppointmentDto(a.Id, a.SlotId, a.Slot.BranchId,
            a.ServiceType, a.Fee, a.CreatedAtUtc, "Booked")).ToList();
    }

    public async Task<List<OfficialAppointmentDto>> ListAllForOfficialAsync(Guid readerId, string role, CancellationToken ct)
    {
        if (role != "Official") return new();

        var list = await _appointments.ListAllAsync(ct);

        // Every record an official reads is logged against the citizen it belongs to.
        foreach (var a in list)
        {
            await _logs.AddAsync(new AccessLog
            {
                SubjectCitizenId = a.CitizenId,
                ReaderCitizenId = readerId,
                ReaderRole = role,
                Reason = "Official overview of bookings",
                Resource = $"appointment:{a.Id}"
            }, ct);
        }
        await _logs.SaveAsync(ct);

        return list.Select(a => new OfficialAppointmentDto(
            a.Id, a.Citizen.FullName, a.Slot.Branch.Name, a.ServiceType, a.Fee, a.Slot.StartsAtUtc)).ToList();
    }

    private async Task SafeNotifyClearanceAsync(Appointment appt, CancellationToken ct)
    {
        try
        {
            await _clearance.NotifyAppointmentCreatedAsync(
                new AppointmentCreatedNotification(appt.Id, appt.CitizenId, appt.ServiceType), ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Clearance notification failed for appointment {Id}; booking still succeeded.", appt.Id);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
}