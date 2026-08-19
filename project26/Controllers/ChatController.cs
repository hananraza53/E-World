using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project26.ApplicationDb;
using project26.Models;

namespace project26.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Chat/GetMessages?sessionId=XYZ
        [HttpGet]
        public async Task<IActionResult> GetMessages(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return Json(new { success = false, message = "Session ID is required." });
            }

            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.SenderName,
                    m.MessageText,
                    Timestamp = m.Timestamp.ToString("hh:mm tt"),
                    m.IsFromAdmin
                })
                .ToListAsync();

            return Json(new { success = true, messages });
        }

        // POST: /Chat/SendMessage
        [HttpPost]
        public async Task<IActionResult> SendMessage(string sessionId, string message)
        {
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            // Find if there is a logged-in user
            int? userId = null;
            string senderName = "Guest";
            if (User.Identity.IsAuthenticated)
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }
                senderName = User.Identity.Name ?? "Customer";
            }

            var chatMessage = new ChatMessage
            {
                UserId = userId,
                SenderName = senderName,
                MessageText = message.Trim(),
                Timestamp = DateTime.Now,
                IsFromAdmin = false,
                SessionId = sessionId
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
