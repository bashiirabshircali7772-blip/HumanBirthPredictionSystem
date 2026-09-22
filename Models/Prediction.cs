using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HumanBirthPredictionSystem.Models
{
    /// <summary>
    /// Stores the output of the Python prediction module. These rows are
    /// always model-generated estimates and are kept in a table separate
    /// from BirthRecords so predicted values can never be confused with
    /// officially recorded statistics.
    /// </summary>
    public class Prediction
    {
        public int Id { get; set; }

        [Required]
        public int CountryId { get; set; }

        [ForeignKey(nameof(CountryId))]
        public Country? Country { get; set; }

        public int? CityId { get; set; }

        [ForeignKey(nameof(CityId))]
        public City? City { get; set; }

        [Required, Range(2000, 2100)]
        public int Year { get; set; }

        [Required]
        public int PredictedTotalBirths { get; set; }

        [Required]
        public int PredictedMaleBirths { get; set; }

        [Required]
        public int PredictedFemaleBirths { get; set; }

        [MaxLength(100)]
        public string PredictionModel { get; set; } = "Linear Regression";

        public DateTime PredictionDate { get; set; } = DateTime.UtcNow;
    }
}
