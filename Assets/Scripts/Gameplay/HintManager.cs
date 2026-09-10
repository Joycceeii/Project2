using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheTasteReviver
{
    public class HintManager : MonoBehaviour
    {
        private readonly Dictionary<MechanicType, int> hintCounts = new Dictionary<MechanicType, int>();
        private readonly List<string> givenHints = new List<string>();

        public IReadOnlyList<string> GivenHints => givenHints;

        public HintResult GetNextHint(RecipeLevelData level, RecipeAttemptManager attempt, EvaluationResult evaluation)
        {
            if (level == null || attempt == null || evaluation == null)
            {
                return new HintResult(MechanicType.IngredientSelection, "No hint available.", 0, givenHints);
            }

            List<MechanicType> priority = level.hintSettings.hintPriority != null && level.hintSettings.hintPriority.Count > 0
                ? level.hintSettings.hintPriority
                : new List<MechanicType> { MechanicType.IngredientOrder, MechanicType.Ratio, MechanicType.Combination, MechanicType.Speed, MechanicType.Force };
            List<MechanicType> activePriority = priority
                .Where(mechanic => IsEnabled(level, mechanic))
                .ToList();

            foreach (MechanicType mechanic in activePriority)
            {
                if (evaluation.IsCorrect(mechanic))
                {
                    continue;
                }

                if ((mechanic == MechanicType.Speed || mechanic == MechanicType.Force)
                    && IsEnabled(level, MechanicType.Combination)
                    && !evaluation.IsCorrect(MechanicType.Combination)
                    && !HasIngredientProfilesForMechanic(level, mechanic))
                {
                    continue;
                }

                if (!CanGiveMore(level, mechanic))
                {
                    continue;
                }

                string hint = BuildHint(level, attempt, mechanic, givenHints);
                if (string.IsNullOrWhiteSpace(hint) || givenHints.Contains(hint))
                {
                    continue;
                }

                Increment(mechanic);
                givenHints.Add(hint);
                return new HintResult(mechanic, hint, hintCounts[mechanic], givenHints);
            }

            DimensionEvaluation missedDimension = evaluation.dimensions.FirstOrDefault(dimension => dimension != null && !dimension.isCorrect && IsEnabled(level, dimension.mechanic));
            if (missedDimension != null)
            {
                string missedHint = BuildHint(level, attempt, missedDimension.mechanic, givenHints);
                if (!string.IsNullOrWhiteSpace(missedHint))
                {
                    return new HintResult(missedDimension.mechanic, missedHint, givenHints.Count, givenHints);
                }
            }

            string fallback = GetReusableLevelHint(level);
            if (string.IsNullOrWhiteSpace(fallback))
            {
                fallback = string.IsNullOrWhiteSpace(level.fallbackHintText)
                    ? "No new hint is available for the current level dimensions."
                    : level.fallbackHintText;
            }

            return new HintResult(MechanicType.IngredientSelection, fallback, givenHints.Count, givenHints);
        }

        public void ResetHints()
        {
            hintCounts.Clear();
            givenHints.Clear();
        }

        public HintResult GetStepHint(RecipeLevelData level, RecipeAttemptManager attempt, MechanicType mechanic, IngredientData targetIngredient = null)
        {
            if (level == null || attempt == null || !IsEnabled(level, mechanic))
            {
                return null;
            }

            string hint = mechanic == MechanicType.Combination
                ? BuildCombinationHint(level, attempt, givenHints, targetIngredient)
                : null;
            if (string.IsNullOrWhiteSpace(hint))
            {
                hint = BuildCustomHint(level, mechanic, givenHints, targetIngredient);
            }
            if (string.IsNullOrWhiteSpace(hint))
            {
                hint = BuildTraitDrivenHint(level, attempt, mechanic, targetIngredient);
            }

            if (string.IsNullOrWhiteSpace(hint))
            {
                return null;
            }

            return new HintResult(mechanic, hint, 0, givenHints);
        }

        private bool CanGiveMore(RecipeLevelData level, MechanicType mechanic)
        {
            int current = hintCounts.TryGetValue(mechanic, out int count) ? count : 0;
            if (mechanic == MechanicType.IngredientOrder) return current < level.hintSettings.maxOrderHints;
            if (mechanic == MechanicType.Ratio) return current < level.hintSettings.maxRatioHints;
            if (mechanic == MechanicType.Combination) return current < level.hintSettings.maxCombinationHints;
            if (mechanic == MechanicType.Speed || mechanic == MechanicType.Force) return current < level.hintSettings.maxProcessHints;
            return false;
        }

        private static bool HasIngredientProfilesForMechanic(RecipeLevelData level, MechanicType mechanic)
        {
            return level != null
                && level.ingredientProfiles != null
                && level.ingredientProfiles.Any(profile => profile != null && profile.IsEnabled(mechanic));
        }

        private void Increment(MechanicType mechanic)
        {
            if (!hintCounts.ContainsKey(mechanic))
            {
                hintCounts[mechanic] = 0;
            }

            hintCounts[mechanic]++;
        }

        private static string BuildHint(RecipeLevelData level, RecipeAttemptManager attempt, MechanicType mechanic, IReadOnlyList<string> usedHints)
        {
            if (mechanic == MechanicType.Combination)
            {
                string combinationHint = BuildCombinationHint(level, attempt, usedHints);
                if (!string.IsNullOrWhiteSpace(combinationHint))
                {
                    return combinationHint;
                }
            }

            string responseHint = BuildIngredientResponseHint(level, attempt, mechanic, usedHints);
            if (!string.IsNullOrWhiteSpace(responseHint))
            {
                return responseHint;
            }

            string customHint = BuildCustomHint(level, mechanic, usedHints);
            if (!string.IsNullOrWhiteSpace(customHint))
            {
                return customHint;
            }

            string traitHint = BuildTraitDrivenHint(level, attempt, mechanic);
            if (!string.IsNullOrWhiteSpace(traitHint))
            {
                return traitHint;
            }

            switch (mechanic)
            {
                case MechanicType.IngredientOrder:
                    return BuildOrderHint(level, attempt);
                case MechanicType.Ratio:
                    return BuildRatioHint(level, attempt);
                case MechanicType.Combination:
                    return BuildCombinationHint(level, attempt, usedHints);
                case MechanicType.Speed:
                case MechanicType.Force:
                    return BuildProcessHint(level, attempt);
                default:
                    return "One key relationship still needs checking.";
            }
        }

        private static string BuildIngredientResponseHint(RecipeLevelData level, RecipeAttemptManager attempt, MechanicType mechanic, IReadOnlyList<string> usedHints)
        {
            if (level == null || attempt == null || level.ingredientProfiles == null || level.ingredientProfiles.Count == 0)
            {
                return null;
            }

            ForceLevel actualForce = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
            SpeedLevel actualSpeed = attempt.pestleController != null ? attempt.pestleController.EvaluatedSpeedLevel : SpeedLevel.Medium;
            Dictionary<IngredientData, RatioLevel> actualRatios = attempt.CalculateRatioPattern(out _);

            return level.ingredientProfiles
                .Where(profile => profile != null
                    && profile.ingredient != null
                    && profile.IsEnabled(mechanic)
                    && IsProfileIncorrect(level, profile, mechanic, attempt, actualForce, actualSpeed, actualRatios))
                .SelectMany(profile => profile.responseHintRules != null
                    ? profile.responseHintRules.Where(rule => MatchesRule(rule, profile, mechanic, attempt, actualForce, actualSpeed, actualRatios))
                    : Enumerable.Empty<IngredientResponseHintRule>())
                .Where(rule => rule != null
                    && !string.IsNullOrWhiteSpace(rule.hintText)
                    && (usedHints == null || !usedHints.Contains(rule.hintText)))
                .OrderByDescending(rule => rule.priority)
                .Select(rule => rule.hintText)
                .FirstOrDefault();
        }

        private static bool IsProfileIncorrect(RecipeLevelData level, LevelIngredientProfile profile, MechanicType mechanic, RecipeAttemptManager attempt, ForceLevel actualForce, SpeedLevel actualSpeed, Dictionary<IngredientData, RatioLevel> actualRatios)
        {
            switch (mechanic)
            {
                case MechanicType.Force:
                    return GetForceForProfile(profile, attempt, actualForce) != profile.targetForceLevel;
                case MechanicType.Speed:
                    return GetSpeedForProfile(profile, attempt, actualSpeed) != profile.targetSpeedLevel;
                case MechanicType.Ratio:
                    return !actualRatios.TryGetValue(profile.ingredient, out RatioLevel ratio) || ratio != profile.targetRatioLevel;
                case MechanicType.Combination:
                    if (level != null && level.requireFinalCombinedBatch)
                    {
                        List<IngredientData> requiredIngredients = GetRequiredCombinationIngredients(level);
                        return !HasPreparedThenFinalCombinedBatches(level, attempt, attempt.GrindingBatches, requiredIngredients);
                    }

                    return FindActualCombinationKey(profile.ingredient, attempt.GrindingBatches) != profile.targetCombinationKey;
                case MechanicType.IngredientOrder:
                    return attempt.IngredientOrder.Where(x => x != null).Distinct().ToList().IndexOf(profile.ingredient) != profile.targetOrderIndex;
                default:
                    return false;
            }
        }

        private static bool MatchesRule(IngredientResponseHintRule rule, LevelIngredientProfile profile, MechanicType mechanic, RecipeAttemptManager attempt, ForceLevel actualForce, SpeedLevel actualSpeed, Dictionary<IngredientData, RatioLevel> actualRatios)
        {
            if (rule == null || rule.mechanic != mechanic)
            {
                return false;
            }

            if (CountMatchConditions(rule) < 2 && !rule.matchCombination)
            {
                return false;
            }

            if (rule.matchForce && actualForce != rule.forceValue)
            {
                return false;
            }

            if (rule.matchSpeed && actualSpeed != rule.speedValue)
            {
                return false;
            }

            if (rule.matchRatio)
            {
                RatioLevel actualRatio = actualRatios.TryGetValue(profile.ingredient, out RatioLevel ratio) ? ratio : RatioLevel.None;
                if (actualRatio != rule.ratioValue)
                {
                    return false;
                }
            }

            if (rule.matchCombination && FindActualCombinationKey(profile.ingredient, attempt.GrindingBatches) != rule.combinationKey)
            {
                return false;
            }

            return true;
        }

        private static int CountMatchConditions(IngredientResponseHintRule rule)
        {
            if (rule == null)
            {
                return 0;
            }

            int count = 0;
            if (rule.matchForce) count++;
            if (rule.matchSpeed) count++;
            if (rule.matchRatio) count++;
            if (rule.matchCombination) count++;
            return count;
        }

        private static string BuildTraitDrivenHint(RecipeLevelData level, RecipeAttemptManager attempt, MechanicType mechanic, IngredientData targetIngredient = null)
        {
            LevelIngredientProfile profile = FindFirstIncorrectProfile(level, attempt, mechanic, targetIngredient);
            if (profile == null || profile.ingredient == null)
            {
                return null;
            }

            string nudge = BuildDimensionNudge(profile, attempt, mechanic);
            return mechanic == MechanicType.Force || mechanic == MechanicType.Speed
                ? nudge
                : profile.ingredient.DisplayName + ": " + nudge;
        }

        private static LevelIngredientProfile FindFirstIncorrectProfile(RecipeLevelData level, RecipeAttemptManager attempt, MechanicType mechanic, IngredientData targetIngredient = null)
        {
            if (level == null || attempt == null || level.ingredientProfiles == null)
            {
                return null;
            }

            ForceLevel actualForce = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
            SpeedLevel actualSpeed = attempt.pestleController != null ? attempt.pestleController.EvaluatedSpeedLevel : SpeedLevel.Medium;
            Dictionary<IngredientData, RatioLevel> actualRatios = attempt.CalculateRatioPattern(out _);
            IEnumerable<LevelIngredientProfile> profiles = level.ingredientProfiles
                .Where(profile => profile != null && profile.ingredient != null && profile.IsEnabled(mechanic))
                .Where(profile => targetIngredient == null || profile.ingredient == targetIngredient);

            return profiles
                .FirstOrDefault(profile => IsProfileIncorrect(level, profile, mechanic, attempt, actualForce, actualSpeed, actualRatios));
        }

        private static string BuildDimensionNudge(LevelIngredientProfile profile, RecipeAttemptManager attempt, MechanicType mechanic)
        {
            switch (mechanic)
            {
                case MechanicType.Force:
                    return BuildForceNudge(profile, attempt);
                case MechanicType.Speed:
                    return BuildSpeedNudge(profile, attempt);
                case MechanicType.Ratio:
                    return BuildRatioNudge(profile, attempt);
                case MechanicType.Combination:
                    return "Check whether this ingredient belongs in this batch.";
                case MechanicType.IngredientOrder:
                    return "Check whether this ingredient should be added earlier or later.";
                default:
                    return "Use its trait to rethink this attempt.";
            }
        }

        private static string BuildForceNudge(LevelIngredientProfile profile, RecipeAttemptManager attempt)
        {
            ForceLevel actual = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
            actual = GetForceForProfile(profile, attempt, actual);
            string name = profile.ingredient != null ? profile.ingredient.DisplayName : "This ingredient";
            return name + " needs " + profile.targetForceLevel + " force.";
        }

        private static string BuildSpeedNudge(LevelIngredientProfile profile, RecipeAttemptManager attempt)
        {
            SpeedLevel actual = attempt.pestleController != null ? attempt.pestleController.EvaluatedSpeedLevel : SpeedLevel.Medium;
            actual = GetSpeedForProfile(profile, attempt, actual);
            string name = profile.ingredient != null ? profile.ingredient.DisplayName : "This ingredient";
            if (profile.IsEnabled(MechanicType.Force))
            {
                return name + " needs " + profile.targetSpeedLevel + " speed and " + profile.targetForceLevel + " force.";
            }

            return name + " needs " + profile.targetSpeedLevel + " speed.";
        }

        private static string BuildRatioNudge(LevelIngredientProfile profile, RecipeAttemptManager attempt)
        {
            Dictionary<IngredientData, RatioLevel> actual = attempt.CalculateRatioPattern(out _);
            RatioLevel actualRatio = actual.TryGetValue(profile.ingredient, out RatioLevel ratio) ? ratio : RatioLevel.None;
            return "Choose " + UIManager.GetRatioDisplayName(profile.targetRatioLevel) + " amount.";
        }

        private static string BuildCustomHint(RecipeLevelData level, MechanicType mechanic, IReadOnlyList<string> usedHints, IngredientData targetIngredient = null)
        {
            if (level == null || level.progressiveHintRules == null || level.progressiveHintRules.Count == 0)
            {
                return null;
            }

            return level.progressiveHintRules
                .Where(rule => rule != null
                    && rule.mechanic == mechanic
                    && !string.IsNullOrWhiteSpace(rule.hintText)
                    && MatchesTargetIngredient(rule, targetIngredient)
                    && (usedHints == null || !usedHints.Contains(rule.hintText)))
                .OrderByDescending(rule => rule.priority)
                .Select(rule => rule.hintText)
                .FirstOrDefault();
        }

        private static bool MatchesTargetIngredient(ProgressiveHintRule rule, IngredientData targetIngredient)
        {
            if (targetIngredient == null || rule == null || string.IsNullOrWhiteSpace(rule.ruleId))
            {
                return true;
            }

            return rule.ruleId.Contains(targetIngredient.ingredientID);
        }

        private static string GetReusableLevelHint(RecipeLevelData level)
        {
            if (level == null || level.progressiveHintRules == null || level.progressiveHintRules.Count == 0)
            {
                return null;
            }

            return level.progressiveHintRules
                .Where(rule => rule != null && !string.IsNullOrWhiteSpace(rule.hintText))
                .OrderByDescending(rule => rule.priority)
                .Select(rule => rule.hintText)
                .FirstOrDefault();
        }

        private static string BuildOrderHint(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            List<IngredientData> target = level.correctIngredientOrder.Where(x => x != null).ToList();
            List<IngredientData> actual = attempt.IngredientOrder.Where(x => x != null).Distinct().ToList();
            for (int i = 0; i < target.Count - 1; i++)
            {
                IngredientData before = target[i];
                IngredientData after = target[i + 1];
                int actualBefore = actual.IndexOf(before);
                int actualAfter = actual.IndexOf(after);
                if (actualBefore >= 0 && actualAfter >= 0 && actualBefore > actualAfter)
                {
                    return "Add " + before.DisplayName + " before " + after.DisplayName + ".";
                }
            }

            if (target.Count > 0)
            {
                return target[0].DisplayName + " should be the starting ingredient.";
            }

            return "One ingredient may be placed too early.";
        }

        private static string BuildRatioHint(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            Dictionary<IngredientData, RatioLevel> actual = attempt.CalculateRatioPattern(out _);
            RatioRequirement target = level.correctRatioPattern
                .Where(x => x.ingredient != null && (!actual.TryGetValue(x.ingredient, out RatioLevel value) || value != x.ratioLevel))
                .OrderByDescending(x =>
                {
                    RatioLevel actualLevel = actual.TryGetValue(x.ingredient, out RatioLevel value) ? value : RatioLevel.None;
                    return Mathf.Abs((int)actualLevel - (int)x.ratioLevel);
                })
                .FirstOrDefault();
            if (target == null)
            {
                return "The amounts are close, but one ingredient still needs adjustment.";
            }

            string direction = "needs " + UIManager.GetRatioDisplayName(target.ratioLevel) + " amount";
            if (actual.TryGetValue(target.ingredient, out RatioLevel actualLevel))
            {
                direction = actualLevel == target.ratioLevel
                    ? "has the right amount"
                    : "needs " + UIManager.GetRatioDisplayName(target.ratioLevel) + " amount";
            }

            return target.ingredient.DisplayName + " " + direction + ".";
        }

        private static string BuildCombinationHint(RecipeLevelData level, RecipeAttemptManager attempt, IReadOnlyList<string> usedHints, IngredientData targetIngredient = null)
        {
            if (level == null || attempt == null || level.correctCombinationPattern == null || level.correctCombinationPattern.groups.Count == 0)
            {
                return "Some ingredients may work together, while others may need separate handling.";
            }

            IEnumerable<CombinationGroup> targetGroups = level.correctCombinationPattern.groups
                .Where(group => group != null && group.ingredients != null && group.ingredients.Any(ingredient => ingredient != null));

            if (targetIngredient != null)
            {
                targetGroups = targetGroups
                    .Where(group => group.ingredients.Contains(targetIngredient))
                    .Concat(level.correctCombinationPattern.groups.Where(group => group != null && group.ingredients != null && !group.ingredients.Contains(targetIngredient)));
            }

            List<GrindingBatch> actualBatches = attempt.GrindingBatches != null
                ? attempt.GrindingBatches.Where(batch => batch != null && batch.ingredientsInBatch != null).ToList()
                : new List<GrindingBatch>();

            if (level.requireFinalCombinedBatch)
            {
                List<IngredientData> requiredIngredients = GetRequiredCombinationIngredients(level);
                if (level.allowAnyPairPreparation)
                {
                    if (!TryFindAnyPairPreparation(attempt, actualBatches, requiredIngredients, out _, out int preparedCount))
                    {
                        string hint = preparedCount == 0
                            ? "Choose any two ingredients and grind them together until they become one prepared powder."
                            : "Press New Batch, then grind the remaining ingredient on its own until it becomes powder.";
                        if (IsUnusedHint(hint, usedHints))
                        {
                            return hint;
                        }
                    }

                    if (!HasFinalCombinedBatchAfterPreparation(level, attempt, actualBatches, requiredIngredients))
                    {
                        string hint = "Put all prepared powders back into the mortar together.";
                        if (IsUnusedHint(hint, usedHints))
                        {
                            return hint;
                        }
                    }

                    return "The ingredient order does not matter; any two can be prepared together, then the remaining powder joins them at the end.";
                }

                foreach (IngredientData ingredient in requiredIngredients)
                {
                    if (!HasIngredientPreparedAlone(attempt, actualBatches, ingredient))
                    {
                        string hint = BuildMissingSingleBatchHint(ingredient, actualBatches);
                        if (IsUnusedHint(hint, usedHints))
                        {
                            return hint;
                        }
                    }
                }

                if (!HasFinalCombinedBatchAfterPreparation(level, attempt, actualBatches, requiredIngredients))
                {
                    string hint = "Put the prepared powders for " + FormatIngredientNames(requiredIngredients) + " back into the mortar, then grind them together.";
                    if (IsUnusedHint(hint, usedHints))
                    {
                        return hint;
                    }
                }

                return "The order of the prepared powders does not matter; what matters is separate preparation followed by one final combined grind.";
            }

            foreach (CombinationGroup group in targetGroups)
            {
                List<IngredientData> target = group.ingredients.Where(x => x != null).Distinct().ToList();
                if (target.Count == 0)
                {
                    continue;
                }

                List<GrindingBatch> containingBatches = actualBatches
                    .Where(batch => target.Any(ingredient => batch.ingredientsInBatch.Contains(ingredient)))
                    .ToList();

                List<IngredientData> actualTogether = containingBatches
                    .SelectMany(batch => batch.ingredientsInBatch)
                    .Where(x => x != null)
                    .Distinct()
                    .ToList();

                bool allTargetTogether = containingBatches.Count == 1 && target.All(ingredient => containingBatches[0].ingredientsInBatch.Contains(ingredient));
                bool hasExtraIngredients = actualTogether.Any(ingredient => !target.Contains(ingredient));
                if (target.Count == 1)
                {
                    IngredientData ingredient = target[0];
                    if (containingBatches.Count == 0)
                    {
                        string hint = BuildMissingSingleBatchHint(ingredient, actualBatches);
                        if (IsUnusedHint(hint, usedHints))
                        {
                            return hint;
                        }
                    }

                    if (hasExtraIngredients)
                    {
                        string hint = BuildSeparateSingleBatchHint(ingredient);
                        if (IsUnusedHint(hint, usedHints))
                        {
                            return hint;
                        }
                    }

                    continue;
                }

                if (!allTargetTogether)
                {
                    string hint = "Grind " + FormatIngredientNames(target) + " together in one batch.";
                    if (IsUnusedHint(hint, usedHints))
                    {
                        return hint;
                    }
                }

                if (hasExtraIngredients)
                {
                    List<IngredientData> extras = actualTogether.Where(ingredient => !target.Contains(ingredient)).ToList();
                    string hint = "Keep " + FormatIngredientNames(target) + " separate from " + FormatIngredientNames(extras) + ".";
                    if (IsUnusedHint(hint, usedHints))
                    {
                        return hint;
                    }
                }
            }

            CombinationGroup grouped = level.correctCombinationPattern.groups.FirstOrDefault(x => x.ingredients.Count > 1);
            if (grouped != null)
            {
                return "Grind " + FormatIngredientNames(grouped.ingredients.Where(x => x != null)) + " together in one batch.";
            }

            CombinationGroup single = level.correctCombinationPattern.groups.FirstOrDefault(x => x.ingredients.Count == 1);
            return single != null && single.ingredients[0] != null
                ? "Each ingredient needs its own batch. Add one ingredient, grind it, press New Batch, then add and grind the next ingredient."
                : "Two ingredients may have been mixed too early.";
        }

        private static string BuildMissingSingleBatchHint(IngredientData ingredient, IReadOnlyList<GrindingBatch> actualBatches)
        {
            string name = ingredient != null ? ingredient.DisplayName : "that ingredient";
            bool hasStartedBatch = actualBatches != null && actualBatches.Any(batch => batch != null
                && batch.ingredientsInBatch != null
                && batch.ingredientsInBatch.Any(x => x != null));
            bool hasEmptyCurrentBatch = actualBatches != null && actualBatches.Count > 0
                && actualBatches[actualBatches.Count - 1] != null
                && actualBatches[actualBatches.Count - 1].ingredientsInBatch != null
                && !actualBatches[actualBatches.Count - 1].ingredientsInBatch.Any(x => x != null);

            if (hasStartedBatch && hasEmptyCurrentBatch)
            {
                return "Add only " + name + " to the empty batch, then grind it on its own.";
            }

            return hasStartedBatch
                ? "Press New Batch, add only " + name + ", then grind it on its own."
                : "Add only " + name + " to the bowl, then grind it on its own.";
        }

        private static string BuildSeparateSingleBatchHint(IngredientData ingredient)
        {
            string name = ingredient != null ? ingredient.DisplayName : "that ingredient";
            return "Put " + name + " in its own batch: press New Batch, add only " + name + ", then grind it.";
        }

        private static bool IsUnusedHint(string hint, IReadOnlyList<string> usedHints)
        {
            return !string.IsNullOrWhiteSpace(hint) && (usedHints == null || !usedHints.Contains(hint));
        }

        private static List<IngredientData> GetRequiredCombinationIngredients(RecipeLevelData level)
        {
            List<IngredientData> ingredients = level != null && level.ingredientProfiles != null
                ? level.ingredientProfiles
                    .Where(profile => profile != null && profile.ingredient != null && profile.IsEnabled(MechanicType.Combination))
                    .Select(profile => profile.ingredient)
                    .Distinct()
                    .ToList()
                : new List<IngredientData>();

            if (ingredients.Count > 0 || level == null || level.correctCombinationPattern == null || level.correctCombinationPattern.groups == null)
            {
                return ingredients;
            }

            return level.correctCombinationPattern.groups
                .Where(group => group != null && group.ingredients != null)
                .SelectMany(group => group.ingredients)
                .Where(ingredient => ingredient != null)
                .Distinct()
                .ToList();
        }

        private static bool HasPreparedThenFinalCombinedBatches(RecipeLevelData level, RecipeAttemptManager attempt, IReadOnlyList<GrindingBatch> batches, IReadOnlyList<IngredientData> requiredIngredients)
        {
            if (level != null && level.allowAnyPairPreparation)
            {
                return TryFindAnyPairPreparation(attempt, batches, requiredIngredients, out _, out _)
                    && HasFinalCombinedBatchAfterPreparation(level, attempt, batches, requiredIngredients);
            }

            return HasSinglePreparationBatches(attempt, batches, requiredIngredients) && HasFinalCombinedBatchAfterPreparation(level, attempt, batches, requiredIngredients);
        }

        private static bool HasSinglePreparationBatches(RecipeAttemptManager attempt, IReadOnlyList<GrindingBatch> batches, IReadOnlyList<IngredientData> requiredIngredients)
        {
            if (batches == null || requiredIngredients == null || requiredIngredients.Count <= 1)
            {
                return false;
            }

            return requiredIngredients.All(ingredient => HasIngredientPreparedAlone(attempt, batches, ingredient));
        }

        private static bool HasFinalCombinedBatchAfterPreparation(RecipeLevelData level, RecipeAttemptManager attempt, IReadOnlyList<GrindingBatch> batches, IReadOnlyList<IngredientData> requiredIngredients)
        {
            if (batches == null || requiredIngredients == null || requiredIngredients.Count <= 1)
            {
                return false;
            }

            if (level != null && level.allowAnyPairPreparation)
            {
                return TryFindAnyPairPreparation(attempt, batches, requiredIngredients, out int lastPreparedBatchID, out _)
                    && batches.Any(batch => batch != null
                        && batch.batchID >= lastPreparedBatchID
                        && batch.ingredientsInBatch != null
                        && HasExactIngredientSet(batch.ingredientsInBatch, requiredIngredients));
            }

            int lastPreparationBatchID = 0;
            foreach (IngredientData ingredient in requiredIngredients)
            {
                int preparationBatchID = GetPreparationBatchID(attempt, batches, new[] { ingredient });
                if (preparationBatchID <= 0)
                {
                    return false;
                }

                lastPreparationBatchID = Mathf.Max(lastPreparationBatchID, preparationBatchID);
            }

            return batches.Any(batch => batch != null
                && batch.batchID >= lastPreparationBatchID
                && batch.ingredientsInBatch != null
                && HasExactIngredientSet(batch.ingredientsInBatch, requiredIngredients));
        }

        private static bool HasIngredientPreparedAlone(RecipeAttemptManager attempt, IReadOnlyList<GrindingBatch> batches, IngredientData ingredient)
        {
            return GetPreparationBatchID(attempt, batches, new[] { ingredient }) > 0;
        }

        private static bool TryFindAnyPairPreparation(RecipeAttemptManager attempt, IReadOnlyList<GrindingBatch> batches, IReadOnlyList<IngredientData> requiredIngredients, out int lastPreparationBatchID, out int preparedCount)
        {
            lastPreparationBatchID = 0;
            preparedCount = 0;
            List<IngredientData> ingredients = requiredIngredients != null
                ? requiredIngredients.Where(ingredient => ingredient != null).Distinct().ToList()
                : new List<IngredientData>();
            if (ingredients.Count < 3)
            {
                return false;
            }

            for (int i = 0; i < ingredients.Count - 1; i++)
            {
                for (int j = i + 1; j < ingredients.Count; j++)
                {
                    List<IngredientData> pair = new List<IngredientData> { ingredients[i], ingredients[j] };
                    List<IngredientData> remaining = ingredients.Where(ingredient => !pair.Contains(ingredient)).ToList();
                    int pairBatchID = GetPreparationBatchID(attempt, batches, pair);
                    int remainingBatchID = remaining.Count == 1 ? GetPreparationBatchID(attempt, batches, remaining) : 0;
                    int candidatePreparedCount = (pairBatchID > 0 ? 1 : 0) + (remainingBatchID > 0 ? 1 : 0);
                    if (candidatePreparedCount > preparedCount)
                    {
                        preparedCount = candidatePreparedCount;
                    }

                    if (pairBatchID > 0 && remainingBatchID > 0)
                    {
                        lastPreparationBatchID = Mathf.Max(pairBatchID, remainingBatchID);
                        preparedCount = 2;
                        return true;
                    }
                }
            }

            return false;
        }

        private static int GetPreparationBatchID(RecipeAttemptManager attempt, IReadOnlyList<GrindingBatch> batches, IEnumerable<IngredientData> ingredients)
        {
            List<IngredientData> target = ingredients != null
                ? ingredients.Where(ingredient => ingredient != null).Distinct().ToList()
                : new List<IngredientData>();
            if (target.Count == 0)
            {
                return 0;
            }

            if (attempt != null && attempt.TryGetPreparedBatchID(ingredients, out int recordedBatchID))
            {
                return recordedBatchID;
            }

            GrindingBatch batch = batches != null
                ? batches
                    .Where(candidate => candidate != null
                        && candidate.ingredientsInBatch != null
                        && HasExactIngredientSet(candidate.ingredientsInBatch, target))
                    .OrderBy(candidate => candidate.batchID)
                    .FirstOrDefault()
                : null;
            return batch != null ? batch.batchID : 0;
        }

        private static bool HasExactIngredientSet(IReadOnlyList<IngredientData> actual, IReadOnlyList<IngredientData> target)
        {
            if (actual == null || target == null)
            {
                return false;
            }

            HashSet<IngredientData> actualSet = new HashSet<IngredientData>(actual.Where(ingredient => ingredient != null));
            HashSet<IngredientData> targetSet = new HashSet<IngredientData>(target.Where(ingredient => ingredient != null));
            return actualSet.SetEquals(targetSet);
        }

        private static string FormatIngredientNames(IEnumerable<IngredientData> ingredients)
        {
            List<string> names = ingredients
                .Where(ingredient => ingredient != null)
                .Select(ingredient => ingredient.DisplayName)
                .ToList();
            if (names.Count == 0)
            {
                return "these ingredients";
            }

            if (names.Count == 1)
            {
                return names[0];
            }

            return string.Join(", ", names.Take(names.Count - 1)) + ", and " + names[names.Count - 1];
        }

        private static string BuildProcessHint(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            ForceLevel actualForce = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
            SpeedLevel actualSpeed = attempt.pestleController != null ? attempt.pestleController.EvaluatedSpeedLevel : SpeedLevel.Medium;

            List<string> parts = new List<string>();
            if (IsEnabled(level, MechanicType.Force) && actualForce != level.targetForceLevel)
            {
                parts.Add((int)actualForce < (int)level.targetForceLevel ? "use a little more force" : "use a little less force");
            }

            if (IsEnabled(level, MechanicType.Speed) && actualSpeed != level.targetSpeedLevel)
            {
                parts.Add((int)actualSpeed < (int)level.targetSpeedLevel ? "grind a little faster" : "grind a little slower");
            }

            if (parts.Count == 0)
            {
                return "The grinding settings are close. Check the remaining mistake.";
            }

            return "For the current batch, " + string.Join(", and ", parts) + ".";
        }

        private static string FindActualCombinationKey(IngredientData ingredient, IReadOnlyList<GrindingBatch> batches)
        {
            if (ingredient == null || batches == null)
            {
                return string.Empty;
            }

            foreach (GrindingBatch batch in batches)
            {
                if (batch != null && batch.ingredientsInBatch != null && batch.ingredientsInBatch.Contains(ingredient))
                {
                    return RecipeLevelData.BuildCombinationKey(batch.ingredientsInBatch);
                }
            }

            return string.Empty;
        }

        private static GrindingBatch FindBatchContaining(IngredientData ingredient, IReadOnlyList<GrindingBatch> batches)
        {
            if (ingredient == null || batches == null)
            {
                return null;
            }

            return batches.FirstOrDefault(batch => batch != null
                && batch.ingredientsInBatch != null
                && batch.ingredientsInBatch.Contains(ingredient));
        }

        private static ForceLevel GetForceForProfile(LevelIngredientProfile profile, RecipeAttemptManager attempt, ForceLevel fallback)
        {
            GrindingBatch batch = profile != null && attempt != null
                ? FindBatchContaining(profile.ingredient, attempt.GrindingBatches)
                : null;
            return batch != null ? batch.forceLevel : fallback;
        }

        private static SpeedLevel GetSpeedForProfile(LevelIngredientProfile profile, RecipeAttemptManager attempt, SpeedLevel fallback)
        {
            GrindingBatch batch = profile != null && attempt != null
                ? FindBatchContaining(profile.ingredient, attempt.GrindingBatches)
                : null;
            return batch != null ? batch.speedLevel : fallback;
        }

        private static bool IsEnabled(RecipeLevelData level, MechanicType mechanic)
        {
            return level != null && level.enabledMechanics != null && level.enabledMechanics.IsEnabled(mechanic);
        }
    }

    public class HintResult
    {
        public MechanicType mechanic;
        public string text;
        public int currentHintCount;
        public List<string> givenHints;

        public HintResult(MechanicType mechanic, string text, int currentHintCount, IEnumerable<string> givenHints)
        {
            this.mechanic = mechanic;
            this.text = text;
            this.currentHintCount = currentHintCount;
            this.givenHints = new List<string>(givenHints);
        }
    }
}
