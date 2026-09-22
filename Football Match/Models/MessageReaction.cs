using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Football_Match.Models
{
    public class MessageReaction
    {
        public int Id { get; set; }

        public int ChatMessageId { get; set; }

        [ForeignKey("ChatMessageId")]
        public ChatMessage? ChatMessage { get; set; }

        public int AttendanceId { get; set; }

        [ForeignKey("AttendanceId")]
        public Attendance? Reacter { get; set; }

        [Required]
        [MaxLength(10)]
        public string ReactionType { get; set; } = string.Empty;
    }
}
