using System;
using System.ComponentModel.DataAnnotations;

namespace project26.Models
{
    public class Banner
    {
        [Key]
        public int bid { get; set; }

        [Required]
        
        public string btitle { get; set; } 

        [Required]
        public string bdescription { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

       
    }
}
