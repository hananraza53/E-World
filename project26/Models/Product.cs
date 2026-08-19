using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project26.Models
{
    public class Product
    {

        [Key]
       public int pid{ get; set; }

        public string pname { get; set; }

        public string pdes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? pprice { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        public decimal? pdis { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal pdisprice { get; set; }

        public int pqty { get; set; }

        public int Categoryid { get; set; }

        public Category category { get; set; }

        public string? producttype { get; set; }

        public DateTime createdat { get; set; }

        public DateTime updatedat { get; set; }

        public ICollection<ProductColor> colors { get; set; }

        public ICollection<ProductSize> Size { get; set; }

        public ICollection<ProductImage> Image { get; set; }



    }
}
