
using _10.Data;
using _10.Models;      
using _10.Models.Api; 
using _10.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace _10.Controllers.Api
{
    [Route("api/addresses")]
    [ApiController]
    public class AddressesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AddressesApiController> _logger;

        public AddressesApiController(ApplicationDbContext context, ILogger<AddressesApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [ApiAdminAuthorize]
        public async Task<ActionResult<IEnumerable<AddressResponseDto>>> GetAddresses()
        {
            return await _context.Addresses
                .AsNoTracking()
                .Select(a => new AddressResponseDto 
                {
                    AddressId = a.AddressId,
                    Street = a.Street,
                    City = a.City,
                    ZipCode = a.ZipCode,
                    Country = a.Country
                })
                .ToListAsync();
        }

        [HttpGet("{id:int}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<AddressResponseDto>> GetAddress(int id)
        {
            var address = await _context.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AddressId == id);

            if (address == null)
            {
                return NotFound(new { message = $"Address with ID {id} not found." });
            }

            return new AddressResponseDto
            {
                AddressId = address.AddressId,
                Street = address.Street,
                City = address.City,
                ZipCode = address.ZipCode,
                Country = address.Country
            };
        }

        // POST: api/addresses
        [HttpPost]
        [ApiAdminAuthorize]
        public async Task<ActionResult<AddressResponseDto>> CreateAddress(CreateAddressRequestDto createAddressDto)
        {
            var existingAddress = await _context.Addresses.FirstOrDefaultAsync(a =>
                a.Street == createAddressDto.Street &&
                a.City == createAddressDto.City &&
                a.ZipCode == createAddressDto.ZipCode &&
                a.Country == createAddressDto.Country);

            if (existingAddress != null)
            {
                return Conflict(new { message = "This address already exists.", addressId = existingAddress.AddressId });
            }

            var addressEntity = new Address 
            {
                Street = createAddressDto.Street,
                City = createAddressDto.City,
                ZipCode = createAddressDto.ZipCode,
                Country = createAddressDto.Country
            };

            _context.Addresses.Add(addressEntity);
            await _context.SaveChangesAsync();

            var addressResponseDto = new AddressResponseDto
            {
                AddressId = addressEntity.AddressId,
                Street = addressEntity.Street,
                City = addressEntity.City,
                ZipCode = addressEntity.ZipCode,
                Country = addressEntity.Country
            };

            return CreatedAtAction(nameof(GetAddress), new { id = addressEntity.AddressId }, addressResponseDto);
        }

        // PUT: api/addresses/{id}
        [HttpPut("{id:int}")]
        [ApiAdminAuthorize]
        public async Task<IActionResult> UpdateAddress(int id, UpdateAddressRequestDto updateAddressDto)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
            {
                return NotFound(new { message = $"Address with ID {id} not found." });
            }

            string newStreet = updateAddressDto.Street ?? address.Street;
            string newCity = updateAddressDto.City ?? address.City;
            string newZipCode = updateAddressDto.ZipCode ?? address.ZipCode;
            string newCountry = updateAddressDto.Country ?? address.Country;

            if ((updateAddressDto.Street != null && updateAddressDto.Street != address.Street) ||
                (updateAddressDto.City != null && updateAddressDto.City != address.City) ||
                (updateAddressDto.ZipCode != null && updateAddressDto.ZipCode != address.ZipCode) ||
                (updateAddressDto.Country != null && updateAddressDto.Country != address.Country))
            {
                var potentialDuplicate = await _context.Addresses.FirstOrDefaultAsync(a =>
                    a.Street == newStreet &&
                    a.City == newCity &&
                    a.ZipCode == newZipCode &&
                    a.Country == newCountry &&
                    a.AddressId != id);

                if (potentialDuplicate != null)
                {
                    return Conflict(new { message = "An address with the updated details already exists.", addressId = potentialDuplicate.AddressId });
                }
            }

            address.Street = newStreet;
            address.City = newCity;
            address.ZipCode = newZipCode;
            address.Country = newCountry;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Addresses.Any(e => e.AddressId == id)) return NotFound(); else throw;
            }
            return NoContent();
        }

        // DELETE: api/addresses/{id} 
         [HttpDelete("{id:int}")]
        [ApiAdminAuthorize]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
            {
                return NotFound(new { message = $"Address with ID {id} not found." });
            }

            bool isUsedByUser = await _context.Users.AnyAsync(u => u.AddressId == id);
            bool isUsedByPackageOrigin = await _context.Packages.AnyAsync(p => p.OriginAddressId == id);
            bool isUsedByPackageDestination = await _context.Packages.AnyAsync(p => p.DestinationAddressId == id);

            if (isUsedByUser || isUsedByPackageOrigin || isUsedByPackageDestination)
            {
                return Conflict(new { message = $"Address with ID {id} is currently in use and cannot be deleted." });
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Address with ID {AddressId} deleted by admin.", id);
            return NoContent();
        }
    }
}