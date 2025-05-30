using _10.Models;
using Microsoft.Extensions.Logging;

namespace _10.Services
{
    /// <summary>
    /// Service for handling package access authorization logic
    /// </summary>
    public class PackageAuthorizationService : IPackageAuthorizationService
    {
        private readonly ILogger<PackageAuthorizationService> _logger;

        public PackageAuthorizationService(ILogger<PackageAuthorizationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Checks if a user is authorized to view package details
        /// </summary>
        public bool IsAuthorizedToViewPackage(Package package, int userId, UserRole userRole)
        {
            if (package == null)
            {
                _logger.LogWarning("Authorization check failed: Package is null");
                return false;
            }

            var result = GetAuthorizationResult(package, userId, userRole);
            return result.IsAuthorized;
        }

        /// <summary>
        /// Checks if a user is authorized to modify package details (more restrictive)
        /// </summary>
        public bool IsAuthorizedToModifyPackage(Package package, int userId, UserRole userRole)
        {
            if (package == null)
            {
                _logger.LogWarning("Authorization check failed: Package is null");
                return false;
            }

            // Only admin and assigned courier can modify packages
            var result = GetAuthorizationResult(package, userId, userRole);
            return result.IsAuthorized &&
                   (result.AccessType == PackageAccessType.Admin ||
                    result.AccessType == PackageAccessType.AssignedCourier);
        }

        /// <summary>
        /// Gets a detailed authorization result with reasoning
        /// </summary>
        public PackageAuthorizationResult GetAuthorizationResult(Package package, int userId, UserRole userRole)
        {
            if (package == null)
            {
                return new PackageAuthorizationResult
                {
                    IsAuthorized = false,
                    Reason = "Package not found",
                    AccessType = PackageAccessType.None
                };
            }

            try
            {
                // Admin has access to all packages
                if (userRole == UserRole.Admin)
                {
                    _logger.LogDebug("Admin user {UserId} accessing package {PackageId}", userId, package.PackageId);
                    return new PackageAuthorizationResult
                    {
                        IsAuthorized = true,
                        Reason = "Admin access",
                        AccessType = PackageAccessType.Admin
                    };
                }

                // Sender has access to packages they sent
                if (package.SenderUserId == userId)
                {
                    _logger.LogDebug("Sender user {UserId} accessing their package {PackageId}", userId, package.PackageId);
                    return new PackageAuthorizationResult
                    {
                        IsAuthorized = true,
                        Reason = "Package sender",
                        AccessType = PackageAccessType.Sender
                    };
                }

                // Recipient has access to packages sent to them
                if (package.RecipientUserId == userId)
                {
                    _logger.LogDebug("Recipient user {UserId} accessing their package {PackageId}", userId, package.PackageId);
                    return new PackageAuthorizationResult
                    {
                        IsAuthorized = true,
                        Reason = "Package recipient",
                        AccessType = PackageAccessType.Recipient
                    };
                }

                // Assigned courier has access to packages assigned to them
                if (userRole == UserRole.Courier && package.AssignedCourierId == userId)
                {
                    _logger.LogDebug("Assigned courier {UserId} accessing package {PackageId}", userId, package.PackageId);
                    return new PackageAuthorizationResult
                    {
                        IsAuthorized = true,
                        Reason = "Assigned courier",
                        AccessType = PackageAccessType.AssignedCourier
                    };
                }

                // No authorization found
                _logger.LogWarning("User {UserId} with role {UserRole} attempted unauthorized access to package {PackageId}. " +
                                  "Sender: {SenderId}, Recipient: {RecipientId}, Assigned Courier: {CourierId}",
                                  userId, userRole, package.PackageId,
                                  package.SenderUserId, package.RecipientUserId, package.AssignedCourierId);

                return new PackageAuthorizationResult
                {
                    IsAuthorized = false,
                    Reason = GetUnauthorizedReason(userRole),
                    AccessType = PackageAccessType.None
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during package authorization check for user {UserId} and package {PackageId}",
                               userId, package.PackageId);
                return new PackageAuthorizationResult
                {
                    IsAuthorized = false,
                    Reason = "Authorization check failed due to system error",
                    AccessType = PackageAccessType.None
                };
            }
        }

        /// <summary>
        /// Gets user-friendly unauthorized reason message
        /// </summary>
        private static string GetUnauthorizedReason(UserRole userRole)
        {
            return userRole switch
            {
                UserRole.Courier => "You can only view packages assigned to you",
                UserRole.User => "You can only view packages you sent or are receiving",
                _ => "You are not authorized to view this package"
            };
        }
    }
}
