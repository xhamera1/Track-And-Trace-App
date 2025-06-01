using Microsoft.AspNetCore.Mvc;
using _10.Models;
using _10.Models.Api;
using _10.Attributes;
using _10.Services;

namespace _10.Controllers.Api
{
    [ApiController]
    [Route("api/packagehistory")]
    public class PackageHistoryApiController : ControllerBase
    {
        private readonly IPackageHistoryService _packageHistoryService;
        private readonly ILogger<PackageHistoryApiController> _logger;

        public PackageHistoryApiController(
            IPackageHistoryService packageHistoryService,
            ILogger<PackageHistoryApiController> logger)
        {
            _packageHistoryService = packageHistoryService;
            _logger = logger;
        }

        [HttpGet]
        [ApiAdminAuthorize]
        public async Task<ActionResult<IEnumerable<PackageHistoryDto>>> GetAllPackageHistory()
        {
            var authenticatedUser = HttpContext.Items["ApiUser"] as User;

            if (authenticatedUser == null)
            {
                _logger.LogError("Authenticated user not found in HttpContext after authentication");
                return StatusCode(500, new { message = "Authentication error - user not found in context." });
            }

            try
            {
                var historyEntries = await _packageHistoryService.GetAllPackageHistoryAsync();

                _logger.LogInformation("Admin user {Username} retrieved {Count} package history entries",
                    authenticatedUser.Username, historyEntries.Count());
                return Ok(historyEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all package history entries for admin user {Username}",
                    authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving package history." });
            }
        }

        [HttpGet("{id}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageHistoryListDto>> GetPackageHistory(int id)
        {
            var authenticatedUser = HttpContext.Items["ApiUser"] as User;

            if (authenticatedUser == null)
            {
                _logger.LogError("Authenticated user not found in HttpContext after authentication");
                return StatusCode(500, new { message = "Authentication error - user not found in context." });
            }

            try
            {
                var packageHistory = await _packageHistoryService.GetPackageHistoryByPackageIdAsync(id);

                if (packageHistory == null)
                {
                    return NotFound(new { message = $"Package with ID {id} not found." });
                }

                _logger.LogInformation("Admin user {Username} retrieved {Count} history entries for package {PackageId}",
                    authenticatedUser.Username, packageHistory.TotalEntries, id);
                return Ok(packageHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving package history for package {PackageId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while retrieving package history." });
            }
        }

        [HttpPost]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageHistoryDto>> CreatePackageHistory([FromBody] CreatePackageHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var authenticatedUser = HttpContext.Items["ApiUser"] as User;

            if (authenticatedUser == null)
            {
                _logger.LogError("Authenticated user not found in HttpContext after authentication");
                return StatusCode(500, new { message = "Authentication error - user not found in context." });
            }

            try
            {
                var result = await _packageHistoryService.CreatePackageHistoryAsync(request);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Admin user {Username} failed to create package history entry: {ErrorMessage}",
                        authenticatedUser.Username, result.ErrorMessage);
                    return BadRequest(new { message = result.ErrorMessage });
                }

                _logger.LogInformation("Admin user {Username} created package history entry {HistoryId} for package {PackageId}",
                    authenticatedUser.Username, result.Data!.PackageHistoryId, request.PackageId);

                return CreatedAtAction(nameof(GetPackageHistory),
                    new { id = request.PackageId },
                    result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating package history entry for admin user {Username}",
                    authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while creating the package history entry." });
            }
        }

        [HttpPut("{id}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageHistoryDto>> UpdatePackageHistory(int id, [FromBody] UpdatePackageHistoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var authenticatedUser = HttpContext.Items["ApiUser"] as User;

            if (authenticatedUser == null)
            {
                _logger.LogError("Authenticated user not found in HttpContext after authentication");
                return StatusCode(500, new { message = "Authentication error - user not found in context." });
            }

            try
            {
                var result = await _packageHistoryService.UpdatePackageHistoryAsync(id, request);

                if (!result.IsSuccess)
                {
                    if (result.ErrorCode == "NOT_FOUND")
                    {
                        return NotFound(new { message = result.ErrorMessage });
                    }

                    _logger.LogWarning("Admin user {Username} failed to update package history entry {HistoryId}: {ErrorMessage}",
                        authenticatedUser.Username, id, result.ErrorMessage);
                    return BadRequest(new { message = result.ErrorMessage });
                }

                _logger.LogInformation("Admin user {Username} updated package history entry {HistoryId}",
                    authenticatedUser.Username, id);

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating package history entry {HistoryId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while updating the package history entry." });
            }
        }

        [HttpDelete("{id}")]
        [ApiAdminAuthorize]
        public async Task<IActionResult> DeletePackageHistory(int id)
        {
            var authenticatedUser = HttpContext.Items["ApiUser"] as User;

            if (authenticatedUser == null)
            {
                _logger.LogError("Authenticated user not found in HttpContext after authentication");
                return StatusCode(500, new { message = "Authentication error - user not found in context." });
            }

            try
            {
                var result = await _packageHistoryService.DeletePackageHistoryAsync(id);

                if (!result.IsSuccess)
                {
                    if (result.ErrorCode == "NOT_FOUND")
                    {
                        return NotFound(new { message = result.ErrorMessage });
                    }

                    return BadRequest(new { message = result.ErrorMessage });
                }

                var packageId = result.Data!.Contains("package") ?
                    result.Data.Split(' ').LastOrDefault() : "unknown";

                _logger.LogInformation("Admin user {Username} deleted package history entry {HistoryId}",
                    authenticatedUser.Username, id);

                return Ok(new { message = result.Data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting package history entry {HistoryId} for admin user {Username}",
                    id, authenticatedUser.Username);
                return StatusCode(500, new { message = "Internal server error occurred while deleting the package history entry." });
            }
        }
    }
}