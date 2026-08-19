using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using project26.ApplicationDb;
using project26.Models;

namespace project26.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class BannersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;

        public BannersController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            
            var account = new Account(
                configuration["CloudinarySettings:CloudName"],
                configuration["CloudinarySettings:ApiKey"],
                configuration["CloudinarySettings:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetBanners()
        {
            var banners = await _context.Banner.OrderByDescending(b => b.bid).ToListAsync();
            return Json(banners);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromForm] Banner banner, IFormFile image)
        {
            try
            {
                if (image != null && image.Length > 0)
                {
                    using var stream = image.OpenReadStream();
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(image.FileName, stream),
                        Folder = "banners"
                    };
                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    banner.ImageUrl = uploadResult?.SecureUrl?.ToString();
                }

                if (banner.bid == 0) // New
                {
                    _context.Banner.Add(banner);
                }
                else // Edit
                {
                    var existing = await _context.Banner.FindAsync(banner.bid);
                    if (existing == null) return Json(new { success = false, message = "Banner not found" });
                    
                    existing.btitle = banner.btitle;
                    existing.bdescription = banner.bdescription;
                    existing.IsActive = banner.IsActive;
                    if (!string.IsNullOrEmpty(banner.ImageUrl))
                    {
                        existing.ImageUrl = banner.ImageUrl;
                    }
                }
                
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.Banner.FindAsync(id);
            if (banner == null) return Json(new { success = false, message = "Banner not found" });

            _context.Banner.Remove(banner);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var banner = await _context.Banner.FindAsync(id);
            if (banner == null) return Json(new { success = false, message = "Banner not found" });

            banner.IsActive = !banner.IsActive;
            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = banner.IsActive });
        }
    }
}
