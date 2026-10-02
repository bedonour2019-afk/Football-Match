using System.ComponentModel.DataAnnotations;

namespace Football_Match.Models
{
    // صف واحد بس دايمًا (Id = 1) بيخزن إعدادات عامة عن الماتش الحالي
    public class MatchSetting
    {
        [Key]
        public int Id { get; set; }

        // رقم الفريق الفايز (1 أو 2)، أو null لو مفيش فريق فايز محدد
        public int? WinnerTeamNumber { get; set; }
    }
}
