using System.ComponentModel.DataAnnotations;

namespace Football_Match.Models
{
    public class Attendance
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "ادخل رقم الموبايل")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "ادخل اسمك")]
        [MaxLength(100)]
        public string FriendName { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = string.Empty;

        public string? Note { get; set; }

        // مسار الصورة الشخصية (اختياري)
        public string? ProfilePicturePath { get; set; }

        public DateTime RespondedAt { get; set; } = DateTime.Now;

        // لتخزين تاريخ التعديل إذا قام بالتعديل
        public DateTime? UpdatedAt { get; set; }

        // نجم المباراة (يحدده الأدمن)
        public bool IsMVP { get; set; } = false;

        // Navigation Properties
        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();
    }
}
