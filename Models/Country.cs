using System.ComponentModel.DataAnnotations;

namespace HumanBirthPredictionSystem.Models
{
    public class Country
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string CountryName { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string CountryCode { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Continent { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<City> Cities { get; set; } = new List<City>();
        public ICollection<BirthRecord> BirthRecords { get; set; } = new List<BirthRecord>();
        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    }
}
