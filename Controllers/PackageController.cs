using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using _10.Models;
using _10.Attributes;
using _10.Services;

namespace _10.Controllers
{
    [SessionAuthorize]
    public class PackageController : Controller
    {
        private readonly ILogger<PackageController> _logger;
        private readonly IPackageManagementService _packageManagementService;

        public PackageController(
            ILogger<PackageController> logger,
            IPackageManagementService packageManagementService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _packageManagementService = packageManagementService ?? throw new ArgumentNullException(nameof(packageManagementService));
        }

        // GET: Package/SendPackage
        [HttpGet]
        public IActionResult SendPackage()
        {
            return View(new SendPackageViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPackage(SendPackageViewModel model)
        {
            var senderUserIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(senderUserIdString))
            {
                TempData["ErrorMessage"] = "Session expired or user not logged in.";
                return RedirectToAction("Login", "Auth");
            }
            var senderUserId = int.Parse(senderUserIdString);

            if (ModelState.IsValid)
            {
                var result = await _packageManagementService.SendPackageAsync(model, senderUserId);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = result.Data!.Message;
                    return RedirectToAction("Details", new { id = result.Data.PackageId });
                }
                else
                {
                    ModelState.AddModelError("", result.ErrorMessage!);
                }
            }

            return View(model);
        }

        // GET: Package/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            var userRoleString = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) || string.IsNullOrEmpty(userRoleString))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdString);
            var userRole = Enum.Parse<UserRole>(userRoleString);

            var result = await _packageManagementService.GetPackageDetailsAsync(id.Value, userId, userRole);

            if (!result.IsSuccess)
            {
                if (result.ErrorCode == "NOT_FOUND")
                {
                    return NotFound();
                }

                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("Index", "Home");
            }

            return View(result.Data);
        }

        // GET: Package/PickUp
        [HttpGet]
        [SessionAuthorize("User", "Admin")]
        public async Task<IActionResult> PickUp()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Session expired or user not logged in.";
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdString);

            var result = await _packageManagementService.GetPackagesForPickupAsync(userId);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return View(new List<Package>());
            }

            return View(result.Data);
        }

        // POST: Package/PickUpPackage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("User", "Admin")]
        public async Task<IActionResult> PickUpPackage(int id)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Session expired or user not logged in.";
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdString);

            var result = await _packageManagementService.PickUpPackageAsync(id, userId);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = result.Data!.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction(nameof(PickUp));
        }
    }
}
