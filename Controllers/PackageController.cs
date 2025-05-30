using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using System;
using System.Linq;
using System.Threading.Tasks;
using _10.Data; // Your DbContext
using _10.Models; // Your Models
using _10.Attributes; // For [SessionAuthorize]

namespace _10.Controllers
{
    [SessionAuthorize] // Apply session authorization to all actions in this controller
    public class PackageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PackageController> _logger;

        public PackageController(ApplicationDbContext context, ILogger<PackageController> logger)
        {
            _context = context;
            _logger = logger;
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
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Find or create recipient user
                    var recipientUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.RecipientEmail);
                    if (recipientUser == null)
                    {
                        // Create recipient with minimal required information
                        recipientUser = new User
                        {
                            Username = model.RecipientEmail,
                            Email = model.RecipientEmail,
                            Password = PasswordHelper.HashPassword("TempPassword123!"), // Temporary password
                            Role = UserRole.User,
                            FirstName = model.RecipientFirstName,
                            LastName = model.RecipientLastName,
                            CreatedAt = DateTime.UtcNow,
                            ApiKey = ApiKeyGenerator.GenerateApiKey()
                        };
                        _context.Users.Add(recipientUser);
                        await _context.SaveChangesAsync(); // Save to get UserId
                    }

                    // Find or create origin address
                    var originAddress = await _context.Addresses.FirstOrDefaultAsync(a =>
                        a.Street == model.OriginStreet && a.City == model.OriginCity &&
                        a.ZipCode == model.OriginZipCode && a.Country == model.OriginCountry);
                    if (originAddress == null)
                    {
                        originAddress = new Address
                        {
                            Street = model.OriginStreet,
                            City = model.OriginCity,
                            ZipCode = model.OriginZipCode,
                            Country = model.OriginCountry
                        };
                        _context.Addresses.Add(originAddress);
                    }

                    // Find or create destination address
                    var destinationAddress = await _context.Addresses.FirstOrDefaultAsync(a =>
                        a.Street == model.DestinationStreet && a.City == model.DestinationCity &&
                        a.ZipCode == model.DestinationZipCode && a.Country == model.DestinationCountry);
                    if (destinationAddress == null)
                    {
                        destinationAddress = new Address
                        {
                            Street = model.DestinationStreet,
                            City = model.DestinationCity,
                            ZipCode = model.DestinationZipCode,
                            Country = model.DestinationCountry
                        };
                        _context.Addresses.Add(destinationAddress);
                    }

                    // Save addresses to get their IDs
                    await _context.SaveChangesAsync();

                    // Get initial status
                    var initialStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.Name == "Sent");
                    if (initialStatus == null)
                    {
                        _logger.LogError("Initial status 'New Order' not found in database.");
                        ModelState.AddModelError("", "System configuration error: Initial package status not found. Please contact support.");
                        await transaction.RollbackAsync();
                        return View(model);
                    }

                    // Assign random courier
                    var assignedCourierId = await AssignRandomCourierAsync();

                    // Create package
                    var package = new Package
                    {
                        TrackingNumber = GenerateTrackingNumber(),
                        SenderUserId = senderUserId,
                        RecipientUserId = recipientUser.UserId,
                        PackageSize = model.PackageSize,
                        WeightInKg = model.WeightInKg,
                        Notes = model.Notes,
                        OriginAddressId = originAddress.AddressId,
                        DestinationAddressId = destinationAddress.AddressId,
                        SubmissionDate = DateTime.UtcNow,
                        StatusId = initialStatus.StatusId,
                        AssignedCourierId = assignedCourierId
                    };
                    _context.Packages.Add(package);
                    await _context.SaveChangesAsync(); // Save to get PackageId

                    // Create initial package history entry
                    var packageHistory = new PackageHistory
                    {
                        PackageId = package.PackageId,
                        StatusId = initialStatus.StatusId,
                        Timestamp = DateTime.UtcNow
                    };
                    _context.PackageHistories.Add(packageHistory);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Package '{package.TrackingNumber}' has been successfully submitted!";
                    return RedirectToAction("Details", new { id = package.PackageId });
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "DbUpdateException: Error saving new package.");
                    ModelState.AddModelError("", "A database error occurred while saving the package. Please ensure all details are correct and try again.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "General error in SendPackage POST action.");
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }

            return View(model);
        }

        private string GenerateTrackingNumber()
        {
            // Generate a more user-friendly tracking number
            var prefix = "TT"; // Track & Trace
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"{prefix}{timestamp}{random}";
        }

        private async Task<int?> AssignRandomCourierAsync()
        {
            try
            {
                // Get all active couriers from the database
                var couriers = await _context.Users
                    .Where(u => u.Role == UserRole.Courier)
                    .Select(u => u.UserId)
                    .ToListAsync();

                if (couriers == null || !couriers.Any())
                {
                    _logger.LogWarning("No couriers found in the database for package assignment.");
                    return null; // No couriers available
                }

                // Randomly select a courier
                var random = new Random();
                var randomIndex = random.Next(couriers.Count);
                var selectedCourierId = couriers[randomIndex];

                _logger.LogInformation($"Randomly assigned courier with ID {selectedCourierId} to package.");
                return selectedCourierId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while assigning random courier to package.");
                return null; // Return null if assignment fails
            }
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
                // SessionAuthorize should handle this, but good for explicit control flow
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdString);
            var userRole = Enum.Parse<UserRole>(userRoleString); //

            var package = await _context.Packages
                .Include(p => p.SenderUser).ThenInclude(su => su.Address) // Eager load sender's address
                .Include(p => p.RecipientUser).ThenInclude(ru => ru.Address) // Eager load recipient's address
                .Include(p => p.OriginAddress) // Eager load origin address for the package
                .Include(p => p.DestinationAddress) // Eager load destination address for the package
                .Include(p => p.CurrentStatus)
                .Include(p => p.History).ThenInclude(ph => ph.Status)
                .Include(p => p.AssignedCourier) // Eager load assigned courier if any
                .AsNoTracking() // Good for read-only scenarios
                .FirstOrDefaultAsync(p => p.PackageId == id);

            if (package == null)
            {
                return NotFound();
            }

            bool isAuthorized = userRole == UserRole.Admin ||
                                package.SenderUserId == userId ||
                                package.RecipientUserId == userId ||
                                (userRole == UserRole.Courier && package.AssignedCourierId == userId);

            if (userRole == UserRole.Courier && !isAuthorized)
            {
                // Special check for courier who might have been previously assigned
                // We can't check history since it doesn't track courier assignments directly
                // This is a limitation that should be addressed with proper audit logging
                // For now, we'll only allow currently assigned couriers
                isAuthorized = false;
            }


            if (!isAuthorized)
            {
                TempData["ErrorMessage"] = "You are not authorized to view these package details.";
                return RedirectToAction("Index", "Home"); // Redirect to a general page
            }

            return View(package);
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

            try
            {
                // Get packages where the current user is the recipient
                var packages = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.CurrentStatus)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Where(p => p.RecipientUserId == userId)
                    .OrderByDescending(p => p.SubmissionDate)
                    .AsNoTracking()
                    .ToListAsync();

                return View(packages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pickup packages for user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while loading your packages. Please try again.";
                return View(new List<Package>());
            }
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

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get the package and verify it belongs to the current user and has "In Delivery" status
                var package = await _context.Packages
                    .Include(p => p.CurrentStatus)
                    .FirstOrDefaultAsync(p => p.PackageId == id && p.RecipientUserId == userId);

                if (package == null)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Package not found or you are not authorized to pick it up.";
                    return RedirectToAction(nameof(PickUp));
                }

                if (package.CurrentStatus?.Name != "In Delivery")
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "This package is not available for pickup. Only packages with 'In Delivery' status can be picked up.";
                    return RedirectToAction(nameof(PickUp));
                }

                // Get the "Delivered" status
                var deliveredStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.Name == "Delivered");
                if (deliveredStatus == null)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError("Delivered status not found in database for package pickup");
                    TempData["ErrorMessage"] = "System configuration error: Delivered status not found. Please contact support.";
                    return RedirectToAction(nameof(PickUp));
                }

                // Update package status to "Delivered" and set delivery date
                package.StatusId = deliveredStatus.StatusId;
                package.DeliveryDate = DateTime.UtcNow;

                // Create package history entry for the pickup
                var packageHistory = new PackageHistory
                {
                    PackageId = package.PackageId,
                    StatusId = deliveredStatus.StatusId,
                    Timestamp = DateTime.UtcNow,
                    Longitude = package.Longitude, // Keep existing location if any
                    Latitude = package.Latitude
                };

                _context.PackageHistories.Add(packageHistory);
                _context.Packages.Update(package);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("User {UserId} picked up package {PackageId} ({TrackingNumber})", userId, package.PackageId, package.TrackingNumber);
                TempData["SuccessMessage"] = $"Package '{package.TrackingNumber}' has been successfully picked up and marked as delivered!";

                return RedirectToAction(nameof(PickUp));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error while picking up package {PackageId} by user {UserId}", id, userId);
                TempData["ErrorMessage"] = "A database error occurred while processing the pickup. Please try again.";
                return RedirectToAction(nameof(PickUp));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error picking up package {PackageId} by user {UserId}", id, userId);
                TempData["ErrorMessage"] = "An unexpected error occurred while picking up the package. Please try again.";
                return RedirectToAction(nameof(PickUp));
            }
        }
    }
}
