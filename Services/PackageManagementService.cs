using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using _10.Data;
using _10.Models;

namespace _10.Services
{
    public class PackageManagementService : IPackageManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PackageManagementService> _logger;
        private readonly IPackageAuthorizationService _authorizationService;
        private readonly IPackageLocationService _packageLocationService;

        public PackageManagementService(
            ApplicationDbContext context,
            ILogger<PackageManagementService> logger,
            IPackageAuthorizationService authorizationService,
            IPackageLocationService packageLocationService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _packageLocationService = packageLocationService ?? throw new ArgumentNullException(nameof(packageLocationService));
        }

        public async Task<ServiceResult<PackageOperationResult>> SendPackageAsync(SendPackageViewModel model, int senderUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var recipientUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.RecipientEmail);
                if (recipientUser == null)
                {
                    recipientUser = new User
                    {
                        Username = model.RecipientEmail,
                        Email = model.RecipientEmail,
                        Password = PasswordHelper.HashPassword("TempPassword123!"),
                        Role = UserRole.User,
                        FirstName = model.RecipientFirstName,
                        LastName = model.RecipientLastName,
                        CreatedAt = DateTime.UtcNow,
                        ApiKey = ApiKeyGenerator.GenerateApiKey()
                    };
                    _context.Users.Add(recipientUser);
                    await _context.SaveChangesAsync();
                }

                var originAddressResult = await FindOrCreateAddressAsync(
                    model.OriginStreet,
                    model.OriginCity,
                    model.OriginZipCode,
                    model.OriginCountry);

                if (!originAddressResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<PackageOperationResult>.Failure(originAddressResult.ErrorMessage!);
                }

                var destinationAddressResult = await FindOrCreateAddressAsync(
                    model.DestinationStreet,
                    model.DestinationCity,
                    model.DestinationZipCode,
                    model.DestinationCountry);

                if (!destinationAddressResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<PackageOperationResult>.Failure(destinationAddressResult.ErrorMessage!);
                }

                var initialStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.Name == "Sent");
                if (initialStatus == null)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError("Initial status 'Sent' not found in database.");
                    return ServiceResult<PackageOperationResult>.Failure("System configuration error: Initial package status not found. Please contact support.");
                }

                var courierResult = await AssignRandomCourierAsync();
                if (!courierResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to assign courier: {Error}", courierResult.ErrorMessage);
                }

                var package = new Package
                {
                    TrackingNumber = GenerateTrackingNumber(),
                    SenderUserId = senderUserId,
                    RecipientUserId = recipientUser.UserId,
                    PackageSize = model.PackageSize,
                    WeightInKg = model.WeightInKg,
                    Notes = model.Notes,
                    OriginAddressId = originAddressResult.Data!.AddressId,
                    DestinationAddressId = destinationAddressResult.Data!.AddressId,
                    SubmissionDate = DateTime.UtcNow,
                    StatusId = initialStatus.StatusId,
                    AssignedCourierId = courierResult.Data
                };
                _context.Packages.Add(package);
                await _context.SaveChangesAsync();

                package.OriginAddress = originAddressResult.Data;
                package.DestinationAddress = destinationAddressResult.Data;

                try
                {
                    var coordinatesPopulated = await _packageLocationService.PopulatePackageCoordinatesAsync(package);
                    if (coordinatesPopulated)
                    {
                        _context.Packages.Update(package);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Successfully populated coordinates for package {PackageId} from origin address", package.PackageId);
                    }
                    else
                    {
                        _logger.LogWarning("Could not populate coordinates for package {PackageId} - geocoding failed", package.PackageId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during geocoding for package {PackageId}. Package will be created without coordinates.", package.PackageId);
                }

                var packageHistory = new PackageHistory
                {
                    PackageId = package.PackageId,
                    StatusId = initialStatus.StatusId,
                    Timestamp = DateTime.UtcNow,
                    Longitude = package.Longitude,
                    Latitude = package.Latitude
                };
                _context.PackageHistories.Add(packageHistory);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Package {TrackingNumber} (ID: {PackageId}) successfully created by user {SenderUserId}",
                    package.TrackingNumber, package.PackageId, senderUserId);

                var result = new PackageOperationResult
                {
                    PackageId = package.PackageId,
                    TrackingNumber = package.TrackingNumber,
                    Message = $"Package '{package.TrackingNumber}' has been successfully submitted!",
                    OperationTimestamp = DateTime.UtcNow
                };

                return ServiceResult<PackageOperationResult>.Success(result);
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "DbUpdateException: Error saving new package for user {SenderUserId}.", senderUserId);
                return ServiceResult<PackageOperationResult>.Failure("A database error occurred while saving the package. Please ensure all details are correct and try again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "General error in SendPackageAsync for user {SenderUserId}.", senderUserId);
                return ServiceResult<PackageOperationResult>.Failure("An unexpected error occurred. Please try again.");
            }
        }

        public async Task<ServiceResult<Package>> GetPackageDetailsAsync(int packageId, int userId, UserRole userRole)
        {
            try
            {
                var package = await _context.Packages
                    .Include(p => p.SenderUser).ThenInclude(su => su.Address)
                    .Include(p => p.RecipientUser).ThenInclude(ru => ru.Address)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .Include(p => p.History).ThenInclude(ph => ph.Status)
                    .Include(p => p.AssignedCourier)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == packageId);

                if (package == null)
                {
                    return ServiceResult<Package>.NotFound("Package", packageId);
                }

                var authResult = _authorizationService.GetAuthorizationResult(package, userId, userRole);
                if (!authResult.IsAuthorized)
                {
                    _logger.LogWarning("User {UserId} with role {UserRole} denied access to package {PackageId}: {Reason}",
                        userId, userRole, package.PackageId, authResult.Reason);

                    return ServiceResult<Package>.Failure($"Access denied: {authResult.Reason}");
                }

                _logger.LogInformation("User {UserId} ({AccessType}) accessing package {PackageId} details",
                    userId, authResult.AccessType, package.PackageId);

                return ServiceResult<Package>.Success(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving package details for package {PackageId} by user {UserId}", packageId, userId);
                return ServiceResult<Package>.Failure("An error occurred while retrieving package details. Please try again.");
            }
        }

        public async Task<ServiceResult<IEnumerable<Package>>> GetPackagesForPickupAsync(int recipientUserId)
        {
            try
            {
                var packages = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.CurrentStatus)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Where(p => p.RecipientUserId == recipientUserId)
                    .OrderByDescending(p => p.SubmissionDate)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Retrieved {PackageCount} packages for pickup by user {UserId}", packages.Count, recipientUserId);
                return ServiceResult<IEnumerable<Package>>.Success(packages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pickup packages for user {UserId}", recipientUserId);
                return ServiceResult<IEnumerable<Package>>.Failure("An error occurred while loading your packages. Please try again.");
            }
        }

        public async Task<ServiceResult<PackageOperationResult>> PickUpPackageAsync(int packageId, int recipientUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var package = await _context.Packages
                    .Include(p => p.CurrentStatus)
                    .FirstOrDefaultAsync(p => p.PackageId == packageId && p.RecipientUserId == recipientUserId);

                if (package == null)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<PackageOperationResult>.NotFound("Package", packageId);
                }

                if (package.CurrentStatus?.Name != "In Delivery")
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<PackageOperationResult>.ValidationFailure("This package is not available for pickup. Only packages with 'In Delivery' status can be picked up.");
                }

                var deliveredStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.Name == "Delivered");
                if (deliveredStatus == null)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError("Delivered status not found in database for package pickup");
                    return ServiceResult<PackageOperationResult>.Failure("System configuration error: Delivered status not found. Please contact support.");
                }

                package.StatusId = deliveredStatus.StatusId;
                package.DeliveryDate = DateTime.UtcNow;

                decimal? deliveryLatitude = package.Latitude;
                decimal? deliveryLongitude = package.Longitude;

                try
                {
                    if (package.DestinationAddress == null)
                    {
                        var destinationAddress = await _context.Addresses
                            .FirstOrDefaultAsync(a => a.AddressId == package.DestinationAddressId);

                        if (destinationAddress != null)
                        {
                            package.DestinationAddress = destinationAddress;
                        }
                    }

                    if (package.DestinationAddress != null)
                    {
                        var geocodingResult = await _packageLocationService.GeocodeAddressAsync(package.DestinationAddress);
                        if (geocodingResult.IsSuccess && geocodingResult.Latitude.HasValue && geocodingResult.Longitude.HasValue)
                        {
                            deliveryLatitude = geocodingResult.Latitude.Value;
                            deliveryLongitude = geocodingResult.Longitude.Value;

                            package.Latitude = deliveryLatitude;
                            package.Longitude = deliveryLongitude;

                            _logger.LogInformation("Updated package {PackageId} with delivery coordinates: {Lat}, {Lon}",
                                package.PackageId, deliveryLatitude, deliveryLongitude);
                        }
                        else
                        {
                            _logger.LogWarning("Could not geocode destination address for package {PackageId} during delivery: {Error}",
                                package.PackageId, geocodingResult.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during destination geocoding for package {PackageId} delivery", package.PackageId);
                }

                var packageHistory = new PackageHistory
                {
                    PackageId = package.PackageId,
                    StatusId = deliveredStatus.StatusId,
                    Timestamp = DateTime.UtcNow,
                    Longitude = deliveryLongitude,
                    Latitude = deliveryLatitude
                };

                _context.PackageHistories.Add(packageHistory);
                _context.Packages.Update(package);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("User {UserId} picked up package {PackageId} ({TrackingNumber})",
                    recipientUserId, package.PackageId, package.TrackingNumber);

                var result = new PackageOperationResult
                {
                    PackageId = package.PackageId,
                    TrackingNumber = package.TrackingNumber,
                    Message = $"Package '{package.TrackingNumber}' has been successfully picked up and marked as delivered!",
                    OperationTimestamp = DateTime.UtcNow
                };

                return ServiceResult<PackageOperationResult>.Success(result);
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error while picking up package {PackageId} by user {UserId}", packageId, recipientUserId);
                return ServiceResult<PackageOperationResult>.Failure("A database error occurred while processing the pickup. Please try again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error picking up package {PackageId} by user {UserId}", packageId, recipientUserId);
                return ServiceResult<PackageOperationResult>.Failure("An unexpected error occurred while picking up the package. Please try again.");
            }
        }

        public async Task<ServiceResult<Address>> FindOrCreateAddressAsync(string street, string city, string zipCode, string country)
        {
            try
            {
                street = street?.Trim() ?? string.Empty;
                city = city?.Trim() ?? string.Empty;
                zipCode = zipCode?.Trim() ?? string.Empty;
                country = country?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(street) || string.IsNullOrEmpty(city) ||
                    string.IsNullOrEmpty(zipCode) || string.IsNullOrEmpty(country))
                {
                    return ServiceResult<Address>.ValidationFailure("All address fields (street, city, zip code, country) are required and cannot be empty.");
                }

                var existingAddress = await _context.Addresses
                    .FirstOrDefaultAsync(a =>
                        a.Street == street &&
                        a.City == city &&
                        a.ZipCode == zipCode &&
                        a.Country == country);

                if (existingAddress != null)
                {
                    return ServiceResult<Address>.Success(existingAddress);
                }

                var newAddress = new Address
                {
                    Street = street,
                    City = city,
                    ZipCode = zipCode,
                    Country = country
                };

                try
                {
                    _context.Addresses.Add(newAddress);
                    await _context.SaveChangesAsync();
                    return ServiceResult<Address>.Success(newAddress);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("Duplicate entry") == true ||
                                                ex.InnerException?.Message?.Contains("uq_address") == true)
                {
                    _context.Entry(newAddress).State = EntityState.Detached;

                    var concurrentlyCreatedAddress = await _context.Addresses
                        .FirstOrDefaultAsync(a =>
                            a.Street == street &&
                            a.City == city &&
                            a.ZipCode == zipCode &&
                            a.Country == country);

                    if (concurrentlyCreatedAddress != null)
                    {
                        return ServiceResult<Address>.Success(concurrentlyCreatedAddress);
                    }

                    _logger.LogError(ex, "Failed to handle duplicate address creation for: {Street}, {City}, {ZipCode}, {Country}",
                        street, city, zipCode, country);

                    return ServiceResult<Address>.Failure("Failed to create or retrieve address due to database constraint violation.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding or creating address: {Street}, {City}, {ZipCode}, {Country}",
                    street, city, zipCode, country);
                return ServiceResult<Address>.Failure("An error occurred while processing the address. Please try again.");
            }
        }

        public string GenerateTrackingNumber()
        {
            var prefix = "TT";
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"{prefix}{timestamp}{random}";
        }

        public async Task<ServiceResult<int?>> AssignRandomCourierAsync()
        {
            try
            {
                var couriers = await _context.Users
                    .Where(u => u.Role == UserRole.Courier)
                    .Select(u => u.UserId)
                    .ToListAsync();

                if (couriers == null || !couriers.Any())
                {
                    _logger.LogWarning("No couriers found in the database for package assignment.");
                    return ServiceResult<int?>.Success(null);
                }

                var random = new Random();
                var randomIndex = random.Next(couriers.Count);
                var selectedCourierId = couriers[randomIndex];

                _logger.LogInformation("Randomly assigned courier with ID {CourierId} to package.", selectedCourierId);
                return ServiceResult<int?>.Success(selectedCourierId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while assigning random courier to package.");
                return ServiceResult<int?>.Failure("Failed to assign courier. The package will be created without an assigned courier.");
            }
        }
    }
}