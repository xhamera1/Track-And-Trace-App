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
