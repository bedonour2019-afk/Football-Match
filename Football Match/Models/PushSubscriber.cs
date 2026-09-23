using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Football_Match.Models
{
    public class PushSubscriber
    {
        [Key]
        public int Id { get; set; }

        public int? AttendanceId { get; set; }

        [ForeignKey(nameof(AttendanceId))]
        public virtual Attendance? Attendance { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string P256dh { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Auth { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
