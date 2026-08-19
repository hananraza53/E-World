using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project26.ApplicationDb;
using project26.Models;

namespace project26.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly Cloudinary _cloudinary;

        public ProductsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            var account = new Account(
                _configuration["CloudinarySettings:CloudName"],
                _configuration["CloudinarySettings:ApiKey"],
                _configuration["CloudinarySettings:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<IActionResult> Index(string category, decimal? minPrice, decimal? maxPrice, string sortBy)
        {
            var query = _context.Product
                .Include(p => p.category)
                .Include(p => p.Image)
                .Include(p => p.colors)
                .Include(p => p.Size)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.category.cname == category);
                ViewBag.CurrentCategory = category;
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.pdisprice >= minPrice.Value);
                ViewBag.MinPrice = minPrice;
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.pdisprice <= maxPrice.Value);
                ViewBag.MaxPrice = maxPrice;
            }

            // Sorting logic
            query = sortBy switch
            {
                "price_low_high" => query.OrderBy(p => p.pdisprice),
                "price_high_low" => query.OrderByDescending(p => p.pdisprice),
                _ => query.OrderByDescending(p => p.pid)
            };
            ViewBag.SortBy = sortBy;

            var products = await query.ToListAsync();
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _context.Product
                    .Include(p => p.category)
                    .Include(p => p.Image)
                    .Include(p => p.colors)
                    .Include(p => p.Size)
                    .OrderByDescending(p => p.pid)
                    .Select(p => new {
                        p.pid,
                        p.pname,
                        p.pdes,
                        p.pprice,
                        p.pdis,
                        p.pdisprice,
                        p.pqty,
                        p.Categoryid,
                        categoryName = p.category.cname,
                        imageUrl = p.Image.FirstOrDefault().ImageUrl ?? "",
                        color = p.colors.FirstOrDefault().pcolor ?? "",
                        size = p.Size.FirstOrDefault().psize ?? ""
                    })
                    .ToListAsync();
                return Json(products);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromForm] Product product, IFormFile imageFile, string selectedSize, string selectedColor, string localImageUrl)
        {
            try
            {
                // 1. Calculate Discount Price
                if (product.pdis.HasValue && product.pprice.HasValue)
                {
                    product.pdisprice = product.pprice.Value - (product.pprice.Value * (product.pdis.Value / 100));
                }
                else if (product.pprice.HasValue)
                {
                    product.pdisprice = product.pprice.Value;
                }

                product.updatedat = DateTime.Now;

                if (product.pid == 0) // Create
                {
                    product.createdat = DateTime.Now;
                    _context.Product.Add(product);
                    await _context.SaveChangesAsync();
                }
                else // Update
                {
                    var existing = await _context.Product
                        .Include(p => p.Image)
                        .Include(p => p.colors)
                        .Include(p => p.Size)
                        .FirstOrDefaultAsync(p => p.pid == product.pid);

                    if (existing == null) return Json(new { success = false, message = "Product not found" });

                    existing.pname = product.pname;
                    existing.pdes = product.pdes;
                    existing.pprice = product.pprice;
                    existing.pdis = product.pdis;
                    existing.pdisprice = product.pdisprice;
                    existing.pqty = product.pqty;
                    existing.Categoryid = product.Categoryid;
                    existing.updatedat = DateTime.Now;

                    product = existing; // Use existing to update related tables
                }

                // 2. Handle Image Upload (Cloudinary or Local)
                string finalImageUrl = null;

                if (imageFile != null && imageFile.Length > 0)
                {
                    using var stream = imageFile.OpenReadStream();
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(imageFile.FileName, stream),
                        Folder = "products"
                    };
                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    finalImageUrl = uploadResult?.SecureUrl?.ToString();
                }
                else if (!string.IsNullOrEmpty(localImageUrl))
                {
                    finalImageUrl = localImageUrl;
                }

                if (!string.IsNullOrEmpty(finalImageUrl))
                {
                    // Clear existing images or just add new one? 
                    // Usually for simple CRUD, we replace.
                    var oldImages = _context.ProductImage.Where(i => i.productid == product.pid);
                    _context.ProductImage.RemoveRange(oldImages);

                    _context.ProductImage.Add(new ProductImage
                    {
                        productid = product.pid,
                        ImageUrl = finalImageUrl
                    });
                }

                // 3. Handle Size and Color
                if (!string.IsNullOrEmpty(selectedSize))
                {
                    var oldSizes = _context.ProductSize.Where(s => s.productid == product.pid);
                    _context.ProductSize.RemoveRange(oldSizes);
                    _context.ProductSize.Add(new ProductSize { productid = product.pid, psize = selectedSize });
                }

                if (!string.IsNullOrEmpty(selectedColor))
                {
                    var oldColors = _context.ProductColor.Where(c => c.productid == product.pid);
                    _context.ProductColor.RemoveRange(oldColors);
                    _context.ProductColor.Add(new ProductColor { productid = product.pid, pcolor = selectedColor });
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
            try
            {
                var product = await _context.Product.FindAsync(id);
                if (product == null) return Json(new { success = false, message = "Product not found" });

                _context.Product.Remove(product);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Product
                .Include(p => p.category)
                .Include(p => p.Image)
                .Include(p => p.colors)
                .Include(p => p.Size)
                .FirstOrDefaultAsync(m => m.pid == id);

            if (product == null)
            {
                return NotFound();
            }

            // Get related products from same category
            ViewBag.RelatedProducts = await _context.Product
                .Include(p => p.Image)
                .Where(p => p.Categoryid == product.Categoryid && p.pid != id)
                .Take(6)
                .ToListAsync();

            // Get available coupons for this product
            ViewBag.Coupons = await _context.Coupons
                .Where(c => (c.ProductId == id || c.ProductId == null) && c.IsActive && (c.ExpiryDate == null || c.ExpiryDate > DateTime.Now))
                .ToListAsync();

            return View(product);
        }
    }
}
