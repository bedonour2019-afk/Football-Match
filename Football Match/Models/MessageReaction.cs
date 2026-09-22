using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Football_Match.Models
{
    public class MessageReaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChatMessageId { get; set; }

        [ForeignKey(nameof(ChatMessageId))]
        public virtual ChatMessage? ChatMessage { get; set; }

        [Required]
        public int AttendanceId { get; set; }

        [ForeignKey(nameof(AttendanceId))]
        public virtual Attendance? Reacter { get; set; }

        [Required]
        [MaxLength(20)]
        public string ReactionType { get; set; } = string.Empty;
    }
}