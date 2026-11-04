using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Business;
using Vulindlela.Booking.Api.Data;
using Vulindlela.Booking.Api.Data.Entities;

namespace Vulindlela.Booking.Tests;

public sealed class SqliteFixture : IDisposable
{
    private readonly SqliteConnection _keepAlive;
    public string ConnectionString { get; }
    public Guid CitizenId { get; }

    public SqliteFixture()
    {
        var name = "vulindlela-tests-" + Guid.NewGuid().ToString("N");
        ConnectionString = $"Data Source={name};Mode=Memory;Cache=Shared";

        // Open the keep-alive FIRST, on the same connection that will run EnsureCreated.
        _keepAlive = new SqliteConnection(ConnectionString);
        _keepAlive.Open();

        // Build the schema on the keep-alive connection itself, not on a transient one.
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlite(_keepAlive)          // <-- pass the CONNECTION, not the string
            .Options;
        using (var ctx = new BookingDbContext(options))
        {
            ctx.Database.EnsureCreated();

            var (hash, salt) = PasswordHasher.Create("Passw0rd!");
            var citizen = new Citizen
            {
                Username = "test-citizen",
                IdNumber = "8001015009087",
                FullName = "Test Citizen",
                Role = "Citizen",
                PasswordHash = hash,
                PasswordSalt = salt
            };
            ctx.Citizens.Add(citizen);
            ctx.SaveChanges();
            CitizenId = citizen.Id;
        }
    }

    public BookingDbContext NewContext() =>
        new BookingDbContext(new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlite(ConnectionString)     // each test context opens its OWN connection
            .Options);

    public void Dispose() => _keepAlive.Dispose();
}