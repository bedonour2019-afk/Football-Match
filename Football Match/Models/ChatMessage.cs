using System.ComponentModel.DataAnnotations.Schema;

namespace Football_Match.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public int AttendanceId { get; set; }

        [ForeignKey("AttendanceId")]
        public Attendance? Sender { get; set; }

        public string? Content { get; set; }

        public string? MediaPath { get; set; }

        public string? MediaType { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    }
}
