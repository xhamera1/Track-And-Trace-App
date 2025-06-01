using _10.Models;
using _10.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace _10.Attributes
{
    public class PackageAccessAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly PackageAccessType _requiredAccessType;
        private readonly string _packageIdParameterName;

        public PackageAccessAuthorizeAttribute(
            PackageAccessType requiredAccessType = PackageAccessType.AssignedCourier,
            string packageIdParameterName = "id")
        {
            _requiredAccessType = requiredAccessType;
            _packageIdParameterName = packageIdParameterName;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

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

            int packageId;
            if (!context.RouteData.Values.TryGetValue(_packageIdParameterName, out var packageIdObj) ||
                !int.TryParse(packageIdObj?.ToString(), out packageId))
            {
                logger.LogWarning("Package ID parameter '{ParameterName}' not found or invalid in route values",
                    _packageIdParameterName);
                context.Result = new BadRequestObjectResult("Invalid package ID");
                return;
            }

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
