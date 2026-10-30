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
        [Tooltip("Fallback powdering time when the current level does not provide one. This controls the ingredient-to-powder transition and is not evaluated as a recipe answer.")]
        public float batchPowderSeconds = 1.5f;
        [Header("Final Crafted Object")]
        [Tooltip("Level ID that should craft a final object instead of turning the final wrapper ingredient into powder.")]
        public string finalCraftLevelID = "L09";
        [Tooltip("Ingredient ID that triggers final wrapping/crafting after the main prepared batch exists.")]
        public string finalCraftWrapperIngredientID = "LotusLeaf";
        [Tooltip("Resources path for the final crafted model, without file extension.")]
        public string finalCraftPrefabResourcePath = "Zhongzi/lod_basic_pbr";
        public string finalCraftDiffuseResourcePath = "Zhongzi/texture_diffuse";
        public string finalCraftNormalResourcePath = "Zhongzi/texture_normal";
        public string finalCraftMetallicResourcePath = "Zhongzi/texture_metallic";
        public string finalCraftRoughnessResourcePath = "Zhongzi/texture_roughness";
        [Tooltip("How many seconds of valid pestle motion the final wrapper needs before it becomes the crafted object.")]
        public float finalCraftMotionSeconds = 0.15f;
        public Vector3 finalCraftPositionOffset = new Vector3(0f, 0.18f, 0f);
        public Vector3 finalCraftEulerAngles = new Vector3(0f, 35f, 0f);
        public Vector3 finalCraftScale = new Vector3(0.42f, 0.42f, 0.42f);

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
        private GameObject finalCraftedObjectInstance;
        private bool finalCraftCompleted;

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
        public bool CurrentBatchHasIngredients => GetCurrentBatchIngredients().Count > 0;
        public bool HasEvaluated { get; private set; }
        public bool HasCorrectEvaluation { get; private set; }
        // Passing the level unlocks progression independently of subsequent attempts.
        public bool HasUnlockedNextLevel { get; private set; }
        public bool HasAutoEvaluated { get; private set; }
        public float RequiredBatchPowderSeconds => Mathf.Max(0.2f, batchPowderSeconds);

        public bool TryAddIngredient(IngredientData ingredient)
        {
            return TryAddIngredients(new[] { ingredient });
        }

        public bool TryAddIngredients(IReadOnlyList<IngredientData> ingredients)
        {
            if (HasEvaluated)
            {
                uiManager?.ShowHint("Press Reset Attempt before changing an evaluated recipe.");
                return false;
            }

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
                bool isPreparedPowder = currentLevel != null
                    && currentLevel.requireFinalCombinedBatch
                    && ingredientBatchEntries.Any(entry => entry != null && entry.ingredient == ingredient);
                AddIngredientWithRatio(ingredient, isPreparedPowder ? GetPreviouslySelectedRatio(ingredient) : RatioLevel.Medium, false);
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

            // A returned prepared powder already has its recipe amount. Do
            // not open the ratio picker a second time when it is dragged back
            // for the final fusion batch.
            if (canReuseGroundIngredient)
            {
                AddIngredientWithRatio(ingredient, GetPreviouslySelectedRatio(ingredient));
                return true;
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
            ingredientBatchEntries.Clear();
            finalizedBatchDurations.Clear();
            finalizedBatchForces.Clear();
            finalizedBatchSpeeds.Clear();
            preparedBatchIDsByKey.Clear();
            currentBatchID = 1;
            finalCraftCompleted = false;
            ClearFinalCraftedObject();
            HasEvaluated = false;
            HasCorrectEvaluation = false;
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
            HasUnlockedNextLevel = false;
            ResetAttempt(false);
        }

        public void MarkAutoEvaluated()
        {
            HasAutoEvaluated = true;
        }

        public void MarkEvaluated(EvaluationResult result)
        {
            HasEvaluated = true;
            HasCorrectEvaluation = result != null && result.judgement == JudgementResult.Correct;
            HasUnlockedNextLevel |= HasCorrectEvaluation;
            uiManager?.RefreshAttemptPanels(this);
        }

        public bool RemoveIngredientFromCurrentBatch(IngredientData ingredient)
        {
            return RemoveIngredientsFromCurrentBatch(new[] { ingredient });
        }

        public bool RemoveIngredientsFromCurrentBatch(IReadOnlyList<IngredientData> ingredients)
        {
            if (HasEvaluated)
            {
                return false;
            }

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
            if (TryShowFinalCraftedObject())
            {
                return true;
            }

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
                RecordCurrentBatchAsPreparedIfReady();
                uiManager?.ShowHint(BuildPowderReadyHint());
            }

            return changedAnyIngredient;
        }

        public bool HasCurrentBatchReachedPowderTime()
        {
            if (IsFinalCraftBatchReady())
            {
                return true;
            }

            // A final-combination batch is made from powders that were already
            // completed in earlier batches. Returning those prepared powders
            // to the mortar is the fusion step; it must not require a second
            // timed grinding pass.
            if (IsPreparedPowderFinalBlendReady())
            {
                return true;
            }

            return GetCurrentBatchGrindDuration() >= GetMinimumBatchPowderSeconds();
        }

        public float GetCurrentBatchRequiredCompletionSeconds()
        {
            if (IsFinalCraftLevel() && HasFinalCraftWrapperInCurrentBatch() && HasRequiredPreparedMainBatch())
            {
                return Mathf.Max(0.01f, finalCraftMotionSeconds);
            }

            return GetMinimumBatchPowderSeconds();
        }

        public string BuildIncompleteEvaluationHint()
        {
            List<IngredientData> remaining = GetRemainingRequiredIngredients();
            if (currentLevel != null
                && currentLevel.requireFinalCombinedBatch
                && currentBatchID > 1
                && HasRequiredPreparationBeforeCurrentBatch()
                && CurrentBatchHasIngredients
                && !IsPreparedPowderFinalBlendReady())
            {
                return "Put all prepared powders back into the mortar together to combine them, then Evaluate. No additional grinding is needed.";
            }

            if (remaining.Count == 0)
            {
                return string.Empty;
            }

            List<IngredientData> currentIngredients = GetCurrentBatchIngredients();
            List<IngredientData> sameGroupMissing = GetIngredientsMissingFromCurrentTargetGroup(currentIngredients, remaining);
            if (sameGroupMissing.Count > 0)
            {
                if (HasCurrentBatchReachedPowderTime())
                {
                    return "The recipe is incomplete: " + FormatIngredientNames(sameGroupMissing)
                        + " belonged in the batch that is already powder. Press Reset Attempt and add the complete batch before grinding.";
                }

                return "The recipe is incomplete. Add " + FormatIngredientNames(sameGroupMissing)
                    + " to the current batch before grinding and evaluating.";
            }

            if (ingredientBatchEntries.Count == 0)
            {
                return "The recipe has no ingredients yet. " + CapitalizeInstruction(BuildAddInstruction(remaining)) + " before grinding.";
            }

            if (!CurrentBatchHasIngredients)
            {
                return "The recipe is incomplete. " + CapitalizeInstruction(BuildAddInstruction(remaining)) + " before evaluating.";
            }

            if (HasCurrentBatchReachedPowderTime())
            {
                return "The recipe is incomplete: " + FormatIngredientNames(remaining)
                    + (remaining.Count == 1 ? " is" : " are")
                    + " still missing. Press New Batch, then " + BuildAddInstruction(remaining) + ".";
            }

            return "The recipe is incomplete: add " + FormatIngredientNames(remaining) + " before evaluating.";
        }

        private bool TryShowFinalCraftedObject()
        {
            if (!IsFinalCraftBatchReady() || finalCraftCompleted)
            {
                return false;
            }

            HideCurrentBatchIngredientsInMortar();
            ingredientDisplayManager?.ClearMixedPowderBatches();
            SpawnFinalCraftedObject();
            finalCraftCompleted = finalCraftedObjectInstance != null;
            if (finalCraftCompleted)
            {
                uiManager?.ShowHint("The filling is wrapped. You can Evaluate when the recipe is ready.");
            }

            return finalCraftCompleted;
        }

        private bool IsFinalCraftBatchReady()
        {
            return IsFinalCraftLevel()
                && HasFinalCraftWrapperInCurrentBatch()
                && HasRequiredPreparedMainBatch()
                && GetCurrentBatchGrindDuration() >= GetCurrentBatchRequiredCompletionSeconds();
        }

        private bool IsPreparedPowderFinalBlendReady()
        {
            if (currentLevel == null
                || !currentLevel.requireFinalCombinedBatch
                || currentBatchID <= 1)
            {
                return false;
            }

            List<IngredientData> currentIngredients = GetCurrentBatchIngredients();
            return HasExactCurrentRequiredIngredients(currentIngredients)
                && HasRequiredPreparationBeforeCurrentBatch();
        }

        private bool IsFinalCraftLevel()
        {
            return currentLevel != null
                && !string.IsNullOrWhiteSpace(finalCraftLevelID)
                && string.Equals(currentLevel.levelID, finalCraftLevelID, System.StringComparison.OrdinalIgnoreCase);
        }

        private bool HasFinalCraftWrapperInCurrentBatch()
        {
            List<IngredientData> currentIngredients = GetCurrentBatchIngredients();
            return currentIngredients.Count == 1
                && currentIngredients.Any(ingredient => ingredient != null
                    && string.Equals(ingredient.ingredientID, finalCraftWrapperIngredientID, System.StringComparison.OrdinalIgnoreCase));
        }

        private bool HasRequiredPreparedMainBatch()
        {
            if (currentLevel == null || currentLevel.correctCombinationPattern == null)
            {
                return false;
            }

            List<IngredientData> currentIngredients = GetCurrentBatchIngredients();
            foreach (CombinationGroup group in currentLevel.correctCombinationPattern.groups)
            {
                List<IngredientData> groupIngredients = group != null && group.ingredients != null
                    ? group.ingredients.Where(ingredient => ingredient != null).ToList()
                    : new List<IngredientData>();
                if (groupIngredients.Count <= 1 || groupIngredients.Any(currentIngredients.Contains))
                {
                    continue;
                }

                string key = RecipeLevelData.BuildCombinationKey(groupIngredients);
                if (!string.IsNullOrWhiteSpace(key) && preparedBatchIDsByKey.ContainsKey(key))
                {
                    return true;
                }

                if (HasCompletedBatchWithExactIngredients(groupIngredients))
                {
                    return true;
                }

                if (grindingBatches.Any(batch => batch != null
                    && batch.batchID != currentBatchID
                    && batch.ingredientsInBatch != null
                    && new HashSet<IngredientData>(batch.ingredientsInBatch.Where(ingredient => ingredient != null)).SetEquals(groupIngredients)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasCompletedBatchWithExactIngredients(IReadOnlyList<IngredientData> groupIngredients)
        {
            if (groupIngredients == null || groupIngredients.Count == 0)
            {
                return false;
            }

            HashSet<IngredientData> target = new HashSet<IngredientData>(groupIngredients.Where(ingredient => ingredient != null));
            return ingredientBatchEntries
                .Where(entry => entry != null && entry.ingredient != null && entry.batchID != currentBatchID)
                .GroupBy(entry => entry.batchID, entry => entry.ingredient)
                .Any(group =>
                {
                    HashSet<IngredientData> ingredients = new HashSet<IngredientData>(group);
                    return ingredients.SetEquals(target)
                        && finalizedBatchDurations.TryGetValue(group.Key, out float duration)
                        && duration >= GetMinimumBatchPowderSeconds();
                });
        }

        private void HideCurrentBatchIngredientsInMortar()
        {
            List<IngredientData> currentBatchIngredients = GetCurrentBatchIngredients();
            foreach (DraggableIngredient ingredient in FindObjectsByType<DraggableIngredient>(FindObjectsSortMode.None))
            {
                if (ingredient == null || !ingredient.gameObject.activeInHierarchy || ingredient.ingredientData == null)
                {
                    continue;
                }

                if (ingredient.IsInMortar && currentBatchIngredients.Contains(ingredient.ingredientData))
                {
                    ingredient.gameObject.SetActive(false);
                }
            }
        }

        private void SpawnFinalCraftedObject()
        {
            ClearFinalCraftedObject();
            GameObject prefab = !string.IsNullOrWhiteSpace(finalCraftPrefabResourcePath)
                ? Resources.Load<GameObject>(finalCraftPrefabResourcePath)
                : null;
            if (prefab == null)
            {
                uiManager?.ShowHint("Final model is missing. Check the final craft prefab path.");
                return;
            }

            Vector3 anchor = GetMortarCenterWorldPosition();
            finalCraftedObjectInstance = Instantiate(prefab, anchor + finalCraftPositionOffset, Quaternion.Euler(finalCraftEulerAngles));
            finalCraftedObjectInstance.name = "Final Crafted Zhongzi";
            finalCraftedObjectInstance.transform.localScale = finalCraftScale;
            ApplyFinalCraftedObjectMaterial(finalCraftedObjectInstance);
            foreach (Collider collider in finalCraftedObjectInstance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        private void ApplyFinalCraftedObjectMaterial(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            Texture2D diffuse = LoadTexture(finalCraftDiffuseResourcePath);
            Texture2D normal = LoadTexture(finalCraftNormalResourcePath);
            Texture2D metallic = LoadTexture(finalCraftMetallicResourcePath);
            Texture2D roughness = LoadTexture(finalCraftRoughnessResourcePath);

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                Material material = new Material(Shader.Find("Standard"));
                if (diffuse != null)
                {
                    material.mainTexture = diffuse;
                }

                if (normal != null)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.EnableKeyword("_NORMALMAP");
                }

                if (metallic != null)
                {
                    material.SetTexture("_MetallicGlossMap", metallic);
                    material.SetFloat("_Metallic", 0.1f);
                    material.EnableKeyword("_METALLICGLOSSMAP");
                }

                if (roughness != null)
                {
                    material.SetTexture("_SpecGlossMap", roughness);
                    material.SetFloat("_Glossiness", 0.35f);
                    material.EnableKeyword("_SPECGLOSSMAP");
                }

                renderer.material = material;
            }
        }

        private static Texture2D LoadTexture(string resourcePath)
        {
            return !string.IsNullOrWhiteSpace(resourcePath)
                ? Resources.Load<Texture2D>(resourcePath)
                : null;
        }

        private Vector3 GetMortarCenterWorldPosition()
        {
            MortarArea activeMortar = ingredientDisplayManager != null && ingredientDisplayManager.mortarArea != null
                ? ingredientDisplayManager.mortarArea
                : FindFirstObjectByType<MortarArea>();
            if (activeMortar == null)
            {
                return transform.position;
            }

            Collider mortarCollider = activeMortar.GetComponent<Collider>();
            if (mortarCollider == null)
            {
                return activeMortar.transform.position;
            }

            Bounds bounds = mortarCollider.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        private void ClearFinalCraftedObject()
        {
            if (finalCraftedObjectInstance == null)
            {
                return;
            }

            Destroy(finalCraftedObjectInstance);
            finalCraftedObjectInstance = null;
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
            return grindingBatches.Count > 0 ? grindingBatches[grindingBatches.Count - 1] : null;
        }

        public void StartNewBatch()
        {
            if (HasEvaluated)
            {
                uiManager?.ShowHint("Press Reset Attempt before starting another batch.");
                return;
            }

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
                && HasExactCurrentRequiredIngredients(currentBatchIngredients)
                && HasRequiredPreparationBeforeCurrentBatch();
            bool combinedAllIngredientsTooEarly = currentLevel != null
                && currentLevel.requireFinalCombinedBatch
                && HasExactCurrentRequiredIngredients(currentBatchIngredients)
                && !isFinalCombinedBatch;
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
            else if ((currentLevel == null || !currentLevel.requireFinalCombinedBatch)
                && currentBatchIngredients.Count > 1)
            {
                ingredientDisplayManager?.ShowMixedPowderBatch(currentBatchID, currentBatchIngredients);
            }

            ReturnCurrentBatchIngredientsHome(!createsMixedPowder);
            currentBatchID++;
            currentBatchStartGrindDuration = pestleController != null ? pestleController.GrindDuration : 0f;
            pestleController?.ResetSpeedAveraging();
            uiManager?.RefreshAttemptPanels(this);
            uiManager?.ShowHint(combinedAllIngredientsTooEarly
                ? "All three spices were ground together too early. Press Reset Attempt, then prepare any two together and the remaining spice in a separate batch."
                : isFinalCombinedBatch
                    ? "The prepared powders are combined. You can Evaluate the recipe; no additional grinding is needed."
                : BuildEmptyBatchNextStepHint(currentBatchIngredients));
        }

        public float GetCurrentBatchGrindDuration()
        {
            float totalDuration = pestleController != null ? pestleController.GrindDuration : 0f;
            return Mathf.Max(0f, totalDuration - currentBatchStartGrindDuration);
        }

        private float GetMinimumBatchPowderSeconds()
        {
            return currentLevel != null
                ? Mathf.Max(0.2f, currentLevel.powderingSeconds)
                : RequiredBatchPowderSeconds;
        }

        private string BuildPowderReadyHint()
        {
            List<IngredientData> currentIngredients = GetCurrentBatchIngredients();
            if (currentLevel != null
                && currentLevel.requireFinalCombinedBatch
                && HasExactCurrentRequiredIngredients(currentIngredients)
                && !HasRequiredPreparationBeforeCurrentBatch())
            {
                return currentLevel.allowAnyPairPreparation
                    ? "All three spices were ground together too early. Press Reset Attempt, then prepare any two together and the remaining spice separately."
                    : "These ingredients needed separate preparation before the final blend. Press Reset Attempt and prepare the required batches first.";
            }

            if (currentLevel != null
                && currentLevel.requireFinalCombinedBatch
                && HasExactCurrentRequiredIngredients(currentIngredients)
                && HasRequiredPreparationBeforeCurrentBatch())
            {
                return "The prepared powders are combined. You can Evaluate the recipe; no additional grinding is needed.";
            }

            List<IngredientData> remaining = GetRemainingRequiredIngredients();
            if (remaining.Count > 0)
            {
                List<IngredientData> sameGroupMissing = GetIngredientsMissingFromCurrentTargetGroup(currentIngredients, remaining);
                if (sameGroupMissing.Count > 0)
                {
                    return "This batch became powder before " + FormatIngredientNames(sameGroupMissing)
                        + " joined it. Press Reset Attempt and add the complete batch before grinding.";
                }

                return "This batch is powder. Press New Batch, then " + BuildAddInstruction(remaining) + ".";
            }

            if (currentLevel != null && currentLevel.requireFinalCombinedBatch)
            {
                return HasRequiredPreparationThroughCurrentBatch()
                    ? "The preparation batches are ready. Put all prepared powders back into the mortar together to combine them, then Evaluate. No additional grinding is needed."
                    : "This batch is powder, but the required preparation structure is incomplete. Evaluate for feedback or press Reset Attempt to rebuild the batches.";
            }

            return "All required ingredients are ready. You can Evaluate the recipe.";
        }

        private string BuildEmptyBatchNextStepHint(IReadOnlyList<IngredientData> finalizedIngredients)
        {
            List<IngredientData> remaining = GetRemainingRequiredIngredients();
            if (remaining.Count > 0)
            {
                List<IngredientData> sameGroupMissing = GetIngredientsMissingFromCurrentTargetGroup(finalizedIngredients, remaining);
                if (sameGroupMissing.Count > 0)
                {
                    return FormatIngredientNames(sameGroupMissing)
                        + " belonged in the batch you just finished. Press Reset Attempt and prepare that complete batch before grinding.";
                }

                return "New batch ready. " + BuildAddInstruction(remaining) + ".";
            }

            if (currentLevel != null && currentLevel.requireFinalCombinedBatch)
            {
                return HasRequiredPreparationBeforeCurrentBatch()
                    ? "All preparation batches are ready. Put all prepared powders back into the mortar together to combine them, then Evaluate. No additional grinding is needed."
                    : "All ingredients have been used, but the preparation batches are incomplete. Evaluate for feedback or press Reset Attempt to try a new batch structure.";
            }

            return "All required ingredients have been prepared. You can Evaluate the recipe.";
        }

        private List<IngredientData> GetRemainingRequiredIngredients()
        {
            if (currentLevel == null || currentLevel.requiredIngredients == null)
            {
                return new List<IngredientData>();
            }

            HashSet<IngredientData> used = new HashSet<IngredientData>(ingredientBatchEntries
                .Where(entry => entry != null && entry.ingredient != null)
                .Select(entry => entry.ingredient));
            List<IngredientData> remaining = currentLevel.requiredIngredients
                .Where(ingredient => ingredient != null && !used.Contains(ingredient))
                .Distinct()
                .ToList();

            if (currentLevel.correctIngredientOrder == null || currentLevel.correctIngredientOrder.Count == 0)
            {
                return remaining;
            }

            return remaining
                .OrderBy(ingredient =>
                {
                    int index = currentLevel.correctIngredientOrder.IndexOf(ingredient);
                    return index >= 0 ? index : int.MaxValue;
                })
                .ToList();
        }

        private List<IngredientData> GetIngredientsMissingFromCurrentTargetGroup(
            IReadOnlyList<IngredientData> currentIngredients,
            IReadOnlyList<IngredientData> remaining)
        {
            if (currentLevel == null
                || currentLevel.allowAnyPairPreparation
                || currentLevel.enabledMechanics == null
                || !currentLevel.enabledMechanics.enableCombination
                || currentLevel.correctCombinationPattern == null
                || currentLevel.correctCombinationPattern.groups == null
                || currentIngredients == null
                || remaining == null)
            {
                return new List<IngredientData>();
            }

            HashSet<IngredientData> current = new HashSet<IngredientData>(currentIngredients.Where(ingredient => ingredient != null));
            HashSet<IngredientData> missing = new HashSet<IngredientData>(remaining.Where(ingredient => ingredient != null));
            CombinationGroup group = currentLevel.correctCombinationPattern.groups.FirstOrDefault(candidate => candidate != null
                && candidate.ingredients != null
                && candidate.ingredients.Any(current.Contains));
            return group != null
                ? group.ingredients.Where(ingredient => ingredient != null && missing.Contains(ingredient)).Distinct().ToList()
                : new List<IngredientData>();
        }

        private string BuildAddInstruction(IReadOnlyList<IngredientData> remaining)
        {
            if (remaining == null || remaining.Count == 0)
            {
                return "continue with the recipe";
            }

            if (currentLevel != null && currentLevel.allowAnyPairPreparation && remaining.Count >= 3)
            {
                return "choose any two of " + FormatIngredientNames(remaining) + " and add them together";
            }

            if (currentLevel != null && currentLevel.allowAnyPairPreparation && remaining.Count == 2)
            {
                return "add " + FormatIngredientNames(remaining) + " together";
            }

            IngredientData next = remaining[0];
            List<IngredientData> nextGroup = currentLevel != null
                && currentLevel.correctCombinationPattern != null
                && currentLevel.correctCombinationPattern.groups != null
                ? currentLevel.correctCombinationPattern.groups
                    .Where(group => group != null && group.ingredients != null && group.ingredients.Contains(next))
                    .Select(group => group.ingredients.Where(remaining.Contains).Distinct().ToList())
                    .FirstOrDefault()
                : null;

            if (currentLevel != null
                && currentLevel.enabledMechanics != null
                && currentLevel.enabledMechanics.enableCombination
                && nextGroup != null
                && nextGroup.Count > 1)
            {
                return "add " + FormatIngredientNames(nextGroup) + " together";
            }

            return "add " + next.DisplayName + (currentLevel != null
                && currentLevel.enabledMechanics != null
                && currentLevel.enabledMechanics.enableCombination
                ? " on its own"
                : string.Empty);
        }

        private static string FormatIngredientNames(IEnumerable<IngredientData> ingredients)
        {
            List<string> names = ingredients != null
                ? ingredients.Where(ingredient => ingredient != null).Select(ingredient => ingredient.DisplayName).Distinct().ToList()
                : new List<string>();
            if (names.Count == 0) return "the missing ingredient";
            if (names.Count == 1) return names[0];
            if (names.Count == 2) return names[0] + " and " + names[1];
            return string.Join(", ", names.Take(names.Count - 1)) + ", and " + names[names.Count - 1];
        }

        private static string CapitalizeInstruction(string instruction)
        {
            return string.IsNullOrWhiteSpace(instruction)
                ? "Continue with the recipe"
                : char.ToUpperInvariant(instruction[0]) + instruction.Substring(1);
        }

        private bool HasRequiredPreparationBeforeCurrentBatch()
        {
            return HasRequiredPreparationByBatch(currentBatchID - 1);
        }

        private bool HasRequiredPreparationThroughCurrentBatch()
        {
            return HasRequiredPreparationByBatch(currentBatchID);
        }

        private bool HasRequiredPreparationByBatch(int latestAllowedBatchID)
        {
            if (currentLevel == null || currentLevel.requiredIngredients == null)
            {
                return false;
            }

            List<IngredientData> required = currentLevel.requiredIngredients
                .Where(ingredient => ingredient != null)
                .Distinct()
                .ToList();
            if (required.Count <= 1)
            {
                return false;
            }

            if (currentLevel.allowAnyPairPreparation && required.Count == 3)
            {
                for (int first = 0; first < required.Count - 1; first++)
                {
                    for (int second = first + 1; second < required.Count; second++)
                    {
                        List<IngredientData> pair = new List<IngredientData> { required[first], required[second] };
                        IngredientData remaining = required.First(ingredient => !pair.Contains(ingredient));
                        if (WasPreparedByBatch(pair, latestAllowedBatchID)
                            && WasPreparedByBatch(new[] { remaining }, latestAllowedBatchID))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            return required.All(ingredient => WasPreparedByBatch(new[] { ingredient }, latestAllowedBatchID));
        }

        private bool WasPreparedByBatch(IEnumerable<IngredientData> ingredients, int latestAllowedBatchID)
        {
            string key = RecipeLevelData.BuildCombinationKey(ingredients);
            return !string.IsNullOrWhiteSpace(key)
                && preparedBatchIDsByKey.TryGetValue(key, out int preparedBatchID)
                && preparedBatchID <= latestAllowedBatchID;
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

        private RatioLevel GetPreviouslySelectedRatio(IngredientData ingredient)
        {
            if (ingredient != null)
            {
                for (int index = selectedRatioPattern.Count - 1; index >= 0; index--)
                {
                    RatioRequirement requirement = selectedRatioPattern[index];
                    if (requirement != null && requirement.ingredient == ingredient)
                    {
                        return requirement.ratioLevel;
                    }
                }
            }

            return RatioLevel.Medium;
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
            List<RatioLevel> choices = new List<RatioLevel>
            {
                RatioLevel.VeryLess,
                RatioLevel.Less,
                RatioLevel.SlightlyMore,
                RatioLevel.More
            };

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
