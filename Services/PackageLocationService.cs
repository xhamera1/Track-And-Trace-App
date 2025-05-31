using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using _10.Data;
using _10.Models;

namespace _10.Services
{
    public class PackageLocationService : IPackageLocationService
    {
        private readonly IGeocodingService _geocodingService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PackageLocationService> _logger;

        public PackageLocationService(
            IGeocodingService geocodingService,
            ApplicationDbContext context,
            ILogger<PackageLocationService> logger)
        {
            _geocodingService = geocodingService ?? throw new ArgumentNullException(nameof(geocodingService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<bool> PopulatePackageCoordinatesAsync(Package package)
        {
            if (package == null)
            {
                _logger.LogWarning("Cannot populate coordinates for null package");
                return false;
            }

            try
            {
                bool coordinatesUpdated = false;
                if (!package.Latitude.HasValue || !package.Longitude.HasValue)
                {
                    var originResult = await GeocodePackageOriginAsync(package);
                    if (originResult.IsSuccess && originResult.Latitude.HasValue && originResult.Longitude.HasValue)
                    {
                        package.Latitude = originResult.Latitude.Value;
                        package.Longitude = originResult.Longitude.Value;
                        coordinatesUpdated = true;

                        _logger.LogInformation("Populated package {PackageId} coordinates from origin address: {Lat}, {Lon}",
                            package.PackageId, package.Latitude, package.Longitude);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to geocode origin address for package {PackageId}: {Error}",
                            package.PackageId, originResult.ErrorMessage);
                    }
                }

                return coordinatesUpdated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating coordinates for package {PackageId}", package.PackageId);
                return false;
            }
        }

        public async Task<GeocodingResult> GeocodeAddressAsync(Address address)
        {
            if (address == null)
            {
                _logger.LogWarning("Cannot geocode null address");
                return GeocodingResult.Failure("Address cannot be null");
            }

            try
            {
                _logger.LogDebug("Geocoding address: {Street}, {City}, {ZipCode}, {Country}",
                    address.Street, address.City, address.ZipCode, address.Country);

                var result = await _geocodingService.GeocodeAddressAsync(
                    address.Street,
                    address.City,
                    address.ZipCode,
                    address.Country);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully geocoded address {AddressId}: {Lat}, {Lon}",
                        address.AddressId, result.Latitude, result.Longitude);
                }
                else
                {
                    _logger.LogWarning("Failed to geocode address {AddressId}: {Error}",
                        address.AddressId, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding address {AddressId}", address.AddressId);
                return GeocodingResult.Failure($"Unexpected error during geocoding: {ex.Message}");
            }
        }


        public async Task<bool> UpdatePackageLocationAsync(Package package, decimal latitude, decimal longitude, bool saveChanges = true)
        {
            if (package == null)
            {
                _logger.LogWarning("Cannot update location for null package");
                return false;
            }

            try
            {
                if (latitude < -90 || latitude > 90)
                {
                    _logger.LogWarning("Invalid latitude value {Latitude} for package {PackageId}", latitude, package.PackageId);
                    return false;
                }

                if (longitude < -180 || longitude > 180)
                {
                    _logger.LogWarning("Invalid longitude value {Longitude} for package {PackageId}", longitude, package.PackageId);
                    return false;
                }

                var oldLatitude = package.Latitude;
                var oldLongitude = package.Longitude;

                package.Latitude = latitude;
                package.Longitude = longitude;

                if (saveChanges)
                {
                    _context.Packages.Update(package);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Updated package {PackageId} location from ({OldLat}, {OldLon}) to ({NewLat}, {NewLon})",
                    package.PackageId, oldLatitude, oldLongitude, latitude, longitude);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location for package {PackageId}", package.PackageId);
                return false;
            }
        }

        public (decimal? Latitude, decimal? Longitude) GetCurrentPackageLocation(Package package)
        {
            if (package == null)
            {
                _logger.LogWarning("Cannot get location for null package");
                return (null, null);
            }

            try
            {
                if (package.Latitude.HasValue && package.Longitude.HasValue)
                {
                    return (package.Latitude.Value, package.Longitude.Value);
                }

                var currentStatus = package.CurrentStatus?.Name;

                switch (currentStatus)
                {
                    case "Sent":
                    case "Processing":
                        return GetAddressCoordinates(package.OriginAddress);

                    case "Delivered":
                        return GetAddressCoordinates(package.DestinationAddress);

                    case "In Delivery":
                        return (package.Latitude, package.Longitude);

                    default:
                        _logger.LogDebug("Unknown status '{Status}' for package {PackageId}, returning package coordinates",
                            currentStatus, package.PackageId);
                        return (package.Latitude, package.Longitude);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current location for package {PackageId}", package.PackageId);
                return (null, null);
            }
        }

        private async Task<GeocodingResult> GeocodePackageOriginAsync(Package package)
        {
            if (package.OriginAddress == null && package.OriginAddressId > 0)
            {
                package.OriginAddress = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == package.OriginAddressId);
            }

            if (package.OriginAddress == null)
            {
                return GeocodingResult.Failure("Package origin address not found");
            }

            return await GeocodeAddressAsync(package.OriginAddress);
        }

        private (decimal? Latitude, decimal? Longitude) GetAddressCoordinates(Address? address)
        {
            if (address == null)
            {
                return (null, null);
            }


            return (null, null);
        }
    }
}
