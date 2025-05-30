using System.Threading.Tasks;
using _10.Models;

namespace _10.Services
{
    /// <summary>
    /// Service interface for handling package location operations
    /// </summary>
    public interface IPackageLocationService
    {
        /// <summary>
        /// Populates coordinates for package addresses using geocoding
        /// </summary>
        /// <param name="package">Package to populate coordinates for</param>
        /// <returns>True if coordinates were successfully populated, false otherwise</returns>
        Task<bool> PopulatePackageCoordinatesAsync(Package package);

        /// <summary>
        /// Geocodes an address and returns coordinates
        /// </summary>
        /// <param name="address">Address to geocode</param>
        /// <returns>Geocoding result with coordinates</returns>
        Task<GeocodingResult> GeocodeAddressAsync(Address address);

        /// <summary>
        /// Updates package location with new coordinates
        /// </summary>
        /// <param name="package">Package to update</param>
        /// <param name="latitude">New latitude</param>
        /// <param name="longitude">New longitude</param>
        /// <param name="saveChanges">Whether to save changes to database immediately</param>
        /// <returns>True if location was successfully updated</returns>
        Task<bool> UpdatePackageLocationAsync(Package package, decimal latitude, decimal longitude, bool saveChanges = true);

        /// <summary>
        /// Gets the current delivery location for a package based on its status
        /// </summary>
        /// <param name="package">Package to get location for</param>
        /// <returns>Current location coordinates or null if not available</returns>
        (decimal? Latitude, decimal? Longitude) GetCurrentPackageLocation(Package package);
    }
}
