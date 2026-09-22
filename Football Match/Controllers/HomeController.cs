using Microsoft.AspNetCore.Mvc;
using Football_Match;
using Football_Match.Models;
using Microsoft.EntityFrameworkCore;

namespace Football_Match.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public HomeController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. الصفحة الرئيسية
        public IActionResult Index()
        {
            return View();
        }

        // 2. استقبال التسجيل
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string PhoneNumber, string FriendName, string Status, string? Note, IFormFile? profilePhoto)
        {
            // تنظيف المدخلات
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
                    // تحديث
                    existingAttendance.FriendName = FriendName;
                    existingAttendance.Status = Status;
                    existingAttendance.Note = Note?.Trim();
                    existingAttendance.UpdatedAt = DateTime.Now;

                    if (profilePhoto != null && profilePhoto.Length > 0)
                        existingAttendance.ProfilePicturePath = await SaveProfilePhoto(profilePhoto);

                    await _context.SaveChangesAsync();

                    HttpContext.Session.SetInt32("AttendanceId", existingAttendance.Id);
                    HttpContext.Session.SetString("UserName", existingAttendance.FriendName);
                    TempData["SuccessMessage"] = "تم تعديل موقفك بنجاح! ✏️";
                }
                else
                {
                    // جديد
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
                    TempData["SuccessMessage"] = "تم تسجيل إجابتك بنجاح! 🚀";
                }

                // لو الطلب جاي عن طريق AJAX في الجافاسكريبت، نرجع مسار التوجيه كـ JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, redirectUrl = Url.Action("Success") });
                }

                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { success = false, message = "حصلت مشكلة: " + ex.Message });
                }

                TempData["ErrorMessage"] = "حصلت مشكلة: " + ex.Message;
                return RedirectToAction("Index");
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

        // رفع ميديا الشات
        [HttpPost]
        public async Task<IActionResult> UploadMedia(IFormFile file)
        {
            var attendanceId = HttpContext.Session.GetInt32("AttendanceId");
            if (attendanceId == null)
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest("لم يتم إرسال ملف");

            var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            var allowedVideoTypes = new[] { "video/mp4", "video/webm", "video/ogg" };
            var allAllowed = allowedImageTypes.Concat(allowedVideoTypes).ToArray();

            if (!allAllowed.Contains(file.ContentType))
                return BadRequest("نوع الملف غير مسموح");

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

        // 4. غرفة الحضور
        public async Task<IActionResult> Room()
        {
            var attendances = await _context.Attendances
                .AsNoTracking()
                .OrderByDescending(a => a.UpdatedAt ?? a.RespondedAt)
                .ToListAsync();

            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Include(m => m.Reactions)
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.CurrentAttendanceId = HttpContext.Session.GetInt32("AttendanceId");
            ViewBag.Messages = messages;

            return View(attendances);
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

        // 9. تسجيل خروج
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        // 10. Error page
        public IActionResult Error()
        {
            return View();
        }
    }
}