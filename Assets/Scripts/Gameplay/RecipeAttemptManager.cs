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

        private readonly Dictionary<IngredientData, int> batchByIngredient = new Dictionary<IngredientData, int>();
        private int currentBatchID = 1;

        public IReadOnlyList<IngredientData> IngredientOrder => ingredientOrder;
        public IReadOnlyList<IngredientInstance> IngredientAmounts => ingredientAmounts;
        public IReadOnlyList<GrindingBatch> GrindingBatches => grindingBatches;
        public bool HasIngredientsInBowl => ingredientAmounts.Any(x => x != null && x.ingredient != null);
        public bool HasEvaluated { get; private set; }
        public bool HasAutoEvaluated { get; private set; }

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
                uiManager.ShowHint("Choose this ingredient's amount first.");
                return false;
            }

            if (!IsRatioSelectionRequired())
            {
                AddIngredientWithRatio(ingredient, RatioLevel.Medium);
                return true;
            }

            List<RatioLevel> availableRatios = GetAvailableRatioChoices();
            if (availableRatios.Count == 0)
            {
                uiManager?.ShowHint("No ratio choices are available.");
                return false;
            }

            if (uiManager == null)
            {
                AddIngredientWithRatio(ingredient, availableRatios[0]);
                return true;
            }

            uiManager.ShowRatioSelection(ingredient, availableRatios, ratio => AddIngredientWithRatio(ingredient, ratio));
            return true;
        }

        private bool IsRatioSelectionRequired()
        {
            return currentLevel != null
                && currentLevel.enabledMechanics != null
                && currentLevel.enabledMechanics.enableRatio;
        }

        public void ResetAttempt(bool clearMessages = true)
        {
            ingredientAmounts.Clear();
            ingredientOrder.Clear();
            selectedRatioPattern.Clear();
            grindingBatches.Clear();
            batchByIngredient.Clear();
            currentBatchID = 1;
            HasEvaluated = false;
            HasAutoEvaluated = false;
            forceController?.ResetToDefault();
            pestleController?.ResetToDefault();
            ReturnIngredientsHome();
            RebuildCurrentBatch();
            uiManager?.RefreshAttemptPanels(this);
            if (clearMessages)
            {
                uiManager?.ShowFeedback(string.Empty);
                uiManager?.ShowHint(string.Empty);
            }
        }

        public void SetLevel(RecipeLevelData level)
        {
            currentLevel = level;
            ResetAttempt(false);
        }

        public void MarkAutoEvaluated()
        {
            HasAutoEvaluated = true;
        }

        public void MarkEvaluated()
        {
            HasEvaluated = true;
        }

        private void ReturnIngredientsHome()
        {
            foreach (DraggableIngredient ingredient in FindObjectsByType<DraggableIngredient>(FindObjectsSortMode.None))
            {
                if (ingredient != null && ingredient.gameObject.activeInHierarchy)
                {
                    ingredient.ReturnHome();
                }
            }
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

        public void StartNewBatch()
        {
            if (!HasIngredientsInBowl)
            {
                uiManager?.ShowHint("Add an ingredient before starting another batch.");
                return;
            }

            bool currentBatchHasIngredients = batchByIngredient.Values.Any(batchID => batchID == currentBatchID);
            if (!currentBatchHasIngredients)
            {
                uiManager?.ShowHint("The next batch is already empty.");
                return;
            }

            currentBatchID++;
            uiManager?.RefreshAttemptPanels(this);
            uiManager?.ShowHint("Next ingredients will start a separate batch.");
        }

        private void RebuildCurrentBatch()
        {
            grindingBatches.Clear();

            Dictionary<IngredientData, RatioLevel> pattern = CalculateRatioPattern(out _);
            IEnumerable<IGrouping<int, IngredientInstance>> groups = ingredientAmounts
                .Where(instance => instance != null && instance.ingredient != null)
                .GroupBy(instance => batchByIngredient.TryGetValue(instance.ingredient, out int batchID) ? batchID : 1)
                .OrderBy(group => group.Key);

            foreach (IGrouping<int, IngredientInstance> group in groups)
            {
                GrindingBatch batch = new GrindingBatch
                {
                    batchID = group.Key,
                    forceLevel = forceController != null ? forceController.CurrentForceLevel : ForceLevel.Medium,
                    speedLevel = pestleController != null ? pestleController.CurrentSpeedLevel : SpeedLevel.Medium,
                    grindDuration = pestleController != null ? pestleController.GrindDuration : 0f
                };

                HashSet<IngredientData> batchIngredients = new HashSet<IngredientData>(group.Select(instance => instance.ingredient));
                batch.ingredientsInBatch.AddRange(batchIngredients);
                batch.ingredientOrderInBatch.AddRange(ingredientOrder.Where(batchIngredients.Contains));
                foreach (KeyValuePair<IngredientData, RatioLevel> pair in pattern.Where(pair => batchIngredients.Contains(pair.Key)))
                {
                    batch.ratioPatternInBatch.Add(new RatioRequirement { ingredient = pair.Key, ratioLevel = pair.Value });
                }

                grindingBatches.Add(batch);
            }
        }

        private void AddIngredientWithRatio(IngredientData ingredient, RatioLevel ratio)
        {
            ingredientAmounts.Add(new IngredientInstance(ingredient, 1));
            ingredientOrder.Add(ingredient);
            selectedRatioPattern.Add(new RatioRequirement { ingredient = ingredient, ratioLevel = ratio });
            batchByIngredient[ingredient] = currentBatchID;
            RebuildCurrentBatch();
            uiManager?.RefreshAttemptPanels(this);
            if (uiManager != null
                && !uiManager.ShowStepFeedback(MechanicType.IngredientOrder, ingredient)
                && !uiManager.ShowStepFeedback(MechanicType.Ratio, ingredient))
            {
                uiManager.ShowStepFeedback(MechanicType.Combination, ingredient);
            }
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
                choices = new List<RatioLevel> { RatioLevel.VeryLess, RatioLevel.Less };
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
