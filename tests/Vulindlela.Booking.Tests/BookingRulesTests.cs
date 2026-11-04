using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Business;
using Vulindlela.Booking.Api.Data.Entities;
using Vulindlela.Shared;
using Xunit;

namespace Vulindlela.Booking.Tests;

public class BookingRulesTests : IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _fixture;
    public BookingRulesTests(SqliteFixture fixture) => _fixture = fixture;

    [Theory]
    [InlineData(ServiceTypes.IdApplication, 140)]
    [InlineData(ServiceTypes.Passport, 465)]
    [InlineData(ServiceTypes.PassportUrgent, 865)]
    [InlineData(ServiceTypes.BirthCert, 75)]
    public void Fee_table_matches_rule(string type, decimal expected) =>
        Assert.Equal(expected, ServiceTypes.FeeFor(type));

    [Fact]
    public async Task Booking_unknown_service_type_is_rejected()
    {
        using var ctx = _fixture.NewContext();
        var branch = new Branch { Code = "TEST-A", Name = "Test A" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        var slot = new Slot { BranchId = branch.Id, StartsAtUtc = DateTime.UtcNow.AddDays(1) };
        ctx.Slots.Add(slot);
        await ctx.SaveChangesAsync();

        var svc = TestFactory.BuildService(ctx);
        var result = await svc.BookAsync(_fixture.CitizenId, new BookRequest(slot.Id, "NOT_A_TYPE"), default);
        Assert.Equal(BookingStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Booking_the_same_slot_twice_returns_conflict()
    {
        using var ctx = _fixture.NewContext();
        var branch = new Branch { Code = "TEST-B", Name = "Test B" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        var slot = new Slot { BranchId = branch.Id, StartsAtUtc = DateTime.UtcNow.AddDays(2) };
        ctx.Slots.Add(slot);
        await ctx.SaveChangesAsync();

        var svc = TestFactory.BuildService(ctx);
        var first = await svc.BookAsync(_fixture.CitizenId, new BookRequest(slot.Id, ServiceTypes.IdCollection), default);
        var second = await svc.BookAsync(_fixture.CitizenId, new BookRequest(slot.Id, ServiceTypes.IdCollection), default);
        Assert.Equal(BookingStatus.Booked, first.Status);
        Assert.Equal(BookingStatus.SlotTaken, second.Status);
    }
}