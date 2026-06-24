using System.ComponentModel.DataAnnotations;

namespace TimeTrackingAPI.DTOs
{
    public class TimeEntryDto
    {
        public int Id { get; set; }

        [Required]
        public DateTime EntryDate { get; set; }

        [Required]
        [Range(0.1, 24)]
        public decimal Hours { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int TaskId { get; set; }

        public string? TaskName { get; set; }
        public string? ProjectName { get; set; }
    }

    public class CreateTimeEntryDto
    {
        [Required]
        public DateTime EntryDate { get; set; }

        [Required]
        [Range(0.1, 24)]
        public decimal Hours { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int TaskId { get; set; }
    }

    public class UpdateTimeEntryDto
    {
        [Required]
        public DateTime EntryDate { get; set; }

        [Required]
        [Range(0.1, 24)]
        public decimal Hours { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int TaskId { get; set; }
    }

    public class DailySummaryDto
    {
        public DateTime Date { get; set; }
        public decimal TotalHours { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<TimeEntryDto> Entries { get; set; } = new List<TimeEntryDto>();
    }
}