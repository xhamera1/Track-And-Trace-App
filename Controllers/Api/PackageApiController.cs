using Microsoft.AspNetCore.Mvc;
using _10.Models;
using _10.Models.Api;
using _10.Services;
using _10.Attributes;

namespace _10.Controllers.Api
{
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

        [HttpGet]
        [ApiAdminAuthorize]
        public async Task<ActionResult<IEnumerable<PackageDto>>> GetAllPackages()
        {
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


        [HttpGet("{id}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageDto>> GetPackage(int id)
        {
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

        [HttpPost]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageDto>> CreatePackage([FromBody] CreatePackageRequest request)
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

        [HttpPut("{id}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<PackageDto>> UpdatePackage(int id, [FromBody] UpdatePackageRequest request)
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


        [HttpDelete("{id}")]
        [ApiAdminAuthorize]
        public async Task<IActionResult> DeletePackage(int id)
        {
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
