using Microsoft.AspNetCore.Mvc;
using Football_Match.Models;
using Microsoft.EntityFrameworkCore;

namespace Football_Match.Controllers
{
    public class TeamsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TeamsController> _logger;

        // رقم الفريق الفايز (static للتبسيط — يُمكن حفظها في DB لاحقاً)
        private static int? _winnerTeam = null;

        public TeamsController(AppDbContext context, ILogger<TeamsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int? CurrentAttendanceId
        {
            get
            {
                var sessionId = HttpContext.Session.GetInt32("AttendanceId");
                if (sessionId != null) return sessionId;
                if (Request.Cookies.TryGetValue("AttId", out var cookieVal) && int.TryParse(cookieVal, out var id))
                {
                    HttpContext.Session.SetInt32("AttendanceId", id);
                    return id;
                }
                return null;
            }
        }

        private bool IsAdmin => HttpContext.Session.GetString("IsAdmin") == "true";

        // 1. عرض صفحة الفرق
        public async Task<IActionResult> Index()
        {
            if (CurrentAttendanceId == null)
            {
                TempData["ErrorMessage"] = "يرجى تسجيل موقفك أولاً!";
                return RedirectToAction("Index", "Home");
            }

            var attendances = await _context.Attendances
                .AsNoTracking()
                .Where(a => a.Status == "جاي أكيد" || a.TeamNumber != null)
                .OrderBy(a => a.TeamNumber)
                .ThenBy(a => a.FriendName)
                .ToListAsync();

            ViewBag.CurrentAttendanceId = CurrentAttendanceId;
            ViewBag.IsAdmin = IsAdmin;
            ViewBag.WinnerTeam = _winnerTeam;
            return View(attendances);
        }

        // 2. تعيين اللاعب لفريق ومركز (Admin)
        [HttpPost]
        public async Task<IActionResult> SetTeam(int attendanceId, int teamNumber, string position)
        {
            if (!IsAdmin)
                return Json(new { success = false, message = "غير مسموح" });

            var player = await _context.Attendances.FindAsync(attendanceId);
            if (player == null)
                return Json(new { success = false, message = "اللاعب مش موجود" });

            player.TeamNumber = teamNumber > 0 ? teamNumber : (int?)null;
            player.PlayerPosition = position?.Trim();
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // 3. تعيين تقييم لاعب (Admin)
        [HttpPost]
        public async Task<IActionResult> SetRating(int attendanceId, int rating)
        {
            if (!IsAdmin)
                return Json(new { success = false, message = "غير مسموح" });

            if (rating < 0 || rating > 10)
                return Json(new { success = false, message = "التقييم بين 0 و 10" });

            var player = await _context.Attendances.FindAsync(attendanceId);
            if (player == null)
                return Json(new { success = false, message = "اللاعب مش موجود" });

            player.PlayerRating = rating;
            await _context.SaveChangesAsync();

            return Json(new { success = true, rating });
        }

        // 4. تعيين الفريق الفايز (Admin)
        [HttpPost]
        public IActionResult SetWinner(int teamNumber)
        {
            if (!IsAdmin)
                return Json(new { success = false, message = "غير مسموح" });

            _winnerTeam = teamNumber > 0 ? teamNumber : (int?)null;
            return Json(new { success = true, winner = _winnerTeam });
        }

        // 5. تعيين لقب/تاج للاعب (Admin)
        [HttpPost]
        public async Task<IActionResult> SetTag(int attendanceId, string tag)
        {
            if (!IsAdmin)
                return Json(new { success = false, message = "غير مسموح" });

            var player = await _context.Attendances.FindAsync(attendanceId);
            if (player == null)
                return Json(new { success = false, message = "اللاعب مش موجود" });

            player.PlayerTag = tag?.Trim();
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // 6. إزالة لاعب من الفريق (Admin)
        [HttpPost]
        public async Task<IActionResult> RemoveFromTeam(int attendanceId)
        {
            if (!IsAdmin)
                return Json(new { success = false, message = "غير مسموح" });

            var player = await _context.Attendances.FindAsync(attendanceId);
            if (player == null)
                return Json(new { success = false, message = "اللاعب مش موجود" });

            player.TeamNumber = null;
            player.PlayerPosition = null;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // API: الحصول على بيانات الفرق
        [HttpGet]
        public async Task<IActionResult> GetTeamsData()
        {
            var players = await _context.Attendances
                .AsNoTracking()
                .Where(a => a.TeamNumber != null)
                .Select(a => new
                {
                    a.Id,
                    a.FriendName,
                    a.Nickname,
                    a.ProfilePicturePath,
                    a.PlayerRating,
                    a.PlayerPosition,
                    a.TeamNumber,
                    a.PlayerTag,
                    a.IsMVP
                })
                .ToListAsync();

            return Json(new { players, winnerTeam = _winnerTeam });
        }
    }
}
