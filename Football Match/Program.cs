using Microsoft.EntityFrameworkCore;
using Football_Match;
using Football_Match.Models;
using Football_Match.Hubs;
using Microsoft.AspNetCore.Http.Connections;

var builder = WebApplication.CreateBuilder(args);

// 1. إضافة الـ Controllers والـ Views
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // منع المشاكل المتعلقة بـ Circular References أثناء تحويل البيانات لـ JSON
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// 2. الوصول لـ HttpContext والـ Memory Cache الخاص بالـ Session
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache(); // ضروري لعمل الـ Session بدون أخطاء

// 3. إعداد الـ Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
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
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB كحد أقصى لحجم رسالة الـ Chat
});

// 6. رفع الحد الأقصى لحجم الملفات (Kestrel + IIS + FormOptions)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
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

// ترتيب الـ Middleware مهم جداً لعمل الـ Session والتأكد من هويتها
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