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
/// Business Logic: Handled by PackageService for maintainability
/// Logging: Comprehensive logging for all operations and error handling
/// Error Handling: Robust error handling with appropriate HTTP status codes
///
/// Author: Generated for Track and Trace System
/// Version: 2.0 - Refactored with Service Layer
/// </summary>
///
using Microsoft.AspNetCore.Mvc;
using _10.Models;
using _10.Models.Api;
using _10.Services;
using _10.Attributes;

namespace _10.Controllers.Api
{
    /// <summary>
    /// REST API controller for managing packages in the track and trace system.
    /// Provides CRUD operations for packages with admin-only authentication.
    /// All endpoints require valid username and API token authentication.
    /// Business logic is handled by the PackageService.
    /// </summary>
    [ApiController]
    [Route("api/package")]
    public class PackageApiController : ControllerBase
    {
        private readonly IPackageService _packageService;
        private readonly ILogger<PackageApiController> _logger;
        private readonly IPackageAuthorizationService _authorizationService;

        public PackageApiController(
            IPackageService packageService,
            ILogger<PackageApiController> logger,
            IPackageAuthorizationService authorizationService)
        {
            _packageService = packageService;
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
                var packages = await _packageService.GetAllPackagesAsync();

                _logger.LogInformation("Admin user {Username} retrieved {Count} packages",
                    authenticatedUser.Username, packages.Count());
                return Ok(packages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all packages for admin user {Username}",
                    authenticatedUser.Username);
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
                var package = await _packageService.GetPackageByIdAsync(id);

                if (package == null)
                {
                    return NotFound(new { message = $"Package with ID {id} not found." });
                }

                _logger.LogInformation("Admin user {Username} retrieved package {PackageId}",
                    authenticatedUser.Username, id);
                return Ok(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving package {PackageId} for admin user {Username}",
                    id, authenticatedUser.Username);
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

            try
            {
                var result = await _packageService.CreatePackageAsync(request);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Admin user {Username} failed to create package: {ErrorMessage}",
                        authenticatedUser.Username, result.ErrorMessage);
                    return BadRequest(new { message = result.ErrorMessage });
                }

                _logger.LogInformation("Admin user {Username} created package {PackageId} with tracking number {TrackingNumber}",
                    authenticatedUser.Username, result.Data!.PackageId, result.Data.TrackingNumber);

                return CreatedAtAction(nameof(GetPackage),
                    new { id = result.Data.PackageId },
                    result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating package for admin user {Username}",
                    authenticatedUser.Username);
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

            try
            {
                var result = await _packageService.UpdatePackageAsync(id, request);

                if (!result.IsSuccess)
                {
                    if (result.ErrorCode == "NOT_FOUND")
                    {
                        return NotFound(new { message = result.ErrorMessage });
                    }

                    _logger.LogWarning("Admin user {Username} failed to update package {PackageId}: {ErrorMessage}",
                        authenticatedUser.Username, id, result.ErrorMessage);
                    return BadRequest(new { message = result.ErrorMessage });
                }

                _logger.LogInformation("Admin user {Username} updated package {PackageId}",
                    authenticatedUser.Username, id);

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating package {PackageId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while updating the package." });
            }
        }

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

            try
            {
                var result = await _packageService.DeletePackageAsync(id);

                if (!result.IsSuccess)
                {
                    if (result.ErrorCode == "NOT_FOUND")
                    {
                        return NotFound(new { message = result.ErrorMessage });
                    }

                    return BadRequest(new { message = result.ErrorMessage });
                }

                _logger.LogInformation("Admin user {Username} deleted package {PackageId}",
                    authenticatedUser.Username, id);

                return Ok(new { message = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting package {PackageId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while deleting the package." });
            }
        }
    }
}
