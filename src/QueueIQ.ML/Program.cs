using Microsoft.ML;
using Microsoft.ML.Data;
using QueueIQ.ML.Models;

// ============================================================================
// QueueIQ ML.NET Model Trainer
// 
// This is a standalone console app that:
//   1. Loads historical queue data from CSV
//   2. Trains a FastTree REGRESSION model for wait-time prediction
//   3. Trains a FastTree BINARY CLASSIFICATION model for no-show risk
//   4. Evaluates both models and prints metrics
//   5. Exports trained models as .zip files for the API to load
//
// Interview talking point: "I used a separate console trainer so the ML pipeline
// can be re-run on fresh data without touching the API. The exported .zip files
// are loaded by PredictionEnginePool in the API, which is thread-safe and 
// designed for high-throughput inference in ASP.NET Core."
// ============================================================================

var mlContext = new MLContext(seed: 42);

// Paths
var baseDir = Directory.GetCurrentDirectory();
var dataPath = Path.Combine(baseDir, "historical_queue_data.csv");
var modelsDir = Path.Combine(baseDir, "models");
Directory.CreateDirectory(modelsDir);

var waitTimeModelPath = Path.Combine(modelsDir, "wait_time_model.zip");
var noShowModelPath = Path.Combine(modelsDir, "no_show_model.zip");

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       QueueIQ ML.NET Model Trainer v1.0             ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

// ---- Step 1: Load Data ----
Console.WriteLine("📂 Loading data from: " + Path.GetFullPath(dataPath));
var dataView = mlContext.Data.LoadFromTextFile<QueueInput>(
    path: dataPath,
    hasHeader: true,
    separatorChar: ',');

// Split into train/test sets (80/20)
var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2, seed: 42);
Console.WriteLine($"   Training set: {split.TrainSet.GetRowCount()} rows");
Console.WriteLine($"   Test set:     {split.TestSet.GetRowCount()} rows");
Console.WriteLine();

// ---- Step 2: Define shared feature engineering pipeline ----
// This is reused by both models. We one-hot encode the categorical ServiceType
// column, then concatenate all numeric features into a single "Features" vector.
var featurePipeline = mlContext.Transforms.Categorical.OneHotEncoding("ServiceTypeEncoded", "ServiceType")
    .Append(mlContext.Transforms.Concatenate("Features",
        "ServiceTypeEncoded",
        "AvgServiceDurationMins",
        "DayOfWeek",
        "HourOfDay",
        "QueueLengthAtJoin",
        "StaffOnDuty"));

// ============================================================================
// MODEL 1: Wait Time Prediction (Regression)
// ============================================================================
Console.WriteLine("🏋️ Training Wait Time Regression Model (FastTree)...");

// Copy ActualWaitMinutes → Label (ML.NET convention for supervised learning)
var waitTimePipeline = mlContext.Transforms.CopyColumns("Label", "ActualWaitMinutes")
    .Append(featurePipeline)
    .Append(mlContext.Regression.Trainers.FastTree(
        labelColumnName: "Label",
        featureColumnName: "Features",
        numberOfLeaves: 20,
        numberOfTrees: 100,
        minimumExampleCountPerLeaf: 10,
        learningRate: 0.2));

var waitTimeModel = waitTimePipeline.Fit(split.TrainSet);

// Evaluate
var waitTimePredictions = waitTimeModel.Transform(split.TestSet);
var waitTimeMetrics = mlContext.Regression.Evaluate(waitTimePredictions, labelColumnName: "Label");

Console.WriteLine();
Console.WriteLine("📊 Wait Time Model Metrics:");
Console.WriteLine($"   RMSE:  {waitTimeMetrics.RootMeanSquaredError:F2} minutes");
Console.WriteLine($"   MAE:   {waitTimeMetrics.MeanAbsoluteError:F2} minutes");
Console.WriteLine($"   R²:    {waitTimeMetrics.RSquared:F4}");
Console.WriteLine();

// Save
mlContext.Model.Save(waitTimeModel, split.TrainSet.Schema, waitTimeModelPath);
Console.WriteLine($"💾 Wait Time model saved to: {Path.GetFullPath(waitTimeModelPath)}");
Console.WriteLine();

// ============================================================================
// MODEL 2: No-Show Risk (Binary Classification)
// ============================================================================
Console.WriteLine("🏋️ Training No-Show Classification Model (FastTree)...");

// Copy IsNoShow → Label
var noShowPipeline = mlContext.Transforms.CopyColumns("Label", "IsNoShow")
    .Append(featurePipeline)
    // Also include the wait time as a feature for the no-show model, since
    // longer waits correlate with higher no-show probability
    .Append(mlContext.Transforms.Concatenate("FeaturesWithWait",
        "Features",
        "ActualWaitMinutes"))
    .Append(mlContext.BinaryClassification.Trainers.FastTree(
        labelColumnName: "Label",
        featureColumnName: "FeaturesWithWait",
        numberOfLeaves: 20,
        numberOfTrees: 100,
        minimumExampleCountPerLeaf: 10,
        learningRate: 0.2));

var noShowModel = noShowPipeline.Fit(split.TrainSet);

// Evaluate
var noShowPredictions = noShowModel.Transform(split.TestSet);
var noShowMetrics = mlContext.BinaryClassification.Evaluate(noShowPredictions, labelColumnName: "Label");

Console.WriteLine();
Console.WriteLine("📊 No-Show Model Metrics:");
Console.WriteLine($"   AUC:       {noShowMetrics.AreaUnderRocCurve:F4}");
Console.WriteLine($"   Accuracy:  {noShowMetrics.Accuracy:F4}");
Console.WriteLine($"   F1 Score:  {noShowMetrics.F1Score:F4}");
Console.WriteLine($"   Precision: {noShowMetrics.PositiveRecall:F4}");
Console.WriteLine($"   Recall:    {noShowMetrics.PositivePrecision:F4}");
Console.WriteLine();

// Save
mlContext.Model.Save(noShowModel, split.TrainSet.Schema, noShowModelPath);
Console.WriteLine($"💾 No-Show model saved to: {Path.GetFullPath(noShowModelPath)}");
Console.WriteLine();

// ============================================================================
// Quick smoke test: predict on a sample input
// ============================================================================
Console.WriteLine("🔬 Smoke Test — Predicting on a sample input:");
Console.WriteLine("   Input: Haircut, Saturday 2pm, 3 people ahead, 3 staff");
Console.WriteLine();

var waitTimeEngine = mlContext.Model.CreatePredictionEngine<QueueInput, WaitTimePrediction>(waitTimeModel);
var noShowEngine = mlContext.Model.CreatePredictionEngine<QueueInput, NoShowPrediction>(noShowModel);

var sampleInput = new QueueInput
{
    ServiceType = "Haircut",
    AvgServiceDurationMins = 30,
    DayOfWeek = 5, // Saturday
    HourOfDay = 14,
    QueueLengthAtJoin = 3,
    StaffOnDuty = 3,
    ActualWaitMinutes = 0 // Not known at prediction time
};

var waitPrediction = waitTimeEngine.Predict(sampleInput);
var noShowPrediction = noShowEngine.Predict(sampleInput);

Console.WriteLine($"   Predicted Wait:    {waitPrediction.PredictedWaitMinutes:F1} minutes");
Console.WriteLine($"   No-Show Risk:      {noShowPrediction.Probability:P1}");
Console.WriteLine($"   No-Show Predicted: {noShowPrediction.IsNoShow}");
Console.WriteLine();
Console.WriteLine("✅ Training complete! Models are ready for the API.");
