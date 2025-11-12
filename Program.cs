using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Areas.Client.Repositories;
using Semester03.Services.Vnpay;
using Semester03.Models.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Đọc chuỗi kết nối từ appsettings.json hoặc fallback mặc định
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? "Server=(local);Database=ABCDMall;uid=sa;pwd=123;Trusted_Connection=True;TrustServerCertificate=true;";

builder.Services.AddDbContext<AbcdmallContext>(options =>
    options.UseSqlServer(conn)
);

// ====== Đăng ký các repository ======
builder.Services.AddScoped<CinemaRepository>();
builder.Services.AddScoped<ShowtimeRepository>();
builder.Services.AddScoped<MovieRepository>();
builder.Services.AddScoped<SeatRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ScreenRepository>();
builder.Services.AddScoped<TenantRepository>();

// ====== Đăng ký dịch vụ bổ sung ======
builder.Services.AddScoped<IPasswordHasher<TblUser>, PasswordHasher<TblUser>>();
builder.Services.AddScoped<IVnPayService, VnPayService>();

// ====== Cấu hình Authentication (Cookie) ======
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = ".AspNetCore.Cookies"; // choose one name — dùng cùng tên khi xóa cookie
        options.LoginPath = "/Client/Account/Login";
        options.LogoutPath = "/Client/Account/Logout";
        options.AccessDeniedPath = "/Client/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// ====== Nếu cần Session ======
// builder.Services.AddDistributedMemoryCache();
// builder.Services.AddSession(options => { options.IdleTimeout = TimeSpan.FromMinutes(30); });

var app = builder.Build();

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
//);


app.MapControllerRoute(
    name: "client_default",
    pattern: "{controller=Event}/{action=Index}/{id?}",
    defaults: new { area = "Client" }
)
.WithStaticAssets();
app.Run();


