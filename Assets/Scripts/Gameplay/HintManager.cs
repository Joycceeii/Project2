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

            foreach (MechanicType mechanic in priority)
            {
                if (!level.enabledMechanics.IsEnabled(mechanic) || evaluation.IsCorrect(mechanic))
                {
                    continue;
                }

                if (!CanGiveMore(level, mechanic))
                {
                    continue;
                }

                string hint = BuildHint(level, attempt, mechanic);
                if (string.IsNullOrWhiteSpace(hint) || givenHints.Contains(hint))
                {
                    continue;
                }

                Increment(mechanic);
                givenHints.Add(hint);
                return new HintResult(mechanic, hint, hintCounts[mechanic], givenHints);
            }

            return new HintResult(MechanicType.IngredientSelection, "No new hint is available. Try adjusting an unfinished mechanic.", givenHints.Count, givenHints);
        }

        public void ResetHints()
        {
            hintCounts.Clear();
            givenHints.Clear();
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

        private static string BuildHint(RecipeLevelData level, RecipeAttemptManager attempt, MechanicType mechanic)
        {
            switch (mechanic)
            {
                case MechanicType.IngredientOrder:
                    return BuildOrderHint(level, attempt);
                case MechanicType.Ratio:
                    return BuildRatioHint(level, attempt);
                case MechanicType.Combination:
                    return BuildCombinationHint(level);
                case MechanicType.Speed:
                    return "Grinding speed does not match the target yet.";
                case MechanicType.Force:
                    return "Grinding force still needs adjustment.";
                default:
                    return "One key relationship still needs checking.";
            }
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
