using System.Threading.Tasks;
using _10.Models;

namespace _10.Services
{
    public interface IPackageLocationService
    {
        Task<bool> PopulatePackageCoordinatesAsync(Package package);

        Task<GeocodingResult> GeocodeAddressAsync(Address address);

        Task<bool> UpdatePackageLocationAsync(Package package, decimal latitude, decimal longitude, bool saveChanges = true);

        (decimal? Latitude, decimal? Longitude) GetCurrentPackageLocation(Package package);
    }
}
