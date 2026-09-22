using Microsoft.AspNetCore.SignalR;
using Football_Match.Models;
using Microsoft.EntityFrameworkCore;

namespace Football_Match.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentAttendanceId()
        {
            return Context.GetHttpContext()?.Session.GetInt32("AttendanceId");
        }

        public async Task SendMessage(string content, string? mediaPath, string? mediaType)
        {
            var attendanceId = GetCurrentAttendanceId();
            if (attendanceId == null) return;

            var attendance = await _context.Attendances.FindAsync(attendanceId.Value);
            if (attendance == null) return;

            var hasContent = !string.IsNullOrWhiteSpace(content);
            var hasMedia = !string.IsNullOrWhiteSpace(mediaPath);
            if (!hasContent && !hasMedia) return;

            var message = new ChatMessage
            {
                AttendanceId = attendanceId.Value,
                Content = hasContent ? content.Trim() : null,
                MediaPath = hasMedia ? mediaPath : null,
                MediaType = hasMedia ? mediaType : null,
                SentAt = DateTime.Now,
                IsDeleted = false
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = attendanceId.Value,
                senderName = attendance.FriendName,
                senderPhoto = attendance.ProfilePicturePath ?? "",
                content = message.Content ?? "",
                mediaPath = message.MediaPath ?? "",
                mediaType = message.MediaType ?? "",
                sentAt = message.SentAt.ToString("HH:mm"),
                reactions = Array.Empty<object>()
            });
        }

        public async Task DeleteMessage(int messageId)
        {
            var attendanceId = GetCurrentAttendanceId();
            if (attendanceId == null) return;

            var message = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.AttendanceId == attendanceId.Value);

            if (message == null) return;

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("MessageDeleted", messageId);
        }

        public async Task AddReaction(int messageId, string reactionType)
        {
            var attendanceId = GetCurrentAttendanceId();
            if (attendanceId == null) return;

            var msgExists = await _context.ChatMessages.AnyAsync(m => m.Id == messageId && !m.IsDeleted);
            if (!msgExists) return;

            var existing = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.ChatMessageId == messageId && r.AttendanceId == attendanceId.Value);

            if (existing != null)
            {
                if (existing.ReactionType == reactionType)
                {
                    // toggle off
                    _context.MessageReactions.Remove(existing);
                }
                else
                {
                    // change reaction
                    existing.ReactionType = reactionType;
                }
            }
            else
            {
                _context.MessageReactions.Add(new MessageReaction
                {
                    ChatMessageId = messageId,
                    AttendanceId = attendanceId.Value,
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
    }
}
