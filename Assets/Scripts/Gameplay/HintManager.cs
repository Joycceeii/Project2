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
                    && !evaluation.IsCorrect(MechanicType.Combination))
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

            string hint = BuildTraitDrivenHint(level, attempt, mechanic, targetIngredient);
            if (string.IsNullOrWhiteSpace(hint))
            {
                hint = BuildCustomHint(level, mechanic, givenHints);
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
            string responseHint = BuildIngredientResponseHint(level, attempt, mechanic, usedHints);
            if (!string.IsNullOrWhiteSpace(responseHint))
            {
                return responseHint;
            }

            string traitHint = BuildTraitDrivenHint(level, attempt, mechanic);
            if (!string.IsNullOrWhiteSpace(traitHint))
            {
                return traitHint;
            }

            string customHint = BuildCustomHint(level, mechanic, usedHints);
            if (!string.IsNullOrWhiteSpace(customHint))
            {
                return customHint;
            }

            switch (mechanic)
            {
                case MechanicType.IngredientOrder:
                    return BuildOrderHint(level, attempt);
                case MechanicType.Ratio:
                    return BuildRatioHint(level, attempt);
                case MechanicType.Combination:
                    return BuildCombinationHint(level);
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
            SpeedLevel actualSpeed = attempt.pestleController != null ? attempt.pestleController.CurrentSpeedLevel : SpeedLevel.Medium;
            Dictionary<IngredientData, RatioLevel> actualRatios = attempt.CalculateRatioPattern(out _);

            return level.ingredientProfiles
                .Where(profile => profile != null
                    && profile.ingredient != null
                    && profile.IsEnabled(mechanic)
                    && IsProfileIncorrect(profile, mechanic, attempt, actualForce, actualSpeed, actualRatios))
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

        private static bool IsProfileIncorrect(LevelIngredientProfile profile, MechanicType mechanic, RecipeAttemptManager attempt, ForceLevel actualForce, SpeedLevel actualSpeed, Dictionary<IngredientData, RatioLevel> actualRatios)
        {
            switch (mechanic)
            {
                case MechanicType.Force:
                    return actualForce != profile.targetForceLevel;
                case MechanicType.Speed:
                    return actualSpeed != profile.targetSpeedLevel;
                case MechanicType.Ratio:
                    return !actualRatios.TryGetValue(profile.ingredient, out RatioLevel ratio) || ratio != profile.targetRatioLevel;
                case MechanicType.Combination:
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

            string trait = profile.ingredient.initialDescription;
            if (string.IsNullOrWhiteSpace(trait))
            {
                trait = profile.ingredient.DisplayName + " has a distinct aroma behavior.";
            }

            return profile.ingredient.DisplayName + ": " + trait + " " + BuildDimensionNudge(profile, attempt, mechanic);
        }

        private static LevelIngredientProfile FindFirstIncorrectProfile(RecipeLevelData level, RecipeAttemptManager attempt, MechanicType mechanic, IngredientData targetIngredient = null)
        {
            if (level == null || attempt == null || level.ingredientProfiles == null)
            {
                return null;
            }

            ForceLevel actualForce = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
            SpeedLevel actualSpeed = attempt.pestleController != null ? attempt.pestleController.CurrentSpeedLevel : SpeedLevel.Medium;
            Dictionary<IngredientData, RatioLevel> actualRatios = attempt.CalculateRatioPattern(out _);
            IEnumerable<LevelIngredientProfile> profiles = level.ingredientProfiles
                .Where(profile => profile != null && profile.ingredient != null && profile.IsEnabled(mechanic))
                .Where(profile => targetIngredient == null || profile.ingredient == targetIngredient);

            return profiles
                .FirstOrDefault(profile => IsProfileIncorrect(profile, mechanic, attempt, actualForce, actualSpeed, actualRatios));
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
                    return "Look again at whether it should work alone or share a batch with a related aroma.";
                case MechanicType.IngredientOrder:
                    return "Look again at whether its role belongs near the base, middle, or finish.";
                default:
                    return "Use its trait to rethink this attempt.";
            }
        }

        private static string BuildForceNudge(LevelIngredientProfile profile, RecipeAttemptManager attempt)
        {
            ForceLevel actual = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
            return (int)actual < (int)profile.targetForceLevel
                ? "It still feels closed; try opening it with more pressure."
                : "It is being pushed too hard; try protecting the aroma with less pressure.";
        }

        private static string BuildSpeedNudge(LevelIngredientProfile profile, RecipeAttemptManager attempt)
        {
            SpeedLevel actual = attempt.pestleController != null ? attempt.pestleController.CurrentSpeedLevel : SpeedLevel.Medium;
            return (int)actual < (int)profile.targetSpeedLevel
                ? "It is releasing too slowly; try a more active grinding rhythm."
                : "It is rushing out too sharply; try a more controlled grinding rhythm.";
        }

        private static string BuildRatioNudge(LevelIngredientProfile profile, RecipeAttemptManager attempt)
        {
            Dictionary<IngredientData, RatioLevel> actual = attempt.CalculateRatioPattern(out _);
            RatioLevel actualRatio = actual.TryGetValue(profile.ingredient, out RatioLevel ratio) ? ratio : RatioLevel.None;
            return (int)actualRatio < (int)profile.targetRatioLevel
                ? "Its aroma is too quiet in the mix; reconsider its weight."
                : "Its aroma is taking too much space; reconsider its weight.";
        }

        private static string BuildCustomHint(RecipeLevelData level, MechanicType mechanic, IReadOnlyList<string> usedHints)
        {
            if (level == null || level.progressiveHintRules == null || level.progressiveHintRules.Count == 0)
            {
                return null;
            }

            return level.progressiveHintRules
                .Where(rule => rule != null
                    && rule.mechanic == mechanic
                    && !string.IsNullOrWhiteSpace(rule.hintText)
                    && (usedHints == null || !usedHints.Contains(rule.hintText)))
                .OrderByDescending(rule => rule.priority)
                .Select(rule => rule.hintText)
                .FirstOrDefault();
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
                    return before.DisplayName + " should appear before " + after.DisplayName + ".";
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
                return "The ratio pattern is close, but the hierarchy can be clearer.";
            }

            string direction = "needs review";
            if (actual.TryGetValue(target.ingredient, out RatioLevel actualLevel))
            {
                direction = (int)actualLevel < (int)target.ratioLevel ? "may be too low" : "may be too high";
            }

            return target.ingredient.DisplayName + " " + direction + ".";
        }

        private static string BuildCombinationHint(RecipeLevelData level)
        {
            if (level.correctCombinationPattern == null || level.correctCombinationPattern.groups.Count == 0)
            {
                return "Some ingredients may work together, while others may need separate handling.";
            }

            CombinationGroup grouped = level.correctCombinationPattern.groups.FirstOrDefault(x => x.ingredients.Count > 1);
            if (grouped != null)
            {
                return string.Join(" and ", grouped.ingredients.Where(x => x != null).Select(x => x.DisplayName)) + " may belong in the same batch.";
            }

            CombinationGroup single = level.correctCombinationPattern.groups.FirstOrDefault(x => x.ingredients.Count == 1);
            return single != null && single.ingredients[0] != null
                ? single.ingredients[0].DisplayName + " may work better as a separate batch."
                : "Two ingredients may have been mixed too early.";
        }

        private static string BuildProcessHint(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            ForceLevel actualForce = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
            SpeedLevel actualSpeed = attempt.pestleController != null ? attempt.pestleController.CurrentSpeedLevel : SpeedLevel.Medium;

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
                return "The process is close. Check the remaining relationship.";
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
