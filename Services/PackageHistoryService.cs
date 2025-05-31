using Microsoft.EntityFrameworkCore;
using _10.Data;
using _10.Models;
using _10.Models.Api;

namespace _10.Services
{
    /// <summary>
    /// Service implementation for Package History operations
    /// </summary>
    public class PackageHistoryService : IPackageHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PackageHistoryService> _logger;

        public PackageHistoryService(
            ApplicationDbContext context,
            ILogger<PackageHistoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<PackageHistoryDto>> GetAllPackageHistoryAsync()
        {
            try
            {
                var historyEntries = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .AsNoTracking()
                    .OrderByDescending(ph => ph.Timestamp)
                    .ToListAsync();

                return historyEntries.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all package history entries");
                throw;
            }
        }

        public async Task<PackageHistoryListDto?> GetPackageHistoryByPackageIdAsync(int packageId)
        {
            try
            {
                // First, verify that the package exists
                var package = await _context.Packages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == packageId);

                if (package == null)
                {
                    return null;
                }

                var historyEntries = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .Where(ph => ph.PackageId == packageId)
                    .AsNoTracking()
                    .OrderByDescending(ph => ph.Timestamp)
                    .ToListAsync();

                var response = new PackageHistoryListDto
                {
                    PackageId = packageId,
                    PackageTrackingNumber = package.TrackingNumber,
                    HistoryEntries = historyEntries.Select(MapToDto),
                    TotalEntries = historyEntries.Count
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving package history for package {PackageId}", packageId);
                throw;
            }
        }

        public async Task<ServiceResult<PackageHistoryDto>> CreatePackageHistoryAsync(CreatePackageHistoryRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate package exists
                var package = await _context.Packages.FirstOrDefaultAsync(p => p.PackageId == request.PackageId);
                if (package == null)
                {
                    return ServiceResult<PackageHistoryDto>.ValidationFailure($"Package with ID {request.PackageId} not found.");
                }

                // Validate status exists
                var status = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.StatusId == request.StatusId);
                if (status == null)
                {
                    return ServiceResult<PackageHistoryDto>.ValidationFailure($"Status with ID {request.StatusId} not found.");
                }

                // Create package history entry
                var packageHistory = new PackageHistory
                {
                    PackageId = request.PackageId,
                    StatusId = request.StatusId,
                    Timestamp = request.Timestamp ?? DateTime.UtcNow,
                    Longitude = request.Longitude,
                    Latitude = request.Latitude
                };

                _context.PackageHistories.Add(packageHistory);
                await _context.SaveChangesAsync();

                // Update package's current status and location if this is the latest entry
                var isLatestEntry = await IsLatestHistoryEntryAsync(packageHistory);
                if (isLatestEntry)
                {
                    package.StatusId = request.StatusId;
                    package.Longitude = request.Longitude;
                    package.Latitude = request.Latitude;

                    // If status is "Delivered", set delivery date
                    if (status.Name.Equals("Delivered", StringComparison.OrdinalIgnoreCase) && package.DeliveryDate == null)
                    {
                        package.DeliveryDate = packageHistory.Timestamp;
                    }

                    _context.Packages.Update(package);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Load the created package history entry with all includes for DTO mapping
                var createdHistoryEntry = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .FirstAsync(ph => ph.PackageHistoryId == packageHistory.PackageHistoryId);

                var historyDto = MapToDto(createdHistoryEntry);

                _logger.LogInformation("Created package history entry {HistoryId} for package {PackageId}",
                    packageHistory.PackageHistoryId, request.PackageId);

                return ServiceResult<PackageHistoryDto>.Success(historyDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating package history entry");
                throw;
            }
        }

        public async Task<ServiceResult<PackageHistoryDto>> UpdatePackageHistoryAsync(int id, UpdatePackageHistoryRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var historyEntry = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .FirstOrDefaultAsync(ph => ph.PackageHistoryId == id);

                if (historyEntry == null)
                {
                    return ServiceResult<PackageHistoryDto>.NotFound("Package history entry", id);
                }

                var originalStatusId = historyEntry.StatusId;

                // Validate status if provided
                StatusDefinition? newStatus = null;
                if (request.StatusId.HasValue)
                {
                    newStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.StatusId == request.StatusId.Value);
                    if (newStatus == null)
                    {
                        return ServiceResult<PackageHistoryDto>.ValidationFailure($"Status with ID {request.StatusId} not found.");
                    }
                }

                // Update history entry properties
                if (request.StatusId.HasValue)
                    historyEntry.StatusId = request.StatusId.Value;

                if (request.Timestamp.HasValue)
                    historyEntry.Timestamp = request.Timestamp.Value;

                if (request.Longitude.HasValue)
                    historyEntry.Longitude = request.Longitude.Value;

                if (request.Latitude.HasValue)
                    historyEntry.Latitude = request.Latitude.Value;

                _context.PackageHistories.Update(historyEntry);
                await _context.SaveChangesAsync();

                // Check if this is the latest entry and update package accordingly
                var isLatestEntry = await IsLatestHistoryEntryAsync(historyEntry);
                if (isLatestEntry)
                {
                    var package = historyEntry.Package;
                    package.StatusId = historyEntry.StatusId;
                    package.Longitude = historyEntry.Longitude;
                    package.Latitude = historyEntry.Latitude;

                    // Handle delivery date for "Delivered" status
                    if (newStatus != null && newStatus.Name.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                    {
                        package.DeliveryDate = historyEntry.Timestamp;
                    }
                    else if (originalStatusId != historyEntry.StatusId)
                    {
                        // If status changed from "Delivered" to something else, clear delivery date
                        var originalStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.StatusId == originalStatusId);
                        if (originalStatus != null && originalStatus.Name.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                        {
                            package.DeliveryDate = null;
                        }
                    }

                    _context.Packages.Update(package);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Load the updated package history entry with all includes for DTO mapping
                var updatedHistoryEntry = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .FirstAsync(ph => ph.PackageHistoryId == id);

                var historyDto = MapToDto(updatedHistoryEntry);

                _logger.LogInformation("Updated package history entry {HistoryId}", id);

                return ServiceResult<PackageHistoryDto>.Success(historyDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating package history entry {HistoryId}", id);
                throw;
            }
        }

        public async Task<ServiceResult<string>> DeletePackageHistoryAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var historyEntry = await _context.PackageHistories
                    .Include(ph => ph.Package)
                    .Include(ph => ph.Status)
                    .FirstOrDefaultAsync(ph => ph.PackageHistoryId == id);

                if (historyEntry == null)
                {
                    return ServiceResult<string>.NotFound("Package history entry", id);
                }

                var packageId = historyEntry.PackageId;
                var wasLatestEntry = await IsLatestHistoryEntryAsync(historyEntry);

                // Delete the history entry
                _context.PackageHistories.Remove(historyEntry);
                await _context.SaveChangesAsync();

                // If this was the latest entry, update package with the new latest entry
                if (wasLatestEntry)
                {
                    var newLatestEntry = await _context.PackageHistories
                        .Include(ph => ph.Status)
                        .Where(ph => ph.PackageId == packageId)
                        .OrderByDescending(ph => ph.Timestamp)
                        .FirstOrDefaultAsync();

                    var package = await _context.Packages.FirstOrDefaultAsync(p => p.PackageId == packageId);
                    if (package != null && newLatestEntry != null)
                    {
                        package.StatusId = newLatestEntry.StatusId;
                        package.Longitude = newLatestEntry.Longitude;
                        package.Latitude = newLatestEntry.Latitude;

                        // Update delivery date based on new latest status
                        if (newLatestEntry.Status.Name.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                        {
                            package.DeliveryDate = newLatestEntry.Timestamp;
                        }
                        else
                        {
                            package.DeliveryDate = null;
                        }

                        _context.Packages.Update(package);
                        await _context.SaveChangesAsync();
                    }
                    else if (package != null && newLatestEntry == null)
                    {
                        // No history entries left, reset to initial status
                        var initialStatus = await _context.StatusDefinitions.FirstOrDefaultAsync(s => s.Name == "Sent");
                        if (initialStatus != null)
                        {
                            package.StatusId = initialStatus.StatusId;
                            package.DeliveryDate = null;
                            package.Longitude = null;
                            package.Latitude = null;
                            _context.Packages.Update(package);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Deleted package history entry {HistoryId} for package {PackageId}", id, packageId);

                return ServiceResult<string>.Success($"Package history entry {id} deleted successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting package history entry {HistoryId}", id);
                throw;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Check if the given history entry is the latest (most recent) for its package
        /// </summary>
        /// <param name="historyEntry">The history entry to check</param>
        /// <returns>True if this is the latest entry, false otherwise</returns>
        private async Task<bool> IsLatestHistoryEntryAsync(PackageHistory historyEntry)
        {
            var latestEntry = await _context.PackageHistories
                .Where(ph => ph.PackageId == historyEntry.PackageId)
                .OrderByDescending(ph => ph.Timestamp)
                .ThenByDescending(ph => ph.PackageHistoryId) // Use ID as tiebreaker for same timestamp
                .FirstOrDefaultAsync();

            return latestEntry != null && latestEntry.PackageHistoryId == historyEntry.PackageHistoryId;
        }

        /// <summary>
        /// Map PackageHistory entity to PackageHistoryDto
        /// </summary>
        /// <param name="historyEntry">The PackageHistory entity</param>
        /// <returns>PackageHistoryDto</returns>
        private static PackageHistoryDto MapToDto(PackageHistory historyEntry)
        {
            return new PackageHistoryDto
            {
                PackageHistoryId = historyEntry.PackageHistoryId,
                PackageId = historyEntry.PackageId,
                PackageTrackingNumber = historyEntry.Package?.TrackingNumber ?? "N/A",
                StatusId = historyEntry.StatusId,
                StatusName = historyEntry.Status?.Name ?? "Unknown",
                StatusDescription = historyEntry.Status?.Description ?? "Unknown",
                Timestamp = historyEntry.Timestamp,
                Longitude = historyEntry.Longitude,
                Latitude = historyEntry.Latitude
            };
        }

        #endregion
    }
}
