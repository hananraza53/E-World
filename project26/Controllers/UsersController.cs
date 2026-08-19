using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project26.ApplicationDb;
using project26.Models;

namespace project26.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Users
        public async Task<IActionResult> Index()
        {
            var users = await _context.User.ToListAsync();
            return View(users);
        }

        // POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string uname, string uemail, string uphone, string upassword, string urole)
        {
            // Check if email already exists
            bool emailExists = await _context.User.AnyAsync(u => u.uemail == uemail);
            if (emailExists)
            {
                TempData["Error"] = "Email already exists.";
                return RedirectToAction("Index");
            }

            var user = new User
            {
                uname        = uname,
                uemail       = uemail,
                uphone       = uphone,
                upassword    = BCrypt.Net.BCrypt.HashPassword(upassword),
                urole        = string.IsNullOrEmpty(urole) ? "user" : urole,
                verifystatus = true,
                Image        = null
            };

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "User created successfully!";
            return RedirectToAction("Index");
        }

        // POST: /Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.User.FindAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            _context.User.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "User deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}
