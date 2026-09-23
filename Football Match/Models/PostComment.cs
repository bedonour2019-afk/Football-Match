using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Football_Match.Models
{
    public class PostComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PostId { get; set; }

        [ForeignKey(nameof(PostId))]
        public virtual Post? Post { get; set; }

        [Required]
        public int AttendanceId { get; set; }

        [ForeignKey(nameof(AttendanceId))]
        public virtual Attendance? Commenter { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;
    }
}
