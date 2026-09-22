using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HumanBirthPredictionSystem.Models
{
    public class City
    {
        public int Id { get; set; }

        [Required]
        public int CountryId { get; set; }

        [ForeignKey(nameof(CountryId))]
        public Country? Country { get; set; }

        [Required, MaxLength(100)]
        public string CityName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BirthRecord> BirthRecords { get; set; } = new List<BirthRecord>();
    }
}
