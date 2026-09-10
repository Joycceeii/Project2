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
        public LevelIngredientDisplayManager ingredientDisplayManager;
        [Header("Grinding Completion")]
        [Tooltip("How many seconds of valid grinding this batch needs before ingredients become powder. Adjust this in the Inspector to tune the whole batch powder pace.")]
        public float batchPowderSeconds = 1.5f;

        public List<IngredientInstance> ingredientAmounts = new List<IngredientInstance>();
        public List<IngredientData> ingredientOrder = new List<IngredientData>();
        public List<RatioRequirement> selectedRatioPattern = new List<RatioRequirement>();
        public List<GrindingBatch> grindingBatches = new List<GrindingBatch>();

        private readonly Dictionary<IngredientData, int> batchByIngredient = new Dictionary<IngredientData, int>();
        private readonly List<IngredientBatchEntry> ingredientBatchEntries = new List<IngredientBatchEntry>();
        private readonly Dictionary<int, float> finalizedBatchDurations = new Dictionary<int, float>();
        private readonly Dictionary<int, ForceLevel> finalizedBatchForces = new Dictionary<int, ForceLevel>();
        private readonly Dictionary<int, SpeedLevel> finalizedBatchSpeeds = new Dictionary<int, SpeedLevel>();
        private readonly Dictionary<string, int> preparedBatchIDsByKey = new Dictionary<string, int>();
        private int currentBatchID = 1;
        private float currentBatchStartGrindDuration;

        public IReadOnlyList<IngredientData> IngredientOrder => ingredientOrder;
        public IReadOnlyList<IngredientInstance> IngredientAmounts => ingredientAmounts;
        public IReadOnlyList<GrindingBatch> GrindingBatches
        {
            get
            {
                RebuildCurrentBatch();
                return grindingBatches;
            }
        }
        public bool HasIngredientsInBowl => ingredientAmounts.Any(x => x != null && x.ingredient != null);
        public bool HasEvaluated { get; private set; }
        public bool HasAutoEvaluated { get; private set; }
        public float RequiredBatchPowderSeconds => Mathf.Max(0.2f, batchPowderSeconds);

        public bool TryAddIngredient(IngredientData ingredient)
        {
            return TryAddIngredients(new[] { ingredient });
        }

        public bool TryAddIngredients(IReadOnlyList<IngredientData> ingredients)
        {
            List<IngredientData> validIngredients = ingredients != null
                ? ingredients.Where(ingredient => ingredient != null).Distinct().ToList()
                : new List<IngredientData>();
            if (validIngredients.Count == 0)
            {
                return false;
            }

            if (validIngredients.Count == 1)
            {
                return TryAddSingleIngredient(validIngredients[0]);
            }

            if (uiManager != null && uiManager.IsRatioSelectionOpen)
            {
                uiManager.ShowHint("Choose this ingredient's amount first.");
                return false;
            }

            int uniqueCount = ingredientBatchEntries
                .Where(entry => entry != null && entry.batchID == currentBatchID && entry.ingredient != null)
                .Select(entry => entry.ingredient)
                .Distinct()
                .Count();
            int limit = currentLevel != null ? Mathf.Clamp(currentLevel.maxIngredientCount, 1, 4) : 4;
            int newIngredientCount = validIngredients.Count(ingredient => !IsIngredientInBatch(ingredient, currentBatchID));
            if (uniqueCount + newIngredientCount > limit)
            {
                uiManager?.ShowHint("This level allows up to four ingredients.");
                return false;
            }

            foreach (IngredientData ingredient in validIngredients)
            {
                if (IsIngredientInBatch(ingredient, currentBatchID))
                {
                    uiManager?.ShowHint("This powder is already in the bowl.");
                    return false;
                }

                bool wasUsedBefore = ingredientBatchEntries.Any(entry => entry != null && entry.ingredient == ingredient);
                bool canReuseGroundIngredient = currentLevel != null
                    && currentLevel.requireFinalCombinedBatch
                    && wasUsedBefore;
                if (wasUsedBefore && !canReuseGroundIngredient)
                {
                    uiManager?.ShowHint("This ingredient is already in the bowl.");
                    return false;
                }
            }

            RecordCurrentBatchAsPreparedIfReady();
            foreach (IngredientData ingredient in validIngredients)
            {
                AddIngredientWithRatio(ingredient, RatioLevel.Medium, false);
            }

            RebuildCurrentBatch();
            uiManager?.RefreshAttemptPanels(this);
            uiManager?.ShowHint("Prepared powder added to the bowl.");
            return true;
        }

        private bool TryAddSingleIngredient(IngredientData ingredient)
        {
            if (ingredient == null)
            {
                return false;
            }

            int uniqueCount = ingredientBatchEntries
                .Where(entry => entry != null && entry.batchID == currentBatchID && entry.ingredient != null)
                .Select(entry => entry.ingredient)
                .Distinct()
                .Count();
            if (IsIngredientInBatch(ingredient, currentBatchID))
            {
                uiManager?.ShowHint("This ingredient is already in the bowl.");
                return false;
            }

            bool wasUsedBefore = ingredientBatchEntries.Any(entry => entry != null && entry.ingredient == ingredient);
            bool canReuseGroundIngredient = currentLevel != null
                && currentLevel.requireFinalCombinedBatch
                && wasUsedBefore
                && !IsIngredientInBatch(ingredient, currentBatchID);
            if (wasUsedBefore && !canReuseGroundIngredient)
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

            RecordCurrentBatchAsPreparedIfReady();

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
            ingredientBatchEntries.Clear();
            finalizedBatchDurations.Clear();
            finalizedBatchForces.Clear();
            finalizedBatchSpeeds.Clear();
            preparedBatchIDsByKey.Clear();
            currentBatchID = 1;
            HasEvaluated = false;
            HasAutoEvaluated = false;
            forceController?.ResetToDefault();
            pestleController?.ResetToDefault();
            currentBatchStartGrindDuration = pestleController != null ? pestleController.GrindDuration : 0f;
            ingredientDisplayManager?.ClearPreparedPowderDisplays();
            ingredientDisplayManager?.ClearMixedPowderBatches();
            ReturnIngredientsHome();
            if (clearMessages && currentLevel != null)
            {
                ingredientDisplayManager?.ShowLevelIngredients(currentLevel);
            }

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

        public bool RemoveIngredientFromCurrentBatch(IngredientData ingredient)
        {
            return RemoveIngredientsFromCurrentBatch(new[] { ingredient });
        }

        public bool RemoveIngredientsFromCurrentBatch(IReadOnlyList<IngredientData> ingredients)
        {
            List<IngredientData> validIngredients = ingredients != null
                ? ingredients.Where(ingredient => ingredient != null).Distinct().ToList()
                : new List<IngredientData>();
            if (validIngredients.Count == 0)
            {
                return false;
            }

            bool removedAny = false;
            foreach (IngredientData ingredient in validIngredients)
            {
                removedAny |= RemoveSingleIngredientFromCurrentBatch(ingredient, false);
            }

            if (!removedAny)
            {
                return false;
            }

            bool currentBatchIsEmpty = !ingredientBatchEntries.Any(entry => entry != null
                && entry.batchID == currentBatchID
                && entry.ingredient != null);
            if (currentBatchIsEmpty)
            {
                currentBatchStartGrindDuration = pestleController != null ? pestleController.GrindDuration : 0f;
                pestleController?.ResetSpeedAveraging();
            }

            RebuildCurrentBatch();
            uiManager?.RefreshAttemptPanels(this);
            return true;
        }

        private bool RemoveSingleIngredientFromCurrentBatch(IngredientData ingredient, bool refresh)
        {
            if (ingredient == null)
            {
                return false;
            }

            int removedEntries = ingredientBatchEntries.RemoveAll(entry => entry != null
                && entry.ingredient == ingredient
                && entry.batchID == currentBatchID);
            if (removedEntries == 0)
            {
                return false;
            }

            RemoveLastMatching(ingredientAmounts, instance => instance != null && instance.ingredient == ingredient);
            RemoveLastMatching(ingredientOrder, item => item == ingredient);
            RemoveLastMatching(selectedRatioPattern, requirement => requirement != null && requirement.ingredient == ingredient);

            IngredientBatchEntry previousEntry = ingredientBatchEntries
                .Where(entry => entry != null && entry.ingredient == ingredient)
                .OrderByDescending(entry => entry.batchID)
                .FirstOrDefault();
            if (previousEntry != null)
            {
                batchByIngredient[ingredient] = previousEntry.batchID;
            }
            else
            {
                batchByIngredient.Remove(ingredient);
            }

            if (refresh)
            {
                bool currentBatchIsEmpty = !ingredientBatchEntries.Any(entry => entry != null
                    && entry.batchID == currentBatchID
                    && entry.ingredient != null);
                if (currentBatchIsEmpty)
                {
                    currentBatchStartGrindDuration = pestleController != null ? pestleController.GrindDuration : 0f;
                    pestleController?.ResetSpeedAveraging();
                }

                RebuildCurrentBatch();
                uiManager?.RefreshAttemptPanels(this);
            }

            return true;
        }

        public bool ShowGroundVisualsForCurrentBatch()
        {
            if (!HasCurrentBatchReachedPowderTime())
            {
                return false;
            }

            bool changedAnyIngredient = false;
            foreach (DraggableIngredient ingredient in FindObjectsByType<DraggableIngredient>(FindObjectsSortMode.None))
            {
                if (ingredient == null || !ingredient.gameObject.activeInHierarchy || !ingredient.IsInMortar || ingredient.ingredientData == null)
                {
                    continue;
                }

                if (!IsIngredientInBatch(ingredient.ingredientData, currentBatchID))
                {
                    continue;
                }

                bool wasGround = ingredient.HasBeenGround;
                ingredient.ShowGroundState();
                changedAnyIngredient |= !wasGround && ingredient.HasBeenGround;
            }

            if (changedAnyIngredient)
            {
                uiManager?.ShowHint(BuildPowderReadyHint());
            }

            return changedAnyIngredient;
        }

        public bool HasCurrentBatchReachedPowderTime()
        {
            return GetCurrentBatchGrindDuration() >= GetMinimumBatchPowderSeconds();
        }

        private void ReturnIngredientsHome()
        {
            foreach (DraggableIngredient ingredient in FindObjectsByType<DraggableIngredient>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (ingredient != null)
                {
                    if (ingredient.ingredientData == null)
                    {
                        ingredient.gameObject.SetActive(false);
                        continue;
                    }

                    ingredient.gameObject.SetActive(true);
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

            bool currentBatchHasIngredients = ingredientBatchEntries.Any(entry => entry != null && entry.batchID == currentBatchID && entry.ingredient != null);
            if (!currentBatchHasIngredients)
            {
                uiManager?.ShowHint("The next batch is already empty.");
                return;
            }

            float currentBatchDuration = GetCurrentBatchGrindDuration();
            if (currentBatchDuration < GetMinimumBatchPowderSeconds())
            {
                uiManager?.ShowHint("Grind this batch until it becomes powder before starting a new batch.");
                return;
            }

            RecordCurrentBatchAsPreparedIfReady();
            finalizedBatchDurations[currentBatchID] = currentBatchDuration;
            finalizedBatchForces[currentBatchID] = GetCurrentForceLevel();
            finalizedBatchSpeeds[currentBatchID] = GetCurrentSpeedLevel();
            List<IngredientData> currentBatchIngredients = GetCurrentBatchIngredients();
            bool isFinalCombinedBatch = currentLevel != null
                && currentLevel.requireFinalCombinedBatch
                && HasExactCurrentRequiredIngredients(currentBatchIngredients);
            bool createsMixedPowder = currentLevel != null
                && currentLevel.requireFinalCombinedBatch
                && !isFinalCombinedBatch
                && currentBatchIngredients.Count > 1;
            if (isFinalCombinedBatch)
            {
                ingredientDisplayManager?.ClearPreparedPowderDisplays();
                ingredientDisplayManager?.ClearMixedPowderBatches();
            }
            else if (createsMixedPowder)
            {
                ingredientDisplayManager?.ShowMixturePowderInIngredientSlot(currentBatchIngredients);
            }
            else if (currentLevel == null || !currentLevel.requireFinalCombinedBatch)
            {
                ingredientDisplayManager?.ShowMixedPowderBatch(currentBatchID, currentBatchIngredients);
            }

            ReturnCurrentBatchIngredientsHome(!createsMixedPowder);
            currentBatchID++;
            currentBatchStartGrindDuration = pestleController != null ? pestleController.GrindDuration : 0f;
            pestleController?.ResetSpeedAveraging();
            uiManager?.RefreshAttemptPanels(this);
            uiManager?.ShowHint("Next ingredients will start a separate batch.");
        }

        public float GetCurrentBatchGrindDuration()
        {
            float totalDuration = pestleController != null ? pestleController.GrindDuration : 0f;
            return Mathf.Max(0f, totalDuration - currentBatchStartGrindDuration);
        }

        private float GetMinimumBatchPowderSeconds()
        {
            return RequiredBatchPowderSeconds;
        }

        private string BuildPowderReadyHint()
        {
            int preparedCount = ingredientBatchEntries
                .Where(entry => entry != null && entry.ingredient != null)
                .Select(entry => entry.batchID)
                .Distinct()
                .Count();
            int requiredCount = currentLevel != null && currentLevel.requiredIngredients != null
                ? currentLevel.requiredIngredients.Where(ingredient => ingredient != null).Distinct().Count()
                : 0;

            if (currentLevel != null
                && currentLevel.enabledMechanics != null
                && currentLevel.enabledMechanics.enableCombination
                && requiredCount > 0
                && preparedCount < requiredCount)
            {
                return "This batch is powder. Press New Batch, then add the next ingredient.";
            }

            return "This batch is powder. You can Evaluate when the recipe is ready.";
        }

        private List<IngredientData> GetCurrentBatchIngredients()
        {
            return batchByIngredient
                .Where(pair => pair.Value == currentBatchID && pair.Key != null)
                .Select(pair => pair.Key)
                .Concat(ingredientBatchEntries
                    .Where(entry => entry != null && entry.batchID == currentBatchID && entry.ingredient != null)
                    .Select(entry => entry.ingredient))
                .Distinct()
                .ToList();
        }

        private void ReturnCurrentBatchIngredientsHome(bool keepGroundState)
        {
            List<IngredientData> currentBatchIngredients = GetCurrentBatchIngredients();
            foreach (DraggableIngredient ingredient in FindObjectsByType<DraggableIngredient>(FindObjectsSortMode.None))
            {
                if (ingredient == null || !ingredient.gameObject.activeInHierarchy || ingredient.ingredientData == null)
                {
                    continue;
                }

                if (currentBatchIngredients.Contains(ingredient.ingredientData))
                {
                    if (keepGroundState)
                    {
                        ingredient.ReturnHomeAsGround();
                    }
                    else
                    {
                        ingredient.HideAtHome();
                    }
                }
            }
        }

        private void RebuildCurrentBatch()
        {
            grindingBatches.Clear();

            Dictionary<IngredientData, RatioLevel> pattern = CalculateRatioPattern(out _);
            IEnumerable<IGrouping<int, IngredientData>> groups = ingredientBatchEntries
                .Where(entry => entry != null && entry.ingredient != null)
                .GroupBy(entry => entry.batchID, entry => entry.ingredient)
                .OrderBy(group => group.Key);

            foreach (IGrouping<int, IngredientData> group in groups)
            {
                GrindingBatch batch = new GrindingBatch
                {
                    batchID = group.Key,
                    forceLevel = GetBatchForceLevel(group.Key),
                    speedLevel = GetBatchSpeedLevel(group.Key),
                    grindDuration = GetBatchGrindDuration(group.Key)
                };

                HashSet<IngredientData> batchIngredients = new HashSet<IngredientData>(group);
                batch.ingredientsInBatch.AddRange(batchIngredients);
                batch.ingredientOrderInBatch.AddRange(ingredientOrder.Where(batchIngredients.Contains));
                foreach (KeyValuePair<IngredientData, RatioLevel> pair in pattern.Where(pair => batchIngredients.Contains(pair.Key)))
                {
                    batch.ratioPatternInBatch.Add(new RatioRequirement { ingredient = pair.Key, ratioLevel = pair.Value });
                }

                grindingBatches.Add(batch);
            }
        }

        public bool TryGetPreparedAloneBatchID(IngredientData ingredient, out int batchID)
        {
            return TryGetPreparedBatchID(new[] { ingredient }, out batchID);
        }

        public bool TryGetPreparedBatchID(IEnumerable<IngredientData> ingredients, out int batchID)
        {
            string key = RecipeLevelData.BuildCombinationKey(ingredients);
            if (!string.IsNullOrWhiteSpace(key) && preparedBatchIDsByKey.TryGetValue(key, out batchID))
            {
                return true;
            }

            batchID = 0;
            return false;
        }

        private float GetBatchGrindDuration(int batchID)
        {
            if (batchID == currentBatchID)
            {
                return GetCurrentBatchGrindDuration();
            }

            return finalizedBatchDurations.TryGetValue(batchID, out float duration) ? duration : 0f;
        }

        private ForceLevel GetBatchForceLevel(int batchID)
        {
            if (batchID == currentBatchID)
            {
                return GetCurrentForceLevel();
            }

            return finalizedBatchForces.TryGetValue(batchID, out ForceLevel force) ? force : ForceLevel.Medium;
        }

        private SpeedLevel GetBatchSpeedLevel(int batchID)
        {
            if (batchID == currentBatchID)
            {
                return GetCurrentSpeedLevel();
            }

            return finalizedBatchSpeeds.TryGetValue(batchID, out SpeedLevel speed) ? speed : SpeedLevel.Medium;
        }

        private ForceLevel GetCurrentForceLevel()
        {
            return forceController != null ? forceController.CurrentForceLevel : ForceLevel.Medium;
        }

        private SpeedLevel GetCurrentSpeedLevel()
        {
            return pestleController != null ? pestleController.EvaluatedSpeedLevel : SpeedLevel.Medium;
        }

        private void AddIngredientWithRatio(IngredientData ingredient, RatioLevel ratio)
        {
            AddIngredientWithRatio(ingredient, ratio, true);
        }

        private void AddIngredientWithRatio(IngredientData ingredient, RatioLevel ratio, bool refresh)
        {
            bool wasCurrentBatchSingle = GetCurrentBatchIngredients().Count == 1;
            ingredientAmounts.Add(new IngredientInstance(ingredient, 1));
            ingredientOrder.Add(ingredient);
            selectedRatioPattern.Add(new RatioRequirement { ingredient = ingredient, ratioLevel = ratio });
            batchByIngredient[ingredient] = currentBatchID;
            ingredientBatchEntries.Add(new IngredientBatchEntry { ingredient = ingredient, batchID = currentBatchID });
            if (currentLevel != null && currentLevel.requireFinalCombinedBatch && wasCurrentBatchSingle && GetCurrentBatchIngredients().Count > 1)
            {
                currentBatchStartGrindDuration = pestleController != null ? pestleController.GrindDuration : 0f;
                pestleController?.ResetSpeedAveraging();
            }

            if (refresh)
            {
                RebuildCurrentBatch();
                uiManager?.RefreshAttemptPanels(this);
                if (uiManager != null
                    && !uiManager.ShowStepFeedback(MechanicType.IngredientOrder, ingredient)
                    && !uiManager.ShowStepFeedback(MechanicType.Ratio, ingredient)
                    && !uiManager.ShowStepFeedback(MechanicType.Speed, ingredient)
                    && !uiManager.ShowStepFeedback(MechanicType.Force, ingredient))
                {
                    uiManager.ShowStepFeedback(MechanicType.Combination, ingredient);
                }
            }
        }

        private bool HasExactCurrentRequiredIngredients(IReadOnlyList<IngredientData> currentBatchIngredients)
        {
            if (currentLevel == null || currentLevel.requiredIngredients == null || currentBatchIngredients == null)
            {
                return false;
            }

            HashSet<IngredientData> current = new HashSet<IngredientData>(currentBatchIngredients.Where(ingredient => ingredient != null));
            HashSet<IngredientData> required = new HashSet<IngredientData>(currentLevel.requiredIngredients.Where(ingredient => ingredient != null));
            return required.Count > 1 && current.SetEquals(required);
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

        private bool IsIngredientInBatch(IngredientData ingredient, int batchID)
        {
            return ingredient != null && ingredientBatchEntries.Any(entry => entry != null && entry.ingredient == ingredient && entry.batchID == batchID);
        }

        private void RecordCurrentBatchAsPreparedIfReady()
        {
            if (currentLevel == null || !currentLevel.requireFinalCombinedBatch || !HasCurrentBatchReachedPowderTime())
            {
                return;
            }

            List<IngredientData> currentIngredients = GetCurrentBatchIngredients();
            if (currentIngredients.Count == 0)
            {
                return;
            }

            string key = RecipeLevelData.BuildCombinationKey(currentIngredients);
            if (!string.IsNullOrWhiteSpace(key) && !preparedBatchIDsByKey.ContainsKey(key))
            {
                preparedBatchIDsByKey[key] = currentBatchID;
            }
        }

        private static void RemoveLastMatching<T>(List<T> list, System.Predicate<T> predicate)
        {
            if (list == null || predicate == null)
            {
                return;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i]))
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }

        private class IngredientBatchEntry
        {
            public IngredientData ingredient;
            public int batchID;
        }
    }
}
