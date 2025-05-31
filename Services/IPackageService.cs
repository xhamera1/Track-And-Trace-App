using _10.Models.Api;

namespace _10.Services
{
    /// <summary>
    /// Service interface for Package operations
    /// </summary>
    public interface IPackageService
    {
        /// <summary>
        /// Get all packages
        /// </summary>
        /// <returns>Collection of package DTOs</returns>
        Task<IEnumerable<PackageDto>> GetAllPackagesAsync();

        /// <summary>
        /// Get a specific package by ID
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <returns>Package DTO or null if not found</returns>
        Task<PackageDto?> GetPackageByIdAsync(int id);

        /// <summary>
        /// Create a new package
        /// </summary>
        /// <param name="request">Create package request</param>
        /// <returns>Service result with created package DTO</returns>
        Task<ServiceResult<PackageDto>> CreatePackageAsync(CreatePackageRequest request);

        /// <summary>
        /// Update an existing package
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <param name="request">Update package request</param>
        /// <returns>Service result with updated package DTO</returns>
        Task<ServiceResult<PackageDto>> UpdatePackageAsync(int id, UpdatePackageRequest request);

        /// <summary>
        /// Delete a package
        /// </summary>
        /// <param name="id">Package ID</param>
        /// <returns>Service result with success or error message</returns>
        Task<ServiceResult<string>> DeletePackageAsync(int id);
    }
}
