using System.ComponentModel.DataAnnotations;
using _10.Models;

namespace _10.Models.Api
{
    /// <summary>
    /// Data Transfer Object for Package information
    /// </summary>
    public class PackageDto
    {
        public int PackageId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public int SenderUserId { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public int RecipientUserId { get; set; }
        public string RecipientUsername { get; set; } = string.Empty;
        public int? AssignedCourierId { get; set; }
        public string? AssignedCourierUsername { get; set; }
        public PackageSize PackageSize { get; set; }
        public decimal? WeightInKg { get; set; }
        public string? Notes { get; set; }
        public AddressDto? OriginAddress { get; set; }
        public AddressDto? DestinationAddress { get; set; }
        public DateTime SubmissionDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public decimal? Longitude { get; set; }
        public decimal? Latitude { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for Address information
    /// </summary>
    public class AddressDto
    {
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

    /// <summary>
    /// Request model for creating a new package
    /// </summary>
    public class CreatePackageRequest
    {
        [Required]
        public int SenderUserId { get; set; }

        [Required]
        public int RecipientUserId { get; set; }

        public int? AssignedCourierId { get; set; }

        [Required]
        public PackageSize PackageSize { get; set; }

        [Range(0.01, 1000.00)]
        public decimal? WeightInKg { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Required]
        public AddressDto OriginAddress { get; set; } = new();

        [Required]
        public AddressDto DestinationAddress { get; set; } = new();

        [Range(-180.0, 180.0)]
        public decimal? Longitude { get; set; }

        [Range(-90.0, 90.0)]
        public decimal? Latitude { get; set; }
    }

    /// <summary>
    /// Request model for updating an existing package
    /// </summary>
    public class UpdatePackageRequest
    {
        public int? AssignedCourierId { get; set; }

        [Range(0.01, 1000.00)]
        public decimal? WeightInKg { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public int? StatusId { get; set; }

        [Range(-180.0, 180.0)]
        public decimal? Longitude { get; set; }

        [Range(-90.0, 90.0)]
        public decimal? Latitude { get; set; }
    }
}
