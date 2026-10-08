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
        public async Task<IActionResult> Submit(string PhoneNumber, string FriendName, string Status, string? Note, string? Password, IFormFile? profilePhoto)
        {
            PhoneNumber = (PhoneNumber ?? "").Trim();
            FriendName = (FriendName ?? "").Trim();
            Status = (Status ?? "جاي أكيد").Trim();
            Password = (Password ?? "").Trim();

            if (string.IsNullOrEmpty(PhoneNumber) || string.IsNullOrEmpty(FriendName))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { success = false, message = "رقم الموبايل والاسم مطلوبان!" });

                TempData["ErrorMessage"] = "رقم الموبايل والاسم مطلوبان!";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(Password))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { success = false, message = "لازم تحط كلمة سر!" });

                TempData["ErrorMessage"] = "لازم تحط كلمة سر!";
                return RedirectToAction("Index");
            }

            try
            {
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.PhoneNumber == PhoneNumber);

                if (existingAttendance != null)
                {
                    // لو الحساب عنده كلمة سر محفوظة قبل كده، لازم تتطابق
                    if (!string.IsNullOrEmpty(existingAttendance.Password) && existingAttendance.Password != Password)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            return BadRequest(new { success = false, message = "كلمة السر غلط! جرب تاني." });

                        TempData["ErrorMessage"] = "كلمة السر غلط! جرب تاني.";
                        return RedirectToAction("Index");
                    }

                    var oldStatus = existingAttendance.Status;
                    existingAttendance.FriendName = FriendName;
                    existingAttendance.Status = Status;
                    existingAttendance.Note = Note?.Trim();
                    existingAttendance.UpdatedAt = DateTime.Now;

                    // حساب قديم من غير كلمة سر (اتسجل قبل الخاصية دي) - نحفظله كلمة السر دلوقتي أول مرة
                    if (string.IsNullOrEmpty(existingAttendance.Password))
                        existingAttendance.Password = Password;

                    if (profilePhoto != null && profilePhoto.Length > 0)
                        existingAttendance.ProfilePicturePath = await SaveProfilePhoto(profilePhoto);

                    await _context.SaveChangesAsync();

                    HttpContext.Session.SetInt32("AttendanceId", existingAttendance.Id);
                    HttpContext.Session.SetString("UserName", existingAttendance.FriendName);
                    SetAttendanceCookie(existingAttendance.Id);
                    TempData["SuccessMessage"] = "تم تعديل موقفك بنجاح! ✏️";

                    if (oldStatus != Status)
                        _ = _push.SendToAllAsync("تحديث الموقف 🔄", $"{FriendName} غيّر موقفه إلى: {Status}", excludeAttendanceId: existingAttendance.Id);
                }
                else
                {
                    var model = new Attendance
                    {
                        PhoneNumber = PhoneNumber,
                        FriendName = FriendName,
                        Status = Status,
                        Note = Note?.Trim(),
                        Password = Password,
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
                    return Json(new { success = true, redirectUrl = Url.Action("Success") });

                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during attendance submission.");
                string fullErrorDetails = $"DB / Operation Exception Details:\n\nMessage: {ex.Message}\n\nInner Exception: {ex.InnerException?.Message}";

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return StatusCode(500, new { success = false, message = fullErrorDetails });

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

        // حفظ ملف عام (غلاف، صور بوستات، ستوري)
        private async Task<string> SaveUploadedFile(IFormFile file, string subfolder)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", subfolder);
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueName = Guid.NewGuid().ToString() + ext;
            var filePath = Path.Combine(uploadsFolder, uniqueName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{subfolder}/" + uniqueName;
        }

        // تعديل بيانات الحساب (الاسم / رقم الموبايل / الصورة)
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

        // تحديث البروفايل الشخصي (Bio, Nickname, Cover)
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string? bio, string? nickname, IFormFile? coverPhoto)
        {
            var attendanceId = GetOrRestoreAttendanceId();
            if (attendanceId == null)
                return Json(new { success = false, message = "غير مصرح" });

            var attendance = await _context.Attendances.FindAsync(attendanceId.Value);
            if (attendance == null)
                return Json(new { success = false, message = "المستخدم مش موجود" });

            try
            {
                attendance.Bio = bio?.Trim();
                attendance.Nickname = nickname?.Trim();
                attendance.UpdatedAt = DateTime.Now;

                if (coverPhoto != null && coverPhoto.Length > 0)
                {
                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                    if (allowedTypes.Contains(coverPhoto.ContentType))
                        attendance.CoverPhotoPath = await SaveUploadedFile(coverPhoto, "covers");
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, bio = attendance.Bio, nickname = attendance.Nickname, cover = attendance.CoverPhotoPath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile.");
                return Json(new { success = false, message = "حصلت مشكلة" });
            }
        }

        // عرض البروفايل الشخصي
        public async Task<IActionResult> Profile(int id)
        {
            var currentAttendanceId = GetOrRestoreAttendanceId();
            if (currentAttendanceId == null)
                return RedirectToAction("Index");

            var profile = await _context.Attendances
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "البروفايل مش موجود";
                return RedirectToAction("Room");
            }

            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Comments.Where(c => !c.IsDeleted)).ThenInclude(c => c.Commenter)
                .Include(p => p.Reactions)
                .Where(p => p.AttendanceId == id && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            ViewBag.CurrentAttendanceId = currentAttendanceId;
            ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
            ViewBag.Posts = posts;
            ViewBag.IsOwnProfile = currentAttendanceId == id;
            return View(profile);
        }

        // رفع ميديا الشات
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

        // 4. غرفة الحضور والشات
        public async Task<IActionResult> Room()
        {
            var currentAttendanceId = GetOrRestoreAttendanceId();

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
                    _logger.LogWarning(ex, "Could not fetch ChatMessages.");
                }

                // تحميل الستوريات النشطة (أقل من 24 ساعة)
                var stories = new List<Story>();
                try
                {
                    stories = await _context.Stories
                        .AsNoTracking()
                        .Include(s => s.Author)
                        .Where(s => !s.IsDeleted && s.ExpiresAt > DateTime.Now)
                        .OrderByDescending(s => s.CreatedAt)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch Stories.");
                }

                // تحميل أول 10 بوستات
                var posts = new List<Post>();
                try
                {
                    posts = await _context.Posts
                        .AsNoTracking()
                        .Include(p => p.Author)
                        .Include(p => p.Comments.Where(c => !c.IsDeleted)).ThenInclude(c => c.Commenter)
                        .Include(p => p.Reactions)
                        .Where(p => !p.IsDeleted)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(10)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch Posts.");
                }

                ViewBag.CurrentAttendanceId = currentAttendanceId;
                ViewBag.Messages = messages ?? new List<ChatMessage>();
                ViewBag.Stories = stories ?? new List<Story>();
                ViewBag.Posts = posts ?? new List<Post>();
                ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin") == "true";

                return View(attendances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Room page.");
                return Content($"Room Page Error:\n\nMessage: {ex.Message}\n\nDetails:\n{ex}", "text/plain; charset=utf-8");
            }
        }

        // API: بوستات إضافية (Infinite Scroll)
        [HttpGet]
        public async Task<IActionResult> GetRoomPosts(int skip = 0, int take = 10)
        {
            var attendanceId = GetOrRestoreAttendanceId();
            if (attendanceId == null)
                return Json(new { success = false });

            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Comments.Where(c => !c.IsDeleted))
                .Include(p => p.Reactions)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(p => new
                {
                    p.Id,
                    p.Content,
                    p.MediaPath,
                    p.MediaType,
                    p.IsAdminPost,
                    createdAt = p.CreatedAt.ToString("dd/MM HH:mm"),
                    author = new
                    {
                        p.Author!.Id,
                        name = p.Author.FriendName,
                        nickname = p.Author.Nickname,
                        photo = p.Author.ProfilePicturePath,
                        isMvp = p.Author.IsMVP,
                        rating = p.Author.PlayerRating,
                        tag = p.Author.PlayerTag
                    },
                    reactions = p.Reactions.GroupBy(r => r.ReactionType)
                        .Select(g => new { type = g.Key, count = g.Count() }),
                    commentsCount = p.Comments.Count(c => !c.IsDeleted)
                })
                .ToListAsync();

            return Json(new { success = true, posts });
        }

        // API: إنشاء بوست جديد
        [HttpPost]
        public async Task<IActionResult> CreatePost(string? content, IFormFile? media, bool isAdminPost = false)
        {
            var attendanceId = GetOrRestoreAttendanceId();
            if (attendanceId == null)
                return Json(new { success = false, message = "يرجى تسجيل موقفك أولاً!" });

            bool isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
            if (isAdminPost && !isAdmin) isAdminPost = false;

            var hasContent = !string.IsNullOrWhiteSpace(content);
            string? mediaPath = null;
            string? mediaType = null;

            try
            {
                if (media != null && media.Length > 0)
                {
                    var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                    var allowedVideoTypes = new[] { "video/mp4", "video/webm", "video/ogg" };
                    var allAllowed = allowedImageTypes.Concat(allowedVideoTypes).ToArray();

                    if (!allAllowed.Contains(media.ContentType))
                        return Json(new { success = false, message = "نوع الملف غير مسموح به" });

                    mediaType = allowedImageTypes.Contains(media.ContentType) ? "image" : "video";
                    mediaPath = await SaveUploadedFile(media, "posts");
                }

                if (!hasContent && mediaPath == null)
                    return Json(new { success = false, message = "اكتب حاجة أو ارفع صورة/فيديو الأول" });

                var post = new Post
                {
                    AttendanceId = attendanceId.Value,
                    Content = hasContent ? content!.Trim() : null,
                    MediaPath = mediaPath,
                    MediaType = mediaType,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    IsAdminPost = isAdminPost
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                var author = await _context.Attendances.FindAsync(attendanceId.Value);

                var notifTitle = isAdminPost ? "منشور مميز جديد ⭐" : "منشور جديد 📝";
                _ = _push.SendToAllAsync(notifTitle, $"{author?.FriendName}: {(hasContent ? content!.Trim() : "📎 صورة/فيديو")}", excludeAttendanceId: attendanceId.Value);

                return Json(new
                {
                    success = true,
                    post = new
                    {
                        post.Id,
                        post.Content,
                        post.MediaPath,
                        post.MediaType,
                        post.IsAdminPost,
                        createdAt = post.CreatedAt.ToString("dd/MM HH:mm"),
                        author = new
                        {
                            id = author?.Id,
                            name = author?.FriendName,
                            nickname = author?.Nickname,
                            photo = author?.ProfilePicturePath,
                            isMvp = author?.IsMVP ?? false,
                            rating = author?.PlayerRating,
                            tag = author?.PlayerTag
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post.");
                return Json(new { success = false, message = "حصل خطأ أثناء نشر البوست" });
            }
        }

        // API: حذف بوست
        [HttpPost]
        public async Task<IActionResult> DeletePost(int id)
        {
            var attendanceId = GetOrRestoreAttendanceId();
            bool isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";

            if (attendanceId == null && !isAdmin)
                return Json(new { success = false });

            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                var isOwner = attendanceId != null && post.AttendanceId == attendanceId.Value;
                if (isOwner || isAdmin)
                {
                    post.IsDeleted = true;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
            }

            return Json(new { success = false });
        }

        // API: إضافة كومنت على بوست
        [HttpPost]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            var attendanceId = GetOrRestoreAttendanceId();
            if (attendanceId == null)
                return Json(new { success = false });

            if (!string.IsNullOrWhiteSpace(content))
            {
                var postExists = await _context.Posts.AnyAsync(p => p.Id == postId && !p.IsDeleted);
                if (postExists)
                {
                    var comment = new PostComment
                    {
                        PostId = postId,
                        AttendanceId = attendanceId.Value,
                        Content = content.Trim(),
                        CreatedAt = DateTime.Now,
                        IsDeleted = false
                    };
                    _context.PostComments.Add(comment);
                    await _context.SaveChangesAsync();

                    var author = await _context.Attendances.FindAsync(attendanceId.Value);
                    return Json(new
                    {
                        success = true,
                        comment = new
                        {
                            comment.Id,
                            comment.Content,
                            createdAt = comment.CreatedAt.ToString("HH:mm"),
                            commenter = new { id = author?.Id, name = author?.FriendName, photo = author?.ProfilePicturePath }
                        }
                    });
                }
            }

            return Json(new { success = false });
        }

        // API: ريأكت على بوست
        [HttpPost]
        public async Task<IActionResult> ReactPost(int postId, string reactionType)
        {
            var attendanceId = GetOrRestoreAttendanceId();
            if (attendanceId == null)
                return Json(new { success = false });

            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId && !p.IsDeleted);
            if (postExists)
            {
                var existing = await _context.PostReactions
                    .FirstOrDefaultAsync(r => r.PostId == postId && r.AttendanceId == attendanceId.Value);

                if (existing != null)
                {
                    if (existing.ReactionType == reactionType)
                        _context.PostReactions.Remove(existing);
                    else
                        existing.ReactionType = reactionType;
                }
                else
                {
                    _context.PostReactions.Add(new PostReaction
                    {
                        PostId = postId,
                        AttendanceId = attendanceId.Value,
                        ReactionType = reactionType
                    });
                }

                await _context.SaveChangesAsync();

                var counts = await _context.PostReactions
                    .Where(r => r.PostId == postId)
                    .GroupBy(r => r.ReactionType)
                    .Select(g => new { type = g.Key, count = g.Count() })
                    .ToListAsync();

                return Json(new { success = true, reactions = counts });
            }

            return Json(new { success = false });
        }

        // ===== STORIES =====

        // API: إنشاء ستوري
        [HttpPost]
        public async Task<IActionResult> CreateStory(string? content, IFormFile? media)
        {
            var attendanceId = GetOrRestoreAttendanceId();
            if (attendanceId == null)
                return Json(new { success = false, message = "يرجى تسجيل موقفك أولاً!" });

            var hasContent = !string.IsNullOrWhiteSpace(content);
            string? mediaPath = null;
            string? mediaType = null;

            try
            {
                if (media != null && media.Length > 0)
                {
                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "video/mp4", "video/webm" };
                    if (!allowedTypes.Contains(media.ContentType))
                        return Json(new { success = false, message = "نوع الملف غير مسموح به" });

                    mediaType = media.ContentType.StartsWith("image/") ? "image" : "video";
                    mediaPath = await SaveUploadedFile(media, "stories");
                }

                if (!hasContent && mediaPath == null)
                    return Json(new { success = false, message = "اكتب حاجة أو ارفع صورة" });

                var story = new Story
                {
                    AttendanceId = attendanceId.Value,
                    Content = hasContent ? content!.Trim() : null,
                    MediaPath = mediaPath,
                    MediaType = mediaType,
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddHours(24),
                    IsDeleted = false
                };

                _context.Stories.Add(story);
                await _context.SaveChangesAsync();

                var author = await _context.Attendances.FindAsync(attendanceId.Value);

                return Json(new
                {
                    success = true,
                    story = new
                    {
                        story.Id,
                        story.Content,
                        story.MediaPath,
                        story.MediaType,
                        createdAt = story.CreatedAt.ToString("HH:mm"),
                        author = new
                        {
                            id = author?.Id,
                            name = author?.FriendName,
                            photo = author?.ProfilePicturePath,
                            isMvp = author?.IsMVP ?? false
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating story.");
                return Json(new { success = false, message = "حصل خطأ" });
            }
        }

        // API: الرد على ستوري — يتبعت في الخاص مع محتوى الستوري
        [HttpPost]
        public async Task<IActionResult> ReplyToStory(int storyId, string replyText)
        {
            var attendanceId = GetOrRestoreAttendanceId();
            if (attendanceId == null)
                return Json(new { success = false, message = "يرجى تسجيل موقفك أولاً!" });

            if (string.IsNullOrWhiteSpace(replyText))
                return Json(new { success = false, message = "اكتب رد أولاً" });

            var story = await _context.Stories
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.Id == storyId && !s.IsDeleted);

            if (story == null)
                return Json(new { success = false, message = "الستوري مش موجود" });

            var replier = await _context.Attendances.FindAsync(attendanceId.Value);
            if (replier == null)
                return Json(new { success = false });

            // بناء محتوى الرسالة
            var storyPreview = story.Content?.Length > 50
                ? story.Content.Substring(0, 50) + "..."
                : story.Content;

            var messageContent = story.MediaPath != null
                ? $"↩️ رد على ستوريك: {replyText}\n[📸 محتوى مرئي]"
                : $"↩️ رد على ستوريك: \"{storyPreview}\"\n{replyText}";

            // 1. إرسال في الخاص لصاحب الستوري
            try
            {
                var dm = new DirectMessage
                {
                    SenderId = attendanceId.Value,
                    ReceiverId = story.AttendanceId,
                    Content = messageContent,
                    SentAt = DateTime.Now,
                    IsRead = false,
                    IsDeleted = false
                };
                _context.DirectMessages.Add(dm);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not save direct message for story reply.");
            }

            return Json(new { success = true, message = "تم إرسال الرد في الخاص ✅" });
        }

        // API: جلب رسائل الشات الخاص بين مستخدمين
        [HttpGet]
        public async Task<IActionResult> GetDirectMessages(int otherUserId)
        {
            var currentId = GetOrRestoreAttendanceId();
            if (currentId == null)
                return Json(new { success = false, message = "سجل موقفك أولاً" });

            var otherUser = await _context.Attendances.FindAsync(otherUserId);
            if (otherUser == null)
                return Json(new { success = false, message = "المستخدم غير موجود" });

            try
            {
                var messages = await _context.DirectMessages
                    .AsNoTracking()
                    .Where(m => !m.IsDeleted &&
                        ((m.SenderId == currentId.Value && m.ReceiverId == otherUserId) ||
                         (m.SenderId == otherUserId && m.ReceiverId == currentId.Value)))
                    .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        m.Id,
                        m.SenderId,
                        m.ReceiverId,
                        m.Content,
                        m.MediaPath,
                        m.MediaType,
                        sentAt = m.SentAt.ToString("HH:mm"),
                        isMine = m.SenderId == currentId.Value
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    user = new
                    {
                        id = otherUser.Id,
                        name = otherUser.FriendName,
                        nickname = otherUser.Nickname,
                        photo = otherUser.ProfilePicturePath,
                        isMvp = otherUser.IsMVP
                    },
                    messages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting direct messages.");
                return Json(new { success = false, message = "حصل خطأ في جلب الرسائل" });
            }
        }

        // API: إرسال رسالة خاصة في الشات الخاص
        [HttpPost]
        public async Task<IActionResult> SendDirectMessage(int receiverId, string content)
        {
            var currentId = GetOrRestoreAttendanceId();
            if (currentId == null)
                return Json(new { success = false, message = "سجل موقفك أولاً" });

            if (string.IsNullOrWhiteSpace(content))
                return Json(new { success = false, message = "اكتب رسالة أولاً" });

            var receiver = await _context.Attendances.FindAsync(receiverId);
            if (receiver == null)
                return Json(new { success = false, message = "المستخدم غير موجود" });

            var sender = await _context.Attendances.FindAsync(currentId.Value);

            try
            {
                var dm = new DirectMessage
                {
                    SenderId = currentId.Value,
                    ReceiverId = receiverId,
                    Content = content.Trim(),
                    SentAt = DateTime.Now,
                    IsRead = false,
                    IsDeleted = false
                };

                _context.DirectMessages.Add(dm);
                await _context.SaveChangesAsync();

                _ = _push.SendToAllAsync($"رسالة خاصة من {sender?.FriendName}", dm.Content, excludeAttendanceId: currentId.Value);

                return Json(new
                {
                    success = true,
                    message = new
                    {
                        dm.Id,
                        dm.SenderId,
                        dm.ReceiverId,
                        dm.Content,
                        sentAt = dm.SentAt.ToString("HH:mm"),
                        isMine = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending direct message.");
                return Json(new { success = false, message = "حصل خطأ أثناء إرسال الرسالة" });
            }
        }

        // API: حذف ستوري
        [HttpPost]
        public async Task<IActionResult> DeleteStory(int id)
        {
            var attendanceId = GetOrRestoreAttendanceId();
            bool isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";

            var story = await _context.Stories.FindAsync(id);
            if (story != null)
            {
                var isOwner = attendanceId != null && story.AttendanceId == attendanceId.Value;
                if (isOwner || isAdmin)
                {
                    story.IsDeleted = true;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
            }

            return Json(new { success = false });
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

            var currentAttendanceId = GetOrRestoreAttendanceId();
            Attendance? currentAttendee = null;
            if (currentAttendanceId != null)
            {
                currentAttendee = await _context.Attendances.FindAsync(currentAttendanceId.Value);
            }
            ViewBag.CurrentAttendee = currentAttendee;

            var responses = await _context.Attendances
                .AsNoTracking()
                .OrderByDescending(a => a.UpdatedAt ?? a.RespondedAt)
                .ToListAsync();

            return View(responses);
        }

        // الخروج من وضع الأدمن والرجوع للحساب العادي
        public IActionResult ExitAdmin()
        {
            HttpContext.Session.Remove("IsAdmin");
            var attendanceId = GetOrRestoreAttendanceId();
            if (attendanceId != null)
            {
                TempData["SuccessMessage"] = "تمت العودة لحسابك الشخصي بنجاح 👋";
                return RedirectToAction("Room");
            }
            return RedirectToAction("Index");
        }

        // دخول الأدمن بحساب عضو معين
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchToUser(int id)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("Login");

            var target = await _context.Attendances.FindAsync(id);
            if (target != null)
            {
                HttpContext.Session.SetInt32("AttendanceId", target.Id);
                HttpContext.Session.SetString("UserName", target.FriendName);
                SetAttendanceCookie(target.Id);
                HttpContext.Session.Remove("IsAdmin");
                TempData["SuccessMessage"] = $"تم الدخول بحساب {target.FriendName} بنجاح! 👋";
                return RedirectToAction("Room");
            }

            return RedirectToAction("Admin");
        }

        // إعادة تعيين إجابات جميع الأعضاء لبدء ماتش جديد (Reset)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAllStatuses()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("Login");

            var attendances = await _context.Attendances.ToListAsync();
            foreach (var item in attendances)
            {
                item.Status = "لم يحدد";
                item.Note = null;
                item.TeamNumber = null;
                item.PlayerPosition = null;
                item.UpdatedAt = DateTime.Now;
            }

            var settings = await _context.MatchSettings.FirstOrDefaultAsync();
            if (settings != null)
            {
                settings.WinnerTeamNumber = null;
            }

            await _context.SaveChangesAsync();

            _ = _push.SendToAllAsync("ماتش جديد! ⚽", "تم فتح باب تسجيل الحضور للماتش القادم، ادخل حدد موقفك الآن!");

            TempData["SuccessMessage"] = "تمت إعادة تعيين إجابات جميع الأعضاء بنجاح! جاهزون للماتش الجديد 🚀";
            return RedirectToAction("Admin");
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

        // 8.5 تعيين نجم المباراة (Admin)
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

                var currentMvps = await _context.Attendances.Where(a => a.IsMVP).ToListAsync();
                foreach (var a in currentMvps)
                    a.IsMVP = false;

                target.IsMVP = makingMvp;
                await _context.SaveChangesAsync();

                if (makingMvp)
                    _ = _push.SendToAllAsync("نجم المباراة 👑", $"{target.FriendName} أفضل لاعب في الماتش!", excludeAttendanceId: target.Id);
            }

            return RedirectToAction("Admin");
        }

        // 8.6 تعديل اسم/صورة/كلمة سر أي شخص (Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttendeeAdmin(int id, string newName, string? newPassword, IFormFile? profilePhoto)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("Login");

            newName = (newName ?? "").Trim();
            newPassword = newPassword?.Trim();

            var target = await _context.Attendances.FindAsync(id);
            if (target != null)
            {
                if (!string.IsNullOrEmpty(newName))
                    target.FriendName = newName;

                if (!string.IsNullOrEmpty(newPassword))
                    target.Password = newPassword;

                if (profilePhoto != null && profilePhoto.Length > 0)
                    target.ProfilePicturePath = await SaveProfilePhoto(profilePhoto);

                target.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"تم حفظ تعديلات {target.FriendName} بنجاح!";
            }

            return RedirectToAction("Admin");
        }

        // Admin: تعيين تقييم لاعب
        [HttpPost]
        public async Task<IActionResult> AdminSetRating(int attendanceId, int rating)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return Json(new { success = false });

            var player = await _context.Attendances.FindAsync(attendanceId);
            if (player == null) return Json(new { success = false });

            player.PlayerRating = Math.Clamp(rating, 0, 10);
            await _context.SaveChangesAsync();
            return Json(new { success = true, rating = player.PlayerRating });
        }

        // Admin: تعيين تاج/لقب للاعب
        [HttpPost]
        public async Task<IActionResult> AdminSetTag(int attendanceId, string tag)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return Json(new { success = false });

            var player = await _context.Attendances.FindAsync(attendanceId);
            if (player == null) return Json(new { success = false });

            player.PlayerTag = tag?.Trim();
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // 9. تسجيل خروج
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("AttId");
            return RedirectToAction("Index");
        }

        // 11. تغيير الموقف بضغطة واحدة
        [HttpPost]
        public async Task<IActionResult> QuickStatus(string status, string? note = null)
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
            if (note != null)
                attendance.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            attendance.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (oldStatus != status)
                _ = _push.SendToAllAsync("تحديث الموقف 🔄", $"{attendance.FriendName} غيّر موقفه إلى: {status}", excludeAttendanceId: attendance.Id);

            return Json(new { success = true, status = attendance.Status, note = attendance.Note });
        }

        // 10. Error page
        public IActionResult Error()
        {
            return View();
        }
    }
}