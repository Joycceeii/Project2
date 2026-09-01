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
        private static readonly List<IngredientData> ingredientCatalog = new List<IngredientData>();
        private const string CluePrefsKey = "TheTasteReviver.UnlockedClues";

        public IReadOnlyList<ExperimentRecord> Records => sharedRecords;
        public static IReadOnlyList<ExperimentRecord> SharedRecords => sharedRecords;
        public static IReadOnlyList<UnlockedClueRecord> UnlockedClues => unlockedClues;

        public static void SetIngredientCatalog(IEnumerable<IngredientData> ingredients)
        {
            ingredientCatalog.Clear();
            if (ingredients == null)
            {
                return;
            }

            ingredientCatalog.AddRange(ingredients.Where(x => x != null && !string.IsNullOrWhiteSpace(x.ingredientID)));
        }

        private void Awake()
        {
            LoadUnlockedClues();
        }

        public void AddRecord(RecipeAttemptManager attempt, EvaluationResult evaluation, HintResult hint, string permanentHint = null)
        {
            if (attempt == null || evaluation == null)
            {
                return;
            }

            Dictionary<IngredientData, RatioLevel> ratio = attempt.CalculateRatioPattern(out _);
            attempt.GetCurrentBatch();
            ExperimentRecord record = new ExperimentRecord
            {
                attemptNumber = sharedRecords.Count + 1,
                ingredientsUsed = string.Join(", ", attempt.IngredientAmounts.Where(x => x.ingredient != null).Select(x => x.ingredient.DisplayName)),
                ingredientOrder = string.Join(" -> ", attempt.IngredientOrder.Where(x => x != null).Select(x => x.DisplayName)),
                ingredientOrderDetails = BuildOrderRelationshipText(attempt.IngredientOrder),
                ratioPattern = string.Join(" / ", ratio.Select(x => x.Key.DisplayName + "=" + UIManager.GetRatioDisplayName(x.Value))),
                combinationPattern = BuildCombinationPatternText(attempt.GrindingBatches),
                forceLevel = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium,
                speedLevel = attempt.pestleController != null ? attempt.pestleController.CurrentSpeedLevel : SpeedLevel.Medium,
                grindDuration = attempt.pestleController != null ? attempt.pestleController.GrindDuration : 0f,
                mainFeedback = evaluation.mainFeedback,
                hintGiven = hint != null ? hint.text : string.Empty,
                permanentHint = permanentHint ?? string.Empty,
                orderStatus = BuildStatus(evaluation, MechanicType.IngredientOrder),
                combinationStatus = BuildStatus(evaluation, MechanicType.Combination),
                ratioStatus = BuildStatus(evaluation, MechanicType.Ratio),
                forceStatus = BuildStatus(evaluation, MechanicType.Force),
                speedStatus = BuildStatus(evaluation, MechanicType.Speed)
            };
            record.ingredientEntries.AddRange(BuildIngredientEntries(attempt, ratio, evaluation));
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
                    relatedDimension = clue.relatedDimension,
                    relatedIngredientIDs = clue.relatedIngredients != null
                        ? clue.relatedIngredients.Where(x => x != null && !string.IsNullOrWhiteSpace(x.ingredientID)).Select(x => x.ingredientID).ToList()
                        : new List<string>()
                };
                unlockedClues.Add(unlocked);
                newlyUnlocked.Add(unlocked);
                SaveUnlockedClues();
                RefreshUI();
                break;
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
                    relatedDimension = mechanic,
                    relatedIngredientIDs = fields.Length >= 5 && !string.IsNullOrWhiteSpace(fields[4])
                        ? fields[4].Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
                        : new List<string>()
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
                    .Append(clue.relatedDimension).Append('\t')
                    .Append(clue.relatedIngredientIDs != null ? string.Join(",", clue.relatedIngredientIDs) : string.Empty).Append('\n');
            }

            PlayerPrefs.SetString(CluePrefsKey, builder.ToString());
            PlayerPrefs.Save();
        }

        private static void AppendRecordSummary(StringBuilder builder, ExperimentRecord record)
        {
            builder.AppendLine("Experiment #" + record.attemptNumber);
            builder.AppendLine("Ingredients: " + record.ingredientsUsed);
            AppendCheckedRecordLine(builder, "Order", record.ingredientOrder, record.orderStatus);
            if (record.orderStatus != "Not Checked" && !string.IsNullOrWhiteSpace(record.ingredientOrderDetails))
            {
                builder.AppendLine("Order Detail: " + record.ingredientOrderDetails);
            }

            AppendCheckedRecordLine(builder, "Ratio", record.ratioPattern, record.ratioStatus);
            AppendCheckedRecordLine(builder, "Combination", record.combinationPattern, record.combinationStatus);
            AppendCheckedRecordLine(builder, "Force", record.forceLevel.ToString(), record.forceStatus);
            AppendCheckedRecordLine(builder, "Speed", record.speedLevel.ToString(), record.speedStatus);
            builder.AppendLine("Duration: " + record.grindDuration.ToString("0.0") + "s");
            builder.AppendLine("Feedback: " + record.mainFeedback);
            if (!string.IsNullOrWhiteSpace(record.permanentHint))
            {
                builder.AppendLine("Permanent Hint: " + record.permanentHint);
            }

            builder.AppendLine("Hint: " + record.hintGiven);
            builder.AppendLine();
        }

        private static void AppendCheckedRecordLine(StringBuilder builder, string label, string value, string status)
        {
            if (status == "Not Checked")
            {
                return;
            }

            builder.AppendLine(label + ": " + value + " [" + status + "]");
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

        private static string BuildCombinationPatternText(IReadOnlyList<GrindingBatch> batches)
        {
            if (batches == null || batches.Count == 0)
            {
                return "None";
            }

            List<string> batchTexts = batches
                .Where(batch => batch != null && batch.ingredientsInBatch != null && batch.ingredientsInBatch.Any(x => x != null))
                .OrderBy(batch => batch.batchID)
                .Select(batch => "Batch " + batch.batchID + ": " + string.Join(" + ", batch.ingredientsInBatch.Where(x => x != null).Select(x => x.DisplayName)))
                .ToList();

            return batchTexts.Count == 0 ? "None" : string.Join("; ", batchTexts);
        }

        private static List<ExperimentIngredientEntry> BuildIngredientEntries(RecipeAttemptManager attempt, Dictionary<IngredientData, RatioLevel> ratio, EvaluationResult evaluation)
        {
            List<ExperimentIngredientEntry> entries = new List<ExperimentIngredientEntry>();
            if (attempt == null || attempt.IngredientAmounts == null)
            {
                return entries;
            }

            RecipeLevelData level = attempt.currentLevel;
            List<IngredientData> order = attempt.IngredientOrder != null
                ? attempt.IngredientOrder.Where(x => x != null).Distinct().ToList()
                : new List<IngredientData>();
            List<GrindingBatch> batches = attempt.GrindingBatches != null
                ? attempt.GrindingBatches.Where(batch => batch != null && batch.ingredientsInBatch != null).ToList()
                : new List<GrindingBatch>();

            foreach (IngredientData ingredient in attempt.IngredientAmounts.Select(x => x.ingredient).Where(x => x != null).Distinct())
            {
                GrindingBatch batch = batches.FirstOrDefault(x => x.ingredientsInBatch.Contains(ingredient));
                RatioLevel ratioLevel = ratio != null && ratio.TryGetValue(ingredient, out RatioLevel value) ? value : RatioLevel.None;
                ForceLevel forceLevel = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
                SpeedLevel speedLevel = attempt.pestleController != null ? attempt.pestleController.CurrentSpeedLevel : SpeedLevel.Medium;
                entries.Add(new ExperimentIngredientEntry
                {
                    ingredientID = ingredient.ingredientID,
                    ingredientName = ingredient.DisplayName,
                    levelID = level != null ? level.levelID : string.Empty,
                    levelName = level != null ? level.levelName : string.Empty,
                    traitDescription = ingredient.initialDescription,
                    ratio = ratioLevel != RatioLevel.None ? UIManager.GetRatioDisplayName(ratioLevel) : "Not checked",
                    order = FormatOrder(order, ingredient),
                    force = attempt.forceController != null ? forceLevel.ToString() : "Not checked",
                    speed = attempt.pestleController != null ? speedLevel.ToString() : "Not checked",
                    combination = FormatIngredientBatch(batch),
                    ratioStatus = BuildIngredientRatioStatus(level, ingredient, ratioLevel, evaluation),
                    orderStatus = BuildIngredientOrderStatus(level, ingredient, order, evaluation),
                    forceStatus = BuildIngredientForceStatus(level, ingredient, forceLevel, evaluation),
                    speedStatus = BuildIngredientSpeedStatus(level, ingredient, speedLevel, evaluation),
                    combinationStatus = BuildIngredientCombinationStatus(level, ingredient, batch, evaluation),
                    checkedRatio = level != null && level.enabledMechanics != null && level.enabledMechanics.enableRatio,
                    checkedOrder = level != null && level.enabledMechanics != null && level.enabledMechanics.enableIngredientOrder,
                    checkedForce = level != null && level.enabledMechanics != null && level.enabledMechanics.enableForce,
                    checkedSpeed = level != null && level.enabledMechanics != null && level.enabledMechanics.enableSpeed,
                    checkedCombination = level != null && level.enabledMechanics != null && level.enabledMechanics.enableCombination
                });
            }

            return entries;
        }

        private static string BuildIngredientRatioStatus(RecipeLevelData level, IngredientData ingredient, RatioLevel actual, EvaluationResult evaluation)
        {
            if (!IsMechanicChecked(level, MechanicType.Ratio))
            {
                return "Not Checked";
            }

            LevelIngredientProfile profile = FindProfile(level, ingredient, MechanicType.Ratio);
            if (profile != null)
            {
                return StatusFromBool(actual == profile.targetRatioLevel);
            }

            RatioRequirement requirement = level.correctRatioPattern != null
                ? level.correctRatioPattern.FirstOrDefault(x => x != null && x.ingredient == ingredient)
                : null;
            return requirement != null ? StatusFromBool(actual == requirement.ratioLevel) : BuildStatus(evaluation, MechanicType.Ratio);
        }

        private static string BuildIngredientOrderStatus(RecipeLevelData level, IngredientData ingredient, List<IngredientData> order, EvaluationResult evaluation)
        {
            if (!IsMechanicChecked(level, MechanicType.IngredientOrder))
            {
                return "Not Checked";
            }

            int actualIndex = order != null ? order.IndexOf(ingredient) : -1;
            LevelIngredientProfile profile = FindProfile(level, ingredient, MechanicType.IngredientOrder);
            if (profile != null)
            {
                return StatusFromBool(actualIndex == profile.targetOrderIndex);
            }

            int targetIndex = level.correctIngredientOrder != null ? level.correctIngredientOrder.IndexOf(ingredient) : -1;
            return targetIndex >= 0 ? StatusFromBool(actualIndex == targetIndex) : BuildStatus(evaluation, MechanicType.IngredientOrder);
        }

        private static string BuildIngredientForceStatus(RecipeLevelData level, IngredientData ingredient, ForceLevel actual, EvaluationResult evaluation)
        {
            if (!IsMechanicChecked(level, MechanicType.Force))
            {
                return "Not Checked";
            }

            LevelIngredientProfile profile = FindProfile(level, ingredient, MechanicType.Force);
            if (profile != null)
            {
                return StatusFromBool(actual == profile.targetForceLevel);
            }

            return StatusFromBool(actual == level.targetForceLevel);
        }

        private static string BuildIngredientSpeedStatus(RecipeLevelData level, IngredientData ingredient, SpeedLevel actual, EvaluationResult evaluation)
        {
            if (!IsMechanicChecked(level, MechanicType.Speed))
            {
                return "Not Checked";
            }

            LevelIngredientProfile profile = FindProfile(level, ingredient, MechanicType.Speed);
            if (profile != null)
            {
                return StatusFromBool(actual == profile.targetSpeedLevel);
            }

            return StatusFromBool(actual == level.targetSpeedLevel);
        }

        private static string BuildIngredientCombinationStatus(RecipeLevelData level, IngredientData ingredient, GrindingBatch batch, EvaluationResult evaluation)
        {
            if (!IsMechanicChecked(level, MechanicType.Combination))
            {
                return "Not Checked";
            }

            string actualKey = batch != null ? RecipeLevelData.BuildCombinationKey(batch.ingredientsInBatch) : string.Empty;
            LevelIngredientProfile profile = FindProfile(level, ingredient, MechanicType.Combination);
            if (profile != null)
            {
                return StatusFromBool(actualKey == profile.targetCombinationKey);
            }

            string targetKey = FindTargetCombinationKey(level, ingredient);
            return !string.IsNullOrWhiteSpace(targetKey)
                ? StatusFromBool(actualKey == targetKey)
                : BuildStatus(evaluation, MechanicType.Combination);
        }

        private static bool IsMechanicChecked(RecipeLevelData level, MechanicType mechanic)
        {
            return level != null && level.enabledMechanics != null && level.enabledMechanics.IsEnabled(mechanic);
        }

        private static LevelIngredientProfile FindProfile(RecipeLevelData level, IngredientData ingredient, MechanicType mechanic)
        {
            return level != null && ingredient != null
                ? level.GetProfilesForMechanic(mechanic).FirstOrDefault(x => x != null && x.ingredient == ingredient)
                : null;
        }

        private static string FindTargetCombinationKey(RecipeLevelData level, IngredientData ingredient)
        {
            if (level == null || ingredient == null || level.correctCombinationPattern == null || level.correctCombinationPattern.groups == null)
            {
                return string.Empty;
            }

            CombinationGroup group = level.correctCombinationPattern.groups
                .FirstOrDefault(x => x != null && x.ingredients != null && x.ingredients.Contains(ingredient));
            return group != null ? RecipeLevelData.BuildCombinationKey(group.ingredients) : string.Empty;
        }

        private static string StatusFromBool(bool correct)
        {
            return correct ? "Correct" : "Needs Work";
        }

        private static string FormatOrder(List<IngredientData> order, IngredientData ingredient)
        {
            int index = order != null ? order.IndexOf(ingredient) : -1;
            if (index < 0)
            {
                return "Not checked";
            }

            string position = "Position " + (index + 1);
            string before = index > 0
                ? "after " + string.Join(", ", order.Take(index).Where(x => x != null).Select(x => x.DisplayName))
                : "first";
            string after = index < order.Count - 1
                ? "before " + string.Join(", ", order.Skip(index + 1).Where(x => x != null).Select(x => x.DisplayName))
                : "last";

            return position + " (" + before + "; " + after + ")";
        }

        private static string BuildOrderRelationshipText(IReadOnlyList<IngredientData> order)
        {
            List<IngredientData> cleanOrder = order != null
                ? order.Where(x => x != null).ToList()
                : new List<IngredientData>();
            if (cleanOrder.Count == 0)
            {
                return string.Empty;
            }

            if (cleanOrder.Count == 1)
            {
                return cleanOrder[0].DisplayName + " was added first and last.";
            }

            List<string> relationships = new List<string>();
            for (int i = 0; i < cleanOrder.Count - 1; i++)
            {
                relationships.Add(cleanOrder[i].DisplayName + " before " + cleanOrder[i + 1].DisplayName);
            }

            return string.Join("; ", relationships);
        }

        private static string FormatIngredientBatch(GrindingBatch batch)
        {
            if (batch == null || batch.ingredientsInBatch == null || batch.ingredientsInBatch.Count == 0)
            {
                return "Not checked";
            }

            return "Batch " + batch.batchID + ": " + string.Join(" + ", batch.ingredientsInBatch.Where(x => x != null).Select(x => x.DisplayName));
        }

        public static List<IngredientLogEntry> BuildIngredientLogEntries()
        {
            LoadUnlockedClues();
            Dictionary<string, IngredientLogEntry> entries = new Dictionary<string, IngredientLogEntry>();

            foreach (ExperimentRecord record in sharedRecords)
            {
                if (record == null || record.ingredientEntries == null)
                {
                    continue;
                }

                foreach (ExperimentIngredientEntry entry in record.ingredientEntries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.ingredientID))
                    {
                        continue;
                    }

                    if (!entries.TryGetValue(entry.ingredientID, out IngredientLogEntry logEntry))
                    {
                        logEntry = new IngredientLogEntry
                        {
                            ingredientID = entry.ingredientID,
                            ingredientName = entry.ingredientName,
                            traitDescription = entry.traitDescription
                        };
                        entries[entry.ingredientID] = logEntry;
                    }

                    logEntry.levelNotes.Add(entry);
                    if (string.IsNullOrWhiteSpace(logEntry.traitDescription) && !string.IsNullOrWhiteSpace(entry.traitDescription))
                    {
                        logEntry.traitDescription = entry.traitDescription;
                    }
                }
            }

            foreach (UnlockedClueRecord clue in unlockedClues)
            {
                if (clue == null || string.IsNullOrWhiteSpace(clue.title))
                {
                    continue;
                }

                IEnumerable<IngredientLogEntry> relatedEntries = clue.relatedIngredientIDs != null && clue.relatedIngredientIDs.Count > 0
                    ? entries.Values.Where(x => clue.relatedIngredientIDs.Contains(x.ingredientID))
                    : entries.Values.Where(x => !string.IsNullOrWhiteSpace(x.ingredientName) && clue.title.Contains(x.ingredientName));
                foreach (IngredientLogEntry entry in relatedEntries)
                {
                    entry.clues.Add(clue);
                }
            }

            return entries.Values.OrderBy(x => x.ingredientName).ToList();
        }
    }

    public class ExperimentRecord
    {
        public int attemptNumber;
        public string ingredientsUsed;
        public string ingredientOrder;
        public string ingredientOrderDetails;
        public string ratioPattern;
        public string combinationPattern;
        public ForceLevel forceLevel;
        public SpeedLevel speedLevel;
        public float grindDuration;
        public string mainFeedback;
        public string hintGiven;
        public string permanentHint;
        public string orderStatus;
        public string combinationStatus;
        public string ratioStatus;
        public string forceStatus;
        public string speedStatus;
        public List<ExperimentIngredientEntry> ingredientEntries = new List<ExperimentIngredientEntry>();
    }

    public class ExperimentIngredientEntry
    {
        public string ingredientID;
        public string ingredientName;
        public string levelID;
        public string levelName;
        public string traitDescription;
        public string ratio;
        public string order;
        public string force;
        public string speed;
        public string combination;
        public string ratioStatus;
        public string orderStatus;
        public string forceStatus;
        public string speedStatus;
        public string combinationStatus;
        public bool checkedRatio;
        public bool checkedOrder;
        public bool checkedForce;
        public bool checkedSpeed;
        public bool checkedCombination;
    }

    public class IngredientLogEntry
    {
        public string ingredientID;
        public string ingredientName;
        public string traitDescription;
        public List<ExperimentIngredientEntry> levelNotes = new List<ExperimentIngredientEntry>();
        public List<UnlockedClueRecord> clues = new List<UnlockedClueRecord>();
    }

    public class UnlockedClueRecord
    {
        public string clueId;
        public string title;
        public string content;
        public MechanicType relatedDimension;
        public List<string> relatedIngredientIDs = new List<string>();
    }
}
