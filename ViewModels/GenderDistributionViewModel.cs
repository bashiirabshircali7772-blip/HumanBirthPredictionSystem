namespace HumanBirthPredictionSystem.ViewModels
{
    public class GenderDistributionViewModel
    {
        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        public int? Year { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }

        public List<HumanBirthPredictionSystem.Models.Country> Countries { get; set; } = new();
        public List<HumanBirthPredictionSystem.Models.City> Cities { get; set; } = new();

        public long TotalMaleBirths { get; set; }
        public long TotalFemaleBirths { get; set; }
        public double MalePercentage { get; set; }
        public double FemalePercentage { get; set; }

        public List<CountryGenderRow> CountryComparison { get; set; } = new();
    }

    public class CountryGenderRow
    {
        public string CountryName { get; set; } = string.Empty;
        public long MaleBirths { get; set; }
        public long FemaleBirths { get; set; }
        public double MalePercentage { get; set; }
        public double FemalePercentage { get; set; }
    }
}
