using _10.Models;
using _10.Models.Api;

namespace _10.Services
{
    /// <summary>
    /// Service interface for Status Definition operations
    /// </summary>
    public interface IStatusDefinitionService
    {
        /// <summary>
        /// Get all status definitions
        /// </summary>
        /// <returns>Collection of status definition DTOs</returns>
        Task<IEnumerable<StatusDefinitionDto>> GetAllStatusDefinitionsAsync();

        /// <summary>
        /// Get a specific status definition by ID
        /// </summary>
        /// <param name="id">Status definition ID</param>
        /// <returns>Status definition DTO or null if not found</returns>
        Task<StatusDefinitionDto?> GetStatusDefinitionByIdAsync(int id);

        /// <summary>
        /// Create a new status definition
        /// </summary>
        /// <param name="request">Create status definition request</param>
        /// <returns>Service result with created status definition DTO</returns>
        Task<ServiceResult<StatusDefinitionDto>> CreateStatusDefinitionAsync(CreateStatusDefinitionRequest request);

        /// <summary>
        /// Update an existing status definition
        /// </summary>
        /// <param name="id">Status definition ID</param>
        /// <param name="request">Update status definition request</param>
        /// <returns>Service result with updated status definition DTO</returns>
        Task<ServiceResult<StatusDefinitionDto>> UpdateStatusDefinitionAsync(int id, UpdateStatusDefinitionRequest request);

        /// <summary>
        /// Delete a status definition
        /// </summary>
        /// <param name="id">Status definition ID</param>
        /// <returns>Service result with success or error message</returns>
        Task<ServiceResult<string>> DeleteStatusDefinitionAsync(int id);
    }
}
