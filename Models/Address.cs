using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _10.Models
{
    public class Address
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AddressId { get; set; }

        [Required]
        [StringLength(255)]
        public string Street { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }

        [Required]
        [StringLength(10)] 
        public string ZipCode { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; }



        [InverseProperty("Address")]
        public virtual ICollection<User> Users { get; set; } = new List<User>();

        [InverseProperty("OriginAddress")]
        public virtual ICollection<Package> PackagesWithThisOrigin { get; set; } = new List<Package>();

        [InverseProperty("DestinationAddress")]
        public virtual ICollection<Package> PackagesWithThisDestination { get; set; } = new List<Package>();
    }
}