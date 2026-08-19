using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project26.ApplicationDb;
using project26.Models;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

namespace project26.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private Cloudinary _cloudinary;

        public UserController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            // ✅ Cloudinary setup using configuration
            var account = new Account(
                _configuration["CloudinarySettings:CloudName"],
                _configuration["CloudinarySettings:ApiKey"],
                _configuration["CloudinarySettings:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
        }



        private async Task SendVerificationEmail(User user)
        {
            try
            {
                string smtpHost = _configuration["SMTP:Host"];
                int smtpPort = int.Parse(_configuration["SMTP:Port"]);
                string smtpUser = _configuration["SMTP:User"];
                string smtpPass = _configuration["SMTP:Pass"];

                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.UseDefaultCredentials = false; // Yeh line upar honi chahiye
                    smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPass);
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpUser, "MyApp Team"),
                        Subject = "Your Email Verification Code",
                        Body = $"Hello {user.uname},<br/>Your verification code is: <strong>{user.verifycode}</strong>",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(user.uemail);

                    // Timeout barha dein taake agar network slow ho toh crash na ho
                    smtpClient.Timeout = 20000;

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception($"SMTP Error: {ex.Message} -> Inner: {ex.InnerException?.Message}");
            }
        }

        private async Task SendForgotPasswordEmail(User user)
        {
            try
            {
                string smtpHost = _configuration["SMTP:Host"];
                int smtpPort = int.Parse(_configuration["SMTP:Port"]);
                string smtpUser = _configuration["SMTP:User"];
                string smtpPass = _configuration["SMTP:Pass"];

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpUser, "MyApp Team"),
                    Subject = "Password Reset Code",
                    Body = $@"
         Hello {user.uemail},<br/><br/>
         Your password reset code is: 
         <strong>{user.forgotpasscode}</strong><br/><br/>
         Please do not share this code with anyone.<br/><br/>
         Regards,<br/>
         MyApp Team
     ",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(user.uemail);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
            }
        }

      
        public IActionResult Login()
        {

            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");

                return RedirectToAction("Index", "Home");
            }
            return View();
        }


     

        [HttpPost]
        public async Task<IActionResult>  Login(string uemail, string upassword)
        {
            // Step 1: Required field validation
            if (string.IsNullOrEmpty(uemail))
                ModelState.AddModelError("uemail", "Email is required");

            if (string.IsNullOrEmpty(upassword))
                ModelState.AddModelError("upassword", "Password is required");

          

            // Step 2: Check if user exists
            var user = _context.User.FirstOrDefault(u => u.uemail == uemail && u.verifystatus == true);

            if (user == null)
            {
                ModelState.AddModelError("uemail", "User does not exist");
                return View();
            }

            // Step 3: Verify password using BCrypt
            bool isValid = BCrypt.Net.BCrypt.Verify(upassword, user.upassword);

            if (!isValid)
            {
                ModelState.AddModelError("upassword", "Invalid password");
                return View();
            }
            var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.uid.ToString()), 
                        new Claim(ClaimTypes.Name, user.uname),                    
                        new Claim(ClaimTypes.Email, user.uemail),                  
                        new Claim(ClaimTypes.Role, user.urole),
                        new Claim("ProfileImage", user.Image ?? "")                     
                    };

            var identity = new ClaimsIdentity(
claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
     CookieAuthenticationDefaults.AuthenticationScheme,
     principal
 );

            if (user.urole == "Admin")
            {
                var rng = new Random();
                user.verifycode = rng.Next(100000, 999999);
                user.verifycodetime = DateTime.Now.AddMinutes(2);

                // 3. Save user in database
                _context.SaveChanges();
                

                await SendVerificationEmail(user);

                return RedirectToAction("Verify");
            }

        
         

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {

            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");

                return RedirectToAction("Index", "Home");
            }
            return View();
        }


        public IActionResult ForgotPassword()
        {

            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");

                return RedirectToAction("Index", "Home");
            }
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string uemail)
        {
            var user = _context.User.FirstOrDefault(u => u.uemail == uemail && u.verifystatus == true);

            if (user == null)
            {
                ModelState.AddModelError("uemail", "User does not exist");
                return View();
            }

            var rng = new Random();
            user.forgotpasscode = rng.Next(100000, 999999);
            user.forgotpasstime = DateTime.Now.AddMinutes(2);

            _context.SaveChanges();

            await SendForgotPasswordEmail(user);


            return RedirectToAction("ResetPassword");
        }

        // ✅ REGISTER POST
        [HttpPost]
        public async Task<IActionResult> Register(User user, IFormFile image)
        {

          
            try
            {



                if (!ModelState.IsValid)
                {
                    return View(user);
                }

                // Store clear password for hashing later to avoid double-hashing if re-rendering view
                string clearPassword = user.upassword;



                string imageUrl = null;

                // 1. Upload image to Cloudinary
                if (image != null && image.Length > 0)
                {
                    using var stream = image.OpenReadStream();

                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(image.FileName, stream),
                        Folder = "user_profiles"
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    imageUrl = uploadResult?.SecureUrl?.ToString();
                }

                // 2. Save image URL and Hash password in DB
                user.Image = imageUrl;
                user.upassword = BCrypt.Net.BCrypt.HashPassword(clearPassword);

                var rng = new Random();
                user.verifycode = rng.Next(100000, 999999);
                user.verifycodetime = DateTime.Now.AddMinutes(2);

                // 3. Save user in database
                _context.User.Add(user);
                await _context.SaveChangesAsync();

                await SendVerificationEmail(user);

                return RedirectToAction("Verify");
            }
            catch (Exception err)
            {
                Console.WriteLine("FULL ERROR: " + err.ToString());
                ViewBag.Error = err.Message; // show real error
                return View(user);
            }
        }

        public IActionResult Verify()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Verify(int verifycode)
        {
            if (verifycode < 100000 || verifycode > 999999)
            {
                ViewBag.Error = "Code must be 6 digits";
                return View();
            }

            var user = await _context.User.FirstOrDefaultAsync(u => u.verifycode == verifycode);


            if (user == null)
            {
                ViewBag.Error = "invalid code";
                return View();
            }

            user.verifycode = null;
            user.verifystatus = true;

            await _context.SaveChangesAsync();


            return RedirectToAction("Login");
        }

        public IActionResult ResetPassword()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");

                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(int code, string Password, string ConfirmPassword)
        {
            if (code < 100000 || code > 999999)
            {
                ViewBag.Error = "Code must be 6 digits";
                return View();
            }

            var user = await _context.User
                .FirstOrDefaultAsync(u => u.forgotpasscode == code);

            if (user == null)
            {
                ViewBag.Error = "Invalid verification code  or expired forget again ";

                user.forgotpasscode = null;

                await _context.SaveChangesAsync();
                return View();
            }

            if (Password != ConfirmPassword)
            {
                ViewBag.Error = "Password and Confirm Password do not match";
                return View();
            }

            if (!string.IsNullOrEmpty(Password))
            {

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);
                user.upassword = hashedPassword;


                user.forgotpasscode = null;



                await _context.SaveChangesAsync();

                return RedirectToAction("Login");
            }

            return View();
        }


        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("login");

            var user = await _context.User.FirstOrDefaultAsync(u => u.uid == int.Parse(userId));
            return View(user);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
