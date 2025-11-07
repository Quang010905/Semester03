using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()

;


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Trong dev, hiển thị trang lỗi chi tiết (tuỳ ý)
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Static files (wwwroot)
app.UseStaticFiles();

// Nếu bạn có extension MapStaticAssets (giữ lại như trước)
app.MapStaticAssets();

// routing & auth
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
    pattern: "{controller=Mall}/{action=TestLayout}/{id?}",
    defaults: new { area = "Client" }
)
.WithStaticAssets();
app.Run();
