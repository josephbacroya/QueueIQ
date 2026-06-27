using Microsoft.ML.Data;

namespace QueueIQ.ML.Models;

/// <summary>
/// Input schema for ML.NET training.
/// Each property maps to a CSV column via [LoadColumn].
/// ML.NET requires public fields (not properties) for schema binding.
/// </summary>
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
