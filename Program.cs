using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
// (tuỳ chọn) nếu muốn hot-reload view khi dev:
// .AddRazorRuntimeCompilation()
;

// Optional: nếu bạn dùng session, authentication... thêm ở đây.

// Build app
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
<<<<<<< HEAD

// Route cho các area (nếu controller nằm trong Areas)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
=======
app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Index}/{id?}",
    defaults: new { area = "Admin" }) 
    .WithStaticAssets();
>>>>>>> 44c44aedf2f4bd2f46c4de0137320e068c055de3

// Route mặc định: nếu người dùng vào '/', sẽ dùng Area = "Client"
app.MapControllerRoute(
    name: "client_default",
    pattern: "{controller=Mall}/{action=TestLayout}/{id?}",
    defaults: new { area = "Client" }
)
.WithStaticAssets(); // giữ lại nếu bạn có extension này

app.Run();
