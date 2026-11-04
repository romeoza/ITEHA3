using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Business;
using Vulindlela.Booking.Api.Data.Entities;
using Vulindlela.Shared;
using Xunit;

namespace Vulindlela.Booking.Tests;

public class ConcurrencyTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _fixture;
    public ConcurrencyTests(SqliteFixture fixture) => _fixture = fixture;

    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(50)]
    public async Task Exactly_one_appointment_survives_concurrent_bookings(int attempts)
    {
        Guid slotId;
        using (var ctx = _fixture.NewContext())
        {
            var branch = new Branch
            {
                Code = "TEST-" + Guid.NewGuid().ToString("N")[..6],
                Name = "Test Branch"
            };
            ctx.Branches.Add(branch);
            await ctx.SaveChangesAsync();

            var slot = new Slot
            {
                BranchId = branch.Id,
                StartsAtUtc = DateTime.UtcNow.AddDays(3).AddMinutes(attempts)
            };
            ctx.Slots.Add(slot);
            await ctx.SaveChangesAsync();
            slotId = slot.Id;
        }

        var tasks = Enumerable.Range(0, attempts).Select(_ => Task.Run(async () =>
        {
            using var ctx = _fixture.NewContext();
            var svc = TestFactory.BuildService(ctx);
            return await svc.BookAsync(_fixture.CitizenId,
                new BookRequest(slotId, ServiceTypes.IdCollection), default);
        }));
        var results = await Task.WhenAll(tasks);
        Assert.Equal(1, results.Count(r => r.Status == BookingStatus.Booked));

        using var verify = _fixture.NewContext();
        Assert.Equal(1, await verify.Appointments.CountAsync(a => a.SlotId == slotId));
    }
}