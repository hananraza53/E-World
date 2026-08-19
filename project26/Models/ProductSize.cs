using System.ComponentModel.DataAnnotations;

namespace project26.Models
{
    public class ProductSize
    {

        [Key]
        public int Id { get; set; }

        public string psize { get; set; }


        public int productid { get; set; }


        public Product Product { get; set; }
    }
}
