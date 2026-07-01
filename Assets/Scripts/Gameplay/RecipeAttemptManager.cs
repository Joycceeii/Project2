using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheTasteReviver
{
    public class RecipeAttemptManager : MonoBehaviour
    {
        public RecipeLevelData currentLevel;
        public UIManager uiManager;
        public ForceSliderController forceController;
        public PestleController pestleController;

        public List<IngredientInstance> ingredientAmounts = new List<IngredientInstance>();
        public List<IngredientData> ingredientOrder = new List<IngredientData>();
        public List<RatioRequirement> selectedRatioPattern = new List<RatioRequirement>();
        public List<GrindingBatch> grindingBatches = new List<GrindingBatch>();

        public IReadOnlyList<IngredientData> IngredientOrder => ingredientOrder;
        public IReadOnlyList<IngredientInstance> IngredientAmounts => ingredientAmounts;
        public IReadOnlyList<GrindingBatch> GrindingBatches => grindingBatches;

        public bool TryAddIngredient(IngredientData ingredient)
        {
            if (ingredient == null)
            {
                return false;
            }

            int uniqueCount = ingredientAmounts.Count(x => x.ingredient != null);
            bool isNewIngredient = ingredientAmounts.All(x => x.ingredient != ingredient);
            if (!isNewIngredient)
            {
                uiManager?.ShowHint("This ingredient is already in the bowl.");
                return false;
            }

            int limit = currentLevel != null ? Mathf.Clamp(currentLevel.maxIngredientCount, 1, 4) : 4;
            if (uniqueCount >= limit)
            {
                uiManager?.ShowHint("This level allows up to four ingredients.");
                return false;
            }

            if (uiManager != null && uiManager.IsRatioSelectionOpen)
            {
                uiManager.ShowHint("Choose the ratio for the current ingredient first.");
                return false;
            }

            List<RatioLevel> availableRatios = GetAvailableRatioChoices();
            if (availableRatios.Count == 0)
            {
                uiManager?.ShowHint("No ratio choices are available.");
                return false;
            }

            if (availableRatios.Count == 1)
            {
                AddIngredientWithRatio(ingredient, availableRatios[0]);
                uiManager?.ShowHint(ingredient.DisplayName + " ratio was set to " + UIManager.GetRatioDisplayName(availableRatios[0]) + ".");
                return true;
            }

            if (uiManager == null)
            {
                AddIngredientWithRatio(ingredient, availableRatios[0]);
                return true;
            }

            uiManager.ShowRatioSelection(ingredient, availableRatios, ratio => AddIngredientWithRatio(ingredient, ratio));
            return true;
        }

        public void ResetAttempt()
        {
            ingredientAmounts.Clear();
            ingredientOrder.Clear();
            selectedRatioPattern.Clear();
            grindingBatches.Clear();
            pestleController?.ResetTracking();
            RebuildCurrentBatch();
            uiManager?.RefreshAttemptPanels(this);
            uiManager?.ShowFeedback(string.Empty);
            uiManager?.ShowHint(string.Empty);
        }

        public void SetLevel(RecipeLevelData level)
        {
            currentLevel = level;
            ResetAttempt();
        }

        public Dictionary<IngredientData, RatioLevel> CalculateRatioPattern(out bool hasAmbiguousTies)
        {
            hasAmbiguousTies = false;
            Dictionary<IngredientData, RatioLevel> result = new Dictionary<IngredientData, RatioLevel>();

            foreach (RatioRequirement requirement in selectedRatioPattern)
            {
                if (requirement != null && requirement.ingredient != null)
                {
                    result[requirement.ingredient] = requirement.ratioLevel;
                }
            }

            return result;
        }

        public GrindingBatch GetCurrentBatch()
        {
            RebuildCurrentBatch();
            return grindingBatches.Count > 0 ? grindingBatches[0] : null;
        }

        private void RebuildCurrentBatch()
        {
            grindingBatches.Clear();
            GrindingBatch batch = new GrindingBatch
            {
                batchID = 1,
                forceLevel = forceController != null ? forceController.CurrentForceLevel : ForceLevel.Medium,
                speedLevel = pestleController != null ? pestleController.CurrentSpeedLevel : SpeedLevel.Medium,
                grindDuration = pestleController != null ? pestleController.GrindDuration : 0f
            };

            foreach (IngredientInstance instance in ingredientAmounts)
            {
                if (instance.ingredient != null)
                {
                    batch.ingredientsInBatch.Add(instance.ingredient);
                }
            }

            batch.ingredientOrderInBatch.AddRange(ingredientOrder);
            Dictionary<IngredientData, RatioLevel> pattern = CalculateRatioPattern(out _);
            foreach (KeyValuePair<IngredientData, RatioLevel> pair in pattern)
            {
                batch.ratioPatternInBatch.Add(new RatioRequirement { ingredient = pair.Key, ratioLevel = pair.Value });
            }

            grindingBatches.Add(batch);
        }

        private void AddIngredientWithRatio(IngredientData ingredient, RatioLevel ratio)
        {
            ingredientAmounts.Add(new IngredientInstance(ingredient, 1));
            ingredientOrder.Add(ingredient);
            selectedRatioPattern.Add(new RatioRequirement { ingredient = ingredient, ratioLevel = ratio });
            RebuildCurrentBatch();
            uiManager?.RefreshAttemptPanels(this);
        }

        private List<RatioLevel> GetAvailableRatioChoices()
        {
            int limit = currentLevel != null ? Mathf.Clamp(currentLevel.maxIngredientCount, 1, 4) : 4;
            List<RatioLevel> choices;
            if (limit <= 1)
            {
                choices = new List<RatioLevel> { RatioLevel.More };
            }
            else if (limit == 2)
            {
                choices = new List<RatioLevel> { RatioLevel.Less, RatioLevel.More };
            }
            else if (limit == 3)
            {
                choices = new List<RatioLevel> { RatioLevel.Less, RatioLevel.SlightlyMore, RatioLevel.More };
            }
            else
            {
                choices = new List<RatioLevel>
                {
                    RatioLevel.VeryLess,
                    RatioLevel.Less,
                    RatioLevel.SlightlyMore,
                    RatioLevel.More
                };
            }

            HashSet<RatioLevel> used = new HashSet<RatioLevel>(selectedRatioPattern.Select(x => x.ratioLevel));
            choices.RemoveAll(used.Contains);
            return choices;
        }
    }
}
