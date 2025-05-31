using _10.Data;
using _10.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

namespace _10.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext context, ILogger<AuthController> logger) 
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult Login()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                                         .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null && PasswordHelper.VerifyHashedPassword(user.Password, model.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),                    
                        new Claim(ClaimTypes.Role, user.Role.ToString()),             
                        new Claim(ClaimTypes.Email, user.Email ?? ""),
                        new Claim("FirstName", user.FirstName ?? "")
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, 
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) 
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("User {Username} logged in successfully at {Time}.", user.Username, DateTime.UtcNow);

                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("UserRole", user.Role.ToString());

                    return RedirectToAction("Index", "Home");
                }
                _logger.LogWarning("Invalid login attempt for username: {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username already taken.");
                    return View(model);
                }
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already registered.");
                    return View(model);
                }

                Address? addressToUse = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Street == model.Street &&
                                            a.City == model.City &&
                                            a.ZipCode == model.ZipCode &&
                                            a.Country == model.Country);

                if (addressToUse == null)
                {
                    addressToUse = new Address
                    {
                        Street = model.Street,
                        City = model.City,
                        ZipCode = model.ZipCode,
                        Country = model.Country
                    };
                    _context.Addresses.Add(addressToUse);
                }

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = PasswordHelper.HashPassword(model.Password),
                    ApiKey = ApiKeyGenerator.GenerateApiKey(),
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Birthday = model.Birthday,
                    Address = addressToUse, 
                    Role = model.IsCourier ? UserRole.Courier : UserRole.User,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {Username} registered successfully at {Time}.", user.Username, DateTime.UtcNow);

                TempData["SuccessMessage"] = "Registration successful! Please log in.";
                return RedirectToAction("Login", "Auth");
            }
            return View(model);
        }
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name ?? "Unknown user"; 

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            HttpContext.Session.Clear();
            _logger.LogInformation("User {Username} logged out at {Time}.", userName, DateTime.UtcNow);

            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {

            TempData["ErrorMessage"] = "You do not have permission to access the requested page.";
            return RedirectToAction("Index", "Home");
        }
    }
    public class LoginViewModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Username { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
