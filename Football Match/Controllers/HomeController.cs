using Microsoft.AspNetCore.Mvc;
using Football_Match;
using Football_Match.Models;

namespace Football_Match.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // 1. صفحة التسجيل الرئيسية
        public IActionResult Index()
        {
            return View();
        }

        // 2. استقبال البيانات أو تعديلها برقم الموبايل
        [HttpPost]
        public IActionResult Submit(Attendance model)
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

                    _context.Attendances.Update(existingAttendance);

                    // نص الرسالة عند التعديل
                    TempData["SuccessMessage"] = "تم تعديل موقفك بنجاح! ✏️";
                }
                else
                {
                    // إنشاء تسجيل جديد
                    model.PhoneNumber = model.PhoneNumber.Trim();
                    model.FriendName = model.FriendName.Trim();
                    model.RespondedAt = DateTime.Now;
                    _context.Attendances.Add(model);

                    // نص الرسالة عند التسجيل الجديد
                    TempData["SuccessMessage"] = "تم تسجيل إجابتك بنجاح! 🚀";
                }

                _context.SaveChanges();
                return RedirectToAction("Success");
            }

            return View("Index", model);
        }

        // 3. صفحة تأكيد الإرسال
        public IActionResult Success()
        {
            ViewBag.Message = TempData["SuccessMessage"] ?? "تم الحفظ بنجاح!";
            return View();
        }

        // 4. عرض صفحة دخول الأدمن (GET)
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 5. التحقق من بيانات دخول الأدمن (POST)
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (username == "admin" && password == "123456")
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                return RedirectToAction("Admin");
            }

            ViewBag.Error = "اسم المستخدم أو كلمة السر غير صحيحة!";
            return View();
        }

        // 6. لوحة الأدمن المحمية
        public IActionResult Admin()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Login");
            }

            var responses = _context.Attendances.OrderByDescending(a => a.UpdatedAt ?? a.RespondedAt).ToList();
            return View(responses);
        }

        // 7. حذف إجابة برقم الـ Id (للأدمن فقط)
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

        // 8. تسجيل الخروج
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("IsAdmin");
            return RedirectToAction("Index");
        }
    }
}