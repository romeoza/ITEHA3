using Microsoft.EntityFrameworkCore;
using Vulindlela.Clearance.Api.Data.Entities;

namespace Vulindlela.Clearance.Api.Data;

public class ClearanceDbContext : DbContext
{
    public ClearanceDbContext(DbContextOptions<ClearanceDbContext> options) : base(options) { }
    public DbSet<DocumentSubmission> Submissions => Set<DocumentSubmission>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<DocumentSubmission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ServiceType).IsRequired().HasMaxLength(32);
            e.Property(x => x.Status).IsRequired().HasMaxLength(16);
            e.Property(x => x.DocumentNamesCsv).IsRequired();
            e.Property(x => x.MissingDocumentsCsv).IsRequired();
            e.HasIndex(x => x.AppointmentId).IsUnique();
        });
    }
}