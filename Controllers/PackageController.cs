using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using _10.Data; 
using _10.Models; 
using _10.Attributes;
using _10.Services; 

namespace _10.Controllers
{
    [SessionAuthorize] 
    public class PackageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PackageController> _logger;
        private readonly IPackageAuthorizationService _authorizationService;
        private readonly IPackageLocationService _packageLocationService;

        public PackageController(
            ApplicationDbContext context,
            ILogger<PackageController> logger,
            IPackageAuthorizationService authorizationService,
            IPackageLocationService packageLocationService)
        {
            _context = context;
            _logger = logger;
            _authorizationService = authorizationService;
            _packageLocationService = packageLocationService;
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
                    var recipientUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.RecipientEmail);
                    if (recipientUser == null)
                    {
                        recipientUser = new User
                        {
                            Username = model.RecipientEmail,
                            Email = model.RecipientEmail,
                            Password = PasswordHelper.HashPassword("TempPassword123!"),
                            Role = UserRole.User,
                            FirstName = model.RecipientFirstName,
                            LastName = model.RecipientLastName,
                            CreatedAt = DateTime.UtcNow,
                            ApiKey = ApiKeyGenerator.GenerateApiKey()
                        };
                        _context.Users.Add(recipientUser);
                        await _context.SaveChangesAsync(); 
                    }

                    var originAddress = await FindOrCreateAddressAsync(
                        model.OriginStreet,
                        model.OriginCity,
                        model.OriginZipCode,
                        model.OriginCountry);

                    var destinationAddress = await FindOrCreateAddressAsync(
                        model.DestinationStreet,
                        model.DestinationCity,
                        model.DestinationZipCode,
                        model.DestinationCountry);

                    var initialStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.Name == "Sent");
                    if (initialStatus == null)
                    {
                        _logger.LogError("Initial status 'New Order' not found in database.");
                        ModelState.AddModelError("", "System configuration error: Initial package status not found. Please contact support.");
                        await transaction.RollbackAsync();
                        return View(model);
                    }

                    var assignedCourierId = await AssignRandomCourierAsync();

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
                    await _context.SaveChangesAsync(); 

                    package.OriginAddress = originAddress;
                    package.DestinationAddress = destinationAddress;

                    try
                    {
                        var coordinatesPopulated = await _packageLocationService.PopulatePackageCoordinatesAsync(package);
                        if (coordinatesPopulated)
                        {
                            _context.Packages.Update(package);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Successfully populated coordinates for package {PackageId} from origin address", package.PackageId);
                        }
                        else
                        {
                            _logger.LogWarning("Could not populate coordinates for package {PackageId} - geocoding failed", package.PackageId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during geocoding for package {PackageId}. Package will be created without coordinates.", package.PackageId);
                    }

                    var packageHistory = new PackageHistory
                    {
                        PackageId = package.PackageId,
                        StatusId = initialStatus.StatusId,
                        Timestamp = DateTime.UtcNow,
                        Longitude = package.Longitude,
                        Latitude = package.Latitude
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
            var prefix = "TT"; // Track & Trace
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"{prefix}{timestamp}{random}";
        }

        private async Task<int?> AssignRandomCourierAsync()
        {
            try
            {
                var couriers = await _context.Users
                    .Where(u => u.Role == UserRole.Courier)
                    .Select(u => u.UserId)
                    .ToListAsync();

                if (couriers == null || !couriers.Any())
                {
                    _logger.LogWarning("No couriers found in the database for package assignment.");
                    return null; 
                }

                var random = new Random();
                var randomIndex = random.Next(couriers.Count);
                var selectedCourierId = couriers[randomIndex];

                _logger.LogInformation($"Randomly assigned courier with ID {selectedCourierId} to package.");
                return selectedCourierId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while assigning random courier to package.");
                return null; 
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
                return RedirectToAction("Login", "Auth");
            }

            var userId = int.Parse(userIdString);
            var userRole = Enum.Parse<UserRole>(userRoleString);

            var package = await _context.Packages
                .Include(p => p.SenderUser).ThenInclude(su => su.Address) 
                .Include(p => p.RecipientUser).ThenInclude(ru => ru.Address)
                .Include(p => p.OriginAddress)
                .Include(p => p.DestinationAddress)
                .Include(p => p.CurrentStatus)
                .Include(p => p.History).ThenInclude(ph => ph.Status)
                .Include(p => p.AssignedCourier)
                .AsNoTracking() 
                .FirstOrDefaultAsync(p => p.PackageId == id);

            if (package == null)
            {
                return NotFound();
            }

            var authResult = _authorizationService.GetAuthorizationResult(package, userId, userRole);
            if (!authResult.IsAuthorized)
            {
                _logger.LogWarning("User {UserId} with role {UserRole} denied access to package {PackageId}: {Reason}",
                    userId, userRole, package.PackageId, authResult.Reason);

                TempData["ErrorMessage"] = $"Access denied: {authResult.Reason}";
                return RedirectToAction("Index", "Home");
            }

            _logger.LogInformation("User {UserId} ({AccessType}) accessing package {PackageId} details",
                userId, authResult.AccessType, package.PackageId);

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

                package.StatusId = deliveredStatus.StatusId;
                package.DeliveryDate = DateTime.UtcNow;

                decimal? deliveryLatitude = package.Latitude;
                decimal? deliveryLongitude = package.Longitude;

                try
                {
                    if (package.DestinationAddress == null)
                    {
                        var destinationAddress = await _context.Addresses
                            .FirstOrDefaultAsync(a => a.AddressId == package.DestinationAddressId);

                        if (destinationAddress != null)
                        {
                            package.DestinationAddress = destinationAddress;
                        }
                    }

                    if (package.DestinationAddress != null)
                    {
                        var geocodingResult = await _packageLocationService.GeocodeAddressAsync(package.DestinationAddress);
                        if (geocodingResult.IsSuccess && geocodingResult.Latitude.HasValue && geocodingResult.Longitude.HasValue)
                        {
                            deliveryLatitude = geocodingResult.Latitude.Value;
                            deliveryLongitude = geocodingResult.Longitude.Value;

                            package.Latitude = deliveryLatitude;
                            package.Longitude = deliveryLongitude;

                            _logger.LogInformation("Updated package {PackageId} with delivery coordinates: {Lat}, {Lon}",
                                package.PackageId, deliveryLatitude, deliveryLongitude);
                        }
                        else
                        {
                            _logger.LogWarning("Could not geocode destination address for package {PackageId} during delivery: {Error}",
                                package.PackageId, geocodingResult.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during destination geocoding for package {PackageId} delivery", package.PackageId);
                }

                var packageHistory = new PackageHistory
                {
                    PackageId = package.PackageId,
                    StatusId = deliveredStatus.StatusId,
                    Timestamp = DateTime.UtcNow,
                    Longitude = deliveryLongitude,
                    Latitude = deliveryLatitude
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

        private async Task<Address> FindOrCreateAddressAsync(string street, string city, string zipCode, string country)
        {
            // Normalize the input to handle potential whitespace issues and ensure non-null values
            street = street?.Trim() ?? string.Empty;
            city = city?.Trim() ?? string.Empty;
            zipCode = zipCode?.Trim() ?? string.Empty;
            country = country?.Trim() ?? string.Empty;

            // Validate required fields
            if (string.IsNullOrEmpty(street) || string.IsNullOrEmpty(city) ||
                string.IsNullOrEmpty(zipCode) || string.IsNullOrEmpty(country))
            {
                throw new ArgumentException("All address fields (street, city, zip code, country) are required and cannot be empty.");
            }

            var existingAddress = await _context.Addresses
                .FirstOrDefaultAsync(a =>
                    a.Street == street &&
                    a.City == city &&
                    a.ZipCode == zipCode &&
                    a.Country == country);

            if (existingAddress != null)
            {
                return existingAddress;
            }

            var newAddress = new Address
            {
                Street = street,
                City = city,
                ZipCode = zipCode,
                Country = country
            };

            try
            {
                _context.Addresses.Add(newAddress);
                await _context.SaveChangesAsync();
                return newAddress;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("Duplicate entry") == true ||
                                             ex.InnerException?.Message?.Contains("uq_address") == true)
            {
                _context.Entry(newAddress).State = EntityState.Detached;

                var concurrentlyCreatedAddress = await _context.Addresses
                    .FirstOrDefaultAsync(a =>
                        a.Street == street &&
                        a.City == city &&
                        a.ZipCode == zipCode &&
                        a.Country == country);

                if (concurrentlyCreatedAddress != null)
                {
                    return concurrentlyCreatedAddress;
                }

                _logger.LogError(ex, "Failed to handle duplicate address creation for: {Street}, {City}, {ZipCode}, {Country}",
                    street, city, zipCode, country);

                throw;
            }
        }
    }
}
