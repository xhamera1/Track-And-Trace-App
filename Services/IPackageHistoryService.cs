using _10.Models.Api;

namespace _10.Services
{
    /// <summary>
    /// Service interface for Package History operations
    /// </summary>
    public interface IPackageHistoryService
    {
        /// <summary>
        /// Get all package history entries
        /// </summary>
        /// <returns>Collection of package history DTOs</returns>
        Task<IEnumerable<PackageHistoryDto>> GetAllPackageHistoryAsync();

        /// <summary>
        /// Get all package history entries for a specific package
        /// </summary>
        /// <param name="packageId">Package ID</param>
        /// <returns>Package history list DTO or null if package not found</returns>
        Task<PackageHistoryListDto?> GetPackageHistoryByPackageIdAsync(int packageId);

        /// <summary>
        /// Create a new package history entry
        /// </summary>
        /// <param name="request">Create package history request</param>
        /// <returns>Service result with created package history DTO</returns>
        Task<ServiceResult<PackageHistoryDto>> CreatePackageHistoryAsync(CreatePackageHistoryRequest request);

        /// <summary>
        /// Update an existing package history entry
        /// </summary>
        /// <param name="id">Package history ID</param>
        /// <param name="request">Update package history request</param>
        /// <returns>Service result with updated package history DTO</returns>
        Task<ServiceResult<PackageHistoryDto>> UpdatePackageHistoryAsync(int id, UpdatePackageHistoryRequest request);

        /// <summary>
        /// Delete a package history entry
        /// </summary>
        /// <param name="id">Package history ID</param>
        /// <returns>Service result with success or error message</returns>
        Task<ServiceResult<string>> DeletePackageHistoryAsync(int id);
    }
}
