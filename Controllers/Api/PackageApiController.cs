/// <summary>
/// Comprehensive REST API Controller for Package Management
///
/// This controller provides complete CRUD operations for packages in the track and trace system.
/// All endpoints require admin-level authentication using X-API-Key header.
///
/// Available Endpoints:
/// - GET /api/package - Retrieve all packages (Admin only)
/// - GET /api/package/{id} - Retrieve specific package by ID (Admin only)
/// - POST /api/package - Create new package (Admin only)
/// - PUT /api/package/{id} - Update existing package (Admin only)
/// - DELETE /api/package/{id} - Delete package (Admin only)
///
/// Authentication: All endpoints use the ApiAdminAuthorize attribute for secure authentication
/// Authorization: Admin role required for all operations
/// Database: Integrates with MySQL database through Entity Framework Core
/// Logging: Comprehensive logging for all operations and error handling
/// Transactions: Database transactions ensure data consistency
/// Error Handling: Robust error handling with appropriate HTTP status codes
///
/// Author: Generated for Track and Trace System
/// Version: 1.0
/// </summary>

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _10.Data;
using _10.Models;
using _10.Models.Api;
using _10.Services;
using _10.Attributes;
using System.ComponentModel.DataAnnotations;

namespace _10.Controllers.Api
{
    /// <summary>
    /// REST API controller for managing packages in the track and trace system.
    /// Provides CRUD operations for packages with admin-only authentication.
    /// All endpoints require valid username and API token authentication.
    /// </summary>
    [ApiController]
    [Route("api/package")]
    public class PackageApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PackageApiController> _logger;
        private readonly IPackageAuthorizationService _authorizationService;

        public PackageApiController(
            ApplicationDbContext context,
            ILogger<PackageApiController> logger,
            IPackageAuthorizationService authorizationService)
        {
            _context = context;
            _logger = logger;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Get all packages (Admin only)
        ///
        /// Authentication: X-API-Key header required
        /// </summary>
        /// <returns>List of all packages</returns>
        [HttpGet]
        [ApiAdminAuthorize]
        public async Task<ActionResult<IEnumerable<PackageDto>>> GetAllPackages()
        {
            // Get the authenticated user from HttpContext
            var authenticatedUser = HttpContext.Items["ApiUser"] as User;

            if (authenticatedUser == null)
            {
                _logger.LogError("Authenticated user not found in HttpContext after authentication");
                return StatusCode(500, new { message = "Authentication error - user not found in context." });
            }

            try
            {
                var packages = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.AssignedCourier)

                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .AsNoTracking()
                    .ToListAsync();

                var packageDtos = packages.Select(MapToDto);

                _logger.LogInformation("Admin user {Username} retrieved {Count} packages", authenticatedUser.Username, packages.Count);
                return Ok(packageDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all packages for admin user {Username}", authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving packages." });
            }
        }

        /// <summary>
        /// Get a specific package by ID (Admin only)
        ///
        /// Authentication: X-API-Key header required
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <returns>Package details</returns>
        [HttpGet("{id}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageDto>> GetPackage(int id)
        {
            // Get the authenticated user from HttpContext
            var authenticatedUser = HttpContext.Items["ApiUser"] as User;

            if (authenticatedUser == null)
            {
                _logger.LogError("Authenticated user not found in HttpContext after authentication");
                return StatusCode(500, new { message = "Authentication error - user not found in context." });
            }

            try
            {
                var package = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.AssignedCourier)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == id);

                if (package == null)
                {
                    return NotFound(new { message = $"Package with ID {id} not found." });
                }

                var packageDto = MapToDto(package);

                _logger.LogInformation("Admin user {Username} retrieved package {PackageId}", authenticatedUser.Username, id);
                return Ok(packageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving package {PackageId} for admin user {Username}", id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving the package." });
            }
        }

        /// <summary>
        /// Create a new package (Admin only)
        /// </summary>
        /// <param name="request">Package creation request</param>
        /// <returns>Created package</returns>
        [HttpPost]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageDto>> CreatePackage([FromBody] CreatePackageRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the authenticated user from HttpContext
            var authenticatedUser = HttpContext.Items["ApiUser"] as User;

            if (authenticatedUser == null)
            {
                _logger.LogError("Authenticated user not found in HttpContext after authentication");
                return StatusCode(500, new { message = "Authentication error - user not found in context." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate sender exists
                var sender = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.SenderUserId);
                if (sender == null)
                {
                    return BadRequest(new { message = $"Sender with ID {request.SenderUserId} not found." });
                }

                // Validate recipient exists
                var recipient = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.RecipientUserId);
                if (recipient == null)
                {
                    return BadRequest(new { message = $"Recipient with ID {request.RecipientUserId} not found." });
                }

                // Validate assigned courier if provided
                User? assignedCourier = null;
                if (request.AssignedCourierId.HasValue)
                {
                    assignedCourier = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.AssignedCourierId.Value && u.Role == UserRole.Courier);
                    if (assignedCourier == null)
                    {
                        return BadRequest(new { message = $"Courier with ID {request.AssignedCourierId} not found or is not a courier." });
                    }
                }

                // Find or create addresses
                var originAddress = await FindOrCreateAddressAsync(request.OriginAddress);
                var destinationAddress = await FindOrCreateAddressAsync(request.DestinationAddress);

                // Get initial status
                var initialStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.Name == "Sent");
                if (initialStatus == null)
                {
                    return BadRequest(new { message = "Initial package status 'Sent' not found in system." });
                }

                // Create package
                var package = new Package
                {
                    TrackingNumber = GenerateTrackingNumber(),
                    SenderUserId = request.SenderUserId,
                    RecipientUserId = request.RecipientUserId,
                    AssignedCourierId = request.AssignedCourierId,
                    PackageSize = request.PackageSize,
                    WeightInKg = request.WeightInKg,
                    Notes = request.Notes,
                    OriginAddressId = originAddress.AddressId,
                    DestinationAddressId = destinationAddress.AddressId,
                    SubmissionDate = DateTime.UtcNow,
                    StatusId = initialStatus.StatusId,
                    Longitude = request.Longitude,
                    Latitude = request.Latitude
                };

                _context.Packages.Add(package);
                await _context.SaveChangesAsync();

                // Create initial package history entry
                var packageHistory = new PackageHistory
                {
                    PackageId = package.PackageId,
                    StatusId = initialStatus.StatusId,
                    Timestamp = DateTime.UtcNow,
                    Longitude = request.Longitude,
                    Latitude = request.Latitude
                };
                _context.PackageHistories.Add(packageHistory);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Load the created package with all includes for DTO mapping
                var createdPackage = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.AssignedCourier)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .FirstAsync(p => p.PackageId == package.PackageId);

                var packageDto = MapToDto(createdPackage);

                _logger.LogInformation("Admin user {Username} created package {PackageId} with tracking number {TrackingNumber}",
                    authenticatedUser.Username, package.PackageId, package.TrackingNumber);

                return CreatedAtAction(nameof(GetPackage),
                    new { id = package.PackageId },
                    packageDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating package for admin user {Username}", authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while creating the package." });
            }
        }

        /// <summary>
        /// Update an existing package (Admin only)
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <param name="request">Package update request</param>
        /// <returns>Updated package</returns>
        [HttpPut("{id}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageDto>> UpdatePackage(int id, [FromBody] UpdatePackageRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the authenticated user from HttpContext
            var authenticatedUser = HttpContext.Items["ApiUser"] as User;

            if (authenticatedUser == null)
            {
                _logger.LogError("Authenticated user not found in HttpContext after authentication");
                return StatusCode(500, new { message = "Authentication error - user not found in context." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var package = await _context.Packages
                    .Include(p => p.CurrentStatus)
                    .FirstOrDefaultAsync(p => p.PackageId == id);

                if (package == null)
                {
                    return NotFound(new { message = $"Package with ID {id} not found." });
                }

                // Validate assigned courier if provided
                if (request.AssignedCourierId.HasValue)
                {
                    var courier = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.AssignedCourierId.Value && u.Role == UserRole.Courier);
                    if (courier == null)
                    {
                        return BadRequest(new { message = $"Courier with ID {request.AssignedCourierId} not found or is not a courier." });
                    }
                }

                // Validate status if provided
                StatusDefinition? newStatus = null;
                if (request.StatusId.HasValue)
                {
                    newStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.StatusId == request.StatusId.Value);
                    if (newStatus == null)
                    {
                        return BadRequest(new { message = $"Status with ID {request.StatusId} not found." });
                    }
                }

                // Update package properties
                if (!string.IsNullOrEmpty(request.Notes))
                    package.Notes = request.Notes;

                if (request.AssignedCourierId.HasValue)
                    package.AssignedCourierId = request.AssignedCourierId.Value;

                if (request.WeightInKg.HasValue)
                    package.WeightInKg = request.WeightInKg.Value;

                if (request.Longitude.HasValue)
                    package.Longitude = request.Longitude.Value;

                if (request.Latitude.HasValue)
                    package.Latitude = request.Latitude.Value;

                if (request.StatusId.HasValue)
                {
                    package.StatusId = request.StatusId.Value;

                    // If status is being updated to "Delivered", set delivery date
                    if (newStatus!.Name.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                    {
                        package.DeliveryDate = DateTime.UtcNow;
                    }

                    // Create package history entry for status change
                    var packageHistory = new PackageHistory
                    {
                        PackageId = package.PackageId,
                        StatusId = request.StatusId.Value,
                        Timestamp = DateTime.UtcNow,
                        Longitude = request.Longitude,
                        Latitude = request.Latitude
                    };
                    _context.PackageHistories.Add(packageHistory);
                }

                _context.Packages.Update(package);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Load the updated package with all includes for DTO mapping
                var updatedPackage = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.AssignedCourier)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .FirstAsync(p => p.PackageId == id);

                var packageDto = MapToDto(updatedPackage);

                _logger.LogInformation("Admin user {Username} updated package {PackageId}", authenticatedUser.Username, id);

                return Ok(packageDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating package {PackageId} for admin user {Username}", id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while updating the package." });
            }
        }

        /// <summary>
        /// Delete a package (Admin only)
        /// </summary>
        /// <summary>
        /// Delete a package (Admin only)
        ///
        /// Authentication: X-API-Key header required
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <returns>Success confirmation</returns>
        [HttpDelete("{id}")]
        [ApiAdminAuthorize]
        public async Task<IActionResult> DeletePackage(int id)
        {
            // Get the authenticated user from HttpContext
            var authenticatedUser = HttpContext.Items["ApiUser"] as User;

            if (authenticatedUser == null)
            {
                _logger.LogError("Authenticated user not found in HttpContext after authentication");
                return StatusCode(500, new { message = "Authentication error - user not found in context." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var package = await _context.Packages.FirstOrDefaultAsync(p => p.PackageId == id);
                if (package == null)
                {
                    return NotFound(new { message = $"Package with ID {id} not found." });
                }

                // Delete associated package history records first (due to foreign key constraint)
                var packageHistories = await _context.PackageHistories
                    .Where(ph => ph.PackageId == id)
                    .ToListAsync();

                _context.PackageHistories.RemoveRange(packageHistories);

                // Delete the package
                _context.Packages.Remove(package);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Admin user {Username} deleted package {PackageId} with tracking number {TrackingNumber}",
                    authenticatedUser.Username, id, package.TrackingNumber);

                return Ok(new { message = $"Package {package.TrackingNumber} deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting package {PackageId} for admin user {Username}", id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while deleting the package." });
            }
        }

        #region Private Helper Methods

        private async Task<Address> FindOrCreateAddressAsync(AddressDto addressDto)
        {
            var existingAddress = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Street == addressDto.Street &&
                                        a.City == addressDto.City &&
                                        a.ZipCode == addressDto.ZipCode &&
                                        a.Country == addressDto.Country);

            if (existingAddress != null)
            {
                return existingAddress;
            }

            var newAddress = new Address
            {
                Street = addressDto.Street,
                City = addressDto.City,
                ZipCode = addressDto.ZipCode,
                Country = addressDto.Country
            };

            _context.Addresses.Add(newAddress);
            await _context.SaveChangesAsync();
            return newAddress;
        }

        private static string GenerateTrackingNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = Random.Shared.Next(1000, 9999);
            return $"TT{timestamp}{random}";
        }

        private static PackageDto MapToDto(Package package)
        {
            return new PackageDto
            {
                PackageId = package.PackageId,
                TrackingNumber = package.TrackingNumber,
                SenderUserId = package.SenderUserId,
                SenderUsername = package.SenderUser?.Username ?? "N/A",
                RecipientUserId = package.RecipientUserId,
                RecipientUsername = package.RecipientUser?.Username ?? "N/A",
                AssignedCourierId = package.AssignedCourierId,
                AssignedCourierUsername = package.AssignedCourier?.Username,
                PackageSize = package.PackageSize,
                WeightInKg = package.WeightInKg,
                Notes = package.Notes,
                OriginAddress = package.OriginAddress != null ? new AddressDto
                {
                    Street = package.OriginAddress.Street,
                    City = package.OriginAddress.City,
                    ZipCode = package.OriginAddress.ZipCode,
                    Country = package.OriginAddress.Country
                } : null,
                DestinationAddress = package.DestinationAddress != null ? new AddressDto
                {
                    Street = package.DestinationAddress.Street,
                    City = package.DestinationAddress.City,
                    ZipCode = package.DestinationAddress.ZipCode,
                    Country = package.DestinationAddress.Country
                } : null,
                SubmissionDate = package.SubmissionDate,
                DeliveryDate = package.DeliveryDate,
                StatusId = package.StatusId,
                StatusName = package.CurrentStatus?.Name ?? "Unknown",
                Longitude = package.Longitude,
                Latitude = package.Latitude
            };
        }

        #endregion
    }
}
