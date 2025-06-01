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
    [Route("api/users")]
    [ApiController]
    public class UsersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersApiController> _logger;

        public UsersApiController(ApplicationDbContext context, ILogger<UsersApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Address)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }

            return Ok(new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                Birthday = user.Birthday,
                CreatedAt = user.CreatedAt,
                ApiKey = user.ApiKey,
                Address = user.Address == null ? null : new _10.Models.Api.AddressDto
                {
                    Street = user.Address.Street,
                    City = user.Address.City,
                    ZipCode = user.Address.ZipCode,
                    Country = user.Address.Country
                }
            });
        }

        [HttpPost]
        [ApiAdminAuthorize]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequestDto createUserDto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == createUserDto.Username))
            {
                return Conflict(new { message = $"Username '{createUserDto.Username}' already exists." });
            }
            if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
            {
                return Conflict(new { message = $"Email '{createUserDto.Email}' is already registered." });
            }

            Address? newAddressEntity = null;
            if (!string.IsNullOrWhiteSpace(createUserDto.Street) &&
                !string.IsNullOrWhiteSpace(createUserDto.City) &&
                !string.IsNullOrWhiteSpace(createUserDto.ZipCode) &&
                !string.IsNullOrWhiteSpace(createUserDto.Country))
            {
                newAddressEntity = await _context.Addresses.FirstOrDefaultAsync(a =>
                    a.Street == createUserDto.Street &&
                    a.City == createUserDto.City &&
                    a.ZipCode == createUserDto.ZipCode &&
                    a.Country == createUserDto.Country);

                if (newAddressEntity == null)
                {
                    newAddressEntity = new Address
                    {
                        Street = createUserDto.Street,
                        City = createUserDto.City,
                        ZipCode = createUserDto.ZipCode,
                        Country = createUserDto.Country
                    };
                    _context.Addresses.Add(newAddressEntity);
                }
            }

            var user = new User
            {
                Username = createUserDto.Username,
                Email = createUserDto.Email,
                Password = PasswordHelper.HashPassword(createUserDto.Password),
                ApiKey = ApiKeyGenerator.GenerateApiKey(),
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Role = createUserDto.Role,
                Birthday = createUserDto.Birthday,
                Address = newAddressEntity,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                Birthday = user.Birthday,
                CreatedAt = user.CreatedAt,
                Address = newAddressEntity == null ? null : new _10.Models.Api.AddressDto
                {
                    Street = newAddressEntity.Street,
                    City = newAddressEntity.City,
                    ZipCode = newAddressEntity.ZipCode,
                    Country = newAddressEntity.Country
                }
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userDto);
        }

        [HttpPut("{id:int}")]
        [ApiAdminAuthorize]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserRequestDto updateUserDto)
        {
            var user = await _context.Users.Include(u => u.Address).FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }

            if (!string.IsNullOrWhiteSpace(updateUserDto.Street) ||
                !string.IsNullOrWhiteSpace(updateUserDto.City) ||
                !string.IsNullOrWhiteSpace(updateUserDto.ZipCode) ||
                !string.IsNullOrWhiteSpace(updateUserDto.Country))
            {
                string street = updateUserDto.Street ?? user.Address?.Street ?? string.Empty;
                string city = updateUserDto.City ?? user.Address?.City ?? string.Empty;
                string zipCode = updateUserDto.ZipCode ?? user.Address?.ZipCode ?? string.Empty;
                string country = updateUserDto.Country ?? user.Address?.Country ?? string.Empty;

                if (!string.IsNullOrEmpty(street) && !string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(zipCode) && !string.IsNullOrEmpty(country))
                {
                    Address? updatedAddressEntity = await _context.Addresses.FirstOrDefaultAsync(a =>
                        a.Street == street &&
                        a.City == city &&
                        a.ZipCode == zipCode &&
                        a.Country == country);

                    if (updatedAddressEntity == null)
                    {
                        updatedAddressEntity = new Address
                        {
                            Street = street,
                            City = city,
                            ZipCode = zipCode,
                            Country = country
                        };
                        _context.Addresses.Add(updatedAddressEntity);
                    }
                    user.Address = updatedAddressEntity;
                }
                else if (user.Address != null && (updateUserDto.Street != null || updateUserDto.City != null || updateUserDto.ZipCode != null || updateUserDto.Country != null))
                {
                        _logger.LogWarning("Partial address update attempted for user {UserId}. All address fields are required to change an address via this DTO.", id);
                }
            }


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.UserId == id)) return NotFound(); else throw;
            }
            return NoContent();
        }

    }
}