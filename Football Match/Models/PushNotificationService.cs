using WebPush;
using Football_Match.Models;
using Microsoft.EntityFrameworkCore;

namespace Football_Match.Services
{
    public class PushNotificationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PushNotificationService> _logger;
        private readonly IConfiguration _config;

        public PushNotificationService(AppDbContext context, ILogger<PushNotificationService> logger, IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _config = config;
        }

        public async Task SendToAllAsync(string title, string body, int? excludeAttendanceId = null)
        {
            try
            {
                var publicKey = _config["Vapid:PublicKey"];
                var privateKey = _config["Vapid:PrivateKey"];
                var subject = _config["Vapid:Subject"] ?? "mailto:admin@example.com";

                if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
                {
                    _logger.LogWarning("Vapid keys are not configured. Skipping push notifications.");
                    return;
                }

                var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
                var webPushClient = new WebPushClient();

                var subs = await _context.PushSubscribers
                    .Where(s => excludeAttendanceId == null || s.AttendanceId != excludeAttendanceId)
                    .ToListAsync();

                var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body });
                var toRemove = new List<PushSubscriber>();

                foreach (var sub in subs)
                {
                    try
                    {
                        var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                        await webPushClient.SendNotificationAsync(pushSub, payload, vapidDetails);
                    }
                    catch (WebPushException ex)
                    {
                        _logger.LogWarning(ex, "Push failed for subscriber {Id}, removing it.", sub.Id);
                        toRemove.Add(sub);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected push error for subscriber {Id}", sub.Id);
                    }
                }

                if (toRemove.Count > 0)
                {
                    _context.PushSubscribers.RemoveRange(toRemove);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendToAllAsync failed unexpectedly.");
            }
        }
    }
}
