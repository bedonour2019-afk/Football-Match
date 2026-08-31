using System.ComponentModel.DataAnnotations;

namespace Football_Match.Models
{
    public class Attendance
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "ادخل رقم الموبايل")]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "ادخل اسمك")]
        public string FriendName { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = string.Empty;

        public string? Note { get; set; }

        public DateTime RespondedAt { get; set; } = DateTime.Now;

        // لتخزين تاريخ التعديل إذا قام بالعديل
        public DateTime? UpdatedAt { get; set; }
    }
}