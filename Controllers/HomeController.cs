using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using _10.Models;
using _10.Attributes; // Add this line
using _10.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace _10.Controllers;

[SessionAuthorize] // Apply to all actions in this controller
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdString))
        {
            return View(); // Or redirect to login
        }

        var userId = int.Parse(userIdString);
        var user = await _context.Users.FindAsync(userId);

        if (user != null && user.Role == UserRole.User)
        {
            var sentPackages = await _context.Packages
                .Include(p => p.RecipientUser) // Eager load RecipientUser
                .Include(p => p.CurrentStatus)   // Eager load CurrentStatus
                .Where(p => p.SenderUserId == userId)
                .ToListAsync();
            var receivedPackages = await _context.Packages
                .Include(p => p.SenderUser)   // Eager load SenderUser
                .Include(p => p.CurrentStatus) // Eager load CurrentStatus
                .Where(p => p.RecipientUserId == userId)
                .ToListAsync();

            ViewData["SentPackages"] = sentPackages;
            ViewData["ReceivedPackages"] = receivedPackages;
        }

        return View();
    }

    public async Task<IActionResult> PackageDetails(int id)
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        var userRoleString = HttpContext.Session.GetString("UserRole");

        if (string.IsNullOrEmpty(userIdString) || string.IsNullOrEmpty(userRoleString))
        {
            return RedirectToAction("Login", "Auth"); // Or appropriate unauthorized access page
        }

        var userId = int.Parse(userIdString);

        var package = await _context.Packages
            .Include(p => p.SenderUser)
            .Include(p => p.RecipientUser)
            .Include(p => p.CurrentStatus)
            .Include(p => p.History)
                .ThenInclude(ph => ph.Status) // Changed StatusDefinition to Status
            .FirstOrDefaultAsync(p => p.PackageId == id); // Changed Id to PackageId

        if (package == null)
        {
            return NotFound();
        }

        bool isAuthorized = false;
        var userRole = Enum.Parse<UserRole>(userRoleString); // Assuming UserRole is an enum

        switch (userRole)
        {
            case UserRole.User:
                if (package.SenderUserId == userId || package.RecipientUserId == userId)
                {
                    isAuthorized = true;
                }
                break;
            case UserRole.Courier:
                // Assuming Courier assignment is checked elsewhere or not required for viewing by courier
                if (package.SenderUserId == userId ||
                    package.RecipientUserId == userId ||
                    package.History.Any(ph => ph.Package.AssignedCourierId == userId)) // Check if courier is assigned to the package
                {
                    isAuthorized = true;
                }
                break;
            case UserRole.Admin: // Admins can view any package
                isAuthorized = true;
                break;
        }

        if (!isAuthorized)
        {
            // You can redirect to an "Access Denied" page or back to Index with a message
            TempData["ErrorMessage"] = "You are not authorized to view these package details.";
            return RedirectToAction("Index");
        }

        return View(package);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
