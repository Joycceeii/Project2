using System;
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
            AddMechanic(result, level, MechanicType.GrindDuration, EvaluateGrindDuration(level, attempt));

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
            IReadOnlyList<LevelIngredientProfile> profiles = level.GetProfilesForMechanic(MechanicType.IngredientOrder);
            if (profiles.Count > 0)
            {
                List<IngredientData> actualProfileOrder = attempt.IngredientOrder.Where(x => level.GetProfile(x) != null).Distinct().ToList();
                int correctProfiles = 0;
                foreach (LevelIngredientProfile profile in profiles)
                {
                    int actualIndex = actualProfileOrder.IndexOf(profile.ingredient);
                    if (actualIndex == profile.targetOrderIndex)
                    {
                        correctProfiles++;
                    }
                }

                float profileScore = correctProfiles / Mathf.Max(1f, profiles.Count);
                string profileFeedback = profileScore >= 0.99f ? level.feedbackTexts.orderCorrect : profileScore > 0f ? level.feedbackTexts.orderPartial : level.feedbackTexts.orderWrong;
                return new DimensionEvaluation(profileScore, profileScore >= 0.99f, profileFeedback);
            }

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

            IReadOnlyList<LevelIngredientProfile> profiles = level.GetProfilesForMechanic(MechanicType.Ratio);
            if (profiles.Count > 0)
            {
                int correctProfiles = profiles.Count(profile => actual.TryGetValue(profile.ingredient, out RatioLevel levelValue) && levelValue == profile.targetRatioLevel);
                float profileScore = correctProfiles / Mathf.Max(1f, profiles.Count);
                return new DimensionEvaluation(profileScore, profileScore >= 0.99f, profileScore >= 0.99f ? level.feedbackTexts.ratioCorrect : "Ratio pattern does not match the ingredient profile targets yet.");
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
            IReadOnlyList<LevelIngredientProfile> profiles = level.GetProfilesForMechanic(MechanicType.Combination);
            if (profiles.Count > 0)
            {
                int correctProfiles = 0;
                foreach (LevelIngredientProfile profile in profiles)
                {
                    string actualKey = FindActualCombinationKey(profile.ingredient, attempt.GrindingBatches);
                    if (actualKey == profile.targetCombinationKey || string.IsNullOrWhiteSpace(profile.targetCombinationKey))
                    {
                        correctProfiles++;
                    }
                }

                float profileScore = correctProfiles / Mathf.Max(1f, profiles.Count);
                return new DimensionEvaluation(profileScore, profileScore >= 0.99f, profileScore >= 0.99f ? level.feedbackTexts.combinationCorrect : level.feedbackTexts.combinationWrong);
            }

            string target = NormalizeCombination(level.correctCombinationPattern);
            string actual = NormalizeBatches(attempt.GrindingBatches);
            bool correct = target == actual || string.IsNullOrEmpty(target);
            return new DimensionEvaluation(correct ? 1f : 0f, correct, correct ? level.feedbackTexts.combinationCorrect : level.feedbackTexts.combinationWrong);
        }

        private static DimensionEvaluation EvaluateForce(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            ForceLevel actual = attempt.forceController != null ? attempt.forceController.CurrentForceLevel : ForceLevel.Medium;
            IReadOnlyList<LevelIngredientProfile> profiles = level.GetProfilesForMechanic(MechanicType.Force);
            if (profiles.Count > 0)
            {
                IReadOnlyList<GrindingBatch> batches = attempt.GrindingBatches ?? Array.Empty<GrindingBatch>();
                int exact = profiles.Count(profile => GetForceForProfile(profile, batches, actual) == profile.targetForceLevel);
                int adjacent = profiles.Count(profile =>
                {
                    ForceLevel profileForce = GetForceForProfile(profile, batches, actual);
                    return profileForce != profile.targetForceLevel && Mathf.Abs((int)profileForce - (int)profile.targetForceLevel) == 1;
                });
                float profileScore = (exact + adjacent * 0.5f) / Mathf.Max(1f, profiles.Count);
                bool correct = exact == profiles.Count;
                string profileFeedback = correct ? level.feedbackTexts.forceCorrect : BuildProfileForceFeedback(profiles, batches, actual);
                return new DimensionEvaluation(profileScore, correct, profileFeedback);
            }

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
            IReadOnlyList<LevelIngredientProfile> profiles = level.GetProfilesForMechanic(MechanicType.Speed);
            if (profiles.Count > 0)
            {
                IReadOnlyList<GrindingBatch> batches = attempt.GrindingBatches ?? Array.Empty<GrindingBatch>();
                int exact = profiles.Count(profile => GetSpeedForProfile(profile, batches, actual) == profile.targetSpeedLevel);
                int adjacent = profiles.Count(profile =>
                {
                    SpeedLevel profileSpeed = GetSpeedForProfile(profile, batches, actual);
                    return profileSpeed != profile.targetSpeedLevel && Mathf.Abs((int)profileSpeed - (int)profile.targetSpeedLevel) == 1;
                });
                float profileScore = (exact + adjacent * 0.5f) / Mathf.Max(1f, profiles.Count);
                bool correct = exact == profiles.Count;
                string profileFeedback = correct ? level.feedbackTexts.speedCorrect : BuildProfileSpeedFeedback(profiles, batches, actual);
                return new DimensionEvaluation(profileScore, correct, profileFeedback);
            }

            if (actual == level.targetSpeedLevel)
            {
                return new DimensionEvaluation(1f, true, level.feedbackTexts.speedCorrect);
            }

            float score = Mathf.Abs((int)actual - (int)level.targetSpeedLevel) == 1 ? 0.5f : 0f;
            string feedback = (int)actual < (int)level.targetSpeedLevel ? level.feedbackTexts.speedTooSlow : level.feedbackTexts.speedTooFast;
            return new DimensionEvaluation(score, false, feedback);
        }

        private static DimensionEvaluation EvaluateGrindDuration(RecipeLevelData level, RecipeAttemptManager attempt)
        {
            IReadOnlyList<LevelIngredientProfile> profiles = level.GetProfilesForMechanic(MechanicType.GrindDuration);
            IReadOnlyList<GrindingBatch> batches = attempt.GrindingBatches ?? Array.Empty<GrindingBatch>();
            if (profiles.Count > 0)
            {
                int correctProfiles = 0;
                int closeProfiles = 0;
                foreach (LevelIngredientProfile profile in profiles)
                {
                    GrindingBatch batch = FindBatchContaining(profile.ingredient, batches);
                    if (batch == null)
                    {
                        continue;
                    }

                    DurationMatch match = ClassifyDuration(batch.grindDuration, profile.minGrindDuration, profile.maxGrindDuration);
                    if (match == DurationMatch.Correct)
                    {
                        correctProfiles++;
                    }
                    else if (match == DurationMatch.Close)
                    {
                        closeProfiles++;
                    }
                }

                float score = (correctProfiles + closeProfiles * 0.5f) / Mathf.Max(1f, profiles.Count);
                bool correct = correctProfiles == profiles.Count;
                return new DimensionEvaluation(score, correct, correct ? "Grinding time is right." : BuildGrindDurationFeedback(level, batches));
            }

            if (batches.Count == 0)
            {
                return new DimensionEvaluation(0f, false, "No ground batch has been made yet.");
            }

            int correctBatches = batches.Count(batch => ClassifyDuration(batch.grindDuration, level.minGrindDuration, level.maxGrindDuration) == DurationMatch.Correct);
            float fallbackScore = correctBatches / Mathf.Max(1f, batches.Count);
            return new DimensionEvaluation(fallbackScore, fallbackScore >= 0.99f, fallbackScore >= 0.99f ? "Grinding time is right." : BuildGrindDurationFeedback(level, batches));
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

        private static DurationMatch ClassifyDuration(float seconds, float minSeconds, float maxSeconds)
        {
            minSeconds = Mathf.Max(0f, minSeconds);
            maxSeconds = Mathf.Max(minSeconds, maxSeconds);
            if (seconds >= minSeconds && seconds <= maxSeconds)
            {
                return DurationMatch.Correct;
            }

            float tolerance = Mathf.Max(1.5f, (maxSeconds - minSeconds) * 0.25f);
            return seconds >= minSeconds - tolerance && seconds <= maxSeconds + tolerance
                ? DurationMatch.Close
                : DurationMatch.Wrong;
        }

        private static string BuildGrindDurationFeedback(RecipeLevelData level, IReadOnlyList<GrindingBatch> batches)
        {
            float minSeconds = Mathf.Max(0f, level.minGrindDuration);
            float maxSeconds = Mathf.Max(minSeconds, level.maxGrindDuration);
            if (batches == null || batches.Count == 0)
            {
                return "Grind the ingredients before evaluating.";
            }

            bool anyTooShort = batches.Any(batch => batch != null && batch.grindDuration < minSeconds);
            bool anyTooLong = batches.Any(batch => batch != null && batch.grindDuration > maxSeconds);
            if (anyTooShort && !anyTooLong)
            {
                return "This batch needs more grinding time.";
            }

            if (anyTooLong && !anyTooShort)
            {
                return "This batch has been ground too long.";
            }

            return "Grinding time is not balanced yet.";
        }

        private enum DurationMatch
        {
            Wrong,
            Close,
            Correct
        }

        private static JudgementResult BuildJudgement(EvaluationResult result)
        {
            if (result.dimensions.Count > 0 && result.dimensions.All(x => x.isCorrect))
            {
                return JudgementResult.Correct;
            }

            if (result.dimensions.Any(x => x.normalizedScore >= 0.5f))
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

            return BuildMainFeedback(result.judgement);
        }

        public static string BuildMainFeedback(JudgementResult judgement)
        {
            if (judgement == JudgementResult.Correct)
            {
                return "Recipe complete.";
            }

            if (judgement == JudgementResult.Close)
            {
                return "Some parts are working, but one or more recipe relationships still need adjustment.";
            }

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

        private static ForceLevel GetForceForProfile(LevelIngredientProfile profile, IReadOnlyList<GrindingBatch> batches, ForceLevel fallback)
        {
            GrindingBatch batch = profile != null ? FindBatchContaining(profile.ingredient, batches) : null;
            return batch != null ? batch.forceLevel : fallback;
        }

        private static SpeedLevel GetSpeedForProfile(LevelIngredientProfile profile, IReadOnlyList<GrindingBatch> batches, SpeedLevel fallback)
        {
            GrindingBatch batch = profile != null ? FindBatchContaining(profile.ingredient, batches) : null;
            return batch != null ? batch.speedLevel : fallback;
        }

        private static string BuildProfileForceFeedback(IReadOnlyList<LevelIngredientProfile> profiles, IReadOnlyList<GrindingBatch> batches, ForceLevel fallback)
        {
            LevelIngredientProfile target = profiles.FirstOrDefault(profile => profile != null && profile.targetForceLevel != GetForceForProfile(profile, batches, fallback));
            if (target == null)
            {
                return "The force does not match the ingredient profile target yet.";
            }

            ForceLevel actual = GetForceForProfile(target, batches, fallback);
            return (int)actual < (int)target.targetForceLevel
                ? target.ingredient.DisplayName + " needs more force in " + target.DisplayTag + "."
                : target.ingredient.DisplayName + " needs less force in " + target.DisplayTag + ".";
        }

        private static string BuildProfileSpeedFeedback(IReadOnlyList<LevelIngredientProfile> profiles, IReadOnlyList<GrindingBatch> batches, SpeedLevel fallback)
        {
            LevelIngredientProfile target = profiles.FirstOrDefault(profile => profile != null && profile.targetSpeedLevel != GetSpeedForProfile(profile, batches, fallback));
            if (target == null)
            {
                return "The speed does not match the ingredient profile target yet.";
            }

            SpeedLevel actual = GetSpeedForProfile(target, batches, fallback);
            return (int)actual < (int)target.targetSpeedLevel
                ? target.ingredient.DisplayName + " needs faster grinding in " + target.DisplayTag + "."
                : target.ingredient.DisplayName + " needs slower grinding in " + target.DisplayTag + ".";
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
