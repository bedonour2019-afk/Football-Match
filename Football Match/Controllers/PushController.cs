using Microsoft.AspNetCore.Mvc;
using Football_Match.Models;
using Microsoft.EntityFrameworkCore;

namespace Football_Match.Controllers
{
    [Route("api/push")]
    public class PushController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public PushController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpGet("vapid-public-key")]
        public IActionResult GetPublicKey()
        {
            return Content(_config["Vapid:PublicKey"] ?? "");
        }

        public class SubscribeRequest
        {
            public string Endpoint { get; set; } = string.Empty;
            public string P256dh { get; set; } = string.Empty;
            public string Auth { get; set; } = string.Empty;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Endpoint))
                return BadRequest();

            var attendanceId = HttpContext.Session.GetInt32("AttendanceId");

            var exists = await _context.PushSubscribers.AnyAsync(s => s.Endpoint == req.Endpoint);
            if (!exists)
            {
                _context.PushSubscribers.Add(new PushSubscriber
                {
                    AttendanceId = attendanceId,
                    Endpoint = req.Endpoint,
                    P256dh = req.P256dh,
                    Auth = req.Auth,
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }
    }
}
