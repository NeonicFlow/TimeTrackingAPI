using System.ComponentModel.DataAnnotations;

namespace TimeTrackingAPI.DTOs
{
    public class ProjectDto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class CreateProjectDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class UpdateProjectDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}