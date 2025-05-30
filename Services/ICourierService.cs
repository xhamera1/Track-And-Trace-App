using _10.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace _10.Services 
{
    public interface ICourierService
    {
        Task<IEnumerable<Package>> GetAllAssignedPackagesAsync(int courierId);

        Task<IEnumerable<Package>> GetActivePackagesAsync(int courierId);

        Task<IEnumerable<Package>> GetDeliveredPackagesAsync(int courierId);

        Task<Package?> GetPackageDetailsForCourierAsync(int packageId, int courierId);

        Task<(bool Success, string? ErrorMessage)> UpdatePackageStatusAndLocationAsync(int packageId, int courierId, int newStatusId, double? longitude, double? latitude, string? notes);
    }
}