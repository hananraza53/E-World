using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project26.ApplicationDb;
using project26.Models;
using System.Linq;
using System.Threading.Tasks;

namespace project26.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel();

            // Fetch User stats
            var users = await _context.User.ToListAsync();
            
            model.TotalUsers = users.Count;
            model.VerifiedUsers = users.Count(u => u.verifystatus == true);
            model.UnverifiedUsers = users.Count(u => u.verifystatus != true);
            model.AdminUsers = users.Count(u => u.urole == "admin");

            // Fetch Recent Users (Top 10)
            model.RecentUsers = users.OrderByDescending(u => u.uid).Take(10).ToList();

            // Mocked growth data for chart
            model.UserGrowth = "+12.5%";
            model.UserGrowthData = new List<int> { 5, 12, 18, 25, 30, 45, 60, 55, 78, 92, 105, 120 };
            model.ChartLabels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

            return View(model);
        }
        public async Task<IActionResult> Products()
        {
            var products = await _context.Product
                .Include(p => p.category)
                .Include(p => p.Image)
                .OrderByDescending(p => p.pid)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> Coupons()
        {
            ViewBag.Products = await _context.Product.OrderBy(p => p.pname).ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCoupons()
        {
            var coupons = await _context.Coupons
                .Include(c => c.Product)
                .OrderByDescending(c => c.Id)
                .Select(c => new {
                    c.Id,
                    c.Code,
                    c.DiscountValue,
                    c.Type,
                    c.ExpiryDate,
                    c.IsActive,
                    c.MinimumAmount,
                    c.ProductId,
                    ProductName = c.Product != null ? c.Product.pname : "All Products"
                })
                .ToListAsync();
            return Json(coupons);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCoupon(Coupon coupon)
        {
            if (coupon.Id == 0)
            {
                _context.Coupons.Add(coupon);
            }
            else
            {
                var existing = await _context.Coupons.FindAsync(coupon.Id);
                if (existing == null) return NotFound();
                
                existing.Code = coupon.Code;
                existing.DiscountValue = coupon.DiscountValue;
                existing.Type = coupon.Type;
                existing.ExpiryDate = coupon.ExpiryDate;
                existing.IsActive = coupon.IsActive;
                existing.MinimumAmount = coupon.MinimumAmount;
                existing.ProductId = coupon.ProductId;
                _context.Update(existing);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        // Live Chat Admin Endpoints
        //[Authorize(Roles = "Admin")]
        public IActionResult Chats()
        {
            return View();
        }

        //[Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetChatSessions()
        {
            var sessions = await _context.ChatMessages
                .GroupBy(m => m.SessionId)
                .Select(g => new
                {
                    SessionId = g.Key,
                    LatestMessage = g.OrderByDescending(m => m.Timestamp).FirstOrDefault()
                })
                .ToListAsync();

            var sortedSessions = sessions
                .Where(s => s.LatestMessage != null)
                .OrderByDescending(s => s.LatestMessage.Timestamp)
                .Select(s => new
                {
                    s.SessionId,
                    SenderName = s.LatestMessage.SenderName,
                    LastMessageText = s.LatestMessage.MessageText,
                    Timestamp = s.LatestMessage.Timestamp.ToString("hh:mm tt"),
                    Date = s.LatestMessage.Timestamp.ToString("yyyy-MM-dd")
                })
                .ToList();

            return Json(sortedSessions);
        }

        //[Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetSessionMessages(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return BadRequest();

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

            return Json(messages);
        }

        //[Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SendAdminReply(string sessionId, string message)
        {
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(message))
            {
                return BadRequest();
            }

            var chatMessage = new ChatMessage
            {
                UserId = null,
                SenderName = "Admin",
                MessageText = message.Trim(),
                Timestamp = DateTime.Now,
                IsFromAdmin = true,
                SessionId = sessionId
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
