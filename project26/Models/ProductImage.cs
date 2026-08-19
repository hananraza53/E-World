using System.ComponentModel.DataAnnotations;

namespace project26.Models
{
    public class ProductImage
    {

        [Key]
        public int Id { get; set; }

        public string ImageUrl { get; set; }


        public int productid { get; set; }


        public Product Product { get; set; }
    }
}
