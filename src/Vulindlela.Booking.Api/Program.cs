using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Business;
using Vulindlela.Booking.Api.Data;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5101");

builder.Services.AddDbContext<BookingDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Booking")
        ?? "Data Source=booking.db"));

builder.Services.AddScoped<ISlotRepository, SlotRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
builder.Services.AddScoped<IAccessLogRepository, AccessLogRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddHttpClient<IClearanceClient, ClearanceClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Clearance:BaseUrl"] ?? "http://localhost:5102");
    c.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.Cookie.Name = "vulindlela.booking"; o.LoginPath = "/api/auth/signin"; });

builder.Services.AddAuthorization(o =>
    o.AddPolicy("CitizenOwnsRecord", p => p
        .RequireAuthenticatedUser()
        .AddRequirements(new OwnsRecordRequirement())));

builder.Services.AddSingleton<IAuthorizationHandler, OwnsRecordHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    db.Database.EnsureCreated();
    await SeedData.EnsureSeededAsync(db);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();