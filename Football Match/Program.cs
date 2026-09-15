using Microsoft.EntityFrameworkCore;
using Football_Match;
using Football_Match.Hubs;

var builder = WebApplication.CreateBuilder(args);

// تسجيل خدمات Controllers & Views
builder.Services.AddControllersWithViews();

// تفعيل Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// تسجيل AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=FootballMatchDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

// إضافة SignalR
builder.Services.AddSignalR();

// زيادة حجم الرفع للصور والفيديو
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// تفعيل Session قبل الـ Authorization
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ربط ChatHub
app.MapHub<ChatHub>("/chathub");

app.Run();