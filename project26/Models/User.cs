using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace project26.Models
{
    public class User
    {
        internal bool isVerified;

        [Key]
            public int uid { get; set; }

            [Required(ErrorMessage = "User Name is required")]
            [StringLength(50)]
            public string uname { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Enter valid email")]
            public string uemail { get; set; }

            [Required(ErrorMessage = "Phone number is required")]
            [Phone(ErrorMessage = "Enter valid phone number")]
            public string uphone { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
            [DataType(DataType.Password)]
            public string upassword { get; set; }

            [Required(ErrorMessage = "Confirm Password is required")]
            [Compare("upassword", ErrorMessage = "Password and Confirm Password must match")]
            [DataType(DataType.Password)]
            [NotMapped]   // database me save nahi hoga
            public string ucpass { get; set; }

            public string? Image { get; set; }

            public string? urole { get; set; }

            public int? verifycode { get; set; }

            public DateTime? verifycodetime { get; set; }

        public bool? verifystatus { get; set; } = false;

            public int? forgotpasscode { get; set; }

            public DateTime? forgotpasstime { get; set; }
        
            public User()
            {
                urole = "user";
            }
        }
    }
    

