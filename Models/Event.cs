using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan Time { get; set; }

        [Required]
        [StringLength(200)]
        public string Venue { get; set; } = string.Empty;

        [Required]
        [Range(1, 10000)]
        public int MaxParticipants { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Upcoming";

        public ICollection<EventRegistration> Registrations { get; set; }
            = new List<EventRegistration>();
    }
}