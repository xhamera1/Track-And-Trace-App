using _10.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace _10.Services
{
    public interface ICourierBusinessService
    {
        Task<ServiceResult<IEnumerable<Package>>> GetActivePackagesAsync(int courierId);

        Task<ServiceResult<IEnumerable<Package>>> GetDeliveredPackagesAsync(int courierId);

        Task<ServiceResult<IEnumerable<Package>>> GetAllAssignedPackagesAsync(int courierId);


        Task<ServiceResult<Package>> GetPackageDetailsAsync(int packageId, int courierId, UserRole userRole);

        Task<ServiceResult<CourierUpdatePackageStatusViewModel>> PrepareUpdateStatusViewModelAsync(int packageId, int courierId, UserRole userRole);

        Task<ServiceResult<Package>> UpdatePackageStatusAsync(CourierUpdatePackageStatusViewModel viewModel, int courierId, UserRole userRole);


        Task PopulateViewModelForErrorAsync(CourierUpdatePackageStatusViewModel viewModel);
    }
}
