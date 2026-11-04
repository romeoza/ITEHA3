namespace Vulindlela.Booking.Api.Data.Entities;

public class Branch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class Slot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public DateTime StartsAtUtc { get; set; }
}

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SlotId { get; set; }
    public Slot Slot { get; set; } = null!;
    public Guid CitizenId { get; set; }
    public Citizen Citizen { get; set; } = null!;
    public string ServiceType { get; set; } = "";
    public decimal Fee { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Citizen
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "";
    public string IdNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "Citizen";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
}

public class AccessLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubjectCitizenId { get; set; }
    public Guid ReaderCitizenId { get; set; }
    public string ReaderRole { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = "";
    public string Resource { get; set; } = "";
}