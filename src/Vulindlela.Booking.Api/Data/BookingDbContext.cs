using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Data.Entities;

namespace Vulindlela.Booking.Api.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Citizen> Citizens => Set<Citizen>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Branch>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(32);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).IsRequired().HasMaxLength(128);
        });

        b.Entity<Slot>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
            e.HasIndex(x => new { x.BranchId, x.StartsAtUtc }).IsUnique();
        });

        b.Entity<Appointment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Slot).WithMany().HasForeignKey(x => x.SlotId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Citizen).WithMany().HasForeignKey(x => x.CitizenId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ServiceType).IsRequired().HasMaxLength(32);
            e.Property(x => x.Fee).HasColumnType("TEXT");
            // THE guarantee: one appointment per slot
            e.HasIndex(x => x.SlotId).IsUnique();
        });

        b.Entity<Citizen>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).IsRequired().HasMaxLength(64);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Role).IsRequired().HasMaxLength(16);
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.PasswordSalt).IsRequired();
        });

        b.Entity<AccessLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).IsRequired().HasMaxLength(256);
            e.Property(x => x.Resource).IsRequired().HasMaxLength(128);
            e.HasIndex(x => x.SubjectCitizenId);
        });
    }
}