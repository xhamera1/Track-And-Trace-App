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
/// Business Logic: Handled by StatusDefinitionService for maintainability
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
using _10.Attributes;
using _10.Services;

namespace _10.Controllers.Api
{
    /// <summary>
    /// REST API controller for managing status definitions in the track and trace system.
    /// Provides CRUD operations for status definitions with admin-only authentication.
    /// All endpoints require valid username and API token authentication.
    /// Business logic is handled by the StatusDefinitionService.
    /// </summary>
    [ApiController]
    [Route("api/status-definition")]
    public class StatusDefinitionApiController : ControllerBase
    {
        private readonly IStatusDefinitionService _statusDefinitionService;
        private readonly ILogger<StatusDefinitionApiController> _logger;

        public StatusDefinitionApiController(
            IStatusDefinitionService statusDefinitionService,
            ILogger<StatusDefinitionApiController> logger)
        {
            _statusDefinitionService = statusDefinitionService;
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
                var statusDefinitions = await _statusDefinitionService.GetAllStatusDefinitionsAsync();

                _logger.LogInformation("Admin user {Username} retrieved {Count} status definitions",
                    authenticatedUser.Username, statusDefinitions.Count());

                return Ok(statusDefinitions);
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
                var statusDefinition = await _statusDefinitionService.GetStatusDefinitionByIdAsync(id);

                if (statusDefinition == null)
                {
                    _logger.LogWarning("Status definition with ID {StatusId} not found for admin user {Username}",
                        id, authenticatedUser.Username);
                    return NotFound(new { message = $"Status definition with ID {id} not found." });
                }

                _logger.LogInformation("Admin user {Username} retrieved status definition {StatusId}",
                    authenticatedUser.Username, id);

                return Ok(statusDefinition);
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

            try
            {
                var result = await _statusDefinitionService.CreateStatusDefinitionAsync(request);

                if (!result.IsSuccess)
                {
                    if (result.ErrorCode == "CONFLICT")
                    {
                        _logger.LogWarning("Admin user {Username} attempted to create status definition with duplicate name: {Name}",
                            authenticatedUser.Username, request.Name);
                        return BadRequest(new { message = result.ErrorMessage });
                    }

                    return BadRequest(new { message = result.ErrorMessage });
                }

                _logger.LogInformation("Admin user {Username} created new status definition with ID {StatusId} and name '{Name}'",
                    authenticatedUser.Username, result.Data!.StatusId, result.Data.Name);

                return CreatedAtAction(
                    nameof(GetStatusDefinition),
                    new { id = result.Data.StatusId },
                    result.Data);
            }
            catch (Exception ex)
            {
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

            try
            {
                var result = await _statusDefinitionService.UpdateStatusDefinitionAsync(id, request);

                if (!result.IsSuccess)
                {
                    if (result.ErrorCode == "NOT_FOUND")
                    {
                        _logger.LogWarning("Status definition with ID {StatusId} not found for update by admin user {Username}",
                            id, authenticatedUser.Username);
                        return NotFound(new { message = result.ErrorMessage });
                    }

                    if (result.ErrorCode == "CONFLICT")
                    {
                        _logger.LogWarning("Admin user {Username} attempted to update status definition {StatusId} with duplicate name: {Name}",
                            authenticatedUser.Username, id, request.Name);
                        return BadRequest(new { message = result.ErrorMessage });
                    }

                    return BadRequest(new { message = result.ErrorMessage });
                }

                _logger.LogInformation("Admin user {Username} updated status definition {StatusId} with name '{Name}'",
                    authenticatedUser.Username, id, result.Data!.Name);

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
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

            try
            {
                var result = await _statusDefinitionService.DeleteStatusDefinitionAsync(id);

                if (!result.IsSuccess)
                {
                    if (result.ErrorCode == "NOT_FOUND")
                    {
                        _logger.LogWarning("Status definition with ID {StatusId} not found for deletion by admin user {Username}",
                            id, authenticatedUser.Username);
                        return NotFound(new { message = result.ErrorMessage });
                    }

                    if (result.ErrorCode == "CONFLICT")
                    {
                        _logger.LogWarning("Admin user {Username} attempted to delete status definition {StatusId} that is in use",
                            authenticatedUser.Username, id);
                        return BadRequest(new { message = result.ErrorMessage });
                    }

                    return BadRequest(new { message = result.ErrorMessage });
                }

                _logger.LogInformation("Admin user {Username} deleted status definition {StatusId}",
                    authenticatedUser.Username, id);

                return Ok(new { message = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting status definition {StatusId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while deleting the status definition." });
            }
        }
    }
}
