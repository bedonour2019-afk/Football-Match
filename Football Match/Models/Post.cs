using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Football_Match.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AttendanceId { get; set; }

        [ForeignKey(nameof(AttendanceId))]
        public virtual Attendance? Author { get; set; }

        [MaxLength(2000)]
        public string? Content { get; set; }

        [MaxLength(500)]
        public string? MediaPath { get; set; }

        [MaxLength(50)]
        public string? MediaType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        public virtual ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
        public virtual ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
    }
}
