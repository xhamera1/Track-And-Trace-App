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
        private readonly ILogger<AuthController> _logger; // Dodano logger

        public AuthController(ApplicationDbContext context, ILogger<AuthController> logger) // Dodano ILogger
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Jeśli użytkownik jest już zalogowany, można go przekierować na stronę główną
            if (User.Identity != null && User.Identity.IsAuthenticated)
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
                    // Tworzenie oświadczeń (claims) dla uwierzytelnionego użytkownika
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // Kluczowy dla GetCurrentUserId()
                        new Claim(ClaimTypes.Name, user.Username),                     // Standardowa nazwa użytkownika
                        new Claim(ClaimTypes.Role, user.Role.ToString()),              // Rola użytkownika
                        // Możesz dodać inne potrzebne oświadczenia, np. email, imię
                        new Claim(ClaimTypes.Email, user.Email ?? ""),
                        new Claim("FirstName", user.FirstName ?? "")
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme); // Określenie schematu uwierzytelniania

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Ciasteczko będzie trwałe (przetrwa zamknięcie przeglądarki)
                                             // Możesz to uzależnić od np. checkboxa "Remember me"
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) // Czas wygaśnięcia ciasteczka
                    };

                    // Logowanie użytkownika - tworzy zaszyfrowane ciasteczko i wysyła je do przeglądarki
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("User {Username} logged in successfully at {Time}.", user.Username, DateTime.UtcNow);

                    // Ustawianie wartości w sesji (opcjonalne, jeśli używasz ClaimsPrincipal)
                    // Może być przydatne dla Twojego istniejącego atrybutu [SessionAuthorize] lub szybkiego dostępu
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
                    // SaveChangesAsync tutaj, aby uzyskać AddressId, jeśli adres jest nowy
                    // LUB można to zrobić w ramach jednej transakcji na końcu.
                    // Dla uproszczenia zakładamy, że EF Core poradzi sobie z przypisaniem FK.
                    // Jeśli nie, musisz tu zrobić SaveChangesAsync dla adresu.
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
                    Address = addressToUse, // Przypisanie obiektu adresu, EF Core zajmie się AddressId
                    Role = model.IsCourier ? UserRole.Courier : UserRole.User,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {Username} registered successfully at {Time}.", user.Username, DateTime.UtcNow);

                // Można rozważyć automatyczne zalogowanie użytkownika po rejestracji
                // lub przekierowanie na stronę logowania z komunikatem o sukcesie.
                TempData["SuccessMessage"] = "Registration successful! Please log in.";
                return RedirectToAction("Login", "Auth");
            }
            return View(model);
        }

        // Zmieniono na async Task<IActionResult>
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name ?? "Unknown user"; // Pobierz nazwę przed wylogowaniem

            // Wylogowanie użytkownika z systemu opartego na ciasteczkach
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            HttpContext.Session.Clear(); // Wyczyść również dane sesji
            _logger.LogInformation("User {Username} logged out at {Time}.", userName, DateTime.UtcNow);

            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            // Opcja 1: Wyświetl dedykowany widok (jak miałeś)
            // return View();

            // Opcja 2: Przekieruj na stronę główną z komunikatem (jak dyskutowaliśmy)
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
