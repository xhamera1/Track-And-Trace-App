using _10.Models.Api;

namespace _10.Services
{

    public interface IPackageHistoryService
    {

        Task<IEnumerable<PackageHistoryDto>> GetAllPackageHistoryAsync();


        Task<PackageHistoryListDto?> GetPackageHistoryByPackageIdAsync(int packageId);


        Task<ServiceResult<PackageHistoryDto>> CreatePackageHistoryAsync(CreatePackageHistoryRequest request);

        Task<ServiceResult<PackageHistoryDto>> UpdatePackageHistoryAsync(int id, UpdatePackageHistoryRequest request);

        Task<ServiceResult<string>> DeletePackageHistoryAsync(int id);
    }
}
