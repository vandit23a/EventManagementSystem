using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventManagementSystem.Data;

namespace EventManagementSystem.Models
{
    public class EventRegistration
    {
        [Key]
        public int EventRegistrationId { get; set; }

        [Required]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public Event Event { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public string StudentName { get; set; } = string.Empty;
    }
}