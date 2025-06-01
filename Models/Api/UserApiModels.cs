using System.ComponentModel.DataAnnotations;
using _10.Models; 

namespace _10.Models.Api
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Role { get; set; }
        public DateTime? Birthday { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ApiKey { get; set; }

        public _10.Models.Api.AddressDto? Address { get; set; }
    }


    public class CreateUserRequestDto
    {
        [Required]
        [StringLength(32)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(128)]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public DateTime? Birthday { get; set; }

        [StringLength(255)]
        public string? Street { get; set; }
        [StringLength(100)]
        public string? City { get; set; }
        [StringLength(10)]
        public string? ZipCode { get; set; }
        [StringLength(100)]
        public string? Country { get; set; }
    }

    public class UpdateUserRequestDto
    {
        [EmailAddress]
        [StringLength(128)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        public UserRole? Role { get; set; }
        public DateTime? Birthday { get; set; }

        [StringLength(100, MinimumLength = 6)]
        public string? NewPassword { get; set; }

        [StringLength(255)]
        public string? Street { get; set; }
        [StringLength(100)]
        public string? City { get; set; }
        [StringLength(10)]
        public string? ZipCode { get; set; }
        [StringLength(100)]
        public string? Country { get; set; }
    }

    public class ApiKeyResponseDto
    {
        public string ApiKey { get; set; }
    }
}