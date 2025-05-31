using _10.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace _10.Services
{
    /// <summary>
    /// Comprehensive business service interface for courier operations
    /// </summary>
    public interface ICourierBusinessService
    {
        /// <summary>
        /// Gets active packages (Sent, In Delivery) for the specified courier
        /// </summary>
        /// <param name="courierId">ID of the courier</param>
        /// <returns>Service result containing list of active packages</returns>
        Task<ServiceResult<IEnumerable<Package>>> GetActivePackagesAsync(int courierId);

        /// <summary>
        /// Gets delivered packages for the specified courier
        /// </summary>
        /// <param name="courierId">ID of the courier</param>
        /// <returns>Service result containing list of delivered packages</returns>
        Task<ServiceResult<IEnumerable<Package>>> GetDeliveredPackagesAsync(int courierId);

        /// <summary>
        /// Gets all packages assigned to the specified courier
        /// </summary>
        /// <param name="courierId">ID of the courier</param>
        /// <returns>Service result containing list of all assigned packages</returns>
        Task<ServiceResult<IEnumerable<Package>>> GetAllAssignedPackagesAsync(int courierId);

        /// <summary>
        /// Gets package details with authorization check
        /// </summary>
        /// <param name="packageId">ID of the package</param>
        /// <param name="courierId">ID of the courier requesting access</param>
        /// <param name="userRole">Role of the user</param>
        /// <returns>Service result containing package details or authorization error</returns>
        Task<ServiceResult<Package>> GetPackageDetailsAsync(int packageId, int courierId, UserRole userRole);

        /// <summary>
        /// Prepares the update status view model with current package data and available statuses
        /// </summary>
        /// <param name="packageId">ID of the package</param>
        /// <param name="courierId">ID of the courier</param>
        /// <param name="userRole">Role of the user</param>
        /// <returns>Service result containing the prepared view model</returns>
        Task<ServiceResult<CourierUpdatePackageStatusViewModel>> PrepareUpdateStatusViewModelAsync(int packageId, int courierId, UserRole userRole);

        /// <summary>
        /// Updates package status with full business logic including location handling and validation
        /// </summary>
        /// <param name="viewModel">The update status view model with new data</param>
        /// <param name="courierId">ID of the courier making the update</param>
        /// <param name="userRole">Role of the user</param>
        /// <returns>Service result indicating success or failure with detailed error information</returns>
        Task<ServiceResult<Package>> UpdatePackageStatusAsync(CourierUpdatePackageStatusViewModel viewModel, int courierId, UserRole userRole);

        /// <summary>
        /// Populates the view model with current package data and available statuses for error scenarios
        /// </summary>
        /// <param name="viewModel">The view model to populate</param>
        /// <returns>Task representing the async operation</returns>
        Task PopulateViewModelForErrorAsync(CourierUpdatePackageStatusViewModel viewModel);
    }
}
