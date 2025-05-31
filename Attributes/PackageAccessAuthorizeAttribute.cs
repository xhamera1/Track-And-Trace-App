using _10.Models;
using _10.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace _10.Attributes
{
    /// <summary>
    /// Custom authorization attribute that ensures only authorized users can access package-related actions.
    /// For courier endpoints, this ensures only assigned couriers and admins can access specific packages.
    /// </summary>
    public class PackageAccessAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly PackageAccessType _requiredAccessType;
        private readonly string _packageIdParameterName;

        /// <summary>
        /// Initializes a new instance of the PackageAccessAuthorizeAttribute
        /// </summary>
        /// <param name="requiredAccessType">The minimum access type required (e.g., AssignedCourier)</param>
        /// <param name="packageIdParameterName">The name of the action parameter containing the package ID (default: "id")</param>
        public PackageAccessAuthorizeAttribute(
            PackageAccessType requiredAccessType = PackageAccessType.AssignedCourier,
            string packageIdParameterName = "id")
        {
            _requiredAccessType = requiredAccessType;
            _packageIdParameterName = packageIdParameterName;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // First, ensure the user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Get required services
            var courierBusinessService = context.HttpContext.RequestServices
                .GetService<ICourierBusinessService>();
            var logger = context.HttpContext.RequestServices
                .GetService<ILogger<PackageAccessAuthorizeAttribute>>();

            if (courierBusinessService == null || logger == null)
            {
                logger?.LogError("Required services not available for package authorization");
                context.Result = new StatusCodeResult(500);
                return;
            }

            // Extract package ID from route parameters
            int packageId;
            if (!context.RouteData.Values.TryGetValue(_packageIdParameterName, out var packageIdObj) ||
                !int.TryParse(packageIdObj?.ToString(), out packageId))
            {
                logger.LogWarning("Package ID parameter '{ParameterName}' not found or invalid in route values",
                    _packageIdParameterName);
                context.Result = new BadRequestObjectResult("Invalid package ID");
                return;
            }

            // Get current user information
            var userIdString = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleString = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);

            if (!int.TryParse(userIdString, out var userId) ||
                !Enum.TryParse<UserRole>(roleString, out var userRole))
            {
                logger.LogWarning("Unable to parse user ID or role from claims. UserId: {UserId}, Role: {Role}",
                    userIdString, roleString);
                context.Result = new UnauthorizedResult();
                return;
            }

            try
            {
                // Use the business service to check authorization
                var result = await courierBusinessService.GetPackageDetailsAsync(packageId, userId, userRole);

                if (!result.IsSuccess)
                {
                    if (result.ErrorCode == "NOT_FOUND")
                    {
                        context.Result = new NotFoundResult();
                        return;
                    }

                    if (result.ErrorCode == "UNAUTHORIZED")
                    {
                        logger.LogWarning("User {UserId} with role {UserRole} denied access to package {PackageId}: {Reason}",
                            userId, userRole, packageId, result.ErrorMessage);

                        // For courier controller, redirect to active packages with error message
                        var controller = context.ActionDescriptor.RouteValues["controller"];
                        if (string.Equals(controller, "Courier", StringComparison.OrdinalIgnoreCase))
                        {
                            var tempData = context.HttpContext.RequestServices
                                .GetService<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>()?
                                .GetTempData(context.HttpContext);

                            if (tempData != null)
                            {
                                tempData["ErrorMessage"] = "You are not authorized to access this package.";
                            }

                            context.Result = new RedirectToActionResult("ActivePackages", "Courier", null);
                            return;
                        }

                        context.Result = new ForbidResult();
                        return;
                    }

                    logger.LogError("Authorization check failed for package {PackageId}: {ErrorMessage}",
                        packageId, result.ErrorMessage);
                    context.Result = new StatusCodeResult(500);
                    return;
                }

                // Store the package in the context for use by the action method
                context.HttpContext.Items["AuthorizedPackage"] = result.Data;

                logger.LogDebug("User {UserId} authorized to access package {PackageId}", userId, packageId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception during package authorization for user {UserId} and package {PackageId}",
                    userId, packageId);
                context.Result = new StatusCodeResult(500);
            }
        }
    }
}
