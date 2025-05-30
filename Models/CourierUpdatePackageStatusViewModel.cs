using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace _10.Models 
{
    public class CourierUpdatePackageStatusViewModel
    {
        public int PackageId { get; set; }

        [Display(Name = "Tracking Number")]
        public string? TrackingNumber { get; set; }

        [Display(Name = "Current Status")]
        public string? CurrentStatusName { get; set; }

        [Display(Name = "Current Longitude")]
        public decimal? CurrentLongitude { get; set; }

        [Display(Name = "Current Latitude")]
        public decimal? CurrentLatitude { get; set; }

        [Required(ErrorMessage = "Please select a new status.")]
        [Display(Name = "New Status")]
        public int NewStatusId { get; set; }

        public IEnumerable<SelectListItem>? AvailableStatuses { get; set; }

        [Display(Name = "New Longitude")]
        [Range(-180.0, 180.0, ErrorMessage = "Value must be between -180 and 180.")]
        public decimal? NewLongitude { get; set; }

        [Display(Name = "New Latitude")]
        [Range(-90.0, 90.0, ErrorMessage = "Value must be between -90 and 90.")]
        public decimal? NewLatitude { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Courier Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }
    }
}