using Microsoft.EntityFrameworkCore;
using _10.Data;
using _10.Models;
using _10.Models.Api;

namespace _10.Services
{
    /// <summary>
    /// Service implementation for Package operations
    /// </summary>
    public class PackageService : IPackageService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PackageService> _logger;

        public PackageService(
            ApplicationDbContext context,
            ILogger<PackageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<PackageDto>> GetAllPackagesAsync()
        {
            try
            {
                var packages = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.AssignedCourier)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .AsNoTracking()
                    .ToListAsync();

                return packages.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all packages");
                throw;
            }
        }

        public async Task<PackageDto?> GetPackageByIdAsync(int id)
        {
            try
            {
                var package = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.AssignedCourier)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == id);

                return package != null ? MapToDto(package) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving package {PackageId}", id);
                throw;
            }
        }

        public async Task<ServiceResult<PackageDto>> CreatePackageAsync(CreatePackageRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate sender exists
                var sender = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.SenderUserId);
                if (sender == null)
                {
                    return ServiceResult<PackageDto>.ValidationFailure($"Sender with ID {request.SenderUserId} not found.");
                }

                // Validate recipient exists
                var recipient = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.RecipientUserId);
                if (recipient == null)
                {
                    return ServiceResult<PackageDto>.ValidationFailure($"Recipient with ID {request.RecipientUserId} not found.");
                }

                // Validate assigned courier if provided
                User? assignedCourier = null;
                if (request.AssignedCourierId.HasValue)
                {
                    assignedCourier = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.AssignedCourierId.Value && u.Role == UserRole.Courier);
                    if (assignedCourier == null)
                    {
                        return ServiceResult<PackageDto>.ValidationFailure($"Courier with ID {request.AssignedCourierId} not found or is not a courier.");
                    }
                }

                // Find or create addresses
                var originAddress = await FindOrCreateAddressAsync(request.OriginAddress);
                var destinationAddress = await FindOrCreateAddressAsync(request.DestinationAddress);

                // Get initial status
                var initialStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.Name == "Sent");
                if (initialStatus == null)
                {
                    return ServiceResult<PackageDto>.ValidationFailure("Initial package status 'Sent' not found in system.");
                }

                // Create package
                var package = new Package
                {
                    TrackingNumber = GenerateTrackingNumber(),
                    SenderUserId = request.SenderUserId,
                    RecipientUserId = request.RecipientUserId,
                    AssignedCourierId = request.AssignedCourierId,
                    PackageSize = request.PackageSize,
                    WeightInKg = request.WeightInKg,
                    Notes = request.Notes,
                    OriginAddressId = originAddress.AddressId,
                    DestinationAddressId = destinationAddress.AddressId,
                    SubmissionDate = DateTime.UtcNow,
                    StatusId = initialStatus.StatusId,
                    Longitude = request.Longitude,
                    Latitude = request.Latitude
                };

                _context.Packages.Add(package);
                await _context.SaveChangesAsync();

                // Create initial package history entry
                var packageHistory = new PackageHistory
                {
                    PackageId = package.PackageId,
                    StatusId = initialStatus.StatusId,
                    Timestamp = DateTime.UtcNow,
                    Longitude = request.Longitude,
                    Latitude = request.Latitude
                };
                _context.PackageHistories.Add(packageHistory);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Load the created package with all includes for DTO mapping
                var createdPackage = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.AssignedCourier)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .FirstAsync(p => p.PackageId == package.PackageId);

                var packageDto = MapToDto(createdPackage);

                _logger.LogInformation("Created package {PackageId} with tracking number {TrackingNumber}",
                    package.PackageId, package.TrackingNumber);

                return ServiceResult<PackageDto>.Success(packageDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating package");
                throw;
            }
        }

        public async Task<ServiceResult<PackageDto>> UpdatePackageAsync(int id, UpdatePackageRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var package = await _context.Packages
                    .Include(p => p.CurrentStatus)
                    .FirstOrDefaultAsync(p => p.PackageId == id);

                if (package == null)
                {
                    return ServiceResult<PackageDto>.NotFound("Package", id);
                }

                // Validate assigned courier if provided
                if (request.AssignedCourierId.HasValue)
                {
                    var courier = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.AssignedCourierId.Value && u.Role == UserRole.Courier);
                    if (courier == null)
                    {
                        return ServiceResult<PackageDto>.ValidationFailure($"Courier with ID {request.AssignedCourierId} not found or is not a courier.");
                    }
                }

                // Validate status if provided
                StatusDefinition? newStatus = null;
                if (request.StatusId.HasValue)
                {
                    newStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.StatusId == request.StatusId.Value);
                    if (newStatus == null)
                    {
                        return ServiceResult<PackageDto>.ValidationFailure($"Status with ID {request.StatusId} not found.");
                    }
                }

                // Update package properties
                if (!string.IsNullOrEmpty(request.Notes))
                    package.Notes = request.Notes;

                if (request.AssignedCourierId.HasValue)
                    package.AssignedCourierId = request.AssignedCourierId.Value;

                if (request.WeightInKg.HasValue)
                    package.WeightInKg = request.WeightInKg.Value;

                if (request.Longitude.HasValue)
                    package.Longitude = request.Longitude.Value;

                if (request.Latitude.HasValue)
                    package.Latitude = request.Latitude.Value;

                if (request.StatusId.HasValue)
                {
                    package.StatusId = request.StatusId.Value;

                    // If status is being updated to "Delivered", set delivery date
                    if (newStatus!.Name.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                    {
                        package.DeliveryDate = DateTime.UtcNow;
                    }

                    // Create package history entry for status change
                    var packageHistory = new PackageHistory
                    {
                        PackageId = package.PackageId,
                        StatusId = request.StatusId.Value,
                        Timestamp = DateTime.UtcNow,
                        Longitude = request.Longitude,
                        Latitude = request.Latitude
                    };
                    _context.PackageHistories.Add(packageHistory);
                }

                _context.Packages.Update(package);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Load the updated package with all includes for DTO mapping
                var updatedPackage = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.AssignedCourier)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .FirstAsync(p => p.PackageId == id);

                var packageDto = MapToDto(updatedPackage);

                _logger.LogInformation("Updated package {PackageId}", id);

                return ServiceResult<PackageDto>.Success(packageDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating package {PackageId}", id);
                throw;
            }
        }

        public async Task<ServiceResult<string>> DeletePackageAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var package = await _context.Packages.FirstOrDefaultAsync(p => p.PackageId == id);
                if (package == null)
                {
                    return ServiceResult<string>.NotFound("Package", id);
                }

                // Delete associated package history records first (due to foreign key constraint)
                var packageHistories = await _context.PackageHistories
                    .Where(ph => ph.PackageId == id)
                    .ToListAsync();

                _context.PackageHistories.RemoveRange(packageHistories);

                // Delete the package
                _context.Packages.Remove(package);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted package {PackageId} with tracking number {TrackingNumber}",
                    id, package.TrackingNumber);

                return ServiceResult<string>.Success($"Package {package.TrackingNumber} deleted successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting package {PackageId}", id);
                throw;
            }
        }

        #region Private Helper Methods

        private async Task<Address> FindOrCreateAddressAsync(AddressDto addressDto)
        {
            var existingAddress = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Street == addressDto.Street &&
                                        a.City == addressDto.City &&
                                        a.ZipCode == addressDto.ZipCode &&
                                        a.Country == addressDto.Country);

            if (existingAddress != null)
            {
                return existingAddress;
            }

            var newAddress = new Address
            {
                Street = addressDto.Street,
                City = addressDto.City,
                ZipCode = addressDto.ZipCode,
                Country = addressDto.Country
            };

            _context.Addresses.Add(newAddress);
            await _context.SaveChangesAsync();
            return newAddress;
        }

        private static string GenerateTrackingNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = Random.Shared.Next(1000, 9999);
            return $"TT{timestamp}{random}";
        }

        private static PackageDto MapToDto(Package package)
        {
            return new PackageDto
            {
                PackageId = package.PackageId,
                TrackingNumber = package.TrackingNumber,
                SenderUserId = package.SenderUserId,
                SenderUsername = package.SenderUser?.Username ?? "N/A",
                RecipientUserId = package.RecipientUserId,
                RecipientUsername = package.RecipientUser?.Username ?? "N/A",
                AssignedCourierId = package.AssignedCourierId,
                AssignedCourierUsername = package.AssignedCourier?.Username,
                PackageSize = package.PackageSize,
                WeightInKg = package.WeightInKg,
                Notes = package.Notes,
                OriginAddress = package.OriginAddress != null ? new AddressDto
                {
                    Street = package.OriginAddress.Street,
                    City = package.OriginAddress.City,
                    ZipCode = package.OriginAddress.ZipCode,
                    Country = package.OriginAddress.Country
                } : null,
                DestinationAddress = package.DestinationAddress != null ? new AddressDto
                {
                    Street = package.DestinationAddress.Street,
                    City = package.DestinationAddress.City,
                    ZipCode = package.DestinationAddress.ZipCode,
                    Country = package.DestinationAddress.Country
                } : null,
                SubmissionDate = package.SubmissionDate,
                DeliveryDate = package.DeliveryDate,
                StatusId = package.StatusId,
                StatusName = package.CurrentStatus?.Name ?? "Unknown",
                Longitude = package.Longitude,
                Latitude = package.Latitude
            };
        }

        #endregion
    }
}
