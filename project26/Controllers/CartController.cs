using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project26.ApplicationDb;
using project26.Models;
using System.Security.Claims;

namespace project26.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Added to cart" });
        }

        [HttpPost]
        public async Task<IActionResult> AddToWishlist(int productId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var existingItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingItem == null)
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    UserId = userId,
                    ProductId = productId
                });
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Added to wishlist" });
            }

            return Json(new { success = false, message = "Already in wishlist" });
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var items = await _context.CartItems
                .Include(c => c.Product)
                .ThenInclude(p => p.Image)
                .Where(c => c.UserId == userId)
                .ToListAsync();
            return View(items);
        }

        public async Task<IActionResult> Wishlist()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var items = await _context.WishlistItems
                .Include(w => w.Product)
                .ThenInclude(p => p.Image)
                .Where(w => w.UserId == userId)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromWishlist(int productId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var item = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetCartItemsJson()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Json(new { items = new List<object>() });

            var userId = int.Parse(userIdStr);
            var items = await _context.CartItems
                .Include(c => c.Product)
                .ThenInclude(p => p.Image)
                .Where(c => c.UserId == userId)
                .Select(c => new {
                    c.ProductId,
                    pname = c.Product.pname,
                    pprice = c.Product.pprice,
                    pdis = c.Product.pdis,
                    pdisprice = c.Product.pdisprice,
                    imageUrl = c.Product.Image.FirstOrDefault() != null ? c.Product.Image.FirstOrDefault().ImageUrl : "/images/mobile1.jfif",
                    quantity = c.Quantity
                })
                .ToListAsync();

            return Json(new { items });
        }
        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            if (string.IsNullOrEmpty(code)) return Json(new { success = false, message = "Please enter a coupon code." });

            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code && c.IsActive);

            if (coupon == null) return Json(new { success = false, message = "Invalid coupon code." });

            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.Now)
                return Json(new { success = false, message = "This coupon has expired." });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            decimal subtotal = cartItems.Sum(i => (i.Product.pdis > 0 ? i.Product.pdisprice : i.Product.pprice ?? 0) * i.Quantity);

            if (coupon.MinimumAmount.HasValue && subtotal < coupon.MinimumAmount.Value)
                return Json(new { success = false, message = $"Minimum order amount for this coupon is Rs. {coupon.MinimumAmount.Value}." });

            decimal discount = 0;
            // Product specific check
            if (coupon.ProductId.HasValue)
            {
                var productInCart = cartItems.FirstOrDefault(i => i.ProductId == coupon.ProductId.Value);
                if (productInCart == null)
                {
                    var product = await _context.Product.FindAsync(coupon.ProductId.Value);
                    return Json(new { success = false, message = $"This coupon is only valid for the product: {product?.pname}." });
                }
                
                // If it's a product-specific coupon, maybe we only discount that product?
                // Let's stick to total discount for now but only if product is present, 
                // or we can calculate discount based on that product's subtotal if it's a percentage.
                if (coupon.Type == "Percentage")
                {
                    decimal productSubtotal = (productInCart.Product.pdis > 0 ? productInCart.Product.pdisprice : productInCart.Product.pprice ?? 0) * productInCart.Quantity;
                    discount = productSubtotal * (coupon.DiscountValue / 100);
                }
                else
                {
                    discount = coupon.DiscountValue;
                }
            }
            else
            {
                if (coupon.Type == "Percentage")
                {
                    discount = subtotal * (coupon.DiscountValue / 100);
                }
                else
                {
                    discount = coupon.DiscountValue;
                }
            }

            return Json(new { 
                success = true, 
                message = "Coupon applied successfully!", 
                discountValue = discount,
                code = coupon.Code,
                type = coupon.Type
            });
        }
    }
}
