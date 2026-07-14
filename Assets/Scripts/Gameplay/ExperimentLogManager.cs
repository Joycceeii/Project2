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
        private static readonly List<UnlockedClueRecord> unlockedClues = new List<UnlockedClueRecord>();
        private const string CluePrefsKey = "TheTasteReviver.UnlockedClues";

        public IReadOnlyList<ExperimentRecord> Records => sharedRecords;
        public static IReadOnlyList<ExperimentRecord> SharedRecords => sharedRecords;
        public static IReadOnlyList<UnlockedClueRecord> UnlockedClues => unlockedClues;

        private void Awake()
        {
            LoadUnlockedClues();
        }

        public void AddRecord(RecipeAttemptManager attempt, EvaluationResult evaluation, HintResult hint, string processCheckHint = null, string permanentHint = null)
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
                processCheckHint = processCheckHint ?? string.Empty,
                permanentHint = permanentHint ?? string.Empty,
                orderStatus = BuildStatus(evaluation, MechanicType.IngredientOrder),
                combinationStatus = BuildStatus(evaluation, MechanicType.Combination),
                ratioStatus = BuildStatus(evaluation, MechanicType.Ratio),
                forceStatus = BuildStatus(evaluation, MechanicType.Force),
                speedStatus = BuildStatus(evaluation, MechanicType.Speed)
            };
            sharedRecords.Add(record);
            RefreshUI();
        }

        public List<UnlockedClueRecord> UnlockClues(RecipeLevelData level)
        {
            if (level == null || level.unlockCluesOnComplete == null)
            {
                return new List<UnlockedClueRecord>();
            }

            LoadUnlockedClues();
            bool changed = false;
            List<UnlockedClueRecord> newlyUnlocked = new List<UnlockedClueRecord>();
            foreach (LevelClueData clue in level.unlockCluesOnComplete)
            {
                if (clue == null || string.IsNullOrWhiteSpace(clue.clueId))
                {
                    continue;
                }

                if (unlockedClues.Any(x => x.clueId == clue.clueId))
                {
                    continue;
                }

                UnlockedClueRecord unlocked = new UnlockedClueRecord
                {
                    clueId = clue.clueId,
                    title = clue.title,
                    content = clue.content,
                    relatedDimension = clue.relatedDimension
                };
                unlockedClues.Add(unlocked);
                newlyUnlocked.Add(unlocked);
                changed = true;
            }

            if (changed)
            {
                SaveUnlockedClues();
                RefreshUI();
            }

            return newlyUnlocked;
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
            LoadUnlockedClues();
            StringBuilder builder = new StringBuilder();
            if (unlockedClues.Count > 0)
            {
                builder.AppendLine("Experiment Manual");
                builder.AppendLine();
                foreach (UnlockedClueRecord clue in unlockedClues)
                {
                    builder.AppendLine("[" + clue.title + "]");
                    builder.AppendLine(clue.content);
                    builder.AppendLine();
                }
            }

            if (sharedRecords.Count == 0)
            {
                if (builder.Length == 0)
                {
                    return "No experiment records yet.";
                }

                return builder.ToString();
            }

            if (builder.Length > 0)
            {
                builder.AppendLine("Attempt History");
                builder.AppendLine();
            }

            foreach (ExperimentRecord record in sharedRecords)
            {
                AppendRecordSummary(builder, record);
            }

            return builder.ToString();
        }

        private static void LoadUnlockedClues()
        {
            if (unlockedClues.Count > 0)
            {
                return;
            }

            string serialized = PlayerPrefs.GetString(CluePrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return;
            }

            string[] records = serialized.Split('\n');
            foreach (string record in records)
            {
                string[] fields = record.Split('\t');
                if (fields.Length < 4 || string.IsNullOrWhiteSpace(fields[0]))
                {
                    continue;
                }

                MechanicType mechanic = MechanicType.IngredientSelection;
                System.Enum.TryParse(fields[3], out mechanic);
                unlockedClues.Add(new UnlockedClueRecord
                {
                    clueId = fields[0],
                    title = fields[1],
                    content = fields[2].Replace("\\n", "\n"),
                    relatedDimension = mechanic
                });
            }
        }

        private static void SaveUnlockedClues()
        {
            StringBuilder builder = new StringBuilder();
            foreach (UnlockedClueRecord clue in unlockedClues)
            {
                builder.Append(clue.clueId).Append('\t')
                    .Append(clue.title).Append('\t')
                    .Append((clue.content ?? string.Empty).Replace("\n", "\\n")).Append('\t')
                    .Append(clue.relatedDimension).Append('\n');
            }

            PlayerPrefs.SetString(CluePrefsKey, builder.ToString());
            PlayerPrefs.Save();
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
            if (!string.IsNullOrWhiteSpace(record.processCheckHint))
            {
                builder.AppendLine("Process Check Hint: " + record.processCheckHint);
            }

            if (!string.IsNullOrWhiteSpace(record.permanentHint))
            {
                builder.AppendLine("Permanent Hint: " + record.permanentHint);
            }

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
        public string processCheckHint;
        public string permanentHint;
        public string orderStatus;
        public string combinationStatus;
        public string ratioStatus;
        public string forceStatus;
        public string speedStatus;
    }

    public class UnlockedClueRecord
    {
        public string clueId;
        public string title;
        public string content;
        public MechanicType relatedDimension;
    }
}
