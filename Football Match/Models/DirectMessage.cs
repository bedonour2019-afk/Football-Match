using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Football_Match.Models
{
    public class DirectMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public virtual Attendance? Sender { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public virtual Attendance? Receiver { get; set; }

        [MaxLength(2000)]
        public string? Content { get; set; }

        [MaxLength(500)]
        public string? MediaPath { get; set; }

        [MaxLength(50)]
        public string? MediaType { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
    }
}
