using _10.Data;
using _10.Models;
using Microsoft.EntityFrameworkCore;
using System; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _10.Services
{
    public class CourierService : ICourierService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourierService> _logger; 

        public CourierService(ApplicationDbContext dbContext, ILogger<CourierService> logger) 
        {
            _context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async Task<IEnumerable<Package>> GetPackagesByCourierAndStatusAsync(int courierId, List<string>? targetStatusNames = null, bool excludeTargetStatuses = false)
        {
            var query = _context.Packages
                                .Where(p => p.AssignedCourierId == courierId);

            if (targetStatusNames != null && targetStatusNames.Any())
            {
                if (excludeTargetStatuses)
                {
                    query = query.Where(p => p.CurrentStatus != null && !targetStatusNames.Contains(p.CurrentStatus.Name));
                }
                else
                {
                    query = query.Where(p => p.CurrentStatus != null && targetStatusNames.Contains(p.CurrentStatus.Name));
                }
            }
        
            query = query
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus) 
                    .OrderByDescending(p => p.SubmissionDate);

            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Package>> GetAllAssignedPackagesAsync(int courierId)
        {
            return await GetPackagesByCourierAndStatusAsync(courierId, null);
        }

        public async Task<IEnumerable<Package>> GetActivePackagesAsync(int courierId)
        {
            var terminalStatuses = new List<string> { "Delivered"}; 
            return await GetPackagesByCourierAndStatusAsync(courierId, terminalStatuses, true);
        }

        public async Task<IEnumerable<Package>> GetDeliveredPackagesAsync(int courierId)
        {
            var deliveredStatus = new List<string> { "Delivered" }; 
            return await GetPackagesByCourierAndStatusAsync(courierId, deliveredStatus, false);
        }
        
        public async Task<Package?> GetPackageDetailsForCourierAsync(int packageId, int courierId)
        {
            var query = _context.Packages
                .Where(p => p.AssignedCourierId == courierId && p.PackageId == packageId);

            query = query
                .Include(p => p.SenderUser)        
                .Include(p => p.RecipientUser)     
                .Include(p => p.OriginAddress)      
                .Include(p => p.DestinationAddress) 
                .Include(p => p.CurrentStatus)      
                .Include(p => p.History)         
                    .ThenInclude(h => h.Status);   

            return await query.AsNoTracking().FirstOrDefaultAsync();
        }
        
        public async Task<(bool Success, string? ErrorMessage)> UpdatePackageStatusAndLocationAsync(
            int packageId, int courierId, int newStatusId, decimal? longitude, decimal? latitude, string? notes)
        {
            decimal? decimalLongitude = longitude.HasValue ? (decimal)longitude.Value : (decimal?)null;
            decimal? decimalLatitude = latitude.HasValue ? (decimal)latitude.Value : (decimal?)null;

            var package = await _context.Packages
                .FirstOrDefaultAsync(p => p.PackageId == packageId && p.AssignedCourierId == courierId);

            if (package == null)
            {
                return (false, "Package not found or not assigned to this courier.");
            }

            var newStatus = await _context.StatusDefinitions.FindAsync(newStatusId);
            if (newStatus == null)
            {
                return (false, "Invalid new status selected.");
            }
            
            if (package.CurrentStatus?.Name == "Delivered" && newStatus.Name != "Delivered") {
                 return (false, "Cannot change the status of a package that has already been delivered.");
            }

            bool statusChanged = package.StatusId != newStatusId;
            bool locationChanged = package.Longitude != decimalLongitude || package.Latitude != decimalLatitude;
            bool notesChanged = package.Notes != notes;

            package.StatusId = newStatusId;
            package.Latitude = decimalLatitude;
            package.Longitude = decimalLongitude;

            if (newStatus.Name == "Delivered" && package.DeliveryDate == null) 
            {
                package.DeliveryDate = DateTime.UtcNow; 
            }

        
            if (statusChanged || locationChanged || notesChanged || 
                (newStatus.Name == "In Delivery" && (await _context.StatusDefinitions.FindAsync(package.StatusId))?.Name == "In Delivery"))
            {
                var packageHistoryEntry = new PackageHistory
                {
                    PackageId = packageId,
                    StatusId = newStatusId,
                    Timestamp = DateTime.UtcNow, 
                    Longitude = decimalLongitude,
                    Latitude = decimalLatitude
                };
                _context.PackageHistories.Add(packageHistoryEntry);
            }
            
            if (notesChanged) 
            {
                 package.Notes = notes;
            }


            try
            {
                _context.Packages.Update(package); 
                await _context.SaveChangesAsync();
                return (true, null); // Sukces
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "DbUpdateConcurrencyException while trying to update package {PackageId} by courier {CourierId}", packageId, courierId);
                return (false, "A concurrency conflict occurred. The data may have been modified by another user. Please refresh and try again.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException while trying to update package {PackageId} by courier {CourierId}", packageId, courierId);
                return (false, "A database error occurred while saving changes.");
            }
             catch (Exception ex) 
            {
                _logger.LogError(ex, "Unexpected error while trying to update package {PackageId} by courier {CourierId}", packageId, courierId);
                return (false, "An unexpected server error occurred.");
            }
        }
    }
}