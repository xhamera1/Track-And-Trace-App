using System.ComponentModel.DataAnnotations;
using _10.Models;

namespace _10.Models.Api
{

    public class PackageHistoryDto
    {
        public int PackageHistoryId { get; set; }
        public int PackageId { get; set; }
        public string PackageTrackingNumber { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? Latitude { get; set; }
    }


    public class CreatePackageHistoryRequest
    {
        [Required]
        public int PackageId { get; set; }

        [Required]
        public int StatusId { get; set; }

        [Range(-180.0, 180.0)]
        public decimal? Longitude { get; set; }

        [Range(-90.0, 90.0)]
        public decimal? Latitude { get; set; }

        public DateTime? Timestamp { get; set; }
    }


    public class UpdatePackageHistoryRequest
    {
        public int? StatusId { get; set; }

        [Range(-180.0, 180.0)]
        public decimal? Longitude { get; set; }

        [Range(-90.0, 90.0)]
        public decimal? Latitude { get; set; }

        public DateTime? Timestamp { get; set; }
    }


    public class PackageHistoryListDto
    {
        public int PackageId { get; set; }
        public string PackageTrackingNumber { get; set; } = string.Empty;
        public IEnumerable<PackageHistoryDto> HistoryEntries { get; set; } = new List<PackageHistoryDto>();
        public int TotalEntries { get; set; }
    }
}
