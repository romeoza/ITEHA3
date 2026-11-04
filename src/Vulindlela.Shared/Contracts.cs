using System;
using System.Collections.Generic;
using System.Text;

namespace Vulindlela.Shared;

// CROS BOUNDARY DTOs

public record BranchDto(Guid Id, string Code, string Name);

public record SlotDto(Guid Id, Guid BranchId, DateTime StartsAtUtc, bool IsTaken);

public record BookRequest(Guid SlotId, string ServiceType);

public record AppointmentDto(
    Guid Id, Guid SlotId, Guid BranchId, string ServiceType,
    decimal Fee, DateTime CreatedAtUtc, string Status);

public record AppointmentCreatedNotification(
    Guid AppointmentId, Guid CitizenId, string ServiceType);

public record ClearanceSubmissionRequest(
    Guid AppointmentId, Guid CitizenId, string ServiceType,
    IReadOnlyList<string> DocumentNames);

public record ClearanceSubmissionDto(
    Guid Id, Guid AppointmentId, string Status,
    IReadOnlyList<string> MissingDocuments, DateTime UpdatedAtUtc);

public record ClearanceStatusDto(
    Guid AppointmentId, string Status, IReadOnlyList<string> MissingDocuments);

public static class ServiceTypes
{
    public const string IdCollection = "ID_COLLECTION";
    public const string IdApplication = "ID_APPLICATION";
    public const string Passport = "PASSPORT";
    public const string PassportUrgent = "PASSPORT_URGENT";
    public const string BirthCert = "BIRTH_CERT";
    public const string MarriageCert = "MARRIAGE_CERT";
    public const string DeathCert = "DEATH_CERT";

    public static readonly IReadOnlyDictionary<string, decimal> BaseFees =
        new Dictionary<string, decimal>
        {
            [IdCollection] = 0m,
            [IdApplication] = 140m,
            [Passport] = 400m,
            [PassportUrgent] = 800m,
            [BirthCert] = 75m,
            [MarriageCert] = 75m,
            [DeathCert] = 75m,
        };

    public const decimal PassportSurcharge = 65m;

    public static bool IsKnown(string serviceType) => BaseFees.ContainsKey(serviceType);

    public static decimal FeeFor(string serviceType)
    {
        if (!IsKnown(serviceType)) throw new ArgumentException("Unknown service type", nameof(serviceType));
        var fee = BaseFees[serviceType];
        if (serviceType is Passport or PassportUrgent) fee += PassportSurcharge;
        return fee;
    }
}


