using _10.Models;

namespace _10.Services
{

    public interface IPackageManagementService
    {

        Task<ServiceResult<PackageOperationResult>> SendPackageAsync(SendPackageViewModel model, int senderUserId);

 
        Task<ServiceResult<Package>> GetPackageDetailsAsync(int packageId, int userId, UserRole userRole);


        Task<ServiceResult<IEnumerable<Package>>> GetPackagesForPickupAsync(int recipientUserId);


        Task<ServiceResult<PackageOperationResult>> PickUpPackageAsync(int packageId, int recipientUserId);


        Task<ServiceResult<Address>> FindOrCreateAddressAsync(string street, string city, string zipCode, string country);

        string GenerateTrackingNumber();

        Task<ServiceResult<int?>> AssignRandomCourierAsync();
    }

    public class PackageOperationResult
    {
        public int PackageId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime OperationTimestamp { get; set; } = DateTime.UtcNow;
    }
}
