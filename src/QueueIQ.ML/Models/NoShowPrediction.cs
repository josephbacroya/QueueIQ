using Microsoft.ML.Data;

namespace QueueIQ.ML.Models;

/// <summary>
/// Output schema for the no-show binary classification model.
/// ML.NET maps the predicted label and probability automatically.
/// </summary>
public class NoShowPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsNoShow { get; set; }

    /// <summary>
    /// Probability that the customer IS a no-show (0.0 to 1.0).
    /// This is the value we surface to staff as a risk score.
    /// </summary>
    public float Probability { get; set; }
}
