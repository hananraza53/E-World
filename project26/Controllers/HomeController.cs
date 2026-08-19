using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using project26.Models;
using System.Diagnostics;
using project26.ApplicationDb;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace project26.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Auto-seed if database is empty
            if (!_context.Category.Any())
            {
                await SeedInitialData();
            }

            ViewBag.Categories = await _context.Category.ToListAsync();
            ViewBag.Banners = await _context.Banner.ToListAsync();
            
            var allProducts = await _context.Product
                .Include(p => p.Image)
                .Include(p => p.category)
                .OrderByDescending(p => p.pid)
                .ToListAsync();

            ViewBag.FeaturedProducts = allProducts;
            ViewBag.FlashSaleProducts = allProducts.Where(p => p.pdis > 0).Take(6).ToList();

            return View();
        }

        private async Task SeedInitialData()
        {
            var categories = new List<Category>
            {
                new Category { cname = "Mobile Phones" },
                new Category { cname = "Watches" },
                new Category { cname = "Skincare" },
                new Category { cname = "Electronics" },
                new Category { cname = "Fashion" }
            };

            _context.Category.AddRange(categories);
            await _context.SaveChangesAsync();

            var localImages = new string[] { "/images/mobile1.jfif", "/images/mobile2.png", "/images/watch1.webp", "/images/watch2.jfif", "/images/skin.webp", "/images/skin2.jpeg", "/images/skin3.webp", "/images/skin4.webp", "/images/skii-pink.jpg" };

            var products = new List<Product>
            {
                new Product { pname = "iPhone 15 Pro Max", pdes = "Latest flagship from Apple", pprice = 350000, pdis = 10, pdisprice = 315000, pqty = 10, Categoryid = categories[0].cid, createdat = DateTime.Now },
                new Product { pname = "Samsung Galaxy S24 Ultra", pdes = "AI-powered flagship", pprice = 320000, pdis = 15, pdisprice = 272000, pqty = 15, Categoryid = categories[0].cid, createdat = DateTime.Now },
                new Product { pname = "Apple Watch Series 9", pdes = "Modern smartwatch", pprice = 95000, pdis = 5, pdisprice = 90250, pqty = 20, Categoryid = categories[1].cid, createdat = DateTime.Now },
                new Product { pname = "Luxury Quartz Watch", pdes = "Elegant timepiece", pprice = 15000, pdis = 50, pdisprice = 7500, pqty = 50, Categoryid = categories[1].cid, createdat = DateTime.Now },
                new Product { pname = "SK-II Facial Treatment", pdes = "Premium skincare essence", pprice = 45000, pdis = 20, pdisprice = 36000, pqty = 30, Categoryid = categories[2].cid, createdat = DateTime.Now },
                new Product { pname = "Hydrating Serum B5", pdes = "Deep hydration for skin", pprice = 8500, pdis = 10, pdisprice = 7650, pqty = 100, Categoryid = categories[2].cid, createdat = DateTime.Now },
                new Product { pname = "Moisturizing Cream", pdes = "Daily face cream", pprice = 4500, pdis = 0, pdisprice = 4500, pqty = 200, Categoryid = categories[2].cid, createdat = DateTime.Now },
                new Product { pname = "Gaming Laptop", pdes = "High performance laptop", pprice = 250000, pdis = 5, pdisprice = 237500, pqty = 5, Categoryid = categories[3].cid, createdat = DateTime.Now }
            };

            _context.Product.AddRange(products);
            await _context.SaveChangesAsync();

            foreach (var p in products)
            {
                _context.ProductImage.Add(new ProductImage
                {
                    productid = p.pid,
                    ImageUrl = localImages[p.pid % localImages.Length]
                });
            }

            await _context.SaveChangesAsync();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Cart()
        {
            return View();
        }

        public IActionResult Wishlist()
        {
            return View();
        }

        // Temporary action to update database with local images
        public async Task<IActionResult> SeedImages()
        {
            try
            {
                var localImages = new string[] { 
                    "/images/mobile1.jfif", "/images/mobile2.png", 
                    "/images/skii-pink.jpg", "/images/skin.webp", 
                    "/images/skin2.jpeg", "/images/skin3.webp", 
                    "/images/skin4.webp", "/images/watch1.webp", 
                    "/images/watch2.jfif" 
                };

                // 1. Update Banners
                var banners = await _context.Banner.ToListAsync();
                for (int i = 0; i < banners.Count; i++)
                {
                    banners[i].ImageUrl = localImages[i % localImages.Length];
                }

                // 2. Update Categories
                var categories = await _context.Category.ToListAsync();
                for (int i = 0; i < categories.Count; i++)
                {
                    categories[i].Imageurl = localImages[(i + 2) % localImages.Length];
                }

                // 3. Update Product Images
                var productImages = await _context.ProductImage.ToListAsync();
                foreach (var img in productImages)
                {
                    img.ImageUrl = localImages[img.productid % localImages.Length];
                }

                // 4. Also update products that might not have images
                var productsWithoutImages = await _context.Product
                    .Include(p => p.Image)
                    .Where(p => !p.Image.Any())
                    .ToListAsync();
                
                foreach (var p in productsWithoutImages)
                {
                    _context.ProductImage.Add(new ProductImage {
                        productid = p.pid,
                        ImageUrl = localImages[p.pid % localImages.Length]
                    });
                }

                await _context.SaveChangesAsync();
                return Content("Success: Database images updated with local paths. Please refresh the homepage.");
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        public async Task<IActionResult> Checkout()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "User");
            
            var userId = int.Parse(userIdStr);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .ThenInclude(p => p.Image)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
