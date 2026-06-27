using Microsoft.ML.Data;

namespace QueueIQ.ML.Models;

/// <summary>
/// Output schema for the wait-time regression model.
/// ML.NET maps the predicted value to a property named 'Score' by convention.
/// </summary>
public class WaitTimePrediction
{
    [ColumnName("Score")]
    public float PredictedWaitMinutes { get; set; }
}
