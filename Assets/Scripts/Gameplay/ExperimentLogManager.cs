using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public class ExperimentLogManager : MonoBehaviour
    {
        public Text logText;
        public int maxVisibleRecords = 5;

        private static readonly List<ExperimentRecord> sharedRecords = new List<ExperimentRecord>();

        public IReadOnlyList<ExperimentRecord> Records => sharedRecords;
        public static IReadOnlyList<ExperimentRecord> SharedRecords => sharedRecords;

        public void AddRecord(RecipeAttemptManager attempt, EvaluationResult evaluation, HintResult hint)
        {
            if (attempt == null || evaluation == null)
            {
                return;
            }

            Dictionary<IngredientData, RatioLevel> ratio = attempt.CalculateRatioPattern(out _);
            GrindingBatch batch = attempt.GetCurrentBatch();
            ExperimentRecord record = new ExperimentRecord
            {
                attemptNumber = sharedRecords.Count + 1,
                ingredientsUsed = string.Join(", ", attempt.IngredientAmounts.Where(x => x.ingredient != null).Select(x => x.ingredient.DisplayName)),
                ingredientOrder = string.Join(" -> ", attempt.IngredientOrder.Where(x => x != null).Select(x => x.DisplayName)),
                ratioPattern = string.Join(" / ", ratio.Select(x => x.Key.DisplayName + "=" + UIManager.GetRatioDisplayName(x.Value))),
                combinationPattern = batch == null ? "None" : string.Join(" + ", batch.ingredientsInBatch.Where(x => x != null).Select(x => x.DisplayName)) + " together",
                forceLevel = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium,
                speedLevel = attempt.pestleController != null ? attempt.pestleController.CurrentSpeedLevel : SpeedLevel.Medium,
                grindDuration = attempt.pestleController != null ? attempt.pestleController.GrindDuration : 0f,
                completenessScore = evaluation.completenessScore,
                mainFeedback = evaluation.mainFeedback,
                hintGiven = hint != null ? hint.text : string.Empty,
                orderStatus = BuildStatus(evaluation, MechanicType.IngredientOrder),
                combinationStatus = BuildStatus(evaluation, MechanicType.Combination),
                ratioStatus = BuildStatus(evaluation, MechanicType.Ratio),
                forceStatus = BuildStatus(evaluation, MechanicType.Force),
                speedStatus = BuildStatus(evaluation, MechanicType.Speed)
            };
            sharedRecords.Add(record);
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (logText == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            foreach (ExperimentRecord record in sharedRecords.Skip(Mathf.Max(0, sharedRecords.Count - maxVisibleRecords)))
            {
                AppendRecordSummary(builder, record);
            }

            logText.text = builder.ToString();
        }

        public static string BuildFullLogText()
        {
            if (sharedRecords.Count == 0)
            {
                return "No experiment records yet.";
            }

            StringBuilder builder = new StringBuilder();
            foreach (ExperimentRecord record in sharedRecords)
            {
                AppendRecordSummary(builder, record);
            }

            return builder.ToString();
        }

        private static void AppendRecordSummary(StringBuilder builder, ExperimentRecord record)
        {
            builder.AppendLine("Experiment #" + record.attemptNumber);
            builder.AppendLine("Ingredients: " + record.ingredientsUsed);
            builder.AppendLine("Order: " + record.ingredientOrder + " [" + record.orderStatus + "]");
            builder.AppendLine("Ratio: " + record.ratioPattern + " [" + record.ratioStatus + "]");
            builder.AppendLine("Combination: " + record.combinationPattern + " [" + record.combinationStatus + "]");
            builder.AppendLine("Force: " + record.forceLevel + " [" + record.forceStatus + "]");
            builder.AppendLine("Speed: " + record.speedLevel + " [" + record.speedStatus + "]");
            builder.AppendLine("Duration: " + record.grindDuration.ToString("0.0") + "s");
            builder.AppendLine("Completeness: " + record.completenessScore + "%");
            builder.AppendLine("Feedback: " + record.mainFeedback);
            builder.AppendLine("Hint: " + record.hintGiven);
            builder.AppendLine();
        }

        private static string BuildStatus(EvaluationResult evaluation, MechanicType mechanic)
        {
            DimensionEvaluation dimension = evaluation.GetDimension(mechanic);
            if (dimension == null)
            {
                return "Not Checked";
            }

            return dimension.isCorrect ? "Correct" : "Needs Work";
        }
    }

    public class ExperimentRecord
    {
        public int attemptNumber;
        public string ingredientsUsed;
        public string ingredientOrder;
        public string ratioPattern;
        public string combinationPattern;
        public ForceLevel forceLevel;
        public SpeedLevel speedLevel;
        public float grindDuration;
        public int completenessScore;
        public string mainFeedback;
        public string hintGiven;
        public string orderStatus;
        public string combinationStatus;
        public string ratioStatus;
        public string forceStatus;
        public string speedStatus;
    }
}
