namespace HumanBirthPredictionSystem.ViewModels
{
    public class BirthTrendsViewModel
    {
        public List<int> SelectedCountryIds { get; set; } = new();
        public int? CityId { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }

        public List<HumanBirthPredictionSystem.Models.Country> Countries { get; set; } = new();
        public List<HumanBirthPredictionSystem.Models.City> Cities { get; set; } = new();

        public List<int> Years { get; set; } = new();
        public List<long> TotalBirths { get; set; } = new();
        public List<long> MaleBirths { get; set; } = new();
        public List<long> FemaleBirths { get; set; } = new();

        // series name -> yearly totals, for multi-country comparison
        public Dictionary<string, List<long>> CountrySeries { get; set; } = new();

        public double? GrowthRatePercent { get; set; }
    }
}
