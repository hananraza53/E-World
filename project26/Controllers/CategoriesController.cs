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
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;

        public CategoriesController(ApplicationDbContext context, IConfiguration configuration)
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
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Category.OrderByDescending(c => c.cid).ToListAsync();
            return Json(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromForm] Category category, IFormFile image)
        {
            try
            {
                if (image != null && image.Length > 0)
                {
                    using var stream = image.OpenReadStream();
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(image.FileName, stream),
                        Folder = "categories"
                    };
                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    category.Imageurl = uploadResult?.SecureUrl?.ToString();
                }

                if (category.cid == 0) // New
                {
                    _context.Category.Add(category);
                }
                else // Edit
                {
                    var existing = await _context.Category.FindAsync(category.cid);
                    if (existing == null) return Json(new { success = false, message = "Category not found" });
                    
                    existing.cname = category.cname;
                    if (!string.IsNullOrEmpty(category.Imageurl))
                    {
                        existing.Imageurl = category.Imageurl;
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
            var category = await _context.Category.FindAsync(id);
            if (category == null) return Json(new { success = false, message = "Category not found" });

            _context.Category.Remove(category);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
