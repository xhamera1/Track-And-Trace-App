using System.ComponentModel.DataAnnotations;

namespace _10.Models.Api
{
    public class AddressResponseDto
    {
        public int AddressId { get; set; }

        [Required]
        [StringLength(255)]
        public string Street { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string ZipCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;
    }

    public class CreateAddressRequestDto
    {
        [Required(ErrorMessage = "Street is required.")]
        [StringLength(255)]
        public string Street { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        public string City { get; set; }

        [Required(ErrorMessage = "Zip code is required.")]
        [StringLength(10)]
        public string ZipCode { get; set; }

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100)]
        public string Country { get; set; }
    }

    public class UpdateAddressRequestDto
    {
        [StringLength(255)]
        public string? Street { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(10)]
        public string? ZipCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }
    }
}