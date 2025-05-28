using _10.Data; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using System.Threading.Tasks; 


// narazie testowo do sprawdzenia polaczneia z baza dnaych i na azure

namespace _10.Controllers
{
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
}