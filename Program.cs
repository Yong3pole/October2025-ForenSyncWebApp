using ForenSync_WebApp_New.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// Add session and HTTP context accessor
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor(); // 👈 Add this line
builder.Services.AddMvc();

builder.Services.AddDbContext<ForenSyncDbContext>(options =>
    options.UseSqlite("Data Source=forensync.db"));

var app = builder.Build();

app.UseStaticFiles();   
app.UseRouting();
app.UseSession();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();