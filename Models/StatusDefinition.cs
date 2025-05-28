// StatusDefinition.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _10.Models
{
    public class StatusDefinition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StatusId { get; set; }

        [Required]
        [StringLength(32)] 
        public string Name { get; set; } 

        [Required]
        [StringLength(255)]
        public string Description { get; set; }

        [InverseProperty("CurrentStatus")]
        public virtual ICollection<Package> PackagesWithThisStatus { get; set; } = new List<Package>();

        [InverseProperty("Status")]
        public virtual ICollection<PackageHistory> PackageHistoriesWithThisStatus { get; set; } = new List<PackageHistory>();
    }
}