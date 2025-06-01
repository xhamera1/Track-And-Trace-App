using Microsoft.EntityFrameworkCore;
using _10.Data;
using _10.Models;
using _10.Models.Api;

namespace _10.Services
{
    public class StatusDefinitionService : IStatusDefinitionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StatusDefinitionService> _logger;

        public StatusDefinitionService(
            ApplicationDbContext context,
            ILogger<StatusDefinitionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<StatusDefinitionDto>> GetAllStatusDefinitionsAsync()
        {
            try
            {
                var statusDefinitions = await _context.StatusDefinitions
                    .AsNoTracking()
                    .OrderBy(sd => sd.StatusId)
                    .ToListAsync();

                return statusDefinitions.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all status definitions");
                throw;
            }
        }

        public async Task<StatusDefinitionDto?> GetStatusDefinitionByIdAsync(int id)
        {
            try
            {
                var statusDefinition = await _context.StatusDefinitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(sd => sd.StatusId == id);

                return statusDefinition != null ? MapToDto(statusDefinition) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status definition with ID {StatusId}", id);
                throw;
            }
        }

        public async Task<ServiceResult<StatusDefinitionDto>> CreateStatusDefinitionAsync(CreateStatusDefinitionRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingStatusDefinition = await _context.StatusDefinitions
                    .FirstOrDefaultAsync(sd => sd.Name.ToLower() == request.Name.ToLower());

                if (existingStatusDefinition != null)
                {
                    _logger.LogWarning("Attempted to create status definition with duplicate name: {Name}", request.Name);
                    return ServiceResult<StatusDefinitionDto>.Conflict($"Status definition with name '{request.Name}' already exists.");
                }

                var statusDefinition = new StatusDefinition
                {
                    Name = request.Name.Trim(),
                    Description = request.Description.Trim()
                };

                _context.StatusDefinitions.Add(statusDefinition);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var statusDefinitionDto = MapToDto(statusDefinition);

                _logger.LogInformation("Created new status definition with ID {StatusId} and name '{Name}'", 
                    statusDefinition.StatusId, statusDefinition.Name);

                return ServiceResult<StatusDefinitionDto>.Success(statusDefinitionDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating status definition with name '{Name}'", request.Name);
                throw;
            }
        }

        public async Task<ServiceResult<StatusDefinitionDto>> UpdateStatusDefinitionAsync(int id, UpdateStatusDefinitionRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Find the existing status definition
                var statusDefinition = await _context.StatusDefinitions
                    .FirstOrDefaultAsync(sd => sd.StatusId == id);

                if (statusDefinition == null)
                {
                    _logger.LogWarning("Status definition with ID {StatusId} not found for update", id);
                    return ServiceResult<StatusDefinitionDto>.NotFound("Status definition", id);
                }

                // Check if another status definition with the same name already exists (excluding current one)
                var existingStatusDefinition = await _context.StatusDefinitions
                    .FirstOrDefaultAsync(sd => sd.StatusId != id && sd.Name.ToLower() == request.Name.ToLower());

                if (existingStatusDefinition != null)
                {
                    _logger.LogWarning("Attempted to update status definition {StatusId} with duplicate name: {Name}", id, request.Name);
                    return ServiceResult<StatusDefinitionDto>.Conflict($"Another status definition with name '{request.Name}' already exists.");
                }

                // Update the status definition
                statusDefinition.Name = request.Name.Trim();
                statusDefinition.Description = request.Description.Trim();

                _context.StatusDefinitions.Update(statusDefinition);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var statusDefinitionDto = MapToDto(statusDefinition);

                _logger.LogInformation("Updated status definition {StatusId} with name '{Name}'", id, statusDefinition.Name);

                return ServiceResult<StatusDefinitionDto>.Success(statusDefinitionDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating status definition {StatusId}", id);
                throw;
            }
        }

        public async Task<ServiceResult<string>> DeleteStatusDefinitionAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var statusDefinition = await _context.StatusDefinitions
                    .FirstOrDefaultAsync(sd => sd.StatusId == id);

                if (statusDefinition == null)
                {
                    _logger.LogWarning("Status definition with ID {StatusId} not found for deletion", id);
                    return ServiceResult<string>.NotFound("Status definition", id);
                }

                var packagesUsingStatus = await _context.Packages
                    .AnyAsync(p => p.StatusId == id);

                if (packagesUsingStatus)
                {
                    _logger.LogWarning("Attempted to delete status definition {StatusId} '{Name}' that is in use by packages", id, statusDefinition.Name);
                    return ServiceResult<string>.Conflict($"Cannot delete status definition '{statusDefinition.Name}' because it is currently used by one or more packages.");
                }

                var packageHistoriesUsingStatus = await _context.PackageHistories
                    .AnyAsync(ph => ph.StatusId == id);

                if (packageHistoriesUsingStatus)
                {
                    _logger.LogWarning("Attempted to delete status definition {StatusId} '{Name}' that is in use by package histories", id, statusDefinition.Name);
                    return ServiceResult<string>.Conflict($"Cannot delete status definition '{statusDefinition.Name}' because it is referenced in package history records.");
                }

                _context.StatusDefinitions.Remove(statusDefinition);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted status definition {StatusId} with name '{Name}'", id, statusDefinition.Name);

                return ServiceResult<string>.Success($"Status definition '{statusDefinition.Name}' has been successfully deleted.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting status definition {StatusId}", id);
                throw;
            }
        }
        private static StatusDefinitionDto MapToDto(StatusDefinition statusDefinition)
        {
            return new StatusDefinitionDto
            {
                StatusId = statusDefinition.StatusId,
                Name = statusDefinition.Name,
                Description = statusDefinition.Description
            };
        }
    }
}
