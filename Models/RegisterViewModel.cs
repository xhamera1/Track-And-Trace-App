using System;
using System.ComponentModel.DataAnnotations;

namespace _10.Models
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(32)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(128)]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(255, MinimumLength = 6)] // Assuming a minimum password length
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        // Address properties
        [Required]
        [StringLength(255)]
        public string Street { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; }

        [Required]
        [StringLength(20)]
        public string ZipCode { get; set; }

        [Required]
        public bool IsCourier { get; set; }
    }
}
