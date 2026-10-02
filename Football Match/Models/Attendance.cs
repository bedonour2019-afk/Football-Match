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

        // غلاف البروفايل
        public string? CoverPhotoPath { get; set; }

        // البايو (نبذة شخصية)
        [MaxLength(300)]
        public string? Bio { get; set; }

        // الاسم المستعار
        [MaxLength(50)]
        public string? Nickname { get; set; }

        // تقييم الأدمن (0-10)
        public int? PlayerRating { get; set; }

        // مركز اللاعب (مهاجم، مدافع، وسط، حارس)
        [MaxLength(50)]
        public string? PlayerPosition { get; set; }

        // رقم الفريق (1 أو 2)
        public int? TeamNumber { get; set; }

        // لقب خاص من الأدمن
        [MaxLength(100)]
        public string? PlayerTag { get; set; }

        // كلمة سر الحساب
        [MaxLength(100)]
        public string? Password { get; set; }

        public DateTime RespondedAt { get; set; } = DateTime.Now;

        // لتخزين تاريخ التعديل إذا قام بالتعديل
        public DateTime? UpdatedAt { get; set; }

        // نجم المباراة (يحدده الأدمن)
        public bool IsMVP { get; set; } = false;

        // Navigation Properties
        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();
        public ICollection<Story> Stories { get; set; } = new List<Story>();
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
