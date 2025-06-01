using _10.Models.Api;

namespace _10.Services
{
    public interface IPackageService
    {

        Task<IEnumerable<PackageDto>> GetAllPackagesAsync();

        Task<PackageDto?> GetPackageByIdAsync(int id);

        Task<ServiceResult<PackageDto>> CreatePackageAsync(CreatePackageRequest request);

        Task<ServiceResult<PackageDto>> UpdatePackageAsync(int id, UpdatePackageRequest request);

        Task<ServiceResult<string>> DeletePackageAsync(int id);
    }
}
