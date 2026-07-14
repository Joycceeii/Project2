using System;
using System.Collections.Generic;
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
    public class ProcessFeedbackRule
    {
        public string ruleId;
        public int priority;
        public List<MechanicType> requiredCorrect = new List<MechanicType>();
        public List<MechanicType> requiredIncorrect = new List<MechanicType>();
        [TextArea] public string hintText;
    }

    [Serializable]
    public class ProgressiveHintRule
    {
        public string ruleId;
        public int priority;
        public MechanicType mechanic = MechanicType.IngredientSelection;
        [TextArea] public string hintText;
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
        public List<ProcessFeedbackRule> processFeedbackRules = new List<ProcessFeedbackRule>();
        [TextArea] public string fallbackHintText;
        public List<ProgressiveHintRule> progressiveHintRules = new List<ProgressiveHintRule>();
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
    }
}
