using Microsoft.EntityFrameworkCore;
using project26.Models;

namespace project26.ApplicationDb
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> User { get; set; }

        public DbSet<Banner> Banner { get; set; }

        public DbSet<Category> Category { get; set; }

        public DbSet<Product> Product { get; set; }

        public DbSet<ProductColor> ProductColor { get; set; }

        public DbSet<ProductSize> ProductSize { get; set; }

        public DbSet<ProductImage> ProductImage { get; set; }
        
        public DbSet<CartItem> CartItems { get; set; }
        
        public DbSet<WishlistItem> WishlistItems { get; set; }
        
        public DbSet<Coupon> Coupons { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }
    }
}
