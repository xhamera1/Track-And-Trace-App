using _10.Data;
using _10.Models;
using _10.Models.Api;
using _10.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        private User? GetAuthenticatedUserFromContext()
        {
            var authUser = HttpContext.Items["ApiUser"] as User;
            if (authUser == null)
            {
                _logger.LogError("Authenticated user not found in HttpContext. Ensure ApiAdminAuthorizeAttribute populates HttpContext.Items[\"ApiUser\"].");
            }
            return authUser;
        }

        private string GetAuthUsernameForLogging() => GetAuthenticatedUserFromContext()?.Username ?? "UnknownUser";

        [HttpGet]
        [ApiAdminAuthorize]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var adminUsername = GetAuthUsernameForLogging();
            _logger.LogInformation("Admin user {AdminUsername} attempting to retrieve all users.", adminUsername);

            try
            {
                var users = await _context.Users
                    .Include(u => u.Address)
                    .AsNoTracking()
                    .ToListAsync();

                var userDtos = users.Select(user => new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(), 
                    Birthday = user.Birthday,
                    CreatedAt = user.CreatedAt,
                    ApiKey = null, 
                    Address = user.Address == null ? null : new _10.Models.Api.AddressDto //
                    {
                        Street = user.Address.Street,
                        City = user.Address.City,
                        ZipCode = user.Address.ZipCode,
                        Country = user.Address.Country
                    }
                }).ToList();

                _logger.LogInformation("Admin user {AdminUsername} retrieved {Count} users successfully.", adminUsername, userDtos.Count);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin user {AdminUsername} encountered an error retrieving all users.", adminUsername);
                return StatusCode(500, new { message = "An internal server error occurred while retrieving users." });
            }
        }


        [HttpGet("{id:int}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var adminUsername = GetAuthUsernameForLogging();
            _logger.LogInformation("Admin user {AdminUsername} attempting to retrieve user with ID {UserId}.", adminUsername, id);

            try
            {
                var user = await _context.Users
                    .Include(u => u.Address)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == id); 

                if (user == null)
                {
                    _logger.LogWarning("Admin user {AdminUsername} failed to find user with ID {UserId}.", adminUsername, id);
                    return NotFound(new { message = $"User with ID {id} not found." });
                }

                var userDto = new UserDto //
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
                    Address = user.Address == null ? null : new _10.Models.Api.AddressDto //
                    {
                        Street = user.Address.Street,
                        City = user.Address.City,
                        ZipCode = user.Address.ZipCode,
                        Country = user.Address.Country
                    }
                };
                _logger.LogInformation("Admin user {AdminUsername} retrieved user {UserId} successfully.", adminUsername, id);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin user {AdminUsername} encountered an error retrieving user {UserId}.", adminUsername, id);
                return StatusCode(500, new { message = "An internal server error occurred while retrieving the user." });
            }
        }

        [HttpPost]
        [ApiAdminAuthorize]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequestDto createUserDto) //
        {
            var adminUsername = GetAuthUsernameForLogging();
            _logger.LogInformation("Admin user {AdminUsername} attempting to create a new user with username {NewUsername}.", adminUsername, createUserDto.Username);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == createUserDto.Username)) //
                {
                    _logger.LogWarning("Admin user {AdminUsername} failed to create user. Username {NewUsername} already exists.", adminUsername, createUserDto.Username);
                    return Conflict(new { message = $"Username '{createUserDto.Username}' already exists." });
                }
                if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email)) //
                {
                    _logger.LogWarning("Admin user {AdminUsername} failed to create user. Email {NewEmail} already registered.", adminUsername, createUserDto.Email);
                    return Conflict(new { message = $"Email '{createUserDto.Email}' is already registered." });
                }

                Address? newAddressEntity = null; //
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
                _logger.LogInformation("Admin user {AdminUsername} created new user {NewUsername} with ID {NewUserId} successfully.", adminUsername, user.Username, user.UserId);

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
                    ApiKey = null, 
                    Address = newAddressEntity == null ? null : new _10.Models.Api.AddressDto //
                    {
                        Street = newAddressEntity.Street,
                        City = newAddressEntity.City,
                        ZipCode = newAddressEntity.ZipCode,
                        Country = newAddressEntity.Country
                    }
                };

                return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin user {AdminUsername} encountered an error creating user {NewUsername}.", adminUsername, createUserDto.Username);
                return StatusCode(500, new { message = "An internal server error occurred while creating the user." });
            }
        }


        [HttpPut("{id:int}")]
        [ApiAdminAuthorize]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, UpdateUserRequestDto updateUserDto) 
        {
            var adminUsername = GetAuthUsernameForLogging();
            _logger.LogInformation("Admin user {AdminUsername} attempting to update user with ID {UserId}.", adminUsername, id);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _context.Users.Include(u => u.Address).FirstOrDefaultAsync(u => u.UserId == id);
                if (user == null)
                {
                    _logger.LogWarning("Admin user {AdminUsername} failed to update user. User with ID {UserId} not found.", adminUsername, id);
                    return NotFound(new { message = $"User with ID {id} not found." });
                }

                if (updateUserDto.Email != null && user.Email != updateUserDto.Email)
                {
                    if (await _context.Users.AnyAsync(u => u.UserId != id && u.Email == updateUserDto.Email))
                    {
                        _logger.LogWarning("Admin user {AdminUsername} failed to update user {UserId}. Email {UpdatedEmail} already registered.", adminUsername, id, updateUserDto.Email);
                        return Conflict(new { message = $"Email '{updateUserDto.Email}' is already registered by another user." });
                    }
                    user.Email = updateUserDto.Email;
                }

                if (updateUserDto.FirstName != null || HttpContext.Request.Form.ContainsKey(nameof(UpdateUserRequestDto.FirstName)) || (HttpContext.Request.ContentType?.Contains("application/json") == true && updateUserDto.GetType().GetProperty(nameof(UpdateUserRequestDto.FirstName)) != null)) // A more robust check if explicit null is sent
                {

                     user.FirstName = updateUserDto.FirstName;
                }
                if (updateUserDto.LastName != null || HttpContext.Request.Form.ContainsKey(nameof(UpdateUserRequestDto.LastName)) || (HttpContext.Request.ContentType?.Contains("application/json") == true && updateUserDto.GetType().GetProperty(nameof(UpdateUserRequestDto.LastName)) != null) )
                {
                     user.LastName = updateUserDto.LastName;
                }


                if (updateUserDto.Role.HasValue)
                {
                    user.Role = updateUserDto.Role.Value;
                }
                user.Birthday = updateUserDto.Birthday;


                if (!string.IsNullOrWhiteSpace(updateUserDto.NewPassword))
                {
                    user.Password = PasswordHelper.HashPassword(updateUserDto.NewPassword); 
                }

                bool hasAddressInfoInDto = !string.IsNullOrWhiteSpace(updateUserDto.Street) ||
                                           !string.IsNullOrWhiteSpace(updateUserDto.City) ||
                                           !string.IsNullOrWhiteSpace(updateUserDto.ZipCode) ||
                                           !string.IsNullOrWhiteSpace(updateUserDto.Country);

                if (hasAddressInfoInDto)
                {
                    if (!string.IsNullOrWhiteSpace(updateUserDto.Street) &&
                        !string.IsNullOrWhiteSpace(updateUserDto.City) &&
                        !string.IsNullOrWhiteSpace(updateUserDto.ZipCode) &&
                        !string.IsNullOrWhiteSpace(updateUserDto.Country))
                    {
                        Address? updatedAddressEntity = await _context.Addresses.FirstOrDefaultAsync(a =>
                            a.Street == updateUserDto.Street &&
                            a.City == updateUserDto.City &&
                            a.ZipCode == updateUserDto.ZipCode &&
                            a.Country == updateUserDto.Country);

                        if (updatedAddressEntity == null)
                        {
                            updatedAddressEntity = new Address
                            {
                                Street = updateUserDto.Street,
                                City = updateUserDto.City,
                                ZipCode = updateUserDto.ZipCode,
                                Country = updateUserDto.Country
                            };
                            _context.Addresses.Add(updatedAddressEntity);
                        }
                        user.Address = updatedAddressEntity;
                    }
                    else
                    {
                        _logger.LogWarning("Admin user {AdminUsername} attempted partial address update for user {UserId}. All address fields (Street, City, ZipCode, Country) are required to change an address. Address not updated.", adminUsername, id);

                    }
                }


                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin user {AdminUsername} updated user {UserId} successfully.", adminUsername, id);

                var userToReturnDto = new UserDto 
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    Birthday = user.Birthday,
                    CreatedAt = user.CreatedAt,
                    ApiKey = null, 
                    Address = user.Address == null ? null : new _10.Models.Api.AddressDto //
                    {
                        Street = user.Address.Street,
                        City = user.Address.City,
                        ZipCode = user.Address.ZipCode,
                        Country = user.Address.Country
                    }
                };
                return Ok(userToReturnDto);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Admin user {AdminUsername} encountered a concurrency error updating user {UserId}.", adminUsername, id);
                if (!_context.Users.Any(e => e.UserId == id))
                {
                    return NotFound(new { message = $"User with ID {id} not found (concurrency error)." });
                }
                else
                {
                    return StatusCode(500, new { message = "A concurrency error occurred while updating the user." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin user {AdminUsername} encountered an error updating user {UserId}.", adminUsername, id);
                return StatusCode(500, new { message = "An internal server error occurred while updating the user." });
            }
        }


        [HttpDelete("{id:int}")]
        [ApiAdminAuthorize]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var adminUsername = GetAuthUsernameForLogging();
            _logger.LogInformation("Admin user {AdminUsername} attempting to delete user with ID {UserId}.", adminUsername, id);

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Admin user {AdminUsername} failed to delete user. User with ID {UserId} not found.", adminUsername, id);
                    return NotFound(new { message = $"User with ID {id} not found." });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin user {AdminUsername} deleted user {UserId} successfully.", adminUsername, id);

                return Ok(new { message = $"User with ID {id} deleted successfully." });
            }
            catch (DbUpdateException ex) 
            {
                 _logger.LogError(ex, "Admin user {AdminUsername} encountered a database update error deleting user {UserId}. This might be due to foreign key constraints.", adminUsername, id);
                 return StatusCode(500, new { message = $"An error occurred while deleting the user. The user might be associated with other records (e.g., packages) that prevent deletion." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin user {AdminUsername} encountered an error deleting user {UserId}.", adminUsername, id);
                return StatusCode(500, new { message = "An internal server error occurred while deleting the user." });
            }
        }
    }

}