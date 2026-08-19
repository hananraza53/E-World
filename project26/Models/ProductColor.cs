using System.ComponentModel.DataAnnotations;

namespace project26.Models
{
    public class ProductColor
    {
        [Key]
        public int Id { get; set; }

        public string pcolor { get; set; }


        public int productid { get; set; }


        public Product Product { get; set; }


    }
}
