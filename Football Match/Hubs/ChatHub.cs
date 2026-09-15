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

        // إرسال رسالة نصية أو ميديا
        public async Task SendMessage(string content, string? mediaPath, string? mediaType)
        {
            var attendanceId = Context.GetHttpContext()?.Session.GetInt32("AttendanceId");
            if (attendanceId == null) return;

            var attendance = await _context.Attendances.FindAsync(attendanceId.Value);
            if (attendance == null) return;

            var message = new ChatMessage
            {
                AttendanceId = attendanceId.Value,
                Content = string.IsNullOrWhiteSpace(content) ? null : content,
                MediaPath = mediaPath,
                MediaType = mediaType,
                SentAt = DateTime.Now
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = attendanceId.Value,
                senderName = attendance.FriendName,
                senderPhoto = attendance.ProfilePicturePath,
                content = message.Content,
                mediaPath = message.MediaPath,
                mediaType = message.MediaType,
                sentAt = message.SentAt.ToString("HH:mm"),
                reactions = new object[] { }
            });
        }

        // حذف رسالة (صاحبها فقط)
        public async Task DeleteMessage(int messageId)
        {
            var attendanceId = Context.GetHttpContext()?.Session.GetInt32("AttendanceId");
            if (attendanceId == null) return;

            var message = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.AttendanceId == attendanceId.Value);

            if (message == null) return;

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("MessageDeleted", messageId);
        }

        // إضافة ريأكشن
        public async Task AddReaction(int messageId, string reactionType)
        {
            var attendanceId = Context.GetHttpContext()?.Session.GetInt32("AttendanceId");
            if (attendanceId == null) return;

            // تحقق لو هو عمل نفس الريأكشن قبل كده
            var existingReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.ChatMessageId == messageId && r.AttendanceId == attendanceId.Value && r.ReactionType == reactionType);

            if (existingReaction != null)
            {
                // إزالة الريأكشن (toggle)
                _context.MessageReactions.Remove(existingReaction);
                await _context.SaveChangesAsync();

                var updatedCounts = await GetReactionCounts(messageId);
                await Clients.All.SendAsync("ReactionUpdated", messageId, updatedCounts);
                return;
            }

            // إزالة أي ريأكشن تاني من نفس الشخص على نفس الرسالة
            var oldReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.ChatMessageId == messageId && r.AttendanceId == attendanceId.Value);

            if (oldReaction != null)
                _context.MessageReactions.Remove(oldReaction);

            var reaction = new MessageReaction
            {
                ChatMessageId = messageId,
                AttendanceId = attendanceId.Value,
                ReactionType = reactionType
            };

            _context.MessageReactions.Add(reaction);
            await _context.SaveChangesAsync();

            var counts = await GetReactionCounts(messageId);
            await Clients.All.SendAsync("ReactionUpdated", messageId, counts);
        }

        private async Task<object> GetReactionCounts(int messageId)
        {
            var reactions = await _context.MessageReactions
                .Where(r => r.ChatMessageId == messageId)
                .GroupBy(r => r.ReactionType)
                .Select(g => new { type = g.Key, count = g.Count() })
                .ToListAsync();

            return reactions;
        }
    }
}
