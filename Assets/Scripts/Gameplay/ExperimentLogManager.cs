using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace TheTasteReviver
{
    public class ExperimentLogManager : MonoBehaviour
    {
        public TMP_Text logText;
        public int maxVisibleRecords = 5;

        private static readonly List<ExperimentRecord> sharedRecords = new List<ExperimentRecord>();
        private static readonly List<UnlockedClueRecord> unlockedClues = new List<UnlockedClueRecord>();
        private static readonly List<IngredientData> ingredientCatalog = new List<IngredientData>();
        private static readonly List<RecipeLevelData> levelCatalog = new List<RecipeLevelData>();
        private static readonly HashSet<string> unlockedIngredientIDs = new HashSet<string>();
        private const string CluePrefsKey = "TheTasteReviver.UnlockedClues.v5";
        private const string IngredientPrefsKey = "TheTasteReviver.UnlockedIngredients.v5";
        private const string OrderArrow = " \u2192 ";

        public IReadOnlyList<ExperimentRecord> Records => sharedRecords;
        public static IReadOnlyList<ExperimentRecord> SharedRecords => sharedRecords;
        public static IReadOnlyList<UnlockedClueRecord> UnlockedClues => unlockedClues;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewGameSession()
        {
            sharedRecords.Clear();
            unlockedClues.Clear();
            ingredientCatalog.Clear();
            levelCatalog.Clear();
            unlockedIngredientIDs.Clear();

            PlayerPrefs.DeleteKey(CluePrefsKey);
            PlayerPrefs.DeleteKey(IngredientPrefsKey);
            PlayerPrefs.Save();
        }

        public static void SetIngredientCatalog(IEnumerable<IngredientData> ingredients)
        {
            ingredientCatalog.Clear();
            if (ingredients == null)
            {
                return;
            }

            ingredientCatalog.AddRange(ingredients.Where(x => x != null && !string.IsNullOrWhiteSpace(x.ingredientID)));
        }

        public static void SetLevelCatalog(IEnumerable<RecipeLevelData> levels)
        {
            levelCatalog.Clear();
            if (levels != null)
            {
                levelCatalog.AddRange(levels.Where(x => x != null));
            }

            RefreshUnlockedClueText();
        }

        private static void RefreshUnlockedClueText()
        {
            LoadUnlockedClues();
            if (unlockedClues.Count == 0 || levelCatalog.Count == 0)
            {
                return;
            }

            Dictionary<string, LevelClueData> currentClues = levelCatalog
                .Where(level => level.unlockCluesOnComplete != null)
                .SelectMany(level => level.unlockCluesOnComplete)
                .Where(clue => clue != null && !string.IsNullOrWhiteSpace(clue.clueId))
                .GroupBy(clue => clue.clueId)
                .ToDictionary(group => group.Key, group => group.First());

            bool changed = false;
            foreach (UnlockedClueRecord unlocked in unlockedClues)
            {
                if (unlocked == null
                    || string.IsNullOrWhiteSpace(unlocked.clueId)
                    || !currentClues.TryGetValue(unlocked.clueId, out LevelClueData current))
                {
                    continue;
                }

                List<string> currentIngredientIDs = current.relatedIngredients != null
                    ? current.relatedIngredients
                        .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ingredientID))
                        .Select(x => x.ingredientID)
                        .ToList()
                    : new List<string>();
                List<string> storedIngredientIDs = unlocked.relatedIngredientIDs ?? new List<string>();

                if (unlocked.title == current.title
                    && unlocked.content == current.content
                    && unlocked.relatedDimension == current.relatedDimension
                    && storedIngredientIDs.SequenceEqual(currentIngredientIDs))
                {
                    continue;
                }

                unlocked.title = current.title;
                unlocked.content = current.content;
                unlocked.relatedDimension = current.relatedDimension;
                unlocked.relatedIngredientIDs = currentIngredientIDs;
                changed = true;
            }

            if (changed)
            {
                SaveUnlockedClues();
            }
        }

        public static void UnlockIngredients(IEnumerable<IngredientData> ingredients)
        {
            LoadUnlockedIngredients();
            bool changed = false;
            if (ingredients != null)
            {
                foreach (IngredientData ingredient in ingredients)
                {
                    if (ingredient == null || string.IsNullOrWhiteSpace(ingredient.ingredientID))
                    {
                        continue;
                    }

                    changed |= unlockedIngredientIDs.Add(ingredient.ingredientID);
                }
            }

            if (changed)
            {
                SaveUnlockedIngredients();
            }
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

            UnlockIngredients(attempt.IngredientAmounts
                .Where(entry => entry != null && entry.ingredient != null)
                .Select(entry => entry.ingredient));

            Dictionary<IngredientData, RatioLevel> ratio = attempt.CalculateRatioPattern(out _);
            attempt.GetCurrentBatch();
            ForceLevel recordedForce = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
            SpeedLevel recordedSpeed = attempt.pestleController != null ? attempt.pestleController.EvaluatedSpeedLevel : SpeedLevel.Medium;
            if (IsFixedDimension(attempt.currentLevel, MechanicType.Force))
            {
                recordedForce = GetFixedForceLevel(attempt.currentLevel, null);
            }
            if (IsFixedDimension(attempt.currentLevel, MechanicType.Speed))
            {
                recordedSpeed = GetFixedSpeedLevel(attempt.currentLevel, null);
            }
            ExperimentRecord record = new ExperimentRecord
            {
                attemptNumber = sharedRecords.Count + 1,
                ingredientsUsed = string.Join(", ", attempt.IngredientAmounts.Where(x => x.ingredient != null).Select(x => x.ingredient.DisplayName)),
                ingredientOrder = BuildOrderArrowText(attempt.IngredientOrder),
                ingredientOrderDetails = BuildOrderRelationshipText(attempt.IngredientOrder),
                ratioPattern = string.Join(" / ", ratio.Select(x => x.Key.DisplayName + "=" + UIManager.GetRatioDisplayName(x.Value))),
                combinationPattern = BuildCombinationPatternText(attempt.GrindingBatches),
                forceLevel = recordedForce,
                speedLevel = recordedSpeed,
                grindDuration = attempt.pestleController != null ? attempt.pestleController.GrindDuration : 0f,
                mainFeedback = evaluation.mainFeedback,
                hintGiven = hint != null ? hint.text : string.Empty,
                permanentHint = permanentHint ?? string.Empty,
                orderStatus = BuildStatus(evaluation, MechanicType.IngredientOrder),
                combinationStatus = BuildStatus(evaluation, MechanicType.Combination),
                ratioStatus = BuildStatus(evaluation, MechanicType.Ratio),
                forceStatus = BuildDimensionStatus(attempt.currentLevel, evaluation, MechanicType.Force),
                speedStatus = BuildDimensionStatus(attempt.currentLevel, evaluation, MechanicType.Speed)
            };
            record.ingredientEntries.AddRange(BuildIngredientEntries(attempt, ratio, evaluation, record.attemptNumber));
            sharedRecords.Add(record);
            RefreshUI();
        }

        public List<UnlockedClueRecord> UnlockClues(RecipeLevelData level)
        {
            if (level == null)
            {
                return new List<UnlockedClueRecord>();
            }

            LoadUnlockedClues();
            List<UnlockedClueRecord> newlyUnlocked = new List<UnlockedClueRecord>();
            foreach (LevelClueData clue in level.unlockCluesOnComplete ?? Enumerable.Empty<LevelClueData>())
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
            }

            if (newlyUnlocked.Count > 0)
            {
                SaveUnlockedClues();
            }

            int clueCountBeforeFixedDimensions = newlyUnlocked.Count;
            UnlockFixedDimensionClues(level, newlyUnlocked);
            if (newlyUnlocked.Count > clueCountBeforeFixedDimensions)
            {
                RefreshUI();
            }

            return newlyUnlocked;
        }

        private static void UnlockFixedDimensionClues(RecipeLevelData level, List<UnlockedClueRecord> newlyUnlocked)
        {
            if (level == null || newlyUnlocked == null)
            {
                return;
            }

            AddFixedDimensionClue(level, MechanicType.Force, GetFixedForceLevel(level, null).ToString(), newlyUnlocked);
            AddFixedDimensionClue(level, MechanicType.Speed, GetFixedSpeedLevel(level, null).ToString(), newlyUnlocked);
            if (newlyUnlocked.Count > 0)
            {
                SaveUnlockedClues();
            }
        }

        private static void AddFixedDimensionClue(RecipeLevelData level, MechanicType mechanic, string value, List<UnlockedClueRecord> newlyUnlocked)
        {
            if (!IsFixedDimension(level, mechanic))
            {
                return;
            }

            string dimension = mechanic == MechanicType.Force ? "Force" : "Speed";
            string clueId = level.levelID + "_Fixed_" + dimension;
            if (unlockedClues.Any(x => x != null && x.clueId == clueId))
            {
                return;
            }

            UnlockedClueRecord clue = new UnlockedClueRecord
            {
                clueId = clueId,
                title = level.levelID + " Fixed " + dimension,
                content = dimension + ": " + value + " (fixed for this level).",
                relatedDimension = mechanic,
                relatedIngredientIDs = level.requiredIngredients != null
                    ? level.requiredIngredients.Where(x => x != null).Select(x => x.ingredientID).ToList()
                    : new List<string>()
            };
            unlockedClues.Add(clue);
            newlyUnlocked.Add(clue);
        }

        private void RefreshUI()
        {
            if (logText == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            foreach (ExperimentRecord record in OrderedRecords().Skip(Mathf.Max(0, sharedRecords.Count - maxVisibleRecords)))
            {
                AppendRecordSummary(builder, record);
            }

            logText.richText = true;
            logText.text = builder.ToString();
        }

        public static string BuildFullLogText()
        {
            LoadUnlockedClues();
            StringBuilder builder = new StringBuilder();

            if (unlockedClues.Count > 0)
            {
                builder.AppendLine("Unlocked Clues");
                builder.AppendLine();
                foreach (UnlockedClueRecord clue in unlockedClues)
                {
                    builder.AppendLine(FormatResultText(clue.title, "Correct"));
                    builder.AppendLine(FormatResultText(clue.content, "Correct"));
                    builder.AppendLine();
                }
            }

            if (sharedRecords.Count > 0)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.AppendLine("Experiment Notes");
                builder.AppendLine(StatusColorGuide);
                builder.AppendLine();
                foreach (ExperimentRecord record in OrderedRecords())
                {
                    AppendRecordSummary(builder, record);
                }
            }

            return builder.Length == 0 ? "No experiment records yet." : builder.ToString();
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

        private static void LoadUnlockedIngredients()
        {
            if (unlockedIngredientIDs.Count > 0)
            {
                return;
            }

            string serialized = PlayerPrefs.GetString(IngredientPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return;
            }

            foreach (string id in serialized.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    unlockedIngredientIDs.Add(id.Trim());
                }
            }
        }

        private static void SaveUnlockedIngredients()
        {
            PlayerPrefs.SetString(IngredientPrefsKey, string.Join("\n", unlockedIngredientIDs.OrderBy(x => x)));
            PlayerPrefs.Save();
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
            string recordStatus = IsRecordCorrect(record) ? "Correct" : "Incorrect";
            builder.AppendLine(FormatResultText("Experiment #" + record.attemptNumber + " (" + recordStatus + ")", recordStatus));
            builder.AppendLine("Ingredients: " + record.ingredientsUsed);
            AppendCheckedRecordLine(builder, "Order", record.ingredientOrder, record.orderStatus);
            AppendCheckedRecordLine(builder, "Ratio", record.ratioPattern, record.ratioStatus);
            AppendCheckedRecordLine(builder, "Combination", record.combinationPattern, record.combinationStatus);
            builder.AppendLine(FormatResultText("Force: " + record.forceLevel, record.forceStatus));
            builder.AppendLine(FormatResultText("Speed: " + record.speedLevel, record.speedStatus));
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

            builder.AppendLine(FormatResultText(label + ": " + value + (status == "Correct" || status == "Incorrect" ? " (" + status + ")" : string.Empty), status));
        }

        public static string StatusColorGuide => "Notes: " + FormatResultText("correct entries in deep green", "Correct") + "; " + FormatResultText("incorrect entries in deep red", "Incorrect") + ".";

        public static string FormatStatus(string status)
        {
            return FormatResultText(status, status);
        }

        public static string FormatResultText(string text, string status)
        {
            // Muted ink colors complement the journal's hand-painted paper.
            if (status == "Correct") return "<color=#2F5D46>" + text + "</color>";
            if (status == "Incorrect") return "<color=#8A3D3A>" + text + "</color>";
            return text;
        }

        private static IEnumerable<ExperimentRecord> OrderedRecords()
        {
            return sharedRecords
                .OrderBy(record => IsRecordCorrect(record) ? 0 : 1)
                .ThenBy(record => record.attemptNumber);
        }

        private static bool IsRecordCorrect(ExperimentRecord record)
        {
            if (record == null)
            {
                return false;
            }

            return record.orderStatus != "Incorrect"
                && record.combinationStatus != "Incorrect"
                && record.ratioStatus != "Incorrect"
                && record.forceStatus != "Incorrect"
                && record.speedStatus != "Incorrect";
        }

        private static string BuildStatus(EvaluationResult evaluation, MechanicType mechanic)
        {
            DimensionEvaluation dimension = evaluation.GetDimension(mechanic);
            if (dimension == null)
            {
                return "Not Checked";
            }

            return dimension.isCorrect ? "Correct" : "Incorrect";
        }

        private static string BuildDimensionStatus(RecipeLevelData level, EvaluationResult evaluation, MechanicType mechanic)
        {
            return IsMechanicChecked(level, mechanic)
                ? BuildStatus(evaluation, mechanic)
                : IsFixedDimension(level, mechanic) ? "Fixed" : "Not Checked";
        }

        private static bool IsFixedDimension(RecipeLevelData level, MechanicType mechanic)
        {
            return level != null
                && (mechanic == MechanicType.Force || mechanic == MechanicType.Speed)
                && !IsMechanicChecked(level, mechanic);
        }

        private static ForceLevel GetFixedForceLevel(RecipeLevelData level, IngredientData ingredient)
        {
            LevelIngredientProfile profile = FindProfile(level, ingredient, MechanicType.Force);
            return profile != null ? profile.targetForceLevel : level != null ? level.targetForceLevel : ForceLevel.Medium;
        }

        private static SpeedLevel GetFixedSpeedLevel(RecipeLevelData level, IngredientData ingredient)
        {
            LevelIngredientProfile profile = FindProfile(level, ingredient, MechanicType.Speed);
            return profile != null ? profile.targetSpeedLevel : level != null ? level.targetSpeedLevel : SpeedLevel.Medium;
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

        private static List<ExperimentIngredientEntry> BuildIngredientEntries(RecipeAttemptManager attempt, Dictionary<IngredientData, RatioLevel> ratio, EvaluationResult evaluation, int attemptNumber)
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
                ForceLevel forceLevel = batch != null ? batch.forceLevel : attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
                SpeedLevel speedLevel = batch != null ? batch.speedLevel : attempt.pestleController != null ? attempt.pestleController.EvaluatedSpeedLevel : SpeedLevel.Medium;
                bool forceIsFixed = IsFixedDimension(level, MechanicType.Force);
                bool speedIsFixed = IsFixedDimension(level, MechanicType.Speed);
                if (forceIsFixed) forceLevel = GetFixedForceLevel(level, ingredient);
                if (speedIsFixed) speedLevel = GetFixedSpeedLevel(level, ingredient);
                float grindDuration = batch != null ? batch.grindDuration : attempt.pestleController != null ? attempt.pestleController.GrindDuration : 0f;
                entries.Add(new ExperimentIngredientEntry
                {
                    attemptNumber = attemptNumber,
                    ingredientID = ingredient.ingredientID,
                    ingredientName = ingredient.DisplayName,
                    levelID = level != null ? level.levelID : string.Empty,
                    levelName = level != null ? level.levelName : string.Empty,
                    traitDescription = ingredient.initialDescription,
                    ratio = ratioLevel != RatioLevel.None ? UIManager.GetRatioDisplayName(ratioLevel) : "Not checked",
                    order = BuildOrderArrowText(order),
                    force = attempt.forceController != null || forceIsFixed ? forceLevel.ToString() : "Not checked",
                    speed = attempt.pestleController != null || speedIsFixed ? speedLevel.ToString() : "Not checked",
                    grindDuration = grindDuration.ToString("0.0") + "s",
                    combination = FormatIngredientBatch(batch),
                    ratioStatus = BuildIngredientRatioStatus(level, ingredient, ratioLevel, evaluation),
                    orderStatus = BuildIngredientOrderStatus(level, ingredient, order, evaluation),
                    forceStatus = BuildIngredientForceStatus(level, ingredient, forceLevel, evaluation),
                    speedStatus = BuildIngredientSpeedStatus(level, ingredient, speedLevel, evaluation),
                    combinationStatus = BuildIngredientCombinationStatus(level, ingredient, batch, evaluation),
                    combinationNote = BuildIngredientCombinationNote(level, ingredient, batch, batches, evaluation),
                    checkedRatio = level != null && level.enabledMechanics != null && level.enabledMechanics.enableRatio,
                    checkedOrder = level != null && level.enabledMechanics != null && level.enabledMechanics.enableIngredientOrder,
                    checkedForce = IsMechanicChecked(level, MechanicType.Force) || forceIsFixed,
                    checkedSpeed = IsMechanicChecked(level, MechanicType.Speed) || speedIsFixed,
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
                return IsFixedDimension(level, MechanicType.Force) ? "Fixed" : "Not Checked";
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
                return IsFixedDimension(level, MechanicType.Speed) ? "Fixed" : "Not Checked";
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

            if (level != null && level.requireFinalCombinedBatch)
            {
                return StatusFromBool(IsValidPreparationBatch(level, ingredient, batch));
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

        private static string BuildIngredientCombinationNote(
            RecipeLevelData level,
            IngredientData ingredient,
            GrindingBatch batch,
            IReadOnlyList<GrindingBatch> batches,
            EvaluationResult evaluation)
        {
            if (level == null || !level.requireFinalCombinedBatch)
            {
                return string.Empty;
            }

            bool preparationBatchIsCorrect = IsValidPreparationBatch(level, ingredient, batch);
            if (!preparationBatchIsCorrect)
            {
                if (level.allowAnyPairPreparation)
                {
                    return "Preparation issue: grind two ingredients together and the remaining ingredient separately before the final blend.";
                }

                IReadOnlyList<IngredientData> targetGroup = level.correctCombinationPattern != null
                    && level.correctCombinationPattern.groups != null
                    ? level.correctCombinationPattern.groups
                        .Where(group => group != null && group.ingredients != null && group.ingredients.Contains(ingredient))
                        .SelectMany(group => group.ingredients)
                        .Where(x => x != null)
                        .Distinct()
                        .ToList()
                    : new List<IngredientData>();
                string targetText = targetGroup.Count > 0
                    ? string.Join(" + ", targetGroup.Select(x => x.DisplayName))
                    : (ingredient != null ? ingredient.DisplayName : "This ingredient");
                return "Preparation issue: " + targetText + " needed its own correct batch before the final blend.";
            }

            if (!AreRequiredPreparationBatchesComplete(level, batches))
            {
                return "Preparation: this batch is correct, but another preparation batch still needs work before the final blend.";
            }

            string combinedIngredients = string.Join(" + ", GetCombinationIngredients(level).Select(x => x.DisplayName));
            bool finalBlendIsCorrect = BuildStatus(evaluation, MechanicType.Combination) == "Correct";
            return finalBlendIsCorrect
                ? "Final Blend: " + combinedIngredients + " powders combined"
                : "Final Blend: MISSING - combine the prepared " + combinedIngredients + " powders.";
        }

        private static bool IsValidPreparationBatch(RecipeLevelData level, IngredientData ingredient, GrindingBatch batch)
        {
            if (level == null || ingredient == null || batch == null || batch.ingredientsInBatch == null)
            {
                return false;
            }

            List<IngredientData> actualIngredients = batch.ingredientsInBatch.Where(x => x != null).Distinct().ToList();
            if (level.allowAnyPairPreparation)
            {
                List<IngredientData> requiredIngredients = GetCombinationIngredients(level);
                return actualIngredients.Count > 0
                    && actualIngredients.Count < requiredIngredients.Count
                    && actualIngredients.All(requiredIngredients.Contains);
            }

            string targetKey = FindTargetCombinationKey(level, ingredient);
            return !string.IsNullOrWhiteSpace(targetKey)
                && RecipeLevelData.BuildCombinationKey(actualIngredients) == targetKey;
        }

        private static bool AreRequiredPreparationBatchesComplete(RecipeLevelData level, IReadOnlyList<GrindingBatch> batches)
        {
            if (level == null || batches == null)
            {
                return false;
            }

            List<IngredientData> requiredIngredients = GetCombinationIngredients(level);
            if (requiredIngredients.Count == 0)
            {
                return false;
            }

            List<List<IngredientData>> preparationBatches = batches
                .Where(batch => batch != null && batch.ingredientsInBatch != null)
                .Select(batch => batch.ingredientsInBatch.Where(x => x != null).Distinct().ToList())
                .Where(ingredients => ingredients.Count > 0
                    && ingredients.Count < requiredIngredients.Count
                    && ingredients.All(requiredIngredients.Contains))
                .ToList();

            if (level.allowAnyPairPreparation)
            {
                return preparationBatches.Count == 2
                    && preparationBatches.SelectMany(x => x).Count() == requiredIngredients.Count
                    && preparationBatches.SelectMany(x => x).Distinct().Count() == requiredIngredients.Count;
            }

            List<string> actualKeys = preparationBatches
                .Select(RecipeLevelData.BuildCombinationKey)
                .ToList();
            List<string> targetKeys = level.correctCombinationPattern != null && level.correctCombinationPattern.groups != null
                ? level.correctCombinationPattern.groups
                    .Where(group => group != null && group.ingredients != null && group.ingredients.Count > 0)
                    .Select(group => RecipeLevelData.BuildCombinationKey(group.ingredients))
                    .ToList()
                : new List<string>();
            return targetKeys.Count > 0 && targetKeys.All(actualKeys.Contains);
        }

        private static List<IngredientData> GetCombinationIngredients(RecipeLevelData level)
        {
            if (level == null)
            {
                return new List<IngredientData>();
            }

            List<IngredientData> profileIngredients = level.GetProfilesForMechanic(MechanicType.Combination)
                .Where(profile => profile != null && profile.ingredient != null)
                .Select(profile => profile.ingredient)
                .Distinct()
                .ToList();
            return profileIngredients.Count > 0
                ? profileIngredients
                : level.requiredIngredients != null
                    ? level.requiredIngredients.Where(x => x != null).Distinct().ToList()
                    : new List<IngredientData>();
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
            return correct ? "Correct" : "Incorrect";
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
                return "1. " + cleanOrder[0].DisplayName;
            }

            return string.Join(OrderArrow, cleanOrder.Select((ingredient, index) => (index + 1) + ". " + ingredient.DisplayName));
        }

        private static string BuildOrderArrowText(IReadOnlyList<IngredientData> order)
        {
            return order != null
                ? string.Join(OrderArrow, order.Where(x => x != null).Select(x => x.DisplayName))
                : string.Empty;
        }

        private static string FormatIngredientBatch(GrindingBatch batch)
        {
            if (batch == null || batch.ingredientsInBatch == null || batch.ingredientsInBatch.Count == 0)
            {
                return "Not checked";
            }

            return "Together: " + string.Join(" + ", batch.ingredientsInBatch.Where(x => x != null).Select(x => x.DisplayName));
        }

        public static List<IngredientLogEntry> BuildIngredientLogEntries()
        {
            LoadUnlockedClues();
            LoadUnlockedIngredients();
            Dictionary<string, IngredientLogEntry> entries = new Dictionary<string, IngredientLogEntry>();

            foreach (string ingredientID in unlockedIngredientIDs)
            {
                EnsureIngredientLogEntry(entries, ingredientID);
            }

            // Permanent clues are only awarded after a correct evaluation, so
            // their related ingredients are also authoritative unlock evidence.
            // This keeps the log working even if PlayerPrefs cannot be written.
            foreach (UnlockedClueRecord clue in unlockedClues)
            {
                if (clue == null || clue.relatedIngredientIDs == null)
                {
                    continue;
                }

                foreach (string ingredientID in clue.relatedIngredientIDs)
                {
                    EnsureIngredientLogEntry(entries, ingredientID);
                }
            }

            // Evaluating an ingredient unlocks its log entry immediately.
            // The attempt note itself remains authoritative even if persistent
            // ingredient unlock storage is unavailable.
            foreach (ExperimentRecord record in sharedRecords)
            {
                if (record == null || record.ingredientEntries == null)
                {
                    continue;
                }

                foreach (ExperimentIngredientEntry note in record.ingredientEntries)
                {
                    if (note == null
                        || string.IsNullOrWhiteSpace(note.ingredientID))
                    {
                        continue;
                    }

                    IngredientLogEntry logEntry = EnsureIngredientLogEntry(entries, note.ingredientID);
                    if (logEntry == null)
                    {
                        continue;
                    }

                    logEntry.levelNotes.Add(note);
                }
            }

            foreach (UnlockedClueRecord clue in unlockedClues)
            {
                if (clue == null || string.IsNullOrWhiteSpace(clue.title))
                {
                    continue;
                }

                IEnumerable<IngredientLogEntry> relatedEntries = clue.relatedIngredientIDs != null && clue.relatedIngredientIDs.Count > 0
                    ? clue.relatedIngredientIDs
                        .Select(id => entries.TryGetValue(id, out IngredientLogEntry entry) ? entry : null)
                        .Where(entry => entry != null)
                    : entries.Values.Where(x => !string.IsNullOrWhiteSpace(x.ingredientName) && clue.title.Contains(x.ingredientName));
                foreach (IngredientLogEntry entry in relatedEntries)
                {
                    entry.clues.Add(clue);
                }
            }

            return entries.Values.OrderBy(x => x.ingredientName).ToList();
        }

        private static IngredientLogEntry EnsureIngredientLogEntry(Dictionary<string, IngredientLogEntry> entries, string ingredientID)
        {
            if (entries == null || string.IsNullOrWhiteSpace(ingredientID))
            {
                return null;
            }

            if (entries.TryGetValue(ingredientID, out IngredientLogEntry existing))
            {
                return existing;
            }

            IngredientData ingredient = ingredientCatalog.FirstOrDefault(x => x != null && x.ingredientID == ingredientID);
            IngredientLogEntry created = new IngredientLogEntry
            {
                ingredientID = ingredientID,
                ingredientName = ingredient != null ? ingredient.DisplayName : ingredientID,
                aromaType = ingredient != null ? ingredient.aromaType : string.Empty,
                traitDescription = ingredient != null ? ingredient.initialDescription : string.Empty
            };
            entries[ingredientID] = created;
            return created;
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
        public int attemptNumber;
        public string ingredientID;
        public string ingredientName;
        public string levelID;
        public string levelName;
        public string traitDescription;
        public string ratio;
        public string order;
        public string force;
        public string speed;
        public string grindDuration;
        public string combination;
        public string ratioStatus;
        public string orderStatus;
        public string forceStatus;
        public string speedStatus;
        public string combinationStatus;
        public string combinationNote;
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
        public string aromaType;
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
