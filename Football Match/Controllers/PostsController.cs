using Microsoft.AspNetCore.Mvc;
using Football_Match.Models;
using Microsoft.EntityFrameworkCore;

namespace Football_Match.Controllers
{
    public class PostsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PostsController> _logger;

        public PostsController(AppDbContext context, IWebHostEnvironment env, ILogger<PostsController> logger)
        {
            _context = context;
            _env = env;
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

        // 1. عرض كل المنشورات
        public async Task<IActionResult> Index()
        {
            if (CurrentAttendanceId == null)
            {
                TempData["ErrorMessage"] = "يرجى تسجيل موقفك ورقم الموبايل أولاً!";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var posts = await _context.Posts
                    .AsNoTracking()
                    .Include(p => p.Author)
                    .Include(p => p.Comments.Where(c => !c.IsDeleted)).ThenInclude(c => c.Commenter)
                    .Include(p => p.Reactions)
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                ViewBag.CurrentAttendanceId = CurrentAttendanceId.Value;
                ViewBag.IsAdmin = IsAdmin;
                return View(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Posts page.");
                return Content($"Posts Page Error:\n\nMessage: {ex.Message}\n\nDetails:\n{ex}", "text/plain; charset=utf-8");
            }
        }

        // 2. نشر بوست جديد
        [HttpPost]
        public async Task<IActionResult> Create(string? content, IFormFile? media)
        {
            var attendanceId = CurrentAttendanceId;
            if (attendanceId == null)
            {
                TempData["ErrorMessage"] = "يرجى تسجيل موقفك أولاً!";
                return RedirectToAction("Index", "Home");
            }

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
                    {
                        TempData["ErrorMessage"] = "نوع الملف غير مسموح به";
                        return RedirectToAction("Index");
                    }

                    mediaType = allowedImageTypes.Contains(media.ContentType) ? "image" : "video";
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "posts");
                    Directory.CreateDirectory(uploadsFolder);

                    var ext = Path.GetExtension(media.FileName).ToLowerInvariant();
                    var uniqueName = Guid.NewGuid().ToString() + ext;
                    var filePath = Path.Combine(uploadsFolder, uniqueName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await media.CopyToAsync(stream);

                    mediaPath = "/uploads/posts/" + uniqueName;
                }

                if (!hasContent && mediaPath == null)
                {
                    TempData["ErrorMessage"] = "اكتب حاجة أو ارفع صورة/فيديو الأول";
                    return RedirectToAction("Index");
                }

                var post = new Post
                {
                    AttendanceId = attendanceId.Value,
                    Content = hasContent ? content!.Trim() : null,
                    MediaPath = mediaPath,
                    MediaType = mediaType,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post.");
                TempData["ErrorMessage"] = "حصل خطأ أثناء نشر البوست";
            }

            return RedirectToAction("Index");
        }

        // 3. حذف بوست (صاحبه أو الأدمن)
        [HttpPost]
        public async Task<IActionResult> DeletePost(int id)
        {
            var attendanceId = CurrentAttendanceId;
            if (attendanceId == null && !IsAdmin)
                return RedirectToAction("Index", "Home");

            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                var isOwner = attendanceId != null && post.AttendanceId == attendanceId.Value;
                if (isOwner || IsAdmin)
                {
                    post.IsDeleted = true;
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }

        // 4. إضافة كومنت
        [HttpPost]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            var attendanceId = CurrentAttendanceId;
            if (attendanceId == null)
                return RedirectToAction("Index", "Home");

            if (!string.IsNullOrWhiteSpace(content))
            {
                var postExists = await _context.Posts.AnyAsync(p => p.Id == postId && !p.IsDeleted);
                if (postExists)
                {
                    _context.PostComments.Add(new PostComment
                    {
                        PostId = postId,
                        AttendanceId = attendanceId.Value,
                        Content = content.Trim(),
                        CreatedAt = DateTime.Now,
                        IsDeleted = false
                    });
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }

        // 5. حذف كومنت (صاحبه أو الأدمن)
        [HttpPost]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var attendanceId = CurrentAttendanceId;
            if (attendanceId == null && !IsAdmin)
                return RedirectToAction("Index", "Home");

            var comment = await _context.PostComments.FindAsync(id);
            if (comment != null)
            {
                var isOwner = attendanceId != null && comment.AttendanceId == attendanceId.Value;
                if (isOwner || IsAdmin)
                {
                    comment.IsDeleted = true;
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }

        // 6. عمل / تغيير / إلغاء ريأكت على بوست
        [HttpPost]
        public async Task<IActionResult> React(int postId, string reactionType)
        {
            var attendanceId = CurrentAttendanceId;
            if (attendanceId == null)
                return RedirectToAction("Index", "Home");

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
            }

            return RedirectToAction("Index");
        }
    }
}
