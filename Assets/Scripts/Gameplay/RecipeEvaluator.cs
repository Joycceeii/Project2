using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheTasteReviver
{
    public class RecipeEvaluator : MonoBehaviour
    {
        public EvaluationResult Evaluate(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            EvaluationResult result = new EvaluationResult();
            if (level == null || attempt == null)
            {
                result.mainFeedback = "Missing level or attempt data.";
                return result;
            }

            AddMechanic(result, level, MechanicType.IngredientSelection, EvaluateSelection(level, attempt));
            AddMechanic(result, level, MechanicType.IngredientOrder, EvaluateOrder(level, attempt));
            AddMechanic(result, level, MechanicType.Ratio, EvaluateRatio(level, attempt));
            AddMechanic(result, level, MechanicType.Combination, EvaluateCombination(level, attempt));
            AddMechanic(result, level, MechanicType.Force, EvaluateForce(level, attempt));
            AddMechanic(result, level, MechanicType.Speed, EvaluateSpeed(level, attempt));
            AddMechanic(result, level, MechanicType.GrindDuration, EvaluateDuration(level, attempt));

            float weighted = 0f;
            float totalWeight = 0f;
            foreach (DimensionEvaluation dimension in result.dimensions)
            {
                weighted += dimension.normalizedScore * dimension.weight;
                totalWeight += dimension.weight;
            }

            if (totalWeight <= 0f)
            {
                result.completenessScore = 0;
            }
            else
            {
                result.completenessScore = Mathf.RoundToInt(weighted / totalWeight * 100f);
            }

            result.judgement = BuildJudgement(result);
            result.passed = result.judgement == JudgementResult.Correct;
            result.mainFeedback = BuildMainFeedback(level, result);
            return result;
        }

        private static void AddMechanic(EvaluationResult result, RecipeLevelData level, MechanicType mechanic, DimensionEvaluation evaluation)
        {
            if (!level.enabledMechanics.IsEnabled(mechanic))
            {
                return;
            }

            evaluation.mechanic = mechanic;
            evaluation.weight = Mathf.Max(0f, level.scoringWeights.GetWeight(mechanic));
            result.dimensions.Add(evaluation);
        }

        private static DimensionEvaluation EvaluateSelection(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            HashSet<IngredientData> used = new HashSet<IngredientData>(attempt.IngredientAmounts.Select(x => x.ingredient).Where(x => x != null));
            HashSet<IngredientData> required = new HashSet<IngredientData>(level.requiredIngredients.Where(x => x != null));
            HashSet<IngredientData> forbidden = new HashSet<IngredientData>(level.forbiddenIngredients.Where(x => x != null));

            int missing = required.Count(x => !used.Contains(x));
            int forbiddenUsed = used.Count(x => forbidden.Contains(x));
            int extra = used.Count(x => !required.Contains(x) && !forbidden.Contains(x));
            int mistakes = missing + forbiddenUsed + extra;
            float score = required.Count == 0 && used.Count == 0 ? 1f : Mathf.Clamp01(1f - mistakes / Mathf.Max(1f, required.Count + used.Count));
            return new DimensionEvaluation(score, score >= 0.99f, mistakes == 0 ? level.feedbackTexts.selectionCorrect : level.feedbackTexts.selectionWrong);
        }

        private static DimensionEvaluation EvaluateOrder(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            List<IngredientData> target = level.correctIngredientOrder.Where(x => x != null).ToList();
            List<IngredientData> actual = attempt.IngredientOrder.Where(x => x != null).Distinct().ToList();
            if (target.Count == 0)
            {
                return new DimensionEvaluation(1f, true, level.feedbackTexts.orderCorrect);
            }

            int correctPositions = 0;
            for (int i = 0; i < Mathf.Min(target.Count, actual.Count); i++)
            {
                if (target[i] == actual[i])
                {
                    correctPositions++;
                }
            }

            float score = correctPositions / Mathf.Max(1f, target.Count);
            bool correct = score >= 0.99f && actual.Count == target.Count;
            string feedback = correct ? level.feedbackTexts.orderCorrect : score > 0f ? level.feedbackTexts.orderPartial : level.feedbackTexts.orderWrong;
            return new DimensionEvaluation(score, correct, feedback);
        }

        private static DimensionEvaluation EvaluateRatio(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            Dictionary<IngredientData, RatioLevel> actual = attempt.CalculateRatioPattern(out bool ambiguous);
            if (ambiguous)
            {
                return new DimensionEvaluation(0.4f, false, level.feedbackTexts.ratioAmbiguous);
            }

            List<RatioRequirement> target = level.correctRatioPattern.Where(x => x.ingredient != null).ToList();
            if (target.Count <= 1)
            {
                return new DimensionEvaluation(1f, true, level.feedbackTexts.ratioCorrect);
            }

            int correct = 0;
            foreach (RatioRequirement requirement in target)
            {
                if (actual.TryGetValue(requirement.ingredient, out RatioLevel levelValue) && levelValue == requirement.ratioLevel)
                {
                    correct++;
                }
            }

            float score = correct / Mathf.Max(1f, target.Count);
            return new DimensionEvaluation(score, score >= 0.99f, score >= 0.99f ? level.feedbackTexts.ratioCorrect : "Ratio pattern does not match the target yet.");
        }

        private static DimensionEvaluation EvaluateCombination(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            string target = NormalizeCombination(level.correctCombinationPattern);
            string actual = NormalizeBatches(attempt.GrindingBatches);
            bool correct = target == actual || string.IsNullOrEmpty(target);
            return new DimensionEvaluation(correct ? 1f : 0f, correct, correct ? level.feedbackTexts.combinationCorrect : level.feedbackTexts.combinationWrong);
        }

        private static DimensionEvaluation EvaluateForce(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            ForceLevel actual = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
            if (actual == level.targetForceLevel)
            {
                return new DimensionEvaluation(1f, true, level.feedbackTexts.forceCorrect);
            }

            float score = Mathf.Abs((int)actual - (int)level.targetForceLevel) == 1 ? 0.5f : 0f;
            string feedback = (int)actual < (int)level.targetForceLevel ? level.feedbackTexts.forceTooLight : level.feedbackTexts.forceTooHeavy;
            return new DimensionEvaluation(score, false, feedback);
        }

        private static DimensionEvaluation EvaluateSpeed(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            SpeedLevel actual = attempt.pestleController != null ? attempt.pestleController.CurrentSpeedLevel : SpeedLevel.Medium;
            if (actual == level.targetSpeedLevel)
            {
                return new DimensionEvaluation(1f, true, level.feedbackTexts.speedCorrect);
            }

            float score = Mathf.Abs((int)actual - (int)level.targetSpeedLevel) == 1 ? 0.5f : 0f;
            string feedback = (int)actual < (int)level.targetSpeedLevel ? level.feedbackTexts.speedTooSlow : level.feedbackTexts.speedTooFast;
            return new DimensionEvaluation(score, false, feedback);
        }

        private static DimensionEvaluation EvaluateDuration(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            float duration = attempt.pestleController != null ? attempt.pestleController.GrindDuration : 0f;
            if (duration < level.minGrindDuration)
            {
                return new DimensionEvaluation(0f, false, level.feedbackTexts.durationTooShort);
            }

            if (duration > level.maxGrindDuration)
            {
                return new DimensionEvaluation(0f, false, level.feedbackTexts.durationTooLong);
            }

            return new DimensionEvaluation(1f, true, level.feedbackTexts.durationCorrect);
        }

        private static JudgementResult BuildJudgement(EvaluationResult result)
        {
            if (result.dimensions.Count > 0 && result.dimensions.All(x => x.isCorrect))
            {
                return JudgementResult.Correct;
            }

            if (result.completenessScore >= 45 || result.dimensions.Any(x => x.normalizedScore >= 0.5f))
            {
                return JudgementResult.Close;
            }

            return JudgementResult.Wrong;
        }

        public static string BuildMainFeedback(RecipeLevelData level, EvaluationResult result)
        {
            if (level != null)
            {
                if (result.judgement == JudgementResult.Correct && !string.IsNullOrWhiteSpace(level.successFeedback))
                {
                    return level.successFeedback;
                }

                if (result.judgement == JudgementResult.Close && !string.IsNullOrWhiteSpace(level.closeFeedback))
                {
                    return level.closeFeedback;
                }

                if (result.judgement == JudgementResult.Wrong && !string.IsNullOrWhiteSpace(level.wrongFeedback))
                {
                    return level.wrongFeedback;
                }
            }

            return BuildMainFeedback(result.completenessScore);
        }

        public static string BuildMainFeedback(int score)
        {
            if (score >= 90) return "Excellent restoration. The structure is close to the target.";
            if (score >= 80) return "Restoration succeeded, with details still available to refine.";
            if (score >= 60) return "The direction is mostly correct, but key layers are unstable.";
            if (score >= 40) return "Some parts work, but the overall structure is unclear.";
            return "The recipe is far from the target and needs rework.";
        }

        private static string NormalizeCombination(CombinationPattern pattern)
        {
            if (pattern == null || pattern.groups == null || pattern.groups.Count == 0)
            {
                return string.Empty;
            }

            List<string> groups = new List<string>();
            foreach (CombinationGroup group in pattern.groups)
            {
                groups.Add(string.Join("+", group.ingredients.Where(x => x != null).Select(x => x.ingredientID).OrderBy(x => x)));
            }

            return string.Join("|", groups.OrderBy(x => x));
        }

        private static string NormalizeBatches(IReadOnlyList<GrindingBatch> batches)
        {
            if (batches == null || batches.Count == 0)
            {
                return string.Empty;
            }

            List<string> groups = new List<string>();
            foreach (GrindingBatch batch in batches)
            {
                groups.Add(string.Join("+", batch.ingredientsInBatch.Where(x => x != null).Select(x => x.ingredientID).OrderBy(x => x)));
            }

            return string.Join("|", groups.OrderBy(x => x));
        }
    }

    public class DimensionEvaluation
    {
        public MechanicType mechanic;
        public float normalizedScore;
        public float weight;
        public bool isCorrect;
        public string feedback;

        public DimensionEvaluation(float normalizedScore, bool isCorrect, string feedback)
        {
            this.normalizedScore = Mathf.Clamp01(normalizedScore);
            this.isCorrect = isCorrect;
            this.feedback = feedback;
        }
    }

    public class EvaluationResult
    {
        public int completenessScore;
        public bool passed;
        public JudgementResult judgement = JudgementResult.Wrong;
        public string mainFeedback;
        public List<DimensionEvaluation> dimensions = new List<DimensionEvaluation>();

        public bool IsCorrect(MechanicType mechanic)
        {
            DimensionEvaluation dimension = dimensions.FirstOrDefault(x => x.mechanic == mechanic);
            return dimension != null && dimension.isCorrect;
        }

        public DimensionEvaluation GetDimension(MechanicType mechanic)
        {
            return dimensions.FirstOrDefault(x => x.mechanic == mechanic);
        }
    }
}
