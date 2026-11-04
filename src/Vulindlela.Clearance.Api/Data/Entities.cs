namespace Vulindlela.Clearance.Api.Data.Entities;

public class DocumentSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AppointmentId { get; set; }
    public Guid CitizenId { get; set; }
    public string ServiceType { get; set; } = "";
    public string DocumentNamesCsv { get; set; } = "";
    public string MissingDocumentsCsv { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}