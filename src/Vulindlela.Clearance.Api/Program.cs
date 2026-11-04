using Microsoft.EntityFrameworkCore;
using Vulindlela.Clearance.Api.Business;
using Vulindlela.Clearance.Api.Data;
using Vulindlela.Clearance.Api.Workers;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5102");

builder.Services.AddDbContext<ClearanceDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Clearance")
        ?? "Data Source=clearance.db"));

builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<IClearanceService, ClearanceService>();
builder.Services.AddSingleton<ISubmissionQueue, ChannelSubmissionQueue>();
builder.Services.AddHostedService<ClearanceWorker>();
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClearanceDbContext>();
    db.Database.EnsureCreated();
}

app.MapControllers();
app.Run();