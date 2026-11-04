using Microsoft.Extensions.Logging.Abstractions;
using Vulindlela.Booking.Api.Business;
using Vulindlela.Booking.Api.Data;

namespace Vulindlela.Booking.Tests;

internal static class TestFactory
{
    public static BookingService BuildService(BookingDbContext ctx) =>
        new(new SlotRepository(ctx), new AppointmentRepository(ctx),
            new AccessLogRepository(ctx), new NoopClearanceClient(),
            NullLogger<BookingService>.Instance);

    private sealed class NoopClearanceClient : IClearanceClient
    {
        public Task NotifyAppointmentCreatedAsync(Vulindlela.Shared.AppointmentCreatedNotification n, CancellationToken ct)
            => Task.CompletedTask;
    }
}