using System.ComponentModel.DataAnnotations;

namespace _10.Models.Api
{
    /// <summary>
    /// Data Transfer Object for Status Definition information
    /// </summary>
    public class StatusDefinitionDto
    {
        public int StatusId { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for creating a new status definition
    /// </summary>
    public class CreateStatusDefinitionRequest
    {
        [Required(ErrorMessage = "Status name is required.")]
        [StringLength(32, ErrorMessage = "Status name cannot exceed 32 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status description is required.")]
        [StringLength(255, ErrorMessage = "Status description cannot exceed 255 characters.")]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for updating an existing status definition
    /// </summary>
    public class UpdateStatusDefinitionRequest
    {
        [Required(ErrorMessage = "Status name is required.")]
        [StringLength(32, ErrorMessage = "Status name cannot exceed 32 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status description is required.")]
        [StringLength(255, ErrorMessage = "Status description cannot exceed 255 characters.")]
        public string Description { get; set; } = string.Empty;
    }
}
