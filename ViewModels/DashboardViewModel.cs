namespace HumanBirthPredictionSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalCountries { get; set; }
        public int TotalCities { get; set; }
        public int TotalBirthRecords { get; set; }
        public long LatestTotalBirths { get; set; }
        public long TotalMaleBirths { get; set; }
        public long TotalFemaleBirths { get; set; }
        public double MalePercentage { get; set; }
        public double FemalePercentage { get; set; }
        public long LatestPredictedBirths { get; set; }

        public List<int> TrendYears { get; set; } = new();
        public List<long> TrendTotals { get; set; } = new();
        public List<long> TrendMale { get; set; } = new();
        public List<long> TrendFemale { get; set; } = new();

        public List<string> CountryNames { get; set; } = new();
        public List<long> CountryTotals { get; set; } = new();

        public List<HistoricalVsPredictedPoint> HistoricalVsPredicted { get; set; } = new();

        public List<RecentRecordRow> RecentRecords { get; set; } = new();
        public List<RecentPredictionRow> RecentPredictions { get; set; } = new();
    }

    public class HistoricalVsPredictedPoint
    {
        public int Year { get; set; }
        public long? HistoricalTotal { get; set; }
        public long? PredictedTotal { get; set; }
    }

    public class RecentRecordRow
    {
        public string Country { get; set; } = string.Empty;
        public string? City { get; set; }
        public int Year { get; set; }
        public long TotalBirths { get; set; }
        public string RecordType { get; set; } = string.Empty;
    }

    public class RecentPredictionRow
    {
        public string Country { get; set; } = string.Empty;
        public string? City { get; set; }
        public int Year { get; set; }
        public long PredictedTotalBirths { get; set; }
        public string PredictionModel { get; set; } = string.Empty;
    }
}
