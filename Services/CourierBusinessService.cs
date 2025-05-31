using _10.Data;
using _10.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace _10.Services
{
    /// <summary>
    /// Comprehensive business service for courier operations with full business logic
    /// </summary>
    public class CourierBusinessService : ICourierBusinessService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourierBusinessService> _logger;
        private readonly IPackageAuthorizationService _authorizationService;
        private readonly IPackageLocationService _packageLocationService;

        public CourierBusinessService(
            ApplicationDbContext context,
            ILogger<CourierBusinessService> logger,
            IPackageAuthorizationService authorizationService,
            IPackageLocationService packageLocationService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _packageLocationService = packageLocationService ?? throw new ArgumentNullException(nameof(packageLocationService));
        }

        public async Task<ServiceResult<IEnumerable<Package>>> GetActivePackagesAsync(int courierId)
        {
            try
            {
                var activeStatusNames = new List<string> { "Sent", "In Delivery" };

                var activePackages = await _context.Packages
                    .Where(p => p.AssignedCourierId == courierId &&
                               p.CurrentStatus != null &&
                               activeStatusNames.Contains(p.CurrentStatus.Name))
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .OrderByDescending(p => p.SubmissionDate)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} active packages for courier {CourierId}",
                    activePackages.Count, courierId);

                return ServiceResult<IEnumerable<Package>>.Success(activePackages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active packages for courier {CourierId}", courierId);
                return ServiceResult<IEnumerable<Package>>.Failure(
                    "An error occurred while retrieving active packages.");
            }
        }

        public async Task<ServiceResult<IEnumerable<Package>>> GetDeliveredPackagesAsync(int courierId)
        {
            try
            {
                var deliveredPackages = await _context.Packages
                    .Where(p => p.AssignedCourierId == courierId &&
                               p.CurrentStatus != null &&
                               p.CurrentStatus.Name == "Delivered")
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .OrderByDescending(p => p.DeliveryDate)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} delivered packages for courier {CourierId}",
                    deliveredPackages.Count, courierId);

                return ServiceResult<IEnumerable<Package>>.Success(deliveredPackages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving delivered packages for courier {CourierId}", courierId);
                return ServiceResult<IEnumerable<Package>>.Failure(
                    "An error occurred while retrieving delivered packages.");
            }
        }

        public async Task<ServiceResult<IEnumerable<Package>>> GetAllAssignedPackagesAsync(int courierId)
        {
            try
            {
                var allAssignedPackages = await _context.Packages
                    .Where(p => p.AssignedCourierId == courierId)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .OrderByDescending(p => p.SubmissionDate)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} total assigned packages for courier {CourierId}",
                    allAssignedPackages.Count, courierId);

                return ServiceResult<IEnumerable<Package>>.Success(allAssignedPackages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all assigned packages for courier {CourierId}", courierId);
                return ServiceResult<IEnumerable<Package>>.Failure(
                    "An error occurred while retrieving assigned packages.");
            }
        }

        public async Task<ServiceResult<Package>> GetPackageDetailsAsync(int packageId, int courierId, UserRole userRole)
        {
            try
            {
                var package = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .Include(p => p.History).ThenInclude(h => h.Status)
                    .Include(p => p.AssignedCourier)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == packageId);

                if (package == null)
                {
                    _logger.LogWarning("Package {PackageId} not found", packageId);
                    return ServiceResult<Package>.NotFound("Package", packageId);
                }

                var authResult = _authorizationService.GetAuthorizationResult(package, courierId, userRole);
                if (!authResult.IsAuthorized)
                {
                    _logger.LogWarning("Courier {CourierId} with role {UserRole} denied access to package {PackageId}: {Reason}",
                        courierId, userRole, packageId, authResult.Reason);

                    return ServiceResult<Package>.Failure(
                        $"Access denied: {authResult.Reason}", "UNAUTHORIZED");
                }

                _logger.LogInformation("Courier {CourierId} ({AccessType}) accessing package {PackageId} details",
                    courierId, authResult.AccessType, packageId);

                return ServiceResult<Package>.Success(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving package details for package {PackageId}", packageId);
                return ServiceResult<Package>.Failure(
                    "An error occurred while retrieving package details.");
            }
        }

        public async Task<ServiceResult<CourierUpdatePackageStatusViewModel>> PrepareUpdateStatusViewModelAsync(
            int packageId, int courierId, UserRole userRole)
        {
            try
            {
                var package = await _context.Packages
                    .Include(p => p.CurrentStatus)
                    .Include(p => p.AssignedCourier)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == packageId);

                if (package == null)
                {
                    _logger.LogWarning("Package {PackageId} not found for status update preparation", packageId);
                    return ServiceResult<CourierUpdatePackageStatusViewModel>.NotFound("Package", packageId);
                }

                if (!_authorizationService.IsAuthorizedToModifyPackage(package, courierId, userRole))
                {
                    _logger.LogWarning("Courier {CourierId} denied access to modify package {PackageId}",
                        courierId, packageId);

                    return ServiceResult<CourierUpdatePackageStatusViewModel>.Failure(
                        "You are not authorized to update this package status.", "UNAUTHORIZED");
                }

                if (package.CurrentStatus?.Name == "Delivered")
                {
                    return ServiceResult<CourierUpdatePackageStatusViewModel>.Conflict(
                        "This package has already been delivered and its status cannot be changed further through this form.");
                }

                var allowedNewStatusNames = GetAllowedStatusNames(package.CurrentStatus?.Name);
                var availableStatuses = await GetAvailableStatusesAsync(allowedNewStatusNames);

                var viewModel = new CourierUpdatePackageStatusViewModel
                {
                    PackageId = package.PackageId,
                    TrackingNumber = package.TrackingNumber,
                    CurrentStatusName = package.CurrentStatus?.Description,
                    NewStatusId = package.StatusId,
                    CurrentLongitude = package.Longitude,
                    CurrentLatitude = package.Latitude,
                    NewLongitude = package.Longitude,
                    NewLatitude = package.Latitude,
                    Notes = package.Notes,
                    AvailableStatuses = availableStatuses
                };

                _logger.LogInformation("Prepared update status view model for package {PackageId}", packageId);
                return ServiceResult<CourierUpdatePackageStatusViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing update status view model for package {PackageId}", packageId);
                return ServiceResult<CourierUpdatePackageStatusViewModel>.Failure(
                    "An error occurred while preparing the status update form.");
            }
        }

        public async Task<ServiceResult<Package>> UpdatePackageStatusAsync(
            CourierUpdatePackageStatusViewModel viewModel, int courierId, UserRole userRole)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var packageToUpdate = await _context.Packages
                    .Include(p => p.CurrentStatus)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.AssignedCourier)
                    .FirstOrDefaultAsync(p => p.PackageId == viewModel.PackageId);

                if (packageToUpdate == null)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<Package>.NotFound("Package", viewModel.PackageId);
                }

                if (!_authorizationService.IsAuthorizedToModifyPackage(packageToUpdate, courierId, userRole))
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning("Courier {CourierId} (Role: {UserRole}) attempt to modify package {PackageId} denied.",
                        courierId, userRole, packageToUpdate.PackageId);

                    return ServiceResult<Package>.Failure(
                        "You are not authorized to update this package.", "UNAUTHORIZED");
                }

                var newStatus = await _context.StatusDefinitions.FindAsync(viewModel.NewStatusId);
                if (newStatus == null)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<Package>.ValidationFailure("The selected new status is invalid.");
                }

                if (packageToUpdate.CurrentStatus?.Name == "Delivered" && newStatus.Name != "Delivered")
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<Package>.Conflict(
                        "Cannot change the status of a package that has already been delivered, unless re-affirming 'Delivered' status with new notes/location.");
                }

                var locationResult = await ProcessLocationUpdateAsync(viewModel, packageToUpdate, newStatus);
                if (!locationResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<Package>.ValidationFailure(locationResult.ErrorMessage!);
                }

                var updateResult = ApplyPackageUpdates(packageToUpdate, viewModel, newStatus,
                    locationResult.Data!.FinalLatitude, locationResult.Data.FinalLongitude,
                    locationResult.Data.LocationChangedByUser);

                if (!updateResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return updateResult;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Courier {CourierId} successfully updated package {PackageId} (Tracking: {TrackingNumber}) to status {NewStatusName}. Location: Lat={Lat}, Lon={Lon}.",
                    courierId, packageToUpdate.PackageId, packageToUpdate.TrackingNumber, newStatus.Name,
                    locationResult.Data.FinalLatitude, locationResult.Data.FinalLongitude);

                return ServiceResult<Package>.Success(packageToUpdate);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Concurrency conflict while updating package {PackageId}.", viewModel.PackageId);
                return ServiceResult<Package>.Conflict(
                    "The package data was modified by another user. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error updating package {PackageId}.", viewModel.PackageId);
                return ServiceResult<Package>.Failure(
                    "An unexpected error occurred while updating the package status.");
            }
        }

        public async Task PopulateViewModelForErrorAsync(CourierUpdatePackageStatusViewModel viewModel)
        {
            try
            {
                var currentPackageData = await _context.Packages
                    .Include(p => p.CurrentStatus)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == viewModel.PackageId);

                if (currentPackageData != null)
                {
                    viewModel.TrackingNumber = currentPackageData.TrackingNumber;
                    viewModel.CurrentStatusName = currentPackageData.CurrentStatus?.Description;
                    viewModel.CurrentLongitude = currentPackageData.Longitude;
                    viewModel.CurrentLatitude = currentPackageData.Latitude;
                }

                var allowedNewStatusNames = GetAllowedStatusNames(currentPackageData?.CurrentStatus?.Name);
                viewModel.AvailableStatuses = await GetAvailableStatusesAsync(allowedNewStatusNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating view model for error for package {PackageId}", viewModel.PackageId);
                // Ensure we have at least an empty list to avoid further errors
                viewModel.AvailableStatuses = new List<SelectListItem>();
            }
        }

        #region Private Helper Methods

        private static List<string> GetAllowedStatusNames(string? currentStatusName)
        {
            var allowedNewStatusNames = new List<string>();

            if (currentStatusName == "Sent")
            {
                allowedNewStatusNames.Add("In Delivery");
                allowedNewStatusNames.Add("Delivered");
            }
            else if (currentStatusName == "In Delivery")
            {
                allowedNewStatusNames.Add("In Delivery");
                allowedNewStatusNames.Add("Delivered");
            }

            return allowedNewStatusNames;
        }

        private async Task<List<SelectListItem>> GetAvailableStatusesAsync(List<string> allowedStatusNames)
        {
            return await _context.StatusDefinitions
                .Where(s => allowedStatusNames.Contains(s.Name))
                .OrderBy(s => s.Description)
                .Select(s => new SelectListItem
                {
                    Value = s.StatusId.ToString(),
                    Text = s.Description
                })
                .ToListAsync();
        }

        private async Task<ServiceResult<LocationUpdateResult>> ProcessLocationUpdateAsync(
            CourierUpdatePackageStatusViewModel viewModel, Package package, StatusDefinition newStatus)
        {
            decimal? finalLatitude = package.Latitude;
            decimal? finalLongitude = package.Longitude;
            bool locationChangedByUser = false;

            // Direct coordinates provided
            if (viewModel.NewLatitude.HasValue && viewModel.NewLongitude.HasValue)
            {
                finalLatitude = viewModel.NewLatitude.Value;
                finalLongitude = viewModel.NewLongitude.Value;
                locationChangedByUser = true;

                _logger.LogInformation("Package {PackageId}: Using directly provided coordinates Lat={Lat}, Lon={Lon}",
                    package.PackageId, finalLatitude, finalLongitude);
            }
            // Address provided for geocoding
            else if (viewModel.HasNewLocationAddress())
            {
                _logger.LogInformation("Package {PackageId}: Attempting to geocode provided address: {Street}, {City}, {Zip}, {Country}",
                    package.PackageId, viewModel.NewLocationStreet, viewModel.NewLocationCity,
                    viewModel.NewLocationZipCode, viewModel.NewLocationCountry);

                var addressToGeocode = new Address
                {
                    Street = viewModel.NewLocationStreet!,
                    City = viewModel.NewLocationCity!,
                    ZipCode = viewModel.NewLocationZipCode!,
                    Country = viewModel.NewLocationCountry!
                };

                var geocodingResult = await _packageLocationService.GeocodeAddressAsync(addressToGeocode);

                if (geocodingResult.IsSuccess && geocodingResult.Latitude.HasValue && geocodingResult.Longitude.HasValue)
                {
                    finalLatitude = geocodingResult.Latitude.Value;
                    finalLongitude = geocodingResult.Longitude.Value;
                    locationChangedByUser = true;

                    _logger.LogInformation("Package {PackageId}: Successfully geocoded provided address to Lat={Lat}, Lon={Lon}",
                        package.PackageId, finalLatitude, finalLongitude);
                }
                else
                {
                    var geocodeError = $"Could not geocode the provided address: {geocodingResult.ErrorMessage ?? "Unknown error."}";
                    _logger.LogWarning("Package {PackageId}: Geocoding failed for provided address. Error: {Error}",
                        package.PackageId, geocodeError);

                    return ServiceResult<LocationUpdateResult>.ValidationFailure(geocodeError);
                }
            }
            // Auto-geocode destination for delivered packages
            else if (newStatus.Name == "Delivered" && package.DestinationAddress != null)
            {
                _logger.LogInformation("Package {PackageId}: Status changed to 'Delivered'. Attempting to geocode destination address.",
                    package.PackageId);

                var geocodingResult = await _packageLocationService.GeocodeAddressAsync(package.DestinationAddress);

                if (geocodingResult.IsSuccess && geocodingResult.Latitude.HasValue && geocodingResult.Longitude.HasValue)
                {
                    finalLatitude = geocodingResult.Latitude.Value;
                    finalLongitude = geocodingResult.Longitude.Value;
                    locationChangedByUser = true;

                    _logger.LogInformation("Package {PackageId}: Geocoded destination for 'Delivered' status to Lat={Lat}, Lon={Lon}",
                        package.PackageId, finalLatitude, finalLongitude);
                }
                else
                {
                    _logger.LogWarning("Package {PackageId}: Failed to geocode destination address for 'Delivered' status. Error: {Error}",
                        package.PackageId, geocodingResult.ErrorMessage);
                }
            }

            var result = new LocationUpdateResult
            {
                FinalLatitude = finalLatitude,
                FinalLongitude = finalLongitude,
                LocationChangedByUser = locationChangedByUser
            };

            return ServiceResult<LocationUpdateResult>.Success(result);
        }

        private ServiceResult<Package> ApplyPackageUpdates(
            Package package, CourierUpdatePackageStatusViewModel viewModel, StatusDefinition newStatus,
            decimal? finalLatitude, decimal? finalLongitude, bool locationChangedByUser)
        {
            try
            {
                bool statusChanged = package.StatusId != viewModel.NewStatusId;
                bool finalLocationIsDifferent = package.Latitude != finalLatitude || package.Longitude != finalLongitude;
                bool notesChanged = package.Notes != viewModel.Notes;

                // Update package properties
                package.StatusId = newStatus.StatusId;
                package.Longitude = finalLongitude;
                package.Latitude = finalLatitude;
                package.Notes = viewModel.Notes;

                // Set delivery date for delivered packages
                if (newStatus.Name == "Delivered" && package.DeliveryDate == null)
                {
                    package.DeliveryDate = DateTime.UtcNow;
                }

                // Create history entry if significant changes occurred
                if (statusChanged || finalLocationIsDifferent || notesChanged ||
                    (newStatus.Name == "In Delivery" && package.CurrentStatus?.Name == "In Delivery"))
                {
                    var packageHistoryEntry = new PackageHistory
                    {
                        PackageId = package.PackageId,
                        StatusId = newStatus.StatusId,
                        Timestamp = DateTime.UtcNow,
                        Longitude = finalLongitude,
                        Latitude = finalLatitude
                    };

                    _context.PackageHistories.Add(packageHistoryEntry);
                    _logger.LogInformation("Package {PackageId}: PackageHistory entry created for status {StatusName}.",
                        package.PackageId, newStatus.Name);
                }

                _context.Packages.Update(package);
                return ServiceResult<Package>.Success(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying updates to package {PackageId}", package.PackageId);
                return ServiceResult<Package>.Failure("An error occurred while applying package updates.");
            }
        }

        #endregion
    }

    /// <summary>
    /// Helper class for location update results
    /// </summary>
    internal class LocationUpdateResult
    {
        public decimal? FinalLatitude { get; set; }
        public decimal? FinalLongitude { get; set; }
        public bool LocationChangedByUser { get; set; }
    }
}
