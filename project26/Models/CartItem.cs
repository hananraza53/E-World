using System.ComponentModel.DataAnnotations;

namespace project26.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;

        public User User { get; set; }
        public Product Product { get; set; }
    }
}
