namespace QueueIQ.Shared.Interfaces;

public interface IPredictionService
{
    /// <summary>
    /// Predicts the wait time and no-show risk for a given customer joining the queue.
    /// </summary>
    Task<(double PredictedWaitMinutes, double NoShowRiskScore)> PredictAsync(
        string serviceTypeName,
        double avgServiceDurationMins,
        int queueLength,
        int staffOnDuty,
        string timeZoneId);
}
