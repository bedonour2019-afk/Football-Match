using Microsoft.EntityFrameworkCore;
using Football_Match;
using Football_Match.Models;
using Football_Match.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. إضافة الـ Controllers والـ Views
builder.Services.AddControllersWithViews();

// 2. الوصول لـ HttpContext جوه الخدمات والـ Hubs
builder.Services.AddHttpContextAccessor();

// 3. إعداد الـ Session - مدة 24 ساعة
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 4. إعداد قاعدة البيانات مع إمكانية إعادة المحاولة عند انقطاع الاتصال
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

// 5. إعداد SignalR مع دعم Long Polling لبيئة الـ Shared Hosting
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// 6. رفع الحد الأقصى لحجم الملفات المرفوعة (50 ميجابايت)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

var app = builder.Build();

// 7. إنشاء وترقية الجداول في قاعدة البيانات تلقائياً عند بدء التشغيل
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "خطأ أثناء تشغيل الـ Migration لقاعدة البيانات عند الإقلاع");
}

// 8. إعداد الـ Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// 9. التوجيه (Routing)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 10. ربط الـ SignalR Hub مع تفعيل Long Polling كـ Fallback للسيرفرات المشتركة
app.MapHub<ChatHub>("/chathub", options =>
{
    options.Transports =
        Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
});

app.Run();