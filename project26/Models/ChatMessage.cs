using System;
using System.ComponentModel.DataAnnotations;

namespace project26.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; } // Null if guest user

        [Required]
        public string SenderName { get; set; }

        [Required]
        public string MessageText { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsFromAdmin { get; set; }

        [Required]
        public string SessionId { get; set; } // Unique token representing user conversation thread
    }
}
