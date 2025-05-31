/// <summary>
/// Comprehensive REST API Controller for Package History Management
///
/// This controller provides complete CRUD operations for package history in the track and trace system.
/// All endpoints require admin-level authentication using X-API-Key header.
///
/// Available Endpoints:
/// - GET /api/packagehistory - Retrieve all package history entries (Admin only)
/// - GET /api/packagehistory/{id} - Retrieve all history entries for a specific package by package ID (Admin only)
/// - POST /api/packagehistory - Create new package history entry (Admin only)
/// - PUT /api/packagehistory/{id} - Update existing package history entry (Admin only)
/// - DELETE /api/packagehistory/{id} - Delete package history entry (Admin only)
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
///
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _10.Data;
using _10.Models;
using _10.Models.Api;
using _10.Attributes;
using System.ComponentModel.DataAnnotations;

namespace _10.Controllers.Api
{
    /// <summary>
    /// REST API controller for managing package history in the track and trace system.
    /// Provides CRUD operations for package history with admin-only authentication.
    /// All endpoints require valid username and API token authentication.
    /// </summary>
    [ApiController]
    [Route("api/packagehistory")]
    public class PackageHistoryApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PackageHistoryApiController> _logger;

        public PackageHistoryApiController(
            ApplicationDbContext context,
            ILogger<PackageHistoryApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all package history entries (Admin only)
        ///
        /// Authentication: X-API-Key header required
        /// </summary>
        /// <returns>List of all package history entries</returns>
        [HttpGet]
        [ApiAdminAuthorize]
        public async Task<ActionResult<IEnumerable<PackageHistoryDto>>> GetAllPackageHistory()
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
                var historyEntries = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .AsNoTracking()
                    .OrderByDescending(ph => ph.Timestamp)
                    .ToListAsync();

                var historyDtos = historyEntries.Select(MapToDto);

                _logger.LogInformation("Admin user {Username} retrieved {Count} package history entries",
                    authenticatedUser.Username, historyEntries.Count);
                return Ok(historyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all package history entries for admin user {Username}",
                    authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving package history." });
            }
        }

        /// <summary>
        /// Get all package history entries for a specific package (Admin only)
        ///
        /// Authentication: X-API-Key header required
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <returns>List of package history entries for the specified package</returns>
        [HttpGet("{id}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageHistoryListDto>> GetPackageHistory(int id)
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
                // First, verify that the package exists
                var package = await _context.Packages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == id);

                if (package == null)
                {
                    return NotFound(new { message = $"Package with ID {id} not found." });
                }

                var historyEntries = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .Where(ph => ph.PackageId == id)
                    .AsNoTracking()
                    .OrderByDescending(ph => ph.Timestamp)
                    .ToListAsync();

                var response = new PackageHistoryListDto
                {
                    PackageId = id,
                    PackageTrackingNumber = package.TrackingNumber,
                    HistoryEntries = historyEntries.Select(MapToDto),
                    TotalEntries = historyEntries.Count
                };

                _logger.LogInformation("Admin user {Username} retrieved {Count} history entries for package {PackageId}",
                    authenticatedUser.Username, historyEntries.Count, id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving package history for package {PackageId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving package history." });
            }
        }

        /// <summary>
        /// Create a new package history entry (Admin only)
        /// </summary>
        /// <param name="request">Package history creation request</param>
        /// <returns>Created package history entry</returns>
        [HttpPost]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageHistoryDto>> CreatePackageHistory([FromBody] CreatePackageHistoryRequest request)
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
                // Validate package exists
                var package = await _context.Packages.FirstOrDefaultAsync(p => p.PackageId == request.PackageId);
                if (package == null)
                {
                    return BadRequest(new { message = $"Package with ID {request.PackageId} not found." });
                }

                // Validate status exists
                var status = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.StatusId == request.StatusId);
                if (status == null)
                {
                    return BadRequest(new { message = $"Status with ID {request.StatusId} not found." });
                }

                // Create package history entry
                var packageHistory = new PackageHistory
                {
                    PackageId = request.PackageId,
                    StatusId = request.StatusId,
                    Timestamp = request.Timestamp ?? DateTime.UtcNow,
                    Longitude = request.Longitude,
                    Latitude = request.Latitude
                };

                _context.PackageHistories.Add(packageHistory);
                await _context.SaveChangesAsync();

                // Update package's current status and location if this is the latest entry
                var isLatestEntry = await IsLatestHistoryEntry(packageHistory);
                if (isLatestEntry)
                {
                    package.StatusId = request.StatusId;
                    package.Longitude = request.Longitude;
                    package.Latitude = request.Latitude;

                    // If status is "Delivered", set delivery date
                    if (status.Name.Equals("Delivered", StringComparison.OrdinalIgnoreCase) && package.DeliveryDate == null)
                    {
                        package.DeliveryDate = packageHistory.Timestamp;
                    }

                    _context.Packages.Update(package);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Load the created package history entry with all includes for DTO mapping
                var createdHistoryEntry = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .FirstAsync(ph => ph.PackageHistoryId == packageHistory.PackageHistoryId);

                var historyDto = MapToDto(createdHistoryEntry);

                _logger.LogInformation("Admin user {Username} created package history entry {HistoryId} for package {PackageId}",
                    authenticatedUser.Username, packageHistory.PackageHistoryId, request.PackageId);

                return CreatedAtAction(nameof(GetPackageHistory),
                    new { id = request.PackageId },
                    historyDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating package history entry for admin user {Username}", authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while creating the package history entry." });
            }
        }

        /// <summary>
        /// Update an existing package history entry (Admin only)
        /// </summary>
        /// <param name="id">Package History ID</param>
        /// <param name="request">Package history update request</param>
        /// <returns>Updated package history entry</returns>
        [HttpPut("{id}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageHistoryDto>> UpdatePackageHistory(int id, [FromBody] UpdatePackageHistoryRequest request)
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
                var historyEntry = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .FirstOrDefaultAsync(ph => ph.PackageHistoryId == id);

                if (historyEntry == null)
                {
                    return NotFound(new { message = $"Package history entry with ID {id} not found." });
                }

                var originalStatusId = historyEntry.StatusId;
                var originalTimestamp = historyEntry.Timestamp;

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

                // Update history entry properties
                if (request.StatusId.HasValue)
                    historyEntry.StatusId = request.StatusId.Value;

                if (request.Timestamp.HasValue)
                    historyEntry.Timestamp = request.Timestamp.Value;

                if (request.Longitude.HasValue)
                    historyEntry.Longitude = request.Longitude.Value;

                if (request.Latitude.HasValue)
                    historyEntry.Latitude = request.Latitude.Value;

                _context.PackageHistories.Update(historyEntry);
                await _context.SaveChangesAsync();

                // Check if this is the latest entry and update package accordingly
                var isLatestEntry = await IsLatestHistoryEntry(historyEntry);
                if (isLatestEntry)
                {
                    var package = historyEntry.Package;
                    package.StatusId = historyEntry.StatusId;
                    package.Longitude = historyEntry.Longitude;
                    package.Latitude = historyEntry.Latitude;

                    // Handle delivery date for "Delivered" status
                    if (newStatus != null && newStatus.Name.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                    {
                        package.DeliveryDate = historyEntry.Timestamp;
                    }
                    else if (originalStatusId != historyEntry.StatusId)
                    {
                        // If status changed from "Delivered" to something else, clear delivery date
                        var originalStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.StatusId == originalStatusId);
                        if (originalStatus != null && originalStatus.Name.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                        {
                            package.DeliveryDate = null;
                        }
                    }

                    _context.Packages.Update(package);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Load the updated package history entry with all includes for DTO mapping
                var updatedHistoryEntry = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .FirstAsync(ph => ph.PackageHistoryId == id);

                var historyDto = MapToDto(updatedHistoryEntry);

                _logger.LogInformation("Admin user {Username} updated package history entry {HistoryId}",
                    authenticatedUser.Username, id);

                return Ok(historyDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating package history entry {HistoryId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while updating the package history entry." });
            }
        }

        /// <summary>
        /// Delete a package history entry (Admin only)
        ///
        /// Authentication: X-API-Key header required
        /// </summary>
        /// <param name="id">Package History ID</param>
        /// <returns>Success confirmation</returns>
        [HttpDelete("{id}")]
        [ApiAdminAuthorize]
        public async Task<IActionResult> DeletePackageHistory(int id)
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
                var historyEntry = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .FirstOrDefaultAsync(ph => ph.PackageHistoryId == id);

                if (historyEntry == null)
                {
                    return NotFound(new { message = $"Package history entry with ID {id} not found." });
                }

                var packageId = historyEntry.PackageId;
                var wasLatestEntry = await IsLatestHistoryEntry(historyEntry);

                // Delete the history entry
                _context.PackageHistories.Remove(historyEntry);
                await _context.SaveChangesAsync();

                // If this was the latest entry, update package with the new latest entry
                if (wasLatestEntry)
                {
                    var newLatestEntry = await _context.PackageHistories
                        .Include(ph => ph.Status)
                        .Where(ph => ph.PackageId == packageId)
                        .OrderByDescending(ph => ph.Timestamp)
                        .FirstOrDefaultAsync();

                    var package = await _context.Packages.FirstOrDefaultAsync(p => p.PackageId == packageId);
                    if (package != null && newLatestEntry != null)
                    {
                        package.StatusId = newLatestEntry.StatusId;
                        package.Longitude = newLatestEntry.Longitude;
                        package.Latitude = newLatestEntry.Latitude;

                        // Update delivery date based on new latest status
                        if (newLatestEntry.Status.Name.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                        {
                            package.DeliveryDate = newLatestEntry.Timestamp;
                        }
                        else
                        {
                            package.DeliveryDate = null;
                        }

                        _context.Packages.Update(package);
                        await _context.SaveChangesAsync();
                    }
                    else if (package != null && newLatestEntry == null)
                    {
                        // No history entries left, reset to initial status
                        var initialStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.Name == "Sent");
                        if (initialStatus != null)
                        {
                            package.StatusId = initialStatus.StatusId;
                            package.DeliveryDate = null;
                            package.Longitude = null;
                            package.Latitude = null;
                            _context.Packages.Update(package);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Admin user {Username} deleted package history entry {HistoryId} for package {PackageId}",
                    authenticatedUser.Username, id, packageId);

                return Ok(new { message = $"Package history entry {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting package history entry {HistoryId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while deleting the package history entry." });
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Check if the given history entry is the latest (most recent) for its package
        /// </summary>
        /// <param name="historyEntry">The history entry to check</param>
        /// <returns>True if this is the latest entry, false otherwise</returns>
        private async Task<bool> IsLatestHistoryEntry(PackageHistory historyEntry)
        {
            var latestEntry = await _context.PackageHistories
                .Where(ph => ph.PackageId == historyEntry.PackageId)
                .OrderByDescending(ph => ph.Timestamp)
                .ThenByDescending(ph => ph.PackageHistoryId) // Use ID as tiebreaker for same timestamp
                .FirstOrDefaultAsync();

            return latestEntry != null && latestEntry.PackageHistoryId == historyEntry.PackageHistoryId;
        }

        /// <summary>
        /// Map PackageHistory entity to PackageHistoryDto
        /// </summary>
        /// <param name="historyEntry">The PackageHistory entity</param>
        /// <returns>PackageHistoryDto</returns>
        private static PackageHistoryDto MapToDto(PackageHistory historyEntry)
        {
            return new PackageHistoryDto
            {
                PackageHistoryId = historyEntry.PackageHistoryId,
                PackageId = historyEntry.PackageId,
                PackageTrackingNumber = historyEntry.Package?.TrackingNumber ?? "N/A",
                StatusId = historyEntry.StatusId,
                StatusName = historyEntry.Status?.Name ?? "Unknown",
                StatusDescription = historyEntry.Status?.Description ?? "Unknown",
                Timestamp = historyEntry.Timestamp,
                Longitude = historyEntry.Longitude,
                Latitude = historyEntry.Latitude
            };
        }

        #endregion
    }
}
