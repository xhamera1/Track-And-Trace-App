/// <summary>
/// Comprehensive REST API Controller for Status Definition Management
///
/// This controller provides complete CRUD operations for status definitions in the track and trace system.
/// All endpoints require admin-level authentication using X-API-Key header.
///
/// Available Endpoints:
/// - GET /api/status-definition - Retrieve all status definitions (Admin only)
/// - GET /api/status-definition/{id} - Retrieve specific status definition by ID (Admin only)
/// - POST /api/status-definition - Create new status definition (Admin only)
/// - PUT /api/status-definition/{id} - Update existing status definition (Admin only)
/// - DELETE /api/status-definition/{id} - Delete status definition (Admin only)
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
    /// REST API controller for managing status definitions in the track and trace system.
    /// Provides CRUD operations for status definitions with admin-only authentication.
    /// All endpoints require valid username and API token authentication.
    /// </summary>
    [ApiController]
    [Route("api/status-definition")]
    public class StatusDefinitionApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StatusDefinitionApiController> _logger;

        public StatusDefinitionApiController(
            ApplicationDbContext context,
            ILogger<StatusDefinitionApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all status definitions (Admin only)
        ///
        /// Authentication: X-API-Key header required
        /// </summary>
        /// <returns>List of all status definitions</returns>
        [HttpGet]
        [ApiAdminAuthorize]
        public async Task<ActionResult<IEnumerable<StatusDefinitionDto>>> GetAllStatusDefinitions()
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
                var statusDefinitions = await _context.StatusDefinitions
                    .AsNoTracking()
                    .OrderBy(sd => sd.StatusId)
                    .ToListAsync();

                var statusDefinitionDtos = statusDefinitions.Select(MapToDto);

                _logger.LogInformation("Admin user {Username} retrieved {Count} status definitions",
                    authenticatedUser.Username, statusDefinitions.Count);

                return Ok(statusDefinitionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all status definitions for admin user {Username}",
                    authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving status definitions." });
            }
        }

        /// <summary>
        /// Get a specific status definition by ID (Admin only)
        ///
        /// Authentication: X-API-Key header required
        /// </summary>
        /// <param name="id">Status definition ID</param>
        /// <returns>Status definition details</returns>
        [HttpGet("{id}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<StatusDefinitionDto>> GetStatusDefinition(int id)
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
                var statusDefinition = await _context.StatusDefinitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(sd => sd.StatusId == id);

                if (statusDefinition == null)
                {
                    _logger.LogWarning("Status definition with ID {StatusId} not found for admin user {Username}",
                        id, authenticatedUser.Username);
                    return NotFound(new { message = $"Status definition with ID {id} not found." });
                }

                var statusDefinitionDto = MapToDto(statusDefinition);

                _logger.LogInformation("Admin user {Username} retrieved status definition {StatusId}",
                    authenticatedUser.Username, id);

                return Ok(statusDefinitionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status definition {StatusId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving the status definition." });
            }
        }

        /// <summary>
        /// Create a new status definition (Admin only)
        /// </summary>
        /// <param name="request">Status definition creation request</param>
        /// <returns>Created status definition</returns>
        [HttpPost]
        [ApiAdminAuthorize]
        public async Task<ActionResult<StatusDefinitionDto>> CreateStatusDefinition([FromBody] CreateStatusDefinitionRequest request)
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
                // Check if status definition with the same name already exists
                var existingStatusDefinition = await _context.StatusDefinitions
                    .FirstOrDefaultAsync(sd => sd.Name.ToLower() == request.Name.ToLower());

                if (existingStatusDefinition != null)
                {
                    _logger.LogWarning("Admin user {Username} attempted to create status definition with duplicate name: {Name}",
                        authenticatedUser.Username, request.Name);
                    return BadRequest(new { message = $"Status definition with name '{request.Name}' already exists." });
                }

                // Create new status definition
                var statusDefinition = new StatusDefinition
                {
                    Name = request.Name.Trim(),
                    Description = request.Description.Trim()
                };

                _context.StatusDefinitions.Add(statusDefinition);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var statusDefinitionDto = MapToDto(statusDefinition);

                _logger.LogInformation("Admin user {Username} created new status definition with ID {StatusId} and name '{Name}'",
                    authenticatedUser.Username, statusDefinition.StatusId, statusDefinition.Name);

                return CreatedAtAction(
                    nameof(GetStatusDefinition),
                    new { id = statusDefinition.StatusId },
                    statusDefinitionDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating status definition for admin user {Username} with name '{Name}'",
                    authenticatedUser.Username, request.Name);
                return StatusCode(500, new { message = "Internal server error occurred while creating the status definition." });
            }
        }

        /// <summary>
        /// Update an existing status definition (Admin only)
        /// </summary>
        /// <param name="id">Status definition ID to update</param>
        /// <param name="request">Status definition update request</param>
        /// <returns>Updated status definition</returns>
        [HttpPut("{id}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<StatusDefinitionDto>> UpdateStatusDefinition(int id, [FromBody] UpdateStatusDefinitionRequest request)
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
                // Find the existing status definition
                var statusDefinition = await _context.StatusDefinitions
                    .FirstOrDefaultAsync(sd => sd.StatusId == id);

                if (statusDefinition == null)
                {
                    _logger.LogWarning("Status definition with ID {StatusId} not found for update by admin user {Username}",
                        id, authenticatedUser.Username);
                    return NotFound(new { message = $"Status definition with ID {id} not found." });
                }

                // Check if another status definition with the same name already exists (excluding current one)
                var existingStatusDefinition = await _context.StatusDefinitions
                    .FirstOrDefaultAsync(sd => sd.StatusId != id && sd.Name.ToLower() == request.Name.ToLower());

                if (existingStatusDefinition != null)
                {
                    _logger.LogWarning("Admin user {Username} attempted to update status definition {StatusId} with duplicate name: {Name}",
                        authenticatedUser.Username, id, request.Name);
                    return BadRequest(new { message = $"Another status definition with name '{request.Name}' already exists." });
                }

                // Update the status definition
                statusDefinition.Name = request.Name.Trim();
                statusDefinition.Description = request.Description.Trim();

                _context.StatusDefinitions.Update(statusDefinition);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var statusDefinitionDto = MapToDto(statusDefinition);

                _logger.LogInformation("Admin user {Username} updated status definition {StatusId} with name '{Name}'",
                    authenticatedUser.Username, id, statusDefinition.Name);

                return Ok(statusDefinitionDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating status definition {StatusId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while updating the status definition." });
            }
        }

        /// <summary>
        /// Delete a status definition (Admin only)
        /// </summary>
        /// <param name="id">Status definition ID to delete</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult> DeleteStatusDefinition(int id)
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
                // Find the existing status definition
                var statusDefinition = await _context.StatusDefinitions
                    .FirstOrDefaultAsync(sd => sd.StatusId == id);

                if (statusDefinition == null)
                {
                    _logger.LogWarning("Status definition with ID {StatusId} not found for deletion by admin user {Username}",
                        id, authenticatedUser.Username);
                    return NotFound(new { message = $"Status definition with ID {id} not found." });
                }

                // Check if there are any packages using this status
                var packagesUsingStatus = await _context.Packages
                    .AnyAsync(p => p.StatusId == id);

                if (packagesUsingStatus)
                {
                    _logger.LogWarning("Admin user {Username} attempted to delete status definition {StatusId} '{Name}' that is in use by packages",
                        authenticatedUser.Username, id, statusDefinition.Name);
                    return BadRequest(new { message = $"Cannot delete status definition '{statusDefinition.Name}' because it is currently used by one or more packages." });
                }

                // Check if there are any package histories using this status
                var packageHistoriesUsingStatus = await _context.PackageHistories
                    .AnyAsync(ph => ph.StatusId == id);

                if (packageHistoriesUsingStatus)
                {
                    _logger.LogWarning("Admin user {Username} attempted to delete status definition {StatusId} '{Name}' that is in use by package histories",
                        authenticatedUser.Username, id, statusDefinition.Name);
                    return BadRequest(new { message = $"Cannot delete status definition '{statusDefinition.Name}' because it is referenced in package history records." });
                }

                // Delete the status definition
                _context.StatusDefinitions.Remove(statusDefinition);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Admin user {Username} deleted status definition {StatusId} with name '{Name}'",
                    authenticatedUser.Username, id, statusDefinition.Name);

                return Ok(new { message = $"Status definition '{statusDefinition.Name}' has been successfully deleted." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting status definition {StatusId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while deleting the status definition." });
            }
        }

        /// <summary>
        /// Maps a StatusDefinition entity to a StatusDefinitionDto
        /// </summary>
        /// <param name="statusDefinition">The status definition entity</param>
        /// <returns>StatusDefinitionDto</returns>
        private static StatusDefinitionDto MapToDto(StatusDefinition statusDefinition)
        {
            return new StatusDefinitionDto
            {
                StatusId = statusDefinition.StatusId,
                Name = statusDefinition.Name,
                Description = statusDefinition.Description
            };
        }
    }
}
