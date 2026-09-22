using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HumanBirthPredictionSystem.Models
{
    public class BirthRecord
    {
        public int Id { get; set; }

        [Required]
        public int CountryId { get; set; }

        [ForeignKey(nameof(CountryId))]
        public Country? Country { get; set; }

        // Optional: some historical datasets are only available at country level
        public int? CityId { get; set; }

        [ForeignKey(nameof(CityId))]
        public City? City { get; set; }

        [Required, Range(1900, 2100)]
        public int Year { get; set; }

        [Required, Range(0, int.MaxValue, ErrorMessage = "Total births cannot be negative.")]
        public int TotalBirths { get; set; }

        [Required, Range(0, int.MaxValue, ErrorMessage = "Male births cannot be negative.")]
        public int MaleBirths { get; set; }

        [Required, Range(0, int.MaxValue, ErrorMessage = "Female births cannot be negative.")]
        public int FemaleBirths { get; set; }

        [MaxLength(150)]
        public string DataSource { get; set; } = string.Empty;

        [MaxLength(300)]
        public string SourceReference { get; set; } = string.Empty;

        [Required]
        public RecordType RecordType { get; set; } = RecordType.Historical;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public double MalePercentage => TotalBirths == 0 ? 0 : Math.Round((double)MaleBirths / TotalBirths * 100, 2);

        [NotMapped]
        public double FemalePercentage => TotalBirths == 0 ? 0 : Math.Round((double)FemaleBirths / TotalBirths * 100, 2);
    }
}
