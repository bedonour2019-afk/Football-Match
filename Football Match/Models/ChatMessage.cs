using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Football_Match.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AttendanceId { get; set; }

        [ForeignKey(nameof(AttendanceId))]
        public virtual Attendance? Sender { get; set; }

        [MaxLength(2000)]
        public string? Content { get; set; }

        [MaxLength(500)]
        public string? MediaPath { get; set; }

        [MaxLength(50)]
        public string? MediaType { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        public virtual ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    }
}