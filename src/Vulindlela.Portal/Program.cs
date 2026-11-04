using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Vulindlela.Portal.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/tmp/vulindlela-keys"));

builder.WebHost.UseUrls("http://localhost:5100");
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<IBookingApiClient, BookingApiClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Booking"] ?? "http://localhost:5101"));
builder.Services.AddHttpClient<IClearanceApiClient, ClearanceApiClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Services:Clearance"] ?? "http://localhost:5102");
    c.Timeout = TimeSpan.FromSeconds(2);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.Cookie.Name = "vulindlela.portal"; o.LoginPath = "/Account/SignIn"; });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();