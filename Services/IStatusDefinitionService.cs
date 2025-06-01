using _10.Models;
using _10.Models.Api;

namespace _10.Services
{
    public interface IStatusDefinitionService
    {
        Task<IEnumerable<StatusDefinitionDto>> GetAllStatusDefinitionsAsync();


        Task<StatusDefinitionDto?> GetStatusDefinitionByIdAsync(int id);

        Task<ServiceResult<StatusDefinitionDto>> CreateStatusDefinitionAsync(CreateStatusDefinitionRequest request);

        Task<ServiceResult<StatusDefinitionDto>> UpdateStatusDefinitionAsync(int id, UpdateStatusDefinitionRequest request);

        Task<ServiceResult<string>> DeleteStatusDefinitionAsync(int id);
    }
}
