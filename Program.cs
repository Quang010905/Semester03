using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Semester03.Services;
using Semester03.Services.Email;
using Semester03.Services.Vnpay;
using Microsoft.AspNetCore.Authentication.Cookies;
using Semester03.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Identity;
using Semester03.Areas.Client.Models.ViewModels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    // Optional: mở cho debug view thay đổi mà không cần rebuild
    //.AddRazorRuntimeCompilation()
    ;


var conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn))
{
    throw new InvalidOperationException("Missing connection string 'DefaultConnection' in appsettings.json or environment variables. Please add it to appsettings.json under ConnectionStrings.");
}



builder.Services.AddDbContext<AbcdmallContext>(options =>
    options.UseSqlServer(conn)
);

// ===== Optional: register HttpContextAccessor (many services expect this) =====
builder.Services.AddHttpContextAccessor();

// ===== Register repositories =====
builder.Services.AddScoped<MovieRepository>();
builder.Services.AddScoped<ShowtimeRepository>();
builder.Services.AddScoped<SeatRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<TicketRepository>();
builder.Services.AddScoped<CinemaRepository>();
builder.Services.AddScoped<ScreenRepository>();
builder.Services.AddScoped<TenantRepository>();
builder.Services.AddScoped<EventRepository>();
builder.Services.AddScoped<EventBookingRepository>();
builder.Services.AddScoped<TenantTypeRepository>();
builder.Services.AddScoped<CouponRepository>();
builder.Services.AddScoped<TenantPositionRepository>();
// ===== Register other services =====
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<TicketEmailService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IPasswordHasher<TblUser>, PasswordHasher<TblUser>>();

// RazorViewToStringRenderer needs IRazorViewEngine, ITempDataProvider, IServiceProvider -> scoped is fine
builder.Services.AddScoped<RazorViewToStringRenderer>();

// ===== Authentication =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = ".AspNetCore.Cookies";
        options.LoginPath = "/Client/Account/Login";
        options.LogoutPath = "/Client/Account/Logout";
        options.AccessDeniedPath = "/Client/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ===== Middleware =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}"
);

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Admin}/{action=Index}/{id?}",
//    defaults: new { area = "Admin" }
//)


app.MapControllerRoute(
    name: "client_default",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Client" }
)

.WithStaticAssets();

app.Run();


