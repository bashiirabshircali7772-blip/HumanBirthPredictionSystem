using System.ComponentModel.DataAnnotations;

namespace HumanBirthPredictionSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Role { get; set; } = "Administrator";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
