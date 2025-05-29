using _10.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _10.Attributes;

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
}
