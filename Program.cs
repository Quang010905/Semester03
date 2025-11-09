using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Areas.Client.Repositories;
using Semester03.Services.Vnpay;
using Semester03.Models.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// read connection string from appsettings.json (DefaultConnection) or fallback
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? "Server=(local);Database=ABCDMall;uid=sa;pwd=123;Trusted_Connection=True;TrustServerCertificate=true;";

builder.Services.AddDbContext<AbcdmallContext>(options =>
    options.UseSqlServer(conn)
);

// Register repositories as Scoped (per-request) - SAFE with DbContext
builder.Services.AddScoped<CinemaRepository>();
builder.Services.AddScoped<ShowtimeRepository>();
builder.Services.AddScoped<MovieRepository>();
builder.Services.AddScoped<SeatRepository>();

builder.Services.AddScoped<IVnPayService, VnPayService>();

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
app.UseAuthorization();

//app.MapControllerRoute(
//    name: "areas",
//    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}"
//);
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Admin}/{action=Index}/{id?}",
//    defaults: new { area = "Admin" }
//);


app.MapControllerRoute(
    name: "client_default",
    pattern: "{controller=Cinema}/{action=Index}/{id?}",
    defaults: new { area = "Client" }
)
.WithStaticAssets();
app.Run();
