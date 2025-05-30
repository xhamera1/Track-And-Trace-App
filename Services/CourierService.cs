using _10.Data;
using _10.Models;
using Microsoft.EntityFrameworkCore;

namespace _10.Services
{
    public class CourierService : ICourierService
    {

        private readonly ApplicationDbContext _context;

        public CourierService(ApplicationDbContext dbContext)
        {
            this._context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IEnumerable<Package>> GetAssignedPackagesAsync(int courierId, bool includeDelivered = false)
        {
            var query = _context.Packages
                                .Where(p => p.AssignedCourierId == courierId);

            if (includeDelivered == true)
            {
                query = query.Where(p => p.DeliveryDate == null);
            }

            query = query
                    .Include(p => p.PackageId)
                    .Include(p => p.TrackingNumber)
                    .Include(p => p.SenderUserId)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.AssignedCourierId)
                    .Include(p => p.PackageSize)
                    .Include(p => p.WeightInKg)
                    .Include(p => p.Notes)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.SubmissionDate)
                    .Include(p => p.DeliveryDate)
                    .Include(p => p.StatusId)
                    .Include(p => p.Longitude)
                    .Include(p => p.Latitude);

            return await query.ToListAsync();
            
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

            return await query.FirstOrDefaultAsync();
        }
        
        
        
        
        
        public async Task<bool> UpdatePackageStatusAndLocationAsync(int packageId, int courierId, int newStatusId, decimal? longitude, decimal? latitude, string? notes)
        {
            var package = await _context.Packages
                .Include(p => p.CurrentStatus)
                .FirstOrDefaultAsync(p => p.PackageId == packageId && p.AssignedCourierId == courierId);

            if (package == null)
            {
                return false;
            }

            var newStatus = await _context.StatusDefinitions.FindAsync(newStatusId);
            if (newStatus == null)
            {
                return false;
            }

            package.StatusId = newStatusId;
            package.Latitude = latitude;
            package.Longitude = longitude;

            if (newStatus.Name == "Delivered")
            {
                package.DeliveryDate = DateTime.Now;
            }

            var packageHistoryEntry = new PackageHistory
            {
                PackageId = packageId,
                StatusId = newStatusId,
                Timestamp = DateTime.Now,
                Longitude = longitude,
                Latitude = latitude
            };

            _context.PackageHistories.Add(packageHistoryEntry);

            if (!string.IsNullOrEmpty(notes))
            {
                package.Notes = notes;
            }

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.Write("DbUpdateConcurrencyException while trying to update package as courier");
                return false;
            }
            catch (DbUpdateException)
            {
                Console.Write("DbUpdateException while trying to update package as courier");
                return false;
            }
        }
        
        
    }
}