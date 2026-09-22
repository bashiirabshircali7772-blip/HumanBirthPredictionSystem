namespace HumanBirthPredictionSystem.ViewModels
{
    public class ReportsViewModel
    {
        public List<HumanBirthPredictionSystem.Models.Country> Countries { get; set; } = new();
        public string ReportType { get; set; } = "BirthTrends";
        public int? CountryId { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }

        public List<Models.BirthRecord> HistoricalRows { get; set; } = new();
        public List<Models.Prediction> PredictionRows { get; set; } = new();

        public long TotalBirthsSum { get; set; }
        public long TotalMaleSum { get; set; }
        public long TotalFemaleSum { get; set; }
    }
}
