using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheTasteReviver
{
    [Serializable]
    public class EnabledMechanics
    {
        public bool enableIngredientSelection = true;
        public bool enableIngredientOrder;
        public bool enableRatio;
        public bool enableCombination;
        public bool enableForce;
        public bool enableSpeed;
        public bool enableGrindDuration;

        public bool IsEnabled(MechanicType mechanic)
        {
            switch (mechanic)
            {
                case MechanicType.IngredientSelection: return enableIngredientSelection;
                case MechanicType.IngredientOrder: return enableIngredientOrder;
                case MechanicType.Ratio: return enableRatio;
                case MechanicType.Combination: return enableCombination;
                case MechanicType.Force: return enableForce;
                case MechanicType.Speed: return enableSpeed;
                case MechanicType.GrindDuration: return enableGrindDuration;
                default: return false;
            }
        }
    }

    [Serializable]
    public class RatioRequirement
    {
        public IngredientData ingredient;
        public RatioLevel ratioLevel = RatioLevel.None;
    }

    [Serializable]
    public class CombinationGroup
    {
        public List<IngredientData> ingredients = new List<IngredientData>();
    }

    [Serializable]
    public class CombinationPattern
    {
        public List<CombinationGroup> groups = new List<CombinationGroup>();

        public string ToDisplayString()
        {
            if (groups == null || groups.Count == 0)
            {
                return "All together";
            }

            List<string> parts = new List<string>();
            foreach (CombinationGroup group in groups)
            {
                List<string> names = new List<string>();
                if (group != null && group.ingredients != null)
                {
                    foreach (IngredientData ingredient in group.ingredients)
                    {
                        if (ingredient != null)
                        {
                            names.Add(ingredient.DisplayName);
                        }
                    }
                }

                if (names.Count > 0)
                {
                    parts.Add(string.Join(" + ", names));
                }
            }

            return parts.Count == 0 ? "All together" : string.Join(" | ", parts);
        }
    }

    [Serializable]
    public class ScoringWeights
    {
        [Range(0, 100)] public float ingredientSelection = 20f;
        [Range(0, 100)] public float ingredientOrder = 15f;
        [Range(0, 100)] public float ratio = 20f;
        [Range(0, 100)] public float combination = 15f;
        [Range(0, 100)] public float force = 10f;
        [Range(0, 100)] public float speed = 10f;
        [Range(0, 100)] public float grindDuration = 10f;

        public float GetWeight(MechanicType mechanic)
        {
            switch (mechanic)
            {
                case MechanicType.IngredientSelection: return ingredientSelection;
                case MechanicType.IngredientOrder: return ingredientOrder;
                case MechanicType.Ratio: return ratio;
                case MechanicType.Combination: return combination;
                case MechanicType.Force: return force;
                case MechanicType.Speed: return speed;
                case MechanicType.GrindDuration: return grindDuration;
                default: return 0f;
            }
        }
    }

    [Serializable]
    public class LevelClueData
    {
        public string clueId;
        public string title;
        [TextArea] public string content;
        public List<IngredientData> relatedIngredients = new List<IngredientData>();
        public MechanicType relatedDimension = MechanicType.IngredientSelection;

        public string ToLogText()
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return content;
            }

            return "[" + title + "]\n" + content;
        }
    }

    [Serializable]
    public class ProgressiveHintRule
    {
        public string ruleId;
        public int priority;
        public MechanicType mechanic = MechanicType.IngredientSelection;
        [TextArea] public string hintText;
    }

    [Serializable]
    public class IngredientResponseHintRule
    {
        public string ruleId;
        public int priority;
        public MechanicType mechanic = MechanicType.Force;
        public bool matchForce;
        public ForceLevel forceValue = ForceLevel.Medium;
        public bool matchSpeed;
        public SpeedLevel speedValue = SpeedLevel.Medium;
        public bool matchRatio;
        public RatioLevel ratioValue = RatioLevel.None;
        public bool matchCombination;
        public string combinationKey;
        [TextArea] public string hintText;
    }

    [Serializable]
    public class LevelIngredientProfile
    {
        public string profileTag;
        public IngredientData ingredient;
        public EnabledMechanics checkedMechanics = new EnabledMechanics();
        public ForceLevel targetForceLevel = ForceLevel.Medium;
        public SpeedLevel targetSpeedLevel = SpeedLevel.Medium;
        public RatioLevel targetRatioLevel = RatioLevel.None;
        public int targetOrderIndex = -1;
        public string targetCombinationKey;
        public float minGrindDuration = 3f;
        public float maxGrindDuration = 6f;
        [TextArea] public string levelTraitDescription;
        public List<IngredientResponseHintRule> responseHintRules = new List<IngredientResponseHintRule>();

        public bool IsEnabled(MechanicType mechanic)
        {
            return checkedMechanics != null && checkedMechanics.IsEnabled(mechanic);
        }

        public string DisplayTag
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(profileTag))
                {
                    return profileTag;
                }

                string ingredientName = ingredient != null ? ingredient.DisplayName : "Ingredient";
                return ingredientName + "_Profile";
            }
        }
    }

    [CreateAssetMenu(menuName = "The Taste Reviver/Recipe Level Data", fileName = "RecipeLevelData")]
    public class RecipeLevelData : ScriptableObject
    {
        public string levelID;
        public string levelName;
        public string cityName;
        public string targetTasteName;
        [TextArea] public string levelIntro;
        [Range(1, 4)] public int maxIngredientCount = 4;
        public List<IngredientData> availableIngredients = new List<IngredientData>();
        public List<IngredientData> requiredIngredients = new List<IngredientData>();
        public List<IngredientData> forbiddenIngredients = new List<IngredientData>();
        public List<MechanicType> unlockedDimensions = new List<MechanicType>();
        public List<MechanicType> lockedDimensions = new List<MechanicType>();
        public List<IngredientData> correctIngredientOrder = new List<IngredientData>();
        public List<RatioRequirement> correctRatioPattern = new List<RatioRequirement>();
        public CombinationPattern correctCombinationPattern = new CombinationPattern();
        public ForceLevel targetForceLevel = ForceLevel.Medium;
        public SpeedLevel targetSpeedLevel = SpeedLevel.Medium;
        public float minGrindDuration = 3f;
        public float maxGrindDuration = 6f;
        [Range(0, 100)] public int passingScore = 80;
        public EnabledMechanics enabledMechanics = new EnabledMechanics();
        public FeedbackTextData feedbackTexts = new FeedbackTextData();
        public HintSettings hintSettings = new HintSettings();
        public ScoringWeights scoringWeights = new ScoringWeights();
        public List<LevelClueData> unlockCluesOnComplete = new List<LevelClueData>();
        [TextArea] public string fallbackHintText;
        public List<ProgressiveHintRule> progressiveHintRules = new List<ProgressiveHintRule>();
        public List<LevelIngredientProfile> ingredientProfiles = new List<LevelIngredientProfile>();
        [TextArea] public string successFeedback;
        [TextArea] public string closeFeedback;
        [TextArea] public string wrongFeedback;

        public void SyncDimensionsFromEnabledMechanics()
        {
            unlockedDimensions.Clear();
            lockedDimensions.Clear();

            AddDimension(MechanicType.Force, enabledMechanics.enableForce);
            AddDimension(MechanicType.Speed, enabledMechanics.enableSpeed);
            AddDimension(MechanicType.Ratio, enabledMechanics.enableRatio);
            AddDimension(MechanicType.Combination, enabledMechanics.enableCombination);
            AddDimension(MechanicType.IngredientOrder, enabledMechanics.enableIngredientOrder);
        }

        private void AddDimension(MechanicType mechanic, bool unlocked)
        {
            if (unlocked)
            {
                unlockedDimensions.Add(mechanic);
            }
            else
            {
                lockedDimensions.Add(mechanic);
            }
        }

        public IReadOnlyList<LevelIngredientProfile> GetProfilesForMechanic(MechanicType mechanic)
        {
            if (ingredientProfiles == null)
            {
                return Array.Empty<LevelIngredientProfile>();
            }

            return ingredientProfiles
                .Where(profile => profile != null && profile.ingredient != null && profile.IsEnabled(mechanic))
                .ToList();
        }

        public LevelIngredientProfile GetProfile(IngredientData ingredient)
        {
            if (ingredient == null || ingredientProfiles == null)
            {
                return null;
            }

            return ingredientProfiles.FirstOrDefault(profile => profile != null && profile.ingredient == ingredient);
        }

        public void BuildIngredientProfilesFromLevelData()
        {
            if (ingredientProfiles == null)
            {
                ingredientProfiles = new List<LevelIngredientProfile>();
            }

            ingredientProfiles.Clear();
            List<IngredientData> sourceIngredients = availableIngredients != null && availableIngredients.Count > 0
                ? availableIngredients
                : requiredIngredients;
            if (sourceIngredients == null)
            {
                return;
            }

            foreach (IngredientData ingredient in sourceIngredients)
            {
                if (ingredient == null)
                {
                    continue;
                }

                LevelIngredientProfile profile = new LevelIngredientProfile
                {
                    profileTag = levelID + "_" + ingredient.ingredientID,
                    ingredient = ingredient,
                    targetForceLevel = targetForceLevel,
                    targetSpeedLevel = targetSpeedLevel,
                    targetRatioLevel = FindTargetRatio(ingredient),
                    targetOrderIndex = FindTargetOrderIndex(ingredient),
                    targetCombinationKey = FindTargetCombinationKey(ingredient),
                    minGrindDuration = minGrindDuration,
                    maxGrindDuration = maxGrindDuration,
                    levelTraitDescription = ingredient.initialDescription
                };

                profile.checkedMechanics.enableIngredientSelection = enabledMechanics != null && enabledMechanics.enableIngredientSelection;
                profile.checkedMechanics.enableIngredientOrder = enabledMechanics != null && enabledMechanics.enableIngredientOrder && profile.targetOrderIndex >= 0;
                profile.checkedMechanics.enableRatio = enabledMechanics != null && enabledMechanics.enableRatio && profile.targetRatioLevel != RatioLevel.None;
                profile.checkedMechanics.enableCombination = enabledMechanics != null && enabledMechanics.enableCombination && !string.IsNullOrWhiteSpace(profile.targetCombinationKey);
                profile.checkedMechanics.enableForce = enabledMechanics != null && enabledMechanics.enableForce;
                profile.checkedMechanics.enableSpeed = enabledMechanics != null && enabledMechanics.enableSpeed;
                profile.checkedMechanics.enableGrindDuration = enabledMechanics != null && enabledMechanics.enableGrindDuration;
                AddDefaultProfileHints(profile);
                ingredientProfiles.Add(profile);
            }
        }

        private RatioLevel FindTargetRatio(IngredientData ingredient)
        {
            RatioRequirement requirement = correctRatioPattern != null
                ? correctRatioPattern.FirstOrDefault(x => x != null && x.ingredient == ingredient)
                : null;
            return requirement != null ? requirement.ratioLevel : RatioLevel.None;
        }

        private int FindTargetOrderIndex(IngredientData ingredient)
        {
            if (correctIngredientOrder == null)
            {
                return -1;
            }

            return correctIngredientOrder.FindIndex(x => x == ingredient);
        }

        private string FindTargetCombinationKey(IngredientData ingredient)
        {
            if (ingredient == null || correctCombinationPattern == null || correctCombinationPattern.groups == null)
            {
                return string.Empty;
            }

            foreach (CombinationGroup group in correctCombinationPattern.groups)
            {
                if (group != null && group.ingredients != null && group.ingredients.Contains(ingredient))
                {
                    return BuildCombinationKey(group.ingredients);
                }
            }

            return string.Empty;
        }

        public static string BuildCombinationKey(IEnumerable<IngredientData> ingredients)
        {
            if (ingredients == null)
            {
                return string.Empty;
            }

            return string.Join("+", ingredients
                .Where(x => x != null)
                .Select(x => x.ingredientID)
                .OrderBy(x => x));
        }

        private static void AddDefaultProfileHints(LevelIngredientProfile profile)
        {
            if (profile == null || profile.ingredient == null)
            {
                return;
            }

            AddDefaultProfileHint(profile, MechanicType.Force, 30, " is not matching the force target for ");
            AddDefaultProfileHint(profile, MechanicType.Speed, 30, " is not matching the speed target for ");
            AddDefaultProfileHint(profile, MechanicType.Ratio, 20, " has the wrong ratio for ");
            AddDefaultProfileHint(profile, MechanicType.Combination, 20, " is in the wrong combination for ");
            AddDefaultProfileHint(profile, MechanicType.IngredientOrder, 20, " is in the wrong order for ");
        }

        private static void AddDefaultProfileHint(LevelIngredientProfile profile, MechanicType mechanic, int priority, string message)
        {
            if (!profile.IsEnabled(mechanic))
            {
                return;
            }

            profile.responseHintRules.Add(new IngredientResponseHintRule
            {
                ruleId = profile.DisplayTag + "_" + mechanic,
                priority = priority,
                mechanic = mechanic,
                hintText = profile.ingredient.DisplayName + message + profile.DisplayTag + "."
            });
        }
    }
}
