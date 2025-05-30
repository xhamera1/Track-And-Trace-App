using System.ComponentModel.DataAnnotations;
using _10.Models; // Assuming your Address, User, PackageSize enums are here

namespace _10.Models
{
    public class SendPackageViewModel
    {
        // Recipient Information
        [Required(ErrorMessage = "Recipient email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Display(Name = "Recipient Email")]
        public string RecipientEmail { get; set; } = string.Empty;

        [Display(Name = "Recipient First Name (Optional)")]
        public string? RecipientFirstName { get; set; }

        [Display(Name = "Recipient Last Name (Optional)")]
        public string? RecipientLastName { get; set; }

        // Origin Address
        [Required(ErrorMessage = "Origin street is required.")]
        [StringLength(255)]
        [Display(Name = "Origin Street")]
        public string OriginStreet { get; set; } = string.Empty;

        [Required(ErrorMessage = "Origin city is required.")]
        [StringLength(100)]
        [Display(Name = "Origin City")]
        public string OriginCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Origin ZIP code is required.")]
        [StringLength(10)]
        [Display(Name = "Origin ZIP Code")]
        public string OriginZipCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Origin country is required.")]
        [StringLength(100)]
        [Display(Name = "Origin Country")]
        public string OriginCountry { get; set; } = string.Empty;

        // Destination Address
        [Required(ErrorMessage = "Destination street is required.")]
        [StringLength(255)]
        [Display(Name = "Destination Street")]
        public string DestinationStreet { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination city is required.")]
        [StringLength(100)]
        [Display(Name = "Destination City")]
        public string DestinationCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination ZIP code is required.")]
        [StringLength(10)]
        [Display(Name = "Destination ZIP Code")]
        public string DestinationZipCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination country is required.")]
        [StringLength(100)]
        [Display(Name = "Destination Country")]
        public string DestinationCountry { get; set; } = string.Empty;

        // Package Details
        [Required(ErrorMessage = "Package size is required.")]
        [Display(Name = "Package Size")]
        public PackageSize PackageSize { get; set; }

        [Display(Name = "Weight (kg, Optional)")]
        [Range(0.01, 1000, ErrorMessage = "Weight must be between 0.01 and 1000 kg.")]
        public decimal? WeightInKg { get; set; }

        [Display(Name = "Notes (Optional)")]
        public string? Notes { get; set; }
    }
}
