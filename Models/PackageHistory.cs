// PackageHistory.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _10.Models
{
    [Table("PackageHistory")] 
    public class PackageHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PackageHistoryId { get; set; }

        [Required]
        public int PackageId { get; set; }
        [ForeignKey("PackageId")]
        public virtual Package Package { get; set; } = null!;

        [Required]
        public int StatusId { get; set; }
        [ForeignKey("StatusId")]
        public virtual StatusDefinition Status { get; set; } = null!;

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(10, 7)")]
        public decimal? Longitude { get; set; }

        [Column(TypeName = "decimal(10, 7)")]
        public decimal? Latitude { get; set; }
    }
}
