using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TimeTrackingAPI.Models
{
    public class TimeEntry
    {
        [Key]
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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public Task? Task { get; set; }
    }
}