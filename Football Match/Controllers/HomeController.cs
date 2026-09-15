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

        // 1. صفحة التسجيل الرئيسية
        public IActionResult Index()
        {
            return View();
        }

        // 2. استقبال البيانات أو تعديلها برقم الموبايل
        [HttpPost]
        public async Task<IActionResult> Submit(Attendance model, IFormFile? profilePhoto)
        {
            if (ModelState.IsValid)
            {
                var existingAttendance = _context.Attendances
                    .FirstOrDefault(a => a.PhoneNumber.Trim() == model.PhoneNumber.Trim());

                if (existingAttendance != null)
                {
                    // تحديث بيانات الحساب القديم وتسجيل وقت التعديل
                    existingAttendance.FriendName = model.FriendName.Trim();
                    existingAttendance.Status = model.Status;
                    existingAttendance.Note = model.Note;
                    existingAttendance.UpdatedAt = DateTime.Now;

                    // تحديث الصورة لو رفع واحدة جديدة
                    if (profilePhoto != null && profilePhoto.Length > 0)
                    {
                        existingAttendance.ProfilePicturePath = await SaveProfilePhoto(profilePhoto);
                    }

                    _context.Attendances.Update(existingAttendance);
                    TempData["SuccessMessage"] = "تم تعديل موقفك بنجاح! ✏️";

                    // حفظ Session
                    HttpContext.Session.SetInt32("AttendanceId", existingAttendance.Id);
                    HttpContext.Session.SetString("UserName", existingAttendance.FriendName);
                }
                else
                {
                    // إنشاء تسجيل جديد
                    model.PhoneNumber = model.PhoneNumber.Trim();
                    model.FriendName = model.FriendName.Trim();
                    model.RespondedAt = DateTime.Now;

                    if (profilePhoto != null && profilePhoto.Length > 0)
                    {
                        model.ProfilePicturePath = await SaveProfilePhoto(profilePhoto);
                    }

                    _context.Attendances.Add(model);
                    await _context.SaveChangesAsync();

                    // حفظ Session
                    HttpContext.Session.SetInt32("AttendanceId", model.Id);
                    HttpContext.Session.SetString("UserName", model.FriendName);

                    TempData["SuccessMessage"] = "تم تسجيل إجابتك بنجاح! 🚀";
                    return RedirectToAction("Success");
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Success");
            }

            return View("Index", model);
        }

        // حفظ الصورة الشخصية
        private async Task<string> SaveProfilePhoto(IFormFile photo)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);

            return "/uploads/profiles/" + uniqueName;
        }

        // حفظ ميديا الشات
        [HttpPost]
        public async Task<IActionResult> UploadMedia(IFormFile file)
        {
            var attendanceId = HttpContext.Session.GetInt32("AttendanceId");
            if (attendanceId == null)
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest("لم يتم إرسال ملف");

            // التحقق من نوع الملف
            var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            var allowedVideoTypes = new[] { "video/mp4", "video/webm", "video/ogg" };
            var allAllowed = allowedImageTypes.Concat(allowedVideoTypes).ToArray();

            if (!allAllowed.Contains(file.ContentType))
                return BadRequest("نوع الملف غير مسموح");

            var mediaType = allowedImageTypes.Contains(file.ContentType) ? "image" : "video";
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "chat");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Json(new { path = "/uploads/chat/" + uniqueName, type = mediaType });
        }

        // 3. صفحة تأكيد الإرسال
        public IActionResult Success()
        {
            ViewBag.Message = TempData["SuccessMessage"] ?? "تم الحفظ بنجاح!";
            return View();
        }

        // 4. غرفة الحضور (متاحة لمن سجّل)
        public async Task<IActionResult> Room()
        {
            var attendances = await _context.Attendances
                .OrderByDescending(a => a.UpdatedAt ?? a.RespondedAt)
                .ToListAsync();

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Reactions)
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var currentAttendanceId = HttpContext.Session.GetInt32("AttendanceId");

            ViewBag.CurrentAttendanceId = currentAttendanceId;
            ViewBag.Messages = messages;

            return View(attendances);
        }

        // 5. عرض صفحة دخول الأدمن (GET)
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 6. التحقق من بيانات دخول الأدمن (POST)
        [HttpPost]
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

        // 7. لوحة الأدمن المحمية
        public IActionResult Admin()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login");
            }

            var responses = _context.Attendances.OrderByDescending(a => a.UpdatedAt ?? a.RespondedAt).ToList();
            return View(responses);
        }

        // 8. حذف إجابة برقم الـ Id (للأدمن فقط)
        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login");
            }

            var attendance = _context.Attendances.Find(id);
            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);
                _context.SaveChanges();
            }

            return RedirectToAction("Admin");
        }

        // 9. تسجيل الخروج
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("IsAdmin");
            return RedirectToAction("Index");
        }
    }
}
