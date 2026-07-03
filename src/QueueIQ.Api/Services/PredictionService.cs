using Microsoft.Extensions.ML;
using QueueIQ.Api.Models;
using QueueIQ.Shared.Interfaces;

namespace QueueIQ.Api.Services;

/// <summary>
/// Wraps ML.NET PredictionEnginePools to provide wait time and no-show predictions.
/// 
/// PredictionEnginePool is the recommended way to serve ML.NET models in ASP.NET Core
/// because a raw PredictionEngine is NOT thread-safe. The pool manages a thread-safe
/// pool of engines, ensuring high throughput for web APIs.
/// </summary>
public class PredictionService : IPredictionService
{
    private readonly PredictionEnginePool<QueueInput, WaitTimePrediction> _waitEnginePool;
    private readonly PredictionEnginePool<QueueInput, NoShowPrediction> _noShowEnginePool;
    private readonly ILogger<PredictionService> _logger;

    public PredictionService(
        PredictionEnginePool<QueueInput, WaitTimePrediction> waitEnginePool,
        PredictionEnginePool<QueueInput, NoShowPrediction> noShowEnginePool,
        ILogger<PredictionService> logger)
    {
        _waitEnginePool = waitEnginePool;
        _noShowEnginePool = noShowEnginePool;
        _logger = logger;
    }

    public Task<(double PredictedWaitMinutes, double NoShowRiskScore)> PredictAsync(
        string serviceTypeName, 
        double avgServiceDurationMins, 
        int queueLength, 
        int staffOnDuty,
        string timeZoneId)
    {
        try
        {
            var utcNow = DateTime.UtcNow;
            
            // Convert to local time based on Business.TimeZoneId
            var tzi = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var now = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tzi);
            
            // Note: ML.NET expects 'DayOfWeek' as 0-6 matching Python's datetime.weekday()
            // In C# DayOfWeek enum, Sunday is 0. Python weekday(): Monday is 0.
            // Let's map C# to Python mapping just to match our training data perfectly:
            // C#: 0=Sun, 1=Mon, ..., 6=Sat
            // Python: 0=Mon, 1=Tue, ..., 6=Sun
            int pyDayOfWeek = now.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)now.DayOfWeek - 1;

            var input = new QueueInput
            {
                ServiceType = serviceTypeName,
                AvgServiceDurationMins = (float)avgServiceDurationMins,
                DayOfWeek = pyDayOfWeek,
                HourOfDay = now.Hour,
                QueueLengthAtJoin = queueLength,
                StaffOnDuty = staffOnDuty,
                ActualWaitMinutes = 0 // Not known yet
            };

            var waitPrediction = _waitEnginePool.Predict("WaitTimeModel", input);
            var noShowPrediction = _noShowEnginePool.Predict("NoShowModel", input);

            // Ensure wait time doesn't predict negative
            double waitMins = Math.Max(0, waitPrediction.PredictedWaitMinutes);
            
            return Task.FromResult((waitMins, (double)noShowPrediction.Probability));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate ML predictions. Falling back to naive averages.");
            
            // Fallback strategy: Naive average calculation if ML fails
            double naiveWait = (queueLength + 1) * avgServiceDurationMins / Math.Max(1, staffOnDuty);
            return Task.FromResult((naiveWait, 0.05)); // 5% base risk fallback
        }
    }
}
