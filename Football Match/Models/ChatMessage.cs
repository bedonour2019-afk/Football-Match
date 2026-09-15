using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Football_Match.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        // ãä ÃÑÓá ÇáÑÓÇáÉ
        public int AttendanceId { get; set; }

        [ForeignKey("AttendanceId")]
        public Attendance? Sender { get; set; }

        // äÕ ÇáÑÓÇáÉ (ÇÎÊíÇÑí áæ İíå ãíÏíÇ)
        public string? Content { get; set; }

        // ãÓÇÑ ÇáÕæÑÉ Ãæ ÇáİíÏíæ
        public string? MediaPath { get; set; }

        // äæÚ ÇáãíÏíÇ: "image" Ãæ "video"
        public string? MediaType { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        // åá Êã ÍĞİ ÇáÑÓÇáÉ¿
        public bool IsDeleted { get; set; } = false;

        // Reactions Úáì ÇáÑÓÇáÉ
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    }
}
