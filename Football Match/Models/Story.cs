using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Football_Match.Models
{
    public class Story
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AttendanceId { get; set; }

        [ForeignKey(nameof(AttendanceId))]
        public virtual Attendance? Author { get; set; }

        [MaxLength(500)]
        public string? Content { get; set; }

        [MaxLength(500)]
        public string? MediaPath { get; set; }

        [MaxLength(50)]
        public string? MediaType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime ExpiresAt { get; set; } = DateTime.Now.AddHours(24);

        public bool IsDeleted { get; set; } = false;
    }
}
