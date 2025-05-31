using _10.Models;

namespace _10.Services
{
    public interface IPackageAuthorizationService
    {
        bool IsAuthorizedToViewPackage(Package package, int userId, UserRole userRole);
        bool IsAuthorizedToModifyPackage(Package package, int userId, UserRole userRole);

        PackageAuthorizationResult GetAuthorizationResult(Package package, int userId, UserRole userRole);
    }

    public class PackageAuthorizationResult
    {
        public bool IsAuthorized { get; set; }
        public string Reason { get; set; } = string.Empty;
        public PackageAccessType AccessType { get; set; }
    }

    public enum PackageAccessType
    {
        None,
        Sender,
        Recipient,
        AssignedCourier,
        Admin
    }
}
