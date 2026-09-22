namespace HumanBirthPredictionSystem.Models
{
    /// <summary>
    /// Distinguishes how a birth record was obtained. This is central to the
    /// system's academic integrity requirement: predicted values must never
    /// be displayed or stored as if they were officially recorded statistics.
    /// </summary>
    public enum RecordType
    {
        Official = 0,
        Historical = 1,
        Estimated = 2,
        Predicted = 3
    }
}
