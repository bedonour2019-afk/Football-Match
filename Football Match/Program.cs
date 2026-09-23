using Microsoft.EntityFrameworkCore;
using Football_Match;
using Football_Match.Models;
using Football_Match.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Football_Match.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. إضافة الـ Controllers والـ Views
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// 2. الوصول لـ HttpContext والـ Memory Cache الخاص بالـ Session
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

// 3. إعداد الـ Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".FootballMatch.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.MaxAge = TimeSpan.FromDays(30);
});

// 4. إعداد قاعدة البيانات
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

// 5. إعداد SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

// 5.5 إعداد سيرفس الإشعارات
builder.Services.AddScoped<PushNotificationService>();

// 6. رفع الحد الأقصى لحجم الملفات (Kestrel + IIS + FormOptions)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024;
    options.ValueLengthLimit = 50 * 1024 * 1024;
    options.MultipartHeadersLengthLimit = 50 * 1024 * 1024;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 50 * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
});

var app = builder.Build();

// 7. إعداد الـ Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// الترتيب الصارم للـ Session والتخويل
app.UseSession();
app.UseAuthorization();

// 8. التوجيه (Routing)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 9. SignalR Hub Mapping
app.MapHub<ChatHub>("/chathub", options =>
{
    options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
});

app.Run();