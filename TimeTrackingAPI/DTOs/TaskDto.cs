using System.ComponentModel.DataAnnotations;

namespace TimeTrackingAPI.DTOs
{
    public class TaskDto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int ProjectId { get; set; }

        public string? ProjectName { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CreateTaskDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int ProjectId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateTaskDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int ProjectId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}