// Package.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _10.Models
{
    public class Package
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PackageId { get; set; }

        [Required]
        [StringLength(255)] 
        public string TrackingNumber { get; set; } 

        [Required]
        public int SenderUserId { get; set; }
        [ForeignKey("SenderUserId")]
        public virtual User SenderUser { get; set; }

        [Required]
        public int RecipientUserId { get; set; }
        [ForeignKey("RecipientUserId")]
        public virtual User RecipientUser { get; set; }

        public int? AssignedCourierId { get; set; }
        [ForeignKey("AssignedCourierId")]
        public virtual User? AssignedCourier { get; set; }

        [Required]
        public PackageSize PackageSize { get; set; }

        [Column(TypeName = "decimal(8, 2)")]
        public decimal? WeightInKg { get; set; } 

        [Column(TypeName = "TEXT")] 
        public string? Notes { get; set; } 

        [Required]
        public int OriginAddressId { get; set; } 
        [ForeignKey("OriginAddressId")]
        public virtual Address OriginAddress { get; set; }

        [Required]
        public int DestinationAddressId { get; set; } 
        [ForeignKey("DestinationAddressId")]
        public virtual Address DestinationAddress { get; set; }

        [Required]
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow; 

        public DateTime? DeliveryDate { get; set; }

        [Required]
        public int StatusId { get; set; }
        [ForeignKey("StatusId")]
        public virtual StatusDefinition CurrentStatus { get; set; }

        [Column(TypeName = "decimal(10, 7)")] 
        public decimal? Longitude { get; set; }

        [Column(TypeName = "decimal(10, 7)")] 
        public decimal? Latitude { get; set; } 

        public virtual ICollection<PackageHistory> History { get; set; } = new List<PackageHistory>();
    }
}