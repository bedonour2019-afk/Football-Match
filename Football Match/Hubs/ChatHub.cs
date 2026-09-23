using Microsoft.AspNetCore.SignalR;
using Football_Match.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Football_Match.Services;

namespace Football_Match.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ChatHub> _logger;
        private readonly PushNotificationService _push;

        public ChatHub(AppDbContext context, ILogger<ChatHub> logger, PushNotificationService push)
        {
            _context = context;
            _logger = logger;
            _push = push;
        }

        public async Task SendMessage(int attendanceId, string content, string? mediaPath, string? mediaType)
        {
            try
            {
                if (attendanceId <= 0)
                {
                    _logger.LogWarning("SendMessage attempted with invalid attendanceId.");
                    return;
                }

                var attendance = await _context.Attendances.FindAsync(attendanceId);
                if (attendance == null)
                {
                    _logger.LogWarning("Attendance record not found for Id: {AttendanceId}", attendanceId);
                    return;
                }

                var hasContent = !string.IsNullOrWhiteSpace(content);
                var hasMedia = !string.IsNullOrWhiteSpace(mediaPath);
                if (!hasContent && !hasMedia) return;

                var message = new ChatMessage
                {
                    AttendanceId = attendanceId,
                    Content = hasContent ? content.Trim() : null,
                    MediaPath = hasMedia ? mediaPath : null,
                    MediaType = hasMedia ? mediaType : null,
                    SentAt = DateTime.Now,
                    IsDeleted = false
                };

                _context.ChatMessages.Add(message);
                await _context.SaveChangesAsync();

                // إرسال الرسالة لجميع العملاء المتصلين
                await Clients.All.SendAsync("ReceiveMessage", new
                {
                    id = message.Id,
                    senderId = attendanceId,
                    senderName = attendance.FriendName,
                    senderPhoto = attendance.ProfilePicturePath ?? "",
                    content = message.Content ?? "",
                    mediaPath = message.MediaPath ?? "",
                    mediaType = message.MediaType ?? "",
                    sentAt = message.SentAt.ToString("HH:mm"),
                    reactions = Array.Empty<object>()
                });

                // إرسال إشعار للباقي (مش المرسل نفسه)
                var notifBody = hasContent ? message.Content! : "📎 أرسل مرفق";
                _ = _push.SendToAllAsync(attendance.FriendName, notifBody, excludeAttendanceId: attendanceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending message in ChatHub.");
                var detail = ex.InnerException?.Message ?? ex.Message;
                throw new HubException($"SendMessage failed: {detail}");
            }
        }

        public async Task DeleteMessage(int attendanceId, int messageId, bool isAdmin)
        {
            try
            {
                if (attendanceId <= 0 && !isAdmin) return;

                var message = await _context.ChatMessages
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null) return;

                var isOwner = attendanceId > 0 && message.AttendanceId == attendanceId;
                if (!isOwner && !isAdmin) return;

                message.IsDeleted = true;
                await _context.SaveChangesAsync();

                await Clients.All.SendAsync("MessageDeleted", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message ID {MessageId} in ChatHub.", messageId);
                var detail = ex.InnerException?.Message ?? ex.Message;
                throw new HubException($"DeleteMessage failed: {detail}");
            }
        }

        public async Task AddReaction(int attendanceId, int messageId, string reactionType)
        {
            try
            {
                if (attendanceId <= 0) return;

                var msgExists = await _context.ChatMessages.AnyAsync(m => m.Id == messageId && !m.IsDeleted);
                if (!msgExists) return;

                var existing = await _context.MessageReactions
                    .FirstOrDefaultAsync(r => r.ChatMessageId == messageId && r.AttendanceId == attendanceId);

                if (existing != null)
                {
                    if (existing.ReactionType == reactionType)
                    {
                        // إزالة التفاعل (Toggle Off)
                        _context.MessageReactions.Remove(existing);
                    }
                    else
                    {
                        // تغيير نوع التفاعل
                        existing.ReactionType = reactionType;
                    }
                }
                else
                {
                    _context.MessageReactions.Add(new MessageReaction
                    {
                        ChatMessageId = messageId,
                        AttendanceId = attendanceId,
                        ReactionType = reactionType
                    });
                }

                await _context.SaveChangesAsync();

                var counts = await _context.MessageReactions
                    .Where(r => r.ChatMessageId == messageId)
                    .GroupBy(r => r.ReactionType)
                    .Select(g => new { type = g.Key, count = g.Count() })
                    .ToListAsync();

                await Clients.All.SendAsync("ReactionUpdated", messageId, counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reaction for message ID {MessageId} in ChatHub.", messageId);
                var detail = ex.InnerException?.Message ?? ex.Message;
                throw new HubException($"AddReaction failed: {detail}");
            }
        }
    }
}