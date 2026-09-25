using Microsoft.AspNetCore.Mvc;
using Football_Match;
using Football_Match.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Football_Match.Services;

namespace Football_Match.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<HomeController> _logger;
        private readonly PushNotificationService _push;

        public HomeController(AppDbContext context, IWebHostEnvironment env, ILogger<HomeController> logger, PushNotificationService push)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _push = push;
        }

        // كوكي دائمة لمدة سنة عشان نتعرف على المستخدم حتى لو السيرفر عمل Restart ومسح الـ Session
        private int? GetOrRestoreAttendanceId()
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

        private void SetAttendanceCookie(int id)
        {
            Response.Cookies.Append("AttId", id.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });
        }

        // 1. الصفحة الرئيسية
        public IActionResult Index(bool edit = false)
        {
            if (!edit && GetOrRestoreAttendanceId() != null)
                return RedirectToAction("Room");

            return View();
        }

        // 2. استقبال التسجيل
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string PhoneNumber, string FriendName, string Status, string? Note, IFormFile? profilePhoto)
        {
            PhoneNumber = (PhoneNumber ?? "").Trim();
            FriendName = (FriendName ?? "").Trim();
            Status = (Status ?? "جاي أكيد").Trim();

            if (string.IsNullOrEmpty(PhoneNumber) || string.IsNullOrEmpty(FriendName))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { success = false, message = "رقم الموبايل والاسم مطلوبان!" });
                }

                TempData["ErrorMessage"] = "رقم الموبايل والاسم مطلوبان!";
                return RedirectToAction("Index");
            }

            try
            {
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.PhoneNumber == PhoneNumber);

                if (existingAttendance != null)
                {
                    var oldStatus = existingAttendance.Status;

                    existingAttendance.FriendName = FriendName;
                    existingAttendance.Status = Status;
                    existingAttendance.Note = Note?.Trim();
                    existingAttendance.UpdatedAt = DateTime.Now;

                    if (profilePhoto != null && profilePhoto.Length > 0)
                        existingAttendance.ProfilePicturePath = await SaveProfilePhoto(profilePhoto);

                    await _context.SaveChangesAsync();

                    HttpContext.Session.SetInt32("AttendanceId", existingAttendance.Id);
                    HttpContext.Session.SetString("UserName", existingAttendance.FriendName);
                    SetAttendanceCookie(existingAttendance.Id);
                    TempData["SuccessMessage"] = "تم تعديل موقفك بنجاح! ✏️";

                    if (oldStatus != Status)
                    {
                        _ = _push.SendToAllAsync("تحديث الموقف 🔄", $"{FriendName} غيّر موقفه إلى: {Status}", excludeAttendanceId: existingAttendance.Id);
                    }
                }
                else
                {
                    var model = new Attendance
                    {
                        PhoneNumber = PhoneNumber,
                        FriendName = FriendName,
                        Status = Status,
                        Note = Note?.Trim(),
                        RespondedAt = DateTime.Now
                    };

                    if (profilePhoto != null && profilePhoto.Length > 0)
                        model.ProfilePicturePath = await SaveProfilePhoto(profilePhoto);

                    _context.Attendances.Add(model);
                    await _context.SaveChangesAsync();

                    HttpContext.Session.SetInt32("AttendanceId", model.Id);
                    HttpContext.Session.SetString("UserName", model.FriendName);
                    SetAttendanceCookie(model.Id);
                    TempData["SuccessMessage"] = "تم تسجيل إجابتك بنجاح! 🚀";

                    _ = _push.SendToAllAsync("تسجيل جديد! 🎉", $"{FriendName} سجّل حضوره في الماتش", excludeAttendanceId: model.Id);
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, redirectUrl = Url.Action("Success") });
                }

                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during attendance submission.");
                string fullErrorDetails = $"DB / Operation Exception Details:\n\nMessage: {ex.Message}\n\nInner Exception: {ex.InnerException?.Message}";

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return StatusCode(500, new { success = false, message = fullErrorDetails });
                }

                return Content(fullErrorDetails, "text/plain; charset=utf-8");
            }
        }

        // حفظ صورة البروفايل
        private async Task<string> SaveProfilePhoto(IFormFile photo)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowed.Contains(ext)) ext = ".jpg";

            var uniqueName = Guid.NewGuid().ToString() + ext;
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);

            return "/uploads/profiles/" + uniqueName;
        }

        // تعديل بيانات الحساب (الاسم / رقم الموبايل / الصورة) من قايمة الإعدادات في الروم
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccount(string PhoneNumber, string FriendName, IFormFile? profilePhoto)
        {
            var attendanceId = GetOrRestoreAttendanceId();
            if (attendanceId == null)
                return RedirectToAction("Index");

            var attendance = await _context.Attendances.FindAsync(attendanceId.Value);
            if (attendance == null)
                return RedirectToAction("Index");

            PhoneNumber = (PhoneNumber ?? "").Trim();
            FriendName = (FriendName ?? "").Trim();

            if (string.IsNullOrEmpty(PhoneNumber) || string.IsNullOrEmpty(FriendName))
            {
                TempData["ErrorMessage"] = "رقم الموبايل والاسم مطلوبان!";
                return RedirectToAction("Room");
            }

            try
            {
                attendance.PhoneNumber = PhoneNumber;
                attendance.FriendName = FriendName;
                attendance.UpdatedAt = DateTime.Now;

                if (profilePhoto != null && profilePhoto.Length > 0)
                    attendance.ProfilePicturePath = await SaveProfilePhoto(profilePhoto);

                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("UserName", attendance.FriendName);
                TempData["SuccessMessage"] = "تم تحديث بياناتك بنجاح ✏️";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account.");
                TempData["ErrorMessage"] = "حصلت مشكلة أثناء تحديث بياناتك";
            }

            return RedirectToAction("Room");
        }

        // رفع ميديا الشات (محسنة لتدعم استجابات JSON دائمًا)
        [HttpPost]
        public async Task<IActionResult> UploadMedia(IFormFile file)
        {
            var attendanceId = HttpContext.Session.GetInt32("AttendanceId");
            if (attendanceId == null)
                return StatusCode(401, new { message = "انتهت الجلسة، يرجى تسجيل الحضور أولاً" });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "لم يتم إرسال ملف" });

            var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            var allowedVideoTypes = new[] { "video/mp4", "video/webm", "video/ogg" };
            var allAllowed = allowedImageTypes.Concat(allowedVideoTypes).ToArray();

            if (!allAllowed.Contains(file.ContentType))
                return BadRequest(new { message = "نوع الملف غير مسموح به" });

            var mediaType = allowedImageTypes.Contains(file.ContentType) ? "image" : "video";
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "chat");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueName = Guid.NewGuid().ToString() + ext;
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Json(new { path = "/uploads/chat/" + uniqueName, type = mediaType });
        }

        // 3. صفحة النجاح
        public IActionResult Success()
        {
            ViewBag.Message = TempData["SuccessMessage"] ?? "تم الحفظ بنجاح!";
            return View();
        }

        // 4. غرفة الحضور والشات (محدثة مع حماية الجلسة)
        public async Task<IActionResult> Room()
        {
            var currentAttendanceId = GetOrRestoreAttendanceId();

            // التحقق من وجود Session قبل الدخول لمنع الفشل الصامت عند إرسال الرسائل
            if (currentAttendanceId == null)
            {
                TempData["ErrorMessage"] = "يرجى تسجيل موقفك ورقم الموبايل أولاً لدخول الشات!";
                return RedirectToAction("Index");
            }

            try
            {
                var attendances = await _context.Attendances
                    .AsNoTracking()
                    .OrderByDescending(a => a.UpdatedAt ?? a.RespondedAt)
                    .ToListAsync();

                var messages = new List<ChatMessage>();
                try
                {
                    messages = await _context.ChatMessages
                        .AsNoTracking()
                        .Include(m => m.Sender)
                        .Include(m => m.Reactions)
                        .Where(m => !m.IsDeleted)
                        .OrderBy(m => m.SentAt)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch ChatMessages. Table might be empty or missing.");
                }

                ViewBag.CurrentAttendanceId = currentAttendanceId;
                ViewBag.Messages = messages ?? new List<ChatMessage>();
                ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin") == "true";

                return View(attendances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Room page.");
                return Content($"Room Page Error:\n\nMessage: {ex.Message}\n\nDetails:\n{ex}", "text/plain; charset=utf-8");
            }
        }

        // 5. Login Admin GET
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 6. Login Admin POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password)
        {
            if (username == "Bruce" && password == "951753")
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                return RedirectToAction("Admin");
            }

            ViewBag.Error = "اسم المستخدم أو كلمة السر غير صحيحة!";
            return View();
        }

        // 7. Admin Panel
        public async Task<IActionResult> Admin()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("Login");

            var responses = await _context.Attendances
                .AsNoTracking()
                .OrderByDescending(a => a.UpdatedAt ?? a.RespondedAt)
                .ToListAsync();

            return View(responses);
        }

        // 8. حذف (Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("Login");

            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Admin");
        }

        // 8.5 تعيين نجم المباراة (Admin) - شخص واحد بس في نفس الوقت
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMVP(int id)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("Login");

            var target = await _context.Attendances.FindAsync(id);
            if (target != null)
            {
                var makingMvp = !target.IsMVP;

                // إلغاء اللقب من أي حد تاني كان حامله قبل كده
                var currentMvps = await _context.Attendances.Where(a => a.IsMVP).ToListAsync();
                foreach (var a in currentMvps)
                    a.IsMVP = false;

                target.IsMVP = makingMvp;
                await _context.SaveChangesAsync();

                if (makingMvp)
                {
                    _ = _push.SendToAllAsync("نجم المباراة 👑", $"{target.FriendName} أفضل لاعب في الماتش!", excludeAttendanceId: target.Id);
                }
            }

            return RedirectToAction("Admin");
        }

        // 8.6 تعديل اسم/صورة أي شخص (Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttendeeAdmin(int id, string newName, IFormFile? profilePhoto)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("Login");

            newName = (newName ?? "").Trim();

            var target = await _context.Attendances.FindAsync(id);
            if (target != null)
            {
                if (!string.IsNullOrEmpty(newName))
                    target.FriendName = newName;

                if (profilePhoto != null && profilePhoto.Length > 0)
                    target.ProfilePicturePath = await SaveProfilePhoto(profilePhoto);

                target.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Admin");
        }

        // 9. تسجيل خروج
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("AttId");
            return RedirectToAction("Index");
        }

        // 11. تغيير الموقف بضغطة واحدة من الروم من غير الرجوع لصفحة التسجيل
        [HttpPost]
        public async Task<IActionResult> QuickStatus(string status)
        {
            var attendanceId = GetOrRestoreAttendanceId();
            if (attendanceId == null)
                return Json(new { success = false, message = "من فضلك سجل موقفك الأول" });

            var allowed = new[] { "جاي أكيد", "مش جاي", "احتمال أجي" };
            if (!allowed.Contains(status))
                return Json(new { success = false, message = "قيمة غير صحيحة" });

            var attendance = await _context.Attendances.FindAsync(attendanceId.Value);
            if (attendance == null)
                return Json(new { success = false, message = "الحساب مش موجود" });

            var oldStatus = attendance.Status;
            attendance.Status = status;
            attendance.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (oldStatus != status)
            {
                _ = _push.SendToAllAsync("تحديث الموقف 🔄", $"{attendance.FriendName} غيّر موقفه إلى: {status}", excludeAttendanceId: attendance.Id);
            }

            return Json(new { success = true, status = attendance.Status });
        }

        // 10. Error page
        public IActionResult Error()
        {
            return View();
        }
    }
}