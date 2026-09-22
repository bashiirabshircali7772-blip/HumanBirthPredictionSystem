namespace HumanBirthPredictionSystem.ViewModels
{
    public class PredictionViewModel
    {
        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        public int StartYear { get; set; } = 2025;
        public int EndYear { get; set; } = 2035;

        public List<HumanBirthPredictionSystem.Models.Country> Countries { get; set; } = new();
        public List<HumanBirthPredictionSystem.Models.City> Cities { get; set; } = new();

        public List<PredictionRow> CombinedRows { get; set; } = new();
        public bool HasResults { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PredictionRow
    {
        public int Year { get; set; }
        public string DataType { get; set; } = string.Empty; // Historical / Predicted
        public long TotalBirths { get; set; }
        public long MaleBirths { get; set; }
        public long FemaleBirths { get; set; }
    }
}
