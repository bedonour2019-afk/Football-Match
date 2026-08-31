using Microsoft.EntityFrameworkCore;
using Football_Match;

var builder = WebApplication.CreateBuilder(args);

// تسجيل خدمات Controllers & Views
builder.Services.AddControllersWithViews();

// تفعيل Session لحماية لوحة الأدمن
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// تسجيل AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=FootballMatchDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

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

app.Run();