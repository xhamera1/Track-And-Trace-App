using _10.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _10.Attributes;
using System.Threading.Tasks; // Added for Task<IActionResult>
using Microsoft.AspNetCore.Http; // Added for HttpContext.Session
using _10.Models; // Added for AccountViewModel
using System;

namespace _10.Controllers;

[SessionAuthorize]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Akcja GET: /Users lub /Users/Index
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users.AsNoTracking().ToListAsync();

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Account()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Auth"); // Redirect to login if no user is logged in
        }

        var user = await _context.Users
                                 .Include(u => u.Address) // Include address details
                                 .FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));

        if (user == null)
        {
            return NotFound(); // User not found
        }

        var viewModel = new AccountViewModel
        {
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Birthday = user.Birthday ?? DateTime.MinValue, // Handle possible null DateTime
            Role = user.Role.ToString(),
            Street = user.Address?.Street ?? string.Empty,
            City = user.Address?.City ?? string.Empty,
            ZipCode = user.Address?.ZipCode ?? string.Empty,
            Country = user.Address?.Country ?? string.Empty
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> EditAccount()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var user = await _context.Users
                                 .Include(u => u.Address)
                                 .FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));

        if (user == null)
        {
            return NotFound();
        }

        var viewModel = new EditAccountViewModel
        {
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Birthday = user.Birthday ?? DateTime.MinValue,
            Street = user.Address?.Street ?? string.Empty,
            City = user.Address?.City ?? string.Empty,
            ZipCode = user.Address?.ZipCode ?? string.Empty,
            Country = user.Address?.Country ?? string.Empty
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAccount(EditAccountViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var user = await _context.Users
                                 .Include(u => u.Address)
                                 .FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));

        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Birthday = model.Birthday;

        // Check if address already exists or create a new one
        var existingAddress = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Street == model.Street &&
                                    a.City == model.City &&
                                    a.ZipCode == model.ZipCode &&
                                    a.Country == model.Country);

        if (existingAddress != null)
        {
            user.AddressId = existingAddress.AddressId;
            user.Address = existingAddress;
        }
        else
        {
            var newAddress = new Address
            {
                Street = model.Street,
                City = model.City,
                ZipCode = model.ZipCode,
                Country = model.Country
            };
            _context.Addresses.Add(newAddress);
            await _context.SaveChangesAsync(); // Save new address to get AddressId
            user.AddressId = newAddress.AddressId;
            user.Address = newAddress;
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return RedirectToAction("Account");
    }
}
