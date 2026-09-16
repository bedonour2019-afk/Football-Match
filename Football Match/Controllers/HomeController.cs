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

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit(Attendance model, IFormFile? profilePhoto)
        {
            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || string.IsNullOrWhiteSpace(model.FriendName))
            {
                ModelState.AddModelError("", "رقم الهاتف والاسم مطلوبان!");
                return View("Index", model);
            }

            model.PhoneNumber = model.PhoneNumber.Trim();
            model.FriendName = model.FriendName.Trim();

            if (ModelState.IsValid)
            {
                // استخدام FirstOrDefaultAsync لمنع تعليق الـ Connection تماماً
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.PhoneNumber == model.PhoneNumber);

                if (existingAttendance != null)
                {
                    existingAttendance.FriendName = model.FriendName;
                    existingAttendance.Status = model.Status;
                    existingAttendance.Note = model.Note;
                    existingAttendance.UpdatedAt = DateTime.Now;

                    if (profilePhoto != null && profilePhoto.Length > 0)
                    {
                        existingAttendance.ProfilePicturePath = await SaveProfilePhoto(profilePhoto);
                    }

                    _context.Attendances.Update(existingAttendance);
                    TempData["SuccessMessage"] = "تم تعديل موقفك بنجاح! ✏️";

                    HttpContext.Session.SetInt32("AttendanceId", existingAttendance.Id);
                    HttpContext.Session.SetString("UserName", existingAttendance.FriendName);
                }
                else
                {
                    model.RespondedAt = DateTime.Now;

                    if (profilePhoto != null && profilePhoto.Length > 0)
                    {
                        model.ProfilePicturePath = await SaveProfilePhoto(profilePhoto);
                    }

                    _context.Attendances.Add(model);
                    TempData["SuccessMessage"] = "تم تسجيل إجابتك بنجاح! 🚀";

                    await _context.SaveChangesAsync();

                    HttpContext.Session.SetInt32("AttendanceId", model.Id);
                    HttpContext.Session.SetString("UserName", model.FriendName);

                    return RedirectToAction("Success");
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Success");
            }

            return View("Index", model);
        }

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

            var uniqueName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Json(new { path = "/uploads/chat/" + uniqueName, type = mediaType });
        }

        public IActionResult Success()
        {
            ViewBag.Message = TempData["SuccessMessage"] ?? "تم الحفظ بنجاح!";
            return View();
        }

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

            var currentAttendanceId = HttpContext.Session.GetInt32("AttendanceId");

            ViewBag.CurrentAttendanceId = currentAttendanceId;
            ViewBag.Messages = messages;

            return View(attendances);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

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

        public IActionResult Admin()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login");
            }

            var responses = _context.Attendances.AsNoTracking().OrderByDescending(a => a.UpdatedAt ?? a.RespondedAt).ToList();
            return View(responses);
        }

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

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("IsAdmin");
            return RedirectToAction("Index");
        }
    }
}