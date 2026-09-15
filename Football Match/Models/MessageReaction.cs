using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Football_Match.Models
{
    public class MessageReaction
    {
        public int Id { get; set; }

        // Úáì Ãí ÑÓÇáÉ
        public int ChatMessageId { get; set; }

        [ForeignKey("ChatMessageId")]
        public ChatMessage? ChatMessage { get; set; }

        // ãíä Úãá ÇáÑíÃßÔä
        public int AttendanceId { get; set; }

        [ForeignKey("AttendanceId")]
        public Attendance? Reacter { get; set; }

        // äæÚ ÇáÑíÃßÔä: "laugh" Ãæ "sad" Ãæ "love"
        [Required]
        public string ReactionType { get; set; } = string.Empty;
    }
}
