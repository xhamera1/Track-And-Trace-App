using _10.Models;

namespace _10.Services
{
    /// <summary>
    /// Service interface for handling package access authorization
    /// </summary>
    public interface IPackageAuthorizationService
    {
        /// <summary>
        /// Checks if a user is authorized to view package details
        /// </summary>
        /// <param name="package">The package to check access for</param>
        /// <param name="userId">The ID of the user requesting access</param>
        /// <param name="userRole">The role of the user requesting access</param>
        /// <returns>True if authorized, false otherwise</returns>
        bool IsAuthorizedToViewPackage(Package package, int userId, UserRole userRole);

        /// <summary>
        /// Checks if a user is authorized to modify package details
        /// </summary>
        /// <param name="package">The package to check access for</param>
        /// <param name="userId">The ID of the user requesting access</param>
        /// <param name="userRole">The role of the user requesting access</param>
        /// <returns>True if authorized, false otherwise</returns>
        bool IsAuthorizedToModifyPackage(Package package, int userId, UserRole userRole);

        /// <summary>
        /// Gets a detailed authorization result with reasoning
        /// </summary>
        /// <param name="package">The package to check access for</param>
        /// <param name="userId">The ID of the user requesting access</param>
        /// <param name="userRole">The role of the user requesting access</param>
        /// <returns>Authorization result with details</returns>
        PackageAuthorizationResult GetAuthorizationResult(Package package, int userId, UserRole userRole);
    }

    /// <summary>
    /// Result of package authorization check
    /// </summary>
    public class PackageAuthorizationResult
    {
        public bool IsAuthorized { get; set; }
        public string Reason { get; set; } = string.Empty;
        public PackageAccessType AccessType { get; set; }
    }

    /// <summary>
    /// Type of access the user has to the package
    /// </summary>
    public enum PackageAccessType
    {
        None,
        Sender,
        Recipient,
        AssignedCourier,
        Admin
    }
}
