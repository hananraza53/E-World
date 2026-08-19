using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace project26.Models
{
    [Index(nameof(cname), IsUnique = true)]
    public class Category
    {
       
        [Key]
        public int cid { get; set; }
        public string cname { get; set; }

        public string? Imageurl { get; set; }


    }
}
