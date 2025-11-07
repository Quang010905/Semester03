using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities; // <-- namespace của AbcdmallContext (điều chỉnh nếu khác)
using Semester03.Areas.Client.Repositories; // <-- namespace repo (điều chỉnh nếu khác)

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register EF DbContext using connection string "DefaultConnection" from appsettings.json
builder.Services.AddDbContext<AbcdmallContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Register application services / repositories
builder.Services.AddScoped<ICinemaRepository, CinemaRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Detailed error page in development
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();


app.MapStaticAssets();

//app.MapControllerRoute(
//    name: "areas",
//    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Admin}/{action=Index}/{id?}",
//    defaults: new { area = "Admin" })
//    .WithStaticAssets();



app.MapControllerRoute(
    name: "client_default",
    pattern: "{controller=Cinema}/{action=Index}/{id?}",
    defaults: new { area = "Client" }
)
.WithStaticAssets();
app.Run();
