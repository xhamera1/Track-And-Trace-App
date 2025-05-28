// User.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _10.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [StringLength(32)]
        public string Username { get; set; } 

        [Required]
        [EmailAddress]
        [StringLength(128)] 
        public string Email { get; set; }

        [Required]
        [StringLength(255)] 
        public string Password { get; set; }

        [StringLength(44)] 
        [Column(TypeName = "char(44)")] 
        public string? ApiKey { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public DateTime? Birthday { get; set; } 

        public int? AddressId { get; set; } 
        [ForeignKey("AddressId")]
        public virtual Address? Address { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 

        [InverseProperty("SenderUser")]
        public virtual ICollection<Package> SentPackages { get; set; } = new List<Package>();

        [InverseProperty("RecipientUser")]
        public virtual ICollection<Package> ReceivedPackages { get; set; } = new List<Package>();

        [InverseProperty("AssignedCourier")]
        public virtual ICollection<Package> AssignedPackagesAsCourier { get; set; } = new List<Package>();
    }
}