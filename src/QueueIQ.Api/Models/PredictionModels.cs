using Microsoft.ML.Data;

namespace QueueIQ.Api.Models;

public class QueueInput
{
    [LoadColumn(0)]
    public string ServiceType { get; set; } = string.Empty;

    [LoadColumn(1)]
    public float AvgServiceDurationMins { get; set; }

    [LoadColumn(2)]
    public float DayOfWeek { get; set; }

    [LoadColumn(3)]
    public float HourOfDay { get; set; }

    [LoadColumn(4)]
    public float QueueLengthAtJoin { get; set; }

    [LoadColumn(5)]
    public float StaffOnDuty { get; set; }

    [LoadColumn(6)]
    public float ActualWaitMinutes { get; set; }

    [LoadColumn(7)]
    public bool IsNoShow { get; set; }
}

public class WaitTimePrediction
{
    [ColumnName("Score")]
    public float PredictedWaitMinutes { get; set; }
}

public class NoShowPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsNoShow { get; set; }

    public float Probability { get; set; }
}
