using _10.Models;

namespace _10.Services
{
    /// <summary>
    /// Service interface for managing package operations from the web interface
    /// </summary>
    public interface IPackageManagementService
    {
        /// <summary>
        /// Sends a new package with the provided details
        /// </summary>
        /// <param name="model">The package sending model</param>
        /// <param name="senderUserId">ID of the user sending the package</param>
        /// <returns>Service result with package details or error information</returns>
        Task<ServiceResult<PackageOperationResult>> SendPackageAsync(SendPackageViewModel model, int senderUserId);

        /// <summary>
        /// Gets package details for authorized user
        /// </summary>
        /// <param name="packageId">ID of the package</param>
        /// <param name="userId">ID of the requesting user</param>
        /// <param name="userRole">Role of the requesting user</param>
        /// <returns>Service result with package details or error information</returns>
        Task<ServiceResult<Package>> GetPackageDetailsAsync(int packageId, int userId, UserRole userRole);

        /// <summary>
        /// Gets packages available for pickup by the recipient
        /// </summary>
        /// <param name="recipientUserId">ID of the recipient user</param>
        /// <returns>Service result with list of packages or error information</returns>
        Task<ServiceResult<IEnumerable<Package>>> GetPackagesForPickupAsync(int recipientUserId);

        /// <summary>
        /// Processes package pickup and marks it as delivered
        /// </summary>
        /// <param name="packageId">ID of the package to pick up</param>
        /// <param name="recipientUserId">ID of the recipient user</param>
        /// <returns>Service result with pickup result or error information</returns>
        Task<ServiceResult<PackageOperationResult>> PickUpPackageAsync(int packageId, int recipientUserId);

        /// <summary>
        /// Finds or creates an address in the database
        /// </summary>
        /// <param name="street">Street address</param>
        /// <param name="city">City</param>
        /// <param name="zipCode">ZIP code</param>
        /// <param name="country">Country</param>
        /// <returns>Service result with address or error information</returns>
        Task<ServiceResult<Address>> FindOrCreateAddressAsync(string street, string city, string zipCode, string country);

        /// <summary>
        /// Generates a unique tracking number for a package
        /// </summary>
        /// <returns>Generated tracking number</returns>
        string GenerateTrackingNumber();

        /// <summary>
        /// Assigns a random courier to a package
        /// </summary>
        /// <returns>Service result with courier ID or null if no couriers available</returns>
        Task<ServiceResult<int?>> AssignRandomCourierAsync();
    }

    /// <summary>
    /// Result model for package operations
    /// </summary>
    public class PackageOperationResult
    {
        public int PackageId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime OperationTimestamp { get; set; } = DateTime.UtcNow;
    }
}
