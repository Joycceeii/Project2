using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheTasteReviver
{
    [Serializable]
    public class HudPanelLayout
    {
        public Vector2 size;
        public Vector2 position;
        public int fontSize;

        public HudPanelLayout()
        {
        }

        public HudPanelLayout(float width, float height, float x, float y, int fontSize = 22)
        {
            size = new Vector2(width, height);
            position = new Vector2(x, y);
            this.fontSize = fontSize;
        }
    }

    public class UIManager : MonoBehaviour
    {
        public RecipeAttemptManager attemptManager;
        public RecipeEvaluator evaluator;
        public HintManager hintManager;
        public ExperimentLogManager logManager;
        public LevelManager levelManager;

        public TMP_Text levelLabel;
        public TMP_Text openingLevelTitleLabel;
        public TMP_Text currentOrderLabel;
        public TMP_Text currentRatioLabel;
        public TMP_Text currentSpeedLabel;
        public TMP_Text hintLabel;
        public TMP_Text ingredientTraitLabel;
        public Button ingredientTraitToggleButton;
        public GameObject ratioSelectionPanel;
        public TMP_Text ratioSelectionTitle;
        public Button[] ratioSelectionButtons = new Button[4];
        public Button experimentLogButton;
        public Button evaluateButton;
        public Button resetAttemptButton;
        public Button newBatchButton;
        public Button nextLevelButton;
        public string experimentLogSceneName = "ExperimentLog";
        public bool autoUpdateLevelLabel;
        [Tooltip("When enabled, grinding can submit an attempt automatically after the gate is reached. Leave off when the player should click Evaluate manually.")]
        public bool enableAutoEvaluation = false;
        public string resetRequiredHint = "Press Reset Attempt before starting a new test.";
        public float resetPromptDelaySeconds = 3f;

        [Header("Layout Control")]
        [Tooltip("Off lets you move panels directly in the Scene view without NormalizeHudLayout overwriting their RectTransforms.")]
        public bool driveHudLayoutFromInspector = false;

        [Header("HUD Layout")]
        public HudPanelLayout levelPanelLayout = new HudPanelLayout(430f, 82f, 288f, -42f, 23);
        public HudPanelLayout currentOrderPanelLayout = new HudPanelLayout(430f, 118f, 56f, -166f, 23);
        public HudPanelLayout currentRatioPanelLayout = new HudPanelLayout(430f, 138f, 56f, -296f, 23);
        public HudPanelLayout currentSpeedPanelLayout = new HudPanelLayout(390f, 58f, 56f, -438f, 20);
        public HudPanelLayout hintPanelLayout = new HudPanelLayout(430f, 258f, -56f, -132f, 20);
        public HudPanelLayout experimentLogButtonLayout = new HudPanelLayout(210f, 54f, 56f, -42f);
        public HudPanelLayout evaluateButtonLayout = new HudPanelLayout(200f, 56f, -56f, 174f);
        public HudPanelLayout resetAttemptButtonLayout = new HudPanelLayout(200f, 56f, -56f, 112f);
        public HudPanelLayout newBatchButtonLayout = new HudPanelLayout(200f, 56f, -56f, 50f);
        public HudPanelLayout nextLevelButtonLayout = new HudPanelLayout(200f, 56f, 56f, 86f);
        public HudPanelLayout ingredientTraitToggleButtonLayout = new HudPanelLayout(178f, 54f, -56f, -264f);
        public HudPanelLayout forceSliderLayout = new HudPanelLayout(390f, 52f, 56f, 220f);
        public HudPanelLayout forceLabelPanelLayout = new HudPanelLayout(390f, 52f, 56f, 282f, 15);
        public HudPanelLayout ingredientTraitsPanelLayout = new HudPanelLayout(430f, 300f, -56f, -438f, 24);
        public HudPanelLayout ratioSelectionPanelLayout = new HudPanelLayout(420f, 260f, 0f, 0f);

        [Header("Traits Overlay")]
        public HudPanelLayout ingredientTraitsExpandedPanelLayout = new HudPanelLayout(820f, 520f, 0f, -12f, 30);
        public Vector2 ingredientTraitExpandedTextInsetMin = new Vector2(70f, 46f);
        public Vector2 ingredientTraitExpandedTextInsetMax = new Vector2(-64f, -56f);
        [Range(0f, 1f)]
        public float ingredientTraitBackdropAlpha = 0.34f;

        [Header("HUD Text Insets")]
        public Vector2 hudTextInsetMin = new Vector2(90f, 46f);
        public Vector2 hudTextInsetMax = new Vector2(-90f, -66f);
        public Vector2 buttonTextInsetMin = new Vector2(46f, 12f);
        public Vector2 buttonTextInsetMax = new Vector2(-46f, -14f);
        public Vector2 forceLabelTextInsetMin = new Vector2(58f, 13f);
        public Vector2 forceLabelTextInsetMax = new Vector2(-58f, -13f);
        public bool showOpeningLevelTitle = true;
        public float openingLevelTitleSeconds = 2.4f;

        [Header("Ingredient Hover Tooltip")]
        public Vector2 ingredientTooltipSize = new Vector2(380f, 170f);
        public Vector2 ingredientTooltipCursorOffset = new Vector2(24f, -22f);
        public int ingredientTooltipFontSize = 18;
        public int ingredientTooltipNameFontSize = 24;

        private Action<RatioLevel> pendingRatioSelection;
        private Coroutine resetPromptCoroutine;
        private Coroutine openingLevelTitleCoroutine;
        private float lastEvaluationTime = -999f;
        private GameObject ingredientTraitPanel;
        private GameObject ingredientTraitBackdrop;
        private ScrollRect ingredientTraitScrollRect;
        private RectTransform ingredientTraitViewport;
        private GameObject ingredientTooltipPanel;
        private RectTransform ingredientTooltipRect;
        private TMP_Text ingredientTooltipLabel;
        private UnityEngine.Object ingredientTooltipOwner;
        private const string ChallengeStartTitleLine = "Tutorial complete. Challenge levels begin now.";
        private const string TutorialCompleteTitle = "Tutorial Complete";
        private const string ChallengeStartSubtitle = "Challenge levels begin now.";
#if UNITY_EDITOR
        private bool layoutUpdateQueued;
#endif
        private static readonly HashSet<string> autoShownIngredientTraitLevels = new HashSet<string>();

        public bool IsRatioSelectionOpen => ratioSelectionPanel != null && ratioSelectionPanel.activeSelf;

        private void Awake()
        {
            EnsureRatioSelectionPanel();
            EnsureExperimentLogButton();
            EnsureActionButtons();
            EnsureIngredientTraitPanel();
            EnsureOpeningLevelTitlePanel();
            EnsureIngredientTooltip();
            NormalizeHudLayout();
            PanelBackgroundStyle.ApplyToNamedPanels(transform);
        }

        private void LateUpdate()
        {
            if (IsAlive(ingredientTooltipPanel) && ingredientTooltipPanel.activeSelf)
            {
                PositionIngredientTooltip();
            }
        }

        public void ShowIngredientTooltip(IReadOnlyList<IngredientData> ingredients, UnityEngine.Object owner)
        {
            List<IngredientData> validIngredients = ingredients != null
                ? ingredients.Where(ingredient => ingredient != null).Distinct().ToList()
                : new List<IngredientData>();
            if (validIngredients.Count == 0)
            {
                return;
            }

            EnsureIngredientTooltip();
            if (!IsAlive(ingredientTooltipPanel) || !IsAlive(ingredientTooltipLabel))
            {
                return;
            }

            ingredientTooltipOwner = owner;
            ingredientTooltipLabel.richText = true;
            ingredientTooltipLabel.text = BuildIngredientTooltipText(validIngredients);
            ingredientTooltipPanel.SetActive(true);
            ingredientTooltipPanel.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();

            float preferredHeight = ingredientTooltipLabel.GetPreferredValues(
                ingredientTooltipLabel.text,
                Mathf.Max(120f, ingredientTooltipSize.x - 112f),
                0f).y;
            ingredientTooltipRect.sizeDelta = new Vector2(
                ingredientTooltipSize.x,
                Mathf.Clamp(preferredHeight + 76f, ingredientTooltipSize.y, 260f));
            PositionIngredientTooltip();
        }

        public void HideIngredientTooltip(UnityEngine.Object owner)
        {
            if (owner != null && ingredientTooltipOwner != null && owner != ingredientTooltipOwner)
            {
                return;
            }

            ingredientTooltipOwner = null;
            if (IsAlive(ingredientTooltipPanel))
            {
                ingredientTooltipPanel.SetActive(false);
            }
        }

        private void EnsureIngredientTooltip()
        {
            if (IsAlive(ingredientTooltipPanel) && IsAlive(ingredientTooltipLabel))
            {
                return;
            }

            Transform existing = transform.Find("Ingredient Hover Tooltip");
            if (IsAlive(existing))
            {
                ingredientTooltipPanel = existing.gameObject;
                ingredientTooltipRect = existing as RectTransform;
                ingredientTooltipLabel = existing.GetComponentInChildren<TMP_Text>(true);
                ingredientTooltipPanel.SetActive(false);
                return;
            }

            ingredientTooltipPanel = new GameObject("Ingredient Hover Tooltip");
            ingredientTooltipPanel.transform.SetParent(transform, false);
            ingredientTooltipRect = ingredientTooltipPanel.AddComponent<RectTransform>();
            ingredientTooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            ingredientTooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            ingredientTooltipRect.pivot = new Vector2(0f, 1f);
            ingredientTooltipRect.sizeDelta = ingredientTooltipSize;

            Image image = ingredientTooltipPanel.AddComponent<Image>();
            PanelBackgroundStyle.Apply(image, 0.97f);
            image.raycastTarget = false;

            ingredientTooltipLabel = CreateRuntimeText(
                ingredientTooltipPanel.transform,
                "Ingredient Hover Tooltip Text",
                Vector2.zero,
                Vector2.zero,
                TextAnchor.UpperLeft,
                ingredientTooltipFontSize);
            RectTransform labelRect = ingredientTooltipLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(56f, 34f);
            labelRect.offsetMax = new Vector2(-56f, -40f);
            ingredientTooltipLabel.fontStyle = FontStyles.Normal;
            ingredientTooltipLabel.enableAutoSizing = true;
            ingredientTooltipLabel.fontSizeMin = 14f;
            ingredientTooltipLabel.fontSizeMax = ingredientTooltipFontSize;
            ingredientTooltipLabel.richText = true;
            ingredientTooltipLabel.overflowMode = TextOverflowModes.Truncate;
            ingredientTooltipLabel.raycastTarget = false;
            ingredientTooltipPanel.SetActive(false);
        }

        private void PositionIngredientTooltip()
        {
            Canvas canvas = GetComponent<Canvas>();
            RectTransform canvasRect = transform as RectTransform;
            if (!IsAlive(canvas) || !IsAlive(canvasRect) || !IsAlive(ingredientTooltipRect))
            {
                return;
            }

            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, eventCamera, out Vector2 localPoint))
            {
                return;
            }

            Vector2 position = localPoint + ingredientTooltipCursorOffset;
            Rect bounds = canvasRect.rect;
            Vector2 size = ingredientTooltipRect.sizeDelta;
            position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax - size.x);
            position.y = Mathf.Clamp(position.y, bounds.yMin + size.y, bounds.yMax);
            ingredientTooltipRect.anchoredPosition = position;
        }

        private string BuildIngredientTooltipText(IReadOnlyList<IngredientData> ingredients)
        {
            if (ingredients.Count > 1)
            {
                return "Prepared mixture\n" + string.Join(" + ", ingredients.Select(BuildIngredientTooltipName));
            }

            IngredientData ingredient = ingredients[0];
            StringBuilder builder = new StringBuilder(BuildIngredientTooltipName(ingredient));

            if (!string.IsNullOrWhiteSpace(ingredient.aromaType))
            {
                builder.AppendLine().Append("Aroma: ").Append(ingredient.aromaType);
            }

            if (!string.IsNullOrWhiteSpace(ingredient.initialDescription))
            {
                builder.AppendLine().Append(ingredient.initialDescription);
            }

            return builder.ToString();
        }

        private string BuildIngredientTooltipName(IngredientData ingredient)
        {
            string displayName = ingredient != null ? ingredient.DisplayName : string.Empty;
            int nameFontSize = Mathf.Max(ingredientTooltipFontSize, ingredientTooltipNameFontSize);
            return "<size=" + nameFontSize + "><b>" + displayName + "</b></size>";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (layoutUpdateQueued)
            {
                return;
            }

            layoutUpdateQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                layoutUpdateQueued = false;
                if (this == null || !driveHudLayoutFromInspector)
                {
                    return;
                }

                NormalizeHudLayout();
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
            };
        }
#endif

        public void EvaluateCurrentAttempt()
        {
            EvaluateCurrentAttempt(false);
        }

        private void EvaluateCurrentAttempt(bool isAutoEvaluation)
        {
            if (IsRatioSelectionOpen)
            {
                ShowHint("Choose the ingredient ratio before evaluating.");
                return;
            }

            if (attemptManager == null || evaluator == null)
            {
                return;
            }

            if (attemptManager.HasEvaluated)
            {
                ShowHint(resetRequiredHint);
                return;
            }

            if (!HasReachedEvaluationGate(attemptManager.currentLevel)
                && (isAutoEvaluation || !CanEvaluateManualSpeedAttemptBeforeGrindGate(attemptManager.currentLevel)))
            {
                ShowHint(BuildEvaluationGateHint(attemptManager.currentLevel));
                return;
            }

            EvaluationResult result = evaluator.Evaluate(attemptManager.currentLevel, attemptManager);
            HintResult hint = result.judgement == JudgementResult.Correct ? null : hintManager != null ? hintManager.GetNextHint(attemptManager.currentLevel, attemptManager, result) : null;
            string permanentHint = string.Empty;
            if (result.judgement == JudgementResult.Correct)
            {
                List<UnlockedClueRecord> unlockedClues = logManager != null ? logManager.UnlockClues(attemptManager.currentLevel) : null;
                permanentHint = BuildPermanentHintText(unlockedClues);
            }

            ShowEvaluationResult(result, hint, permanentHint);
            logManager?.AddRecord(attemptManager, result, hint, permanentHint);
            attemptManager.MarkEvaluated();
            lastEvaluationTime = Time.time;
            if (isAutoEvaluation)
            {
                attemptManager.MarkAutoEvaluated();
            }
        }

        public void ResetAttempt()
        {
            StopResetPrompt();
            CloseRatioSelection();
            attemptManager?.ResetAttempt();
            ShowFeedback(string.Empty);
            ShowHint(BuildLevelStartHint(attemptManager != null ? attemptManager.currentLevel : null));
        }

        public void StartNewBatch()
        {
            if (IsRatioSelectionOpen)
            {
                ShowHint("Choose the ingredient amount before starting another batch.");
                return;
            }

            attemptManager?.StartNewBatch();
        }

        public void NextLevel()
        {
            StopResetPrompt();
            levelManager?.NextLevel();
        }

        public void OpenExperimentLog()
        {
            if (levelManager != null)
            {
                GameSceneReturnState.SetPendingLevelIndex(levelManager.CurrentLevelIndex);
            }

            SceneManager.LoadScene(experimentLogSceneName);
        }

        public void ShowLevel(RecipeLevelData level)
        {
            ApplyLevelPanelVisibility(level);

            if (levelLabel != null)
            {
                levelLabel.text = BuildCollapsedLevelTitle(level);
            }
            if (GameSceneReturnState.ConsumeSkipOpeningLevelTitle())
            {
                HideOpeningLevelTitle();
            }
            else
            {
                ShowOpeningLevelTitle(level);
            }

            RefreshAttemptPanels(attemptManager);
            ShowHint(BuildLevelStartHint(level));

            ShowIngredientTraits(level);
        }

        private void ApplyLevelPanelVisibility(RecipeLevelData level)
        {
            EnabledMechanics mechanics = level != null ? level.enabledMechanics : null;
            bool showSpeedPanel = mechanics == null || mechanics.enableSpeed;
            bool showForceControls = mechanics == null || mechanics.enableForce;

            SetTextPanelVisible(currentOrderLabel, false);
            SetTextPanelVisible(currentRatioLabel, false);
            SetTextPanelVisible(currentSpeedLabel, showSpeedPanel);
            SetNamedObjectVisible("Force Slider", showForceControls);
            SetNamedObjectVisible("Force Label Panel", showForceControls);
        }

        private static void SetTextPanelVisible(TMP_Text label, bool visible)
        {
            if (!IsAlive(label))
            {
                return;
            }

            Transform panel = label.transform.parent;
            GameObject target = IsAlive(panel) ? panel.gameObject : label.gameObject;
            if (IsAlive(target))
            {
                target.SetActive(visible);
            }
        }

        private void SetNamedObjectVisible(string objectName, bool visible)
        {
            Transform target = transform.Find(objectName);
            if (IsAlive(target))
            {
                target.gameObject.SetActive(visible);
            }
        }

        public void RefreshAttemptPanels(RecipeAttemptManager attempt)
        {
            if (attempt == null)
            {
                return;
            }

            EnabledMechanics mechanics = attempt.currentLevel != null ? attempt.currentLevel.enabledMechanics : null;
            bool hasIngredients = attempt.HasIngredientsInBowl;

            if (currentOrderLabel != null)
            {
                string order = string.Join(" \u2192 ", attempt.IngredientOrder.Where(x => x != null).Select(x => x.DisplayName));
                currentOrderLabel.text = "Ingredient Order:\n" + (string.IsNullOrWhiteSpace(order) ? "None" : order);
                SetTextPanelVisible(currentOrderLabel, hasIngredients && (mechanics == null || mechanics.enableIngredientOrder));
            }

            if (currentRatioLabel != null)
            {
                Dictionary<IngredientData, RatioLevel> ratio = attempt.CalculateRatioPattern(out bool ambiguous);
                string prefix = ambiguous ? "Ratio:\nRatio values are still close\n" : "Ratio:\n";
                string ratioText = string.Join("\n", ratio.Select(x => x.Key.DisplayName + ": " + GetRatioDisplayName(x.Value)));
                currentRatioLabel.text = prefix + (string.IsNullOrWhiteSpace(ratioText) ? "None" : ratioText);
                SetTextPanelVisible(currentRatioLabel, hasIngredients && (mechanics == null || mechanics.enableRatio));
            }

            if (currentSpeedLabel != null && attempt.pestleController != null)
            {
                if (attempt.pestleController.speedLabel == null)
                {
                    attempt.pestleController.speedLabel = currentSpeedLabel;
                }

                currentSpeedLabel.text = attempt.pestleController.HasSpeedReading
                    ? "Current Grind Speed: " + attempt.pestleController.CurrentSpeedLevel
                    : string.Empty;
            }

            if (IsAlive(newBatchButton))
            {
                bool showNewBatch = mechanics != null && mechanics.enableCombination && hasIngredients;
                newBatchButton.gameObject.SetActive(showNewBatch);
            }
        }

        public void ShowFeedback(string text)
        {
        }

        public void ShowHint(string text)
        {
            if (hintLabel != null)
            {
                hintLabel.text = text;
            }
        }

        private void ShowEvaluationResult(EvaluationResult result, HintResult hint, string permanentHint)
        {
            string feedback = BuildPanelEvaluationFeedback(result, permanentHint);
            string hintText = result != null && result.judgement == JudgementResult.Correct
                ? string.Empty
                : BuildEvaluationHint(result, hint, permanentHint);
            string displayedText;
            if (string.IsNullOrWhiteSpace(hintText))
            {
                displayedText = feedback;
            }
            else if (string.IsNullOrWhiteSpace(feedback))
            {
                displayedText = hintText;
            }
            else
            {
                displayedText = feedback + "\n\n" + hintText;
            }

            ShowHint(displayedText);
            ScheduleResetPrompt(displayedText);
        }

        public bool ShowStepFeedback(MechanicType mechanic, IngredientData targetIngredient = null)
        {
            if (attemptManager == null || hintManager == null)
            {
                return false;
            }

            if (!attemptManager.HasIngredientsInBowl)
            {
                return false;
            }

            if (attemptManager.HasEvaluated)
            {
                if (Time.time - lastEvaluationTime >= resetPromptDelaySeconds)
                {
                    ShowHint(resetRequiredHint);
                }

                return false;
            }

            RecipeLevelData level = attemptManager.currentLevel;
            if (level == null || level.enabledMechanics == null || !level.enabledMechanics.IsEnabled(mechanic))
            {
                return false;
            }

            if ((mechanic == MechanicType.Force || mechanic == MechanicType.Speed) && targetIngredient == null)
            {
                return false;
            }

            HintResult hint = hintManager.GetStepHint(level, attemptManager, mechanic, targetIngredient);
            if (hint == null || string.IsNullOrWhiteSpace(hint.text))
            {
                return false;
            }

            ShowHint(GetMechanicDisplayName(mechanic) + " Hint:\n" + hint.text);
            return true;
        }

        public void TryAutoEvaluateAfterGrinding()
        {
            if (!enableAutoEvaluation || attemptManager == null || evaluator == null || IsRatioSelectionOpen)
            {
                return;
            }

            RecipeLevelData level = attemptManager.currentLevel;
            if (level == null || attemptManager.HasEvaluated || attemptManager.HasAutoEvaluated || !attemptManager.HasIngredientsInBowl)
            {
                return;
            }

            if (level.enabledMechanics != null && level.enabledMechanics.enableCombination)
            {
                return;
            }

            if (!HasReachedEvaluationGate(level))
            {
                return;
            }

            EvaluateCurrentAttempt(true);
        }

        private bool HasReachedEvaluationGate(RecipeLevelData level)
        {
            if (level == null || attemptManager == null || attemptManager.pestleController == null)
            {
                return false;
            }

            bool reachedMinimumGrindTime = attemptManager.GetCurrentBatchGrindDuration() >= GetEvaluationGateSeconds(level);
            if (!reachedMinimumGrindTime)
            {
                return false;
            }

            bool checksSpeed = level.enabledMechanics != null && level.enabledMechanics.IsEnabled(MechanicType.Speed);
            return !checksSpeed || attemptManager.pestleController.HasEvaluatedSpeedLevel;
        }

        private bool CanEvaluateManualSpeedAttemptBeforeGrindGate(RecipeLevelData level)
        {
            if (level == null
                || level.enabledMechanics == null
                || !level.enabledMechanics.IsEnabled(MechanicType.Speed)
                || attemptManager == null
                || attemptManager.pestleController == null)
            {
                return false;
            }

            return attemptManager.pestleController.HasEvaluatedSpeedLevel;
        }

        private float GetEvaluationGateSeconds(RecipeLevelData level)
        {
            if (attemptManager != null)
            {
                return attemptManager.GetCurrentBatchRequiredCompletionSeconds();
            }

            return 0f;
        }

        private string BuildEvaluationGateHint(RecipeLevelData level)
        {
            float requiredSeconds = GetEvaluationGateSeconds(level);
            float currentSeconds = attemptManager != null
                ? attemptManager.GetCurrentBatchGrindDuration()
                : 0f;
            if (currentSeconds < requiredSeconds)
            {
                return "Keep grinding before checking. Time: "
                    + currentSeconds.ToString("0.0") + "s / " + requiredSeconds.ToString("0.0") + "s.";
            }

            if (level != null
                && level.enabledMechanics != null
                && level.enabledMechanics.IsEnabled(MechanicType.Speed)
                && attemptManager != null
                && attemptManager.pestleController != null
                && !attemptManager.pestleController.HasEvaluatedSpeedLevel)
            {
                float heldSeconds = attemptManager.pestleController.CurrentSpeedHoldSeconds;
                float requiredHoldSeconds = attemptManager.pestleController.RequiredSpeedHoldSeconds;
                return "Hold one grinding speed steadily before checking. Speed held: "
                    + heldSeconds.ToString("0.0") + "s / " + requiredHoldSeconds.ToString("0.0") + "s.";
            }

            return "Keep grinding before checking. Time: " + currentSeconds.ToString("0.0") + "s / " + requiredSeconds.ToString("0.0") + "s.";
        }

        private static string BuildLevelStartHint(RecipeLevelData level)
        {
            if (level == null || level.enabledMechanics == null)
            {
                return string.Empty;
            }

            List<string> checkedMechanics = new List<string>();
            List<string> notCheckedMechanics = new List<string>();
            AddMechanicState(level, MechanicType.Combination, "batch grouping", checkedMechanics, notCheckedMechanics);
            AddMechanicState(level, MechanicType.Force, "force", checkedMechanics, notCheckedMechanics);
            AddMechanicState(level, MechanicType.Speed, "speed", checkedMechanics, notCheckedMechanics);
            AddMechanicState(level, MechanicType.IngredientOrder, "ingredient order", checkedMechanics, notCheckedMechanics);
            AddMechanicState(level, MechanicType.Ratio, "amounts", checkedMechanics, notCheckedMechanics);

            string text = "This level checks: " + FormatList(checkedMechanics) + ".\n"
                + "Not checked yet: " + FormatList(notCheckedMechanics) + ".";
            if (level.enabledMechanics.enableCombination)
            {
                text += "\nUse New Batch when ingredients should be ground separately.";
            }

            return text;
        }

        private static void AddMechanicState(RecipeLevelData level, MechanicType mechanic, string label, List<string> checkedMechanics, List<string> notCheckedMechanics)
        {
            if (level.enabledMechanics.IsEnabled(mechanic))
            {
                checkedMechanics.Add(label);
            }
            else
            {
                notCheckedMechanics.Add(label);
            }
        }

        private static string FormatList(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "none";
            }

            if (values.Count == 1)
            {
                return values[0];
            }

            return string.Join(", ", values.Take(values.Count - 1)) + ", and " + values[values.Count - 1];
        }

        public void ShowIngredientTraits(RecipeLevelData level)
        {
            EnsureIngredientTraitPanel();
            if (!IsAlive(ingredientTraitLabel))
            {
                return;
            }

            string text = BuildIngredientTraitText(level);
            ingredientTraitLabel.text = text;
            ResetIngredientTraitScroll();
            if (string.IsNullOrWhiteSpace(text))
            {
                SetIngredientTraitsExpanded(false);
                if (IsAlive(ingredientTraitToggleButton))
                {
                    ingredientTraitToggleButton.gameObject.SetActive(false);
                }
                return;
            }

            SetIngredientTraitsExpanded(ShouldAutoExpandIngredientTraits(level));
        }

        public void ExpandIngredientTraits()
        {
            SetIngredientTraitsExpanded(true);
        }

        public void CollapseIngredientTraits()
        {
            SetIngredientTraitsExpanded(false);
        }

        private void SetIngredientTraitsExpanded(bool expanded)
        {
            EnsureIngredientTraitPanel();
            bool hasText = IsAlive(ingredientTraitLabel) && !string.IsNullOrWhiteSpace(ingredientTraitLabel.text);

            if (IsAlive(ingredientTraitBackdrop))
            {
                ingredientTraitBackdrop.SetActive(expanded && hasText);
                if (expanded && hasText)
                {
                    Image backdropImage = ingredientTraitBackdrop.GetComponent<Image>();
                    if (IsAlive(backdropImage))
                    {
                        backdropImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(ingredientTraitBackdropAlpha));
                    }

                    ingredientTraitBackdrop.transform.SetAsLastSibling();
                }
            }

            if (IsAlive(ingredientTraitPanel))
            {
                ingredientTraitPanel.SetActive(expanded && hasText);
                if (expanded && hasText)
                {
                    ConfigureExpandedIngredientTraitsPanel();
                    ingredientTraitPanel.transform.SetAsLastSibling();
                    ResetIngredientTraitScroll();
                }
            }

            if (IsAlive(ingredientTraitToggleButton))
            {
                ingredientTraitToggleButton.gameObject.SetActive(!expanded && hasText);
                if (!expanded && hasText)
                {
                    ingredientTraitToggleButton.transform.SetAsLastSibling();
                }
            }
        }

        private void ConfigureExpandedIngredientTraitsPanel()
        {
            if (!IsAlive(ingredientTraitPanel))
            {
                return;
            }

            ConfigureRect(
                ingredientTraitPanel.transform as RectTransform,
                GetLayoutSize(ingredientTraitsExpandedPanelLayout),
                GetLayoutPosition(ingredientTraitsExpandedPanelLayout),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f));
            PanelBackgroundStyle.Apply(ingredientTraitPanel.GetComponent<Image>(), 0.96f);

            if (!IsAlive(ingredientTraitLabel))
            {
                return;
            }

            ingredientTraitLabel.font = ThemeFontProvider.GetTmpFont();
            ingredientTraitLabel.fontStyle = FontStyles.Normal;
            ingredientTraitLabel.fontSize = GetLayoutFontSize(ingredientTraitsExpandedPanelLayout, 32);
            ingredientTraitLabel.enableAutoSizing = true;
            ingredientTraitLabel.fontSizeMin = 22;
            ingredientTraitLabel.fontSizeMax = ingredientTraitLabel.fontSize;
            ingredientTraitLabel.alignment = TextAlignmentOptions.TopLeft;
            ingredientTraitLabel.textWrappingMode = TextWrappingModes.Normal;
            ingredientTraitLabel.overflowMode = TextOverflowModes.Overflow;

            EnsureIngredientTraitScrollView();
            if (IsAlive(ingredientTraitViewport))
            {
                ingredientTraitViewport.anchorMin = Vector2.zero;
                ingredientTraitViewport.anchorMax = Vector2.one;
                ingredientTraitViewport.pivot = new Vector2(0.5f, 0.5f);
                ingredientTraitViewport.anchoredPosition = Vector2.zero;
                ingredientTraitViewport.offsetMin = ingredientTraitExpandedTextInsetMin;
                ingredientTraitViewport.offsetMax = ingredientTraitExpandedTextInsetMax;
                ingredientTraitViewport.localRotation = Quaternion.identity;
                ingredientTraitViewport.localScale = Vector3.one;
            }
        }

        private static bool ShouldAutoExpandIngredientTraits(RecipeLevelData level)
        {
            return false;
        }

        private static string GetIngredientTraitLevelKey(RecipeLevelData level)
        {
            if (level == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrWhiteSpace(level.levelID) ? level.levelID : level.name;
        }

        public void ShowRatioSelection(IngredientData ingredient, IReadOnlyList<RatioLevel> options, Action<RatioLevel> onSelected)
        {
            EnsureRatioSelectionPanel();
            if (options == null || options.Count == 0)
            {
                return;
            }

            pendingRatioSelection = onSelected;
            if (ratioSelectionTitle != null)
            {
                string ingredientName = ingredient != null ? ingredient.DisplayName : "Ingredient";
                ratioSelectionTitle.text = "Choose ratio for " + ingredientName;
            }

            for (int i = 0; i < ratioSelectionButtons.Length; i++)
            {
                Button button = ratioSelectionButtons[i];
            if (!IsAlive(button))
            {
                continue;
            }

                bool hasOption = i < options.Count;
                button.gameObject.SetActive(hasOption);
                button.onClick.RemoveAllListeners();
                if (!hasOption)
                {
                    continue;
                }

                RatioLevel selected = options[i];
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (IsAlive(label))
                {
                    label.text = GetRatioDisplayName(selected);
                }

                button.onClick.AddListener(() => SelectRatio(selected));
            }

            ratioSelectionPanel.transform.SetAsLastSibling();
            ratioSelectionPanel.SetActive(true);
        }

        public static string GetRatioDisplayName(RatioLevel ratio)
        {
            switch (ratio)
            {
                case RatioLevel.VeryLess: return "Very Small";
                case RatioLevel.Less: return "Small";
                case RatioLevel.Medium: return "Medium";
                case RatioLevel.SlightlyMore: return "Medium";
                case RatioLevel.More: return "Large";
                default: return "None";
            }
        }

        private static string GetMechanicDisplayName(MechanicType mechanic)
        {
            switch (mechanic)
            {
                case MechanicType.IngredientOrder: return "Order";
                case MechanicType.Ratio: return "Ratio";
                case MechanicType.Combination: return "Combination";
                case MechanicType.Force: return "Force";
                case MechanicType.Speed: return "Speed";
                default: return "Experiment";
            }
        }

        public void EnsureRatioSelectionPanel()
        {
            if (ratioSelectionPanel != null)
            {
                PanelBackgroundStyle.Apply(ratioSelectionPanel.GetComponent<Image>(), 0.94f);
                ratioSelectionPanel.SetActive(false);
                return;
            }

            Transform parent = transform;
            ratioSelectionPanel = new GameObject("Ratio Selection Panel");
            ratioSelectionPanel.transform.SetParent(parent, false);

            RectTransform panelRect = ratioSelectionPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(360f, 220f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = ratioSelectionPanel.AddComponent<Image>();
            PanelBackgroundStyle.Apply(panelImage, 0.94f);

            ratioSelectionTitle = CreateRuntimeText(ratioSelectionPanel.transform, "Ratio Selection Title", new Vector2(320f, 44f), new Vector2(0f, 72f), TextAnchor.MiddleCenter, 22);
            ratioSelectionButtons = new Button[4];

            for (int i = 0; i < ratioSelectionButtons.Length; i++)
            {
                float x = i % 2 == 0 ? -88f : 88f;
                float y = i < 2 ? 12f : -52f;
                ratioSelectionButtons[i] = CreateRuntimeButton(ratioSelectionPanel.transform, "Ratio Option " + (i + 1), new Vector2(150f, 46f), new Vector2(x, y));
            }

            ratioSelectionPanel.SetActive(false);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        public void EnsureExperimentLogButton()
        {
            if (IsAlive(experimentLogButton))
            {
                experimentLogButton.onClick.RemoveAllListeners();
                experimentLogButton.onClick.AddListener(OpenExperimentLog);
                EnsureButtonLabel(experimentLogButton, "Experiment Log");
                return;
            }

            Transform existing = transform.Find("Experiment Log");
            if (IsAlive(existing))
            {
                experimentLogButton = existing.GetComponent<Button>();
                if (IsAlive(experimentLogButton))
                {
                    experimentLogButton.onClick.RemoveAllListeners();
                    experimentLogButton.onClick.AddListener(OpenExperimentLog);
                    EnsureButtonLabel(experimentLogButton, "Experiment Log");
                    return;
                }
            }

            experimentLogButton = CreateAnchoredRuntimeButton(transform, "Experiment Log", new Vector2(132f, 36f), new Vector2(-24f, 184f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            experimentLogButton.onClick.AddListener(OpenExperimentLog);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        public void EnsureActionButtons()
        {
            evaluateButton = BindButton(evaluateButton, "Evaluate", EvaluateCurrentAttempt);
            resetAttemptButton = BindButton(resetAttemptButton, "Reset Attempt", ResetAttempt);
            if (!IsAlive(newBatchButton))
            {
                Transform existing = transform.Find("New Batch");
                newBatchButton = IsAlive(existing)
                    ? existing.GetComponent<Button>()
                    : CreateAnchoredRuntimeButton(transform, "New Batch", new Vector2(132f, 36f), new Vector2(-24f, 64f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            }
            newBatchButton = BindButton(newBatchButton, "New Batch", StartNewBatch);
            if (IsAlive(newBatchButton))
            {
                newBatchButton.gameObject.SetActive(false);
            }
            nextLevelButton = BindButton(nextLevelButton, "Next Level", NextLevel);
        }

        public void NormalizeHudLayout()
        {
            if (driveHudLayoutFromInspector)
            {
                ConfigureTextPanel(levelLabel, levelPanelLayout, new Vector2(0f, 1f), new Vector2(0f, 1f), TextAnchor.MiddleCenter);
                ConfigureTextPanel(currentOrderLabel, currentOrderPanelLayout, new Vector2(0f, 1f), new Vector2(0f, 1f), TextAnchor.UpperLeft);
                ConfigureTextPanel(currentRatioLabel, currentRatioPanelLayout, new Vector2(0f, 1f), new Vector2(0f, 1f), TextAnchor.UpperLeft);
                ConfigureTextPanel(currentSpeedLabel, currentSpeedPanelLayout, new Vector2(0f, 1f), new Vector2(0f, 1f), TextAnchor.MiddleLeft);
                ConfigureTextPanel(hintLabel, hintPanelLayout, new Vector2(1f, 1f), new Vector2(1f, 1f), TextAnchor.UpperLeft);

                ConfigureButton(experimentLogButton, experimentLogButtonLayout, new Vector2(0f, 1f), new Vector2(0f, 1f));
                ConfigureButton(evaluateButton, evaluateButtonLayout, new Vector2(1f, 0f), new Vector2(1f, 0f));
                ConfigureButton(resetAttemptButton, resetAttemptButtonLayout, new Vector2(1f, 0f), new Vector2(1f, 0f));
                ConfigureButton(newBatchButton, newBatchButtonLayout, new Vector2(1f, 0f), new Vector2(1f, 0f));
                ConfigureButton(nextLevelButton, nextLevelButtonLayout, new Vector2(0f, 0f), new Vector2(0f, 0f));
                ConfigureButton(ingredientTraitToggleButton, ingredientTraitToggleButtonLayout, new Vector2(1f, 1f), new Vector2(1f, 1f));

                ConfigureNamedRect("Force Slider", forceSliderLayout, new Vector2(0f, 0f), new Vector2(0f, 0f));
                ConfigureNamedRect("Force Label Panel", forceLabelPanelLayout, new Vector2(0f, 0f), new Vector2(0f, 0f));
                ConfigureNamedRect("Ingredient Traits Panel", ingredientTraitsPanelLayout, new Vector2(1f, 1f), new Vector2(1f, 1f));
                ConfigureNamedRect("Ratio Selection Panel", ratioSelectionPanelLayout, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                ConfigureManagedHudTextFrames();
                ConfigureForceLabelPanel(true);
            }
            else
            {
                StyleExistingHud();
                ConfigureForceLabelPanel(false);
            }

            ConfigureLevelLabelText();
            ConfigureHintText();
            ConfigureTraitText(driveHudLayoutFromInspector);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        private void ConfigureTraitText(bool updateRectTransform)
        {
            if (!IsAlive(ingredientTraitLabel))
            {
                return;
            }

            ingredientTraitLabel.fontSize = 24;
            ingredientTraitLabel.font = ThemeFontProvider.GetTmpFont();
            ingredientTraitLabel.fontStyle = FontStyles.Normal;
            ingredientTraitLabel.enableAutoSizing = true;
            ingredientTraitLabel.fontSizeMin = 17;
            ingredientTraitLabel.fontSizeMax = 24;
            ingredientTraitLabel.alignment = TextAlignmentOptions.TopLeft;
            ingredientTraitLabel.textWrappingMode = TextWrappingModes.Normal;
            ingredientTraitLabel.overflowMode = Application.isPlaying
                ? TextOverflowModes.Overflow
                : TextOverflowModes.Truncate;

            if (Application.isPlaying)
            {
                EnsureIngredientTraitScrollView();
                return;
            }

            if (!updateRectTransform)
            {
                return;
            }

            RectTransform rect = ingredientTraitLabel.GetComponent<RectTransform>();
            if (IsAlive(rect))
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(52f, 30f);
                rect.offsetMax = new Vector2(-44f, -40f);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }
        }

        private void ConfigureHintText()
        {
            if (!IsAlive(hintLabel))
            {
                return;
            }

            hintLabel.alignment = TextAlignmentOptions.TopLeft;
            hintLabel.fontStyle = FontStyles.Normal;
            hintLabel.textWrappingMode = TextWrappingModes.Normal;
            hintLabel.overflowMode = TextOverflowModes.Truncate;

            RectTransform rect = hintLabel.GetComponent<RectTransform>();
            if (IsAlive(rect))
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMin = new Vector2(52f, 30f);
                rect.offsetMax = new Vector2(-44f, -40f);
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }
        }

        private static void ConfigureTextPanel(TMP_Text label, HudPanelLayout layout, Vector2 anchor, Vector2 pivot, TextAnchor alignment)
        {
            ConfigureTextPanel(label, GetLayoutSize(layout), GetLayoutPosition(layout), anchor, pivot, alignment, GetLayoutFontSize(layout, 22));
        }

        private static void ConfigureTextPanel(TMP_Text label, Vector2 size, Vector2 position, Vector2 anchor, Vector2 pivot, TextAnchor alignment, int fontSize)
        {
            if (!IsAlive(label))
            {
                return;
            }

            Transform panel = label.transform.parent;
            if (IsAlive(panel))
            {
                ConfigureRect(panel as RectTransform, size, position, anchor, pivot);
                PanelBackgroundStyle.Apply(panel.GetComponent<Image>());
            }

            label.alignment = TmpTextUtility.ToAlignment(alignment);
            label.fontSize = fontSize;
            label.font = ThemeFontProvider.GetTmpFont(fontSize);
            label.fontStyle = alignment == TextAnchor.MiddleCenter ? FontStyles.Bold : FontStyles.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(14, fontSize - 8);
            label.fontSizeMax = fontSize;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;
            ConfigureTextInset(label, alignment);
        }

        private static void ConfigureTextInset(TMP_Text label, TextAnchor alignment)
        {
            RectTransform rect = IsAlive(label) ? label.GetComponent<RectTransform>() : null;
            if (!IsAlive(rect))
            {
                return;
            }

            bool leftAligned = alignment == TextAnchor.UpperLeft
                || alignment == TextAnchor.MiddleLeft
                || alignment == TextAnchor.LowerLeft;
            bool upperAligned = alignment == TextAnchor.UpperLeft
                || alignment == TextAnchor.UpperCenter
                || alignment == TextAnchor.UpperRight;
            float horizontalInset = leftAligned ? 52f : 60f;
            float bottomInset = alignment == TextAnchor.MiddleLeft || alignment == TextAnchor.MiddleCenter ? 18f : 28f;
            float topInset = upperAligned ? 42f : 24f;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(horizontalInset, bottomInset);
            rect.offsetMax = new Vector2(-horizontalInset, -topInset);
            rect.sizeDelta = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private void ConfigureManagedHudTextFrames()
        {
            ConfigureHudTextInset(levelLabel, TextAnchor.MiddleCenter);
            ConfigureHudTextInset(currentOrderLabel, TextAnchor.UpperLeft);
            ConfigureHudTextInset(currentRatioLabel, TextAnchor.UpperLeft);
            ConfigureHudTextInset(currentSpeedLabel, TextAnchor.MiddleLeft);
            ConfigureHudTextInset(hintLabel, TextAnchor.UpperLeft);
        }

        private void ConfigureHudTextInset(TMP_Text label, TextAnchor alignment)
        {
            RectTransform rect = IsAlive(label) ? label.GetComponent<RectTransform>() : null;
            if (!IsAlive(rect))
            {
                return;
            }

            Vector2 min = hudTextInsetMin;
            Vector2 max = hudTextInsetMax;
            if (alignment == TextAnchor.MiddleLeft || alignment == TextAnchor.MiddleCenter)
            {
                min.y = Mathf.Max(18f, min.y - 8f);
                max.y = Mathf.Min(-18f, max.y + 8f);
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = min;
            rect.offsetMax = max;
            rect.sizeDelta = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private void ConfigureButtonTextInset(TMP_Text label)
        {
            RectTransform rect = IsAlive(label) ? label.GetComponent<RectTransform>() : null;
            if (!IsAlive(rect))
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = buttonTextInsetMin;
            rect.offsetMax = buttonTextInsetMax;
            rect.sizeDelta = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private void ConfigureButton(Button button, HudPanelLayout layout, Vector2 anchor, Vector2 pivot)
        {
            ConfigureButton(button, GetLayoutSize(layout), GetLayoutPosition(layout), anchor, pivot);
        }

        private void ConfigureButton(Button button, Vector2 size, Vector2 position, Vector2 anchor, Vector2 pivot)
        {
            if (!IsAlive(button))
            {
                return;
            }

            ConfigureRect(button.transform as RectTransform, size, position, anchor, pivot);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (IsAlive(label))
            {
                label.fontSize = label.text != null && label.text.Length > 12 ? 22 : 24;
                label.font = ThemeFontProvider.GetTmpFont();
                label.fontStyle = FontStyles.Bold;
                label.enableAutoSizing = true;
                label.fontSizeMin = 16;
                label.fontSizeMax = label.fontSize;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Truncate;
                ConfigureButtonTextInset(label);
            }
        }

        private void ConfigureNamedRect(string objectName, HudPanelLayout layout, Vector2 anchor, Vector2 pivot)
        {
            ConfigureNamedRect(objectName, GetLayoutSize(layout), GetLayoutPosition(layout), anchor, pivot);
        }

        private void ConfigureNamedRect(string objectName, Vector2 size, Vector2 position, Vector2 anchor, Vector2 pivot)
        {
            Transform target = transform.Find(objectName);
            if (IsAlive(target))
            {
                ConfigureRect(target as RectTransform, size, position, anchor, pivot);
                PanelBackgroundStyle.Apply(target.GetComponent<Image>());
            }
        }

        private void StyleExistingHud()
        {
            ApplyPanelStyle(levelLabel);
            ApplyPanelStyle(currentOrderLabel);
            ApplyPanelStyle(currentRatioLabel);
            ApplyPanelStyle(currentSpeedLabel);
            ApplyPanelStyle(hintLabel);
            ApplyButtonStyle(experimentLogButton);
            ApplyButtonStyle(evaluateButton);
            ApplyButtonStyle(resetAttemptButton);
            ApplyButtonStyle(newBatchButton);
            ApplyButtonStyle(nextLevelButton);
            ApplyButtonStyle(ingredientTraitToggleButton);
            ApplyNamedPanelStyle("Force Slider");
            ApplyNamedPanelStyle("Force Label Panel");
            ApplyNamedPanelStyle("Ingredient Traits Panel");
            ApplyNamedPanelStyle("Ratio Selection Panel");
        }

        private void ConfigureLevelLabelText()
        {
            if (!IsAlive(levelLabel))
            {
                return;
            }

            levelLabel.alignment = TextAlignmentOptions.Center;
            levelLabel.fontStyle = FontStyles.Bold;
        }

        private void EnsureOpeningLevelTitlePanel()
        {
            if (IsAlive(openingLevelTitleLabel))
            {
                ConfigureOpeningLevelTitlePanel(openingLevelTitleLabel);
                openingLevelTitleLabel.transform.parent.gameObject.SetActive(false);
                return;
            }

            Transform existing = transform.Find("Opening Level Title Panel/Opening Level Title Text");
            if (IsAlive(existing))
            {
                openingLevelTitleLabel = existing.GetComponent<TMP_Text>();
                if (IsAlive(openingLevelTitleLabel))
                {
                    ConfigureOpeningLevelTitlePanel(openingLevelTitleLabel);
                    openingLevelTitleLabel.transform.parent.gameObject.SetActive(false);
                    return;
                }
            }

            GameObject panel = new GameObject("Opening Level Title Panel");
            panel.transform.SetParent(transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1040f, 230f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panel.AddComponent<Image>();
            PanelBackgroundStyle.Apply(panelImage, 0.96f);

            openingLevelTitleLabel = CreateRuntimeText(panel.transform, "Opening Level Title Text", Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 34);
            ConfigureOpeningLevelTitlePanel(openingLevelTitleLabel);
            panel.SetActive(false);
        }

        private static void ConfigureOpeningLevelTitlePanel(TMP_Text titleLabel)
        {
            if (!IsAlive(titleLabel))
            {
                return;
            }

            RectTransform panelRect = titleLabel.transform.parent != null
                ? titleLabel.transform.parent.GetComponent<RectTransform>()
                : null;
            if (IsAlive(panelRect))
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(1120f, 300f);
                panelRect.anchoredPosition = Vector2.zero;
            }

            Image panelImage = titleLabel.transform.parent != null
                ? titleLabel.transform.parent.GetComponent<Image>()
                : null;
            if (IsAlive(panelImage))
            {
                PanelBackgroundStyle.Apply(panelImage, 0.96f);
            }

            RectTransform textRect = titleLabel.GetComponent<RectTransform>();
            if (IsAlive(textRect))
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(140f, 72f);
                textRect.offsetMax = new Vector2(-140f, -82f);
                textRect.sizeDelta = Vector2.zero;
            }

            titleLabel.font = ThemeFontProvider.GetTmpFont(34);
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMin = 18;
            titleLabel.fontSizeMax = 34;
            titleLabel.textWrappingMode = TextWrappingModes.Normal;
            titleLabel.overflowMode = TextOverflowModes.Overflow;
        }

        private void ShowOpeningLevelTitle(RecipeLevelData level)
        {
            if (!Application.isPlaying || !showOpeningLevelTitle)
            {
                return;
            }

            EnsureOpeningLevelTitlePanel();
            if (!IsAlive(openingLevelTitleLabel) || level == null)
            {
                return;
            }

            if (openingLevelTitleCoroutine != null)
            {
                StopCoroutine(openingLevelTitleCoroutine);
            }

            openingLevelTitleCoroutine = StartCoroutine(ShowOpeningLevelTitleRoutine(level));
        }

        private void HideOpeningLevelTitle()
        {
            if (openingLevelTitleCoroutine != null)
            {
                StopCoroutine(openingLevelTitleCoroutine);
                openingLevelTitleCoroutine = null;
            }

            if (IsAlive(openingLevelTitleLabel) && openingLevelTitleLabel.transform.parent != null)
            {
                openingLevelTitleLabel.transform.parent.gameObject.SetActive(false);
            }
        }

        private static string BuildCollapsedLevelTitle(RecipeLevelData level)
        {
            if (level == null)
            {
                return "No Level";
            }

            return TryGetLevelNumber(level, out int number) ? "Level " + number : level.levelID;
        }

        private static string BuildFullLevelTitle(RecipeLevelData level)
        {
            if (level == null)
            {
                return "No Level";
            }

            string header = level.levelID + " " + level.cityName + " - " + level.levelName;
            string intro = CleanChallengeStartLine(level.levelIntro);

            return string.IsNullOrWhiteSpace(intro) ? header : header + "\n" + intro;
        }

        private static string CleanChallengeStartLine(string intro)
        {
            if (string.IsNullOrWhiteSpace(intro))
            {
                return string.Empty;
            }

            return intro.Replace(ChallengeStartTitleLine, string.Empty).Trim();
        }

        private static bool TryGetLevelNumber(RecipeLevelData level, out int number)
        {
            number = 0;
            if (level == null)
            {
                return false;
            }

            string digits = new string((level.levelID ?? string.Empty).Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out number);
        }

        private IEnumerator ShowOpeningLevelTitleRoutine(RecipeLevelData level)
        {
            GameObject panel = openingLevelTitleLabel != null && openingLevelTitleLabel.transform.parent != null
                ? openingLevelTitleLabel.transform.parent.gameObject
                : null;
            if (!IsAlive(panel))
            {
                yield break;
            }

            panel.transform.SetAsLastSibling();
            if (IsTutorialCompleteTitleLevel(level))
            {
                openingLevelTitleLabel.text = TutorialCompleteTitle + "\n" + ChallengeStartSubtitle;
                openingLevelTitleLabel.fontSizeMax = 42;
                panel.SetActive(true);
                yield return new WaitForSeconds(1.2f);
                panel.SetActive(false);
                yield return new WaitForSeconds(0.18f);
                openingLevelTitleLabel.fontSizeMax = 34;
            }

            openingLevelTitleLabel.text = BuildFullLevelTitle(level);
            panel.SetActive(true);
            yield return new WaitForSeconds(Mathf.Max(0.1f, openingLevelTitleSeconds));
            panel.SetActive(false);
            openingLevelTitleCoroutine = null;
        }

        private static bool IsTutorialCompleteTitleLevel(RecipeLevelData level)
        {
            return TryGetLevelNumber(level, out int number) && number == 7;
        }

        private void ApplyPanelStyle(TMP_Text label)
        {
            if (!IsAlive(label))
            {
                return;
            }

            if (IsAlive(label.transform.parent))
            {
                PanelBackgroundStyle.Apply(label.transform.parent.GetComponent<Image>());
            }

            label.font = ThemeFontProvider.GetTmpFont();
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;
        }

        private void ApplyButtonStyle(Button button)
        {
            if (!IsAlive(button))
            {
                return;
            }

            PanelBackgroundStyle.Apply(button.GetComponent<Image>());
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (IsAlive(label))
            {
                label.font = ThemeFontProvider.GetTmpFont();
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        private void ApplyNamedPanelStyle(string objectName)
        {
            Transform target = transform.Find(objectName);
            if (IsAlive(target))
            {
                PanelBackgroundStyle.Apply(target.GetComponent<Image>());
            }
        }

        private void ConfigureForceLabelPanel(bool updateRectTransforms)
        {
            Transform panel = transform.Find("Force Label Panel");
            if (!IsAlive(panel))
            {
                return;
            }

            PanelBackgroundStyle.Apply(panel.GetComponent<Image>(), 0.9f);
            if (!updateRectTransforms)
            {
                return;
            }

            TMP_Text label = panel.GetComponentInChildren<TMP_Text>(true);
            if (!IsAlive(label))
            {
                return;
            }

            label.font = ThemeFontProvider.GetTmpFont();
            label.fontSize = GetLayoutFontSize(forceLabelPanelLayout, 15);
            label.fontStyle = FontStyles.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10;
            label.fontSizeMax = GetLayoutFontSize(forceLabelPanelLayout, 15);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;

            RectTransform rect = label.GetComponent<RectTransform>();
            if (IsAlive(rect))
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMin = forceLabelTextInsetMin;
                rect.offsetMax = forceLabelTextInsetMax;
                rect.sizeDelta = Vector2.zero;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }
        }

        private static Vector2 GetLayoutSize(HudPanelLayout layout)
        {
            return layout != null ? layout.size : Vector2.zero;
        }

        private static Vector2 GetLayoutPosition(HudPanelLayout layout)
        {
            return layout != null ? layout.position : Vector2.zero;
        }

        private static int GetLayoutFontSize(HudPanelLayout layout, int fallback)
        {
            return layout != null && layout.fontSize > 0 ? layout.fontSize : fallback;
        }

        private static void ConfigureRect(RectTransform rect, Vector2 size, Vector2 position, Vector2 anchor, Vector2 pivot)
        {
            if (!IsAlive(rect))
            {
                return;
            }

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private Button BindButton(Button button, string objectName, UnityEngine.Events.UnityAction action)
        {
            if (!IsAlive(button))
            {
                Transform existing = transform.Find(objectName);
                if (IsAlive(existing))
                {
                    button = existing.GetComponent<Button>();
                }
            }

            if (IsAlive(button))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(action);
                EnsureButtonLabel(button, objectName);
            }

            return button;
        }

        private static void EnsureButtonLabel(Button button, string label)
        {
            if (!IsAlive(button))
            {
                return;
            }

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (!IsAlive(text))
            {
                text = CreateRuntimeText(button.transform, label + " Text", Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 14);
            }

            text.gameObject.SetActive(true);
            text.enabled = true;
            text.text = label;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Center;
            text.font = ThemeFontProvider.GetTmpFont();
            text.fontSize = label.Length > 12 ? 22 : 24;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16;
            text.fontSizeMax = text.fontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            text.transform.SetAsLastSibling();

            CanvasRenderer canvasRenderer = text.GetComponent<CanvasRenderer>();
            if (IsAlive(canvasRenderer))
            {
                canvasRenderer.cullTransparentMesh = false;
            }

            RectTransform rect = text.GetComponent<RectTransform>();
            if (IsAlive(rect))
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMin = new Vector2(6f, 0f);
                rect.offsetMax = new Vector2(-6f, 0f);
                rect.sizeDelta = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            Image image = button.GetComponent<Image>();
            if (IsAlive(image))
            {
                button.targetGraphic = image;
            }
        }

        public void EnsureIngredientTraitPanel()
        {
            if (IsAlive(ingredientTraitLabel))
            {
                if (!IsAlive(ingredientTraitPanel))
                {
                    Transform panelTransform = ingredientTraitLabel.transform;
                    while (panelTransform.parent != null && panelTransform.name != "Ingredient Traits Panel")
                    {
                        panelTransform = panelTransform.parent;
                    }

                    ingredientTraitPanel = panelTransform.gameObject;
                }

                PanelBackgroundStyle.Apply(ingredientTraitPanel.GetComponent<Image>(), 0.82f);
                EnsureIngredientTraitScrollView();
                EnsureIngredientTraitBackdrop();
                EnsureIngredientTraitToggleButton();
                return;
            }

            Transform existing = transform.Find("Ingredient Traits Panel/Ingredient Traits Text");
            if (!IsAlive(existing))
            {
                Transform oldPanel = transform.Find("Permanent Hint Panel");
                if (IsAlive(oldPanel))
                {
                    oldPanel.name = "Ingredient Traits Panel";
                    Transform oldText = oldPanel.Find("Permanent Hint Text");
                    if (IsAlive(oldText))
                    {
                        oldText.name = "Ingredient Traits Text";
                    }

                    existing = oldPanel.Find("Ingredient Traits Text");
                }
            }

            if (IsAlive(existing))
            {
                ingredientTraitLabel = existing.GetComponent<TMP_Text>();
                if (!IsAlive(ingredientTraitLabel))
                {
                    return;
                }

                ingredientTraitPanel = ingredientTraitLabel.transform.parent != null
                    ? ingredientTraitLabel.transform.parent.gameObject
                    : ingredientTraitLabel.gameObject;
                PanelBackgroundStyle.Apply(ingredientTraitPanel.GetComponent<Image>(), 0.82f);
                EnsureIngredientTraitScrollView();
                EnsureIngredientTraitBackdrop();
                EnsureIngredientTraitToggleButton();
                return;
            }

            GameObject panel = new GameObject("Ingredient Traits Panel");
            panel.transform.SetParent(transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.sizeDelta = new Vector2(380f, 400f);
            panelRect.anchoredPosition = new Vector2(-24f, -252f);

            Image image = panel.AddComponent<Image>();
            PanelBackgroundStyle.Apply(image, 0.82f);

            ingredientTraitLabel = CreateRuntimeText(panel.transform, "Ingredient Traits Text", Vector2.zero, Vector2.zero, TextAnchor.UpperLeft, 14);
            ingredientTraitLabel.fontSize = 14;
            ingredientTraitLabel.alignment = TextAlignmentOptions.TopLeft;
            ingredientTraitLabel.textWrappingMode = TextWrappingModes.Normal;
            ingredientTraitLabel.overflowMode = TextOverflowModes.Truncate;

            RectTransform textRect = ingredientTraitLabel.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);
            textRect.sizeDelta = Vector2.zero;
            ingredientTraitLabel.text = "Ingredient Traits";
            ingredientTraitPanel = panel;
            EnsureIngredientTraitScrollView();
            EnsureIngredientTraitBackdrop();
            EnsureIngredientTraitToggleButton();
            SetIngredientTraitsExpanded(false);
        }

        private void EnsureIngredientTraitScrollView()
        {
            if (!IsAlive(ingredientTraitPanel) || !IsAlive(ingredientTraitLabel))
            {
                return;
            }

            Transform viewportTransform = ingredientTraitPanel.transform.Find("Ingredient Traits Viewport");
            if (!IsAlive(viewportTransform))
            {
                GameObject viewport = new GameObject("Ingredient Traits Viewport");
                viewport.transform.SetParent(ingredientTraitPanel.transform, false);
                ingredientTraitViewport = viewport.AddComponent<RectTransform>();

                Image viewportImage = viewport.AddComponent<Image>();
                viewportImage.color = Color.clear;
                viewportImage.raycastTarget = true;
                viewport.AddComponent<RectMask2D>();
            }
            else
            {
                ingredientTraitViewport = viewportTransform as RectTransform;
                if (!IsAlive(viewportTransform.GetComponent<RectMask2D>()))
                {
                    viewportTransform.gameObject.AddComponent<RectMask2D>();
                }

                Image viewportImage = viewportTransform.GetComponent<Image>();
                if (!IsAlive(viewportImage))
                {
                    viewportImage = viewportTransform.gameObject.AddComponent<Image>();
                }

                viewportImage.color = Color.clear;
                viewportImage.raycastTarget = true;
            }

            if (!IsAlive(ingredientTraitViewport))
            {
                return;
            }

            ingredientTraitViewport.anchorMin = Vector2.zero;
            ingredientTraitViewport.anchorMax = Vector2.one;
            ingredientTraitViewport.offsetMin = new Vector2(12f, 12f);
            ingredientTraitViewport.offsetMax = new Vector2(-12f, -12f);

            if (ingredientTraitLabel.transform.parent != ingredientTraitViewport)
            {
                ingredientTraitLabel.transform.SetParent(ingredientTraitViewport, false);
            }

            RectTransform textRect = ingredientTraitLabel.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            textRect.localRotation = Quaternion.identity;
            textRect.localScale = Vector3.one;

            ContentSizeFitter fitter = ingredientTraitLabel.GetComponent<ContentSizeFitter>();
            if (!IsAlive(fitter))
            {
                fitter = ingredientTraitLabel.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ingredientTraitLabel.raycastTarget = false;

            ingredientTraitScrollRect = ingredientTraitPanel.GetComponent<ScrollRect>();
            if (!IsAlive(ingredientTraitScrollRect))
            {
                ingredientTraitScrollRect = ingredientTraitPanel.AddComponent<ScrollRect>();
            }

            ingredientTraitScrollRect.viewport = ingredientTraitViewport;
            ingredientTraitScrollRect.content = textRect;
            ingredientTraitScrollRect.horizontal = false;
            ingredientTraitScrollRect.vertical = true;
            ingredientTraitScrollRect.movementType = ScrollRect.MovementType.Clamped;
            ingredientTraitScrollRect.inertia = true;
            ingredientTraitScrollRect.decelerationRate = 0.12f;
            ingredientTraitScrollRect.scrollSensitivity = 36f;
        }

        private void ResetIngredientTraitScroll()
        {
            EnsureIngredientTraitScrollView();
            if (!IsAlive(ingredientTraitScrollRect))
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            ingredientTraitScrollRect.StopMovement();
            ingredientTraitScrollRect.verticalNormalizedPosition = 1f;
        }

        private void EnsureIngredientTraitBackdrop()
        {
            if (IsAlive(ingredientTraitBackdrop))
            {
                return;
            }

            Transform existing = transform.Find("Ingredient Traits Backdrop");
            Image backdropImage;
            if (IsAlive(existing))
            {
                ingredientTraitBackdrop = existing.gameObject;
                backdropImage = ingredientTraitBackdrop.GetComponent<Image>();
            }
            else
            {
                ingredientTraitBackdrop = new GameObject("Ingredient Traits Backdrop");
                ingredientTraitBackdrop.transform.SetParent(transform, false);
                RectTransform rect = ingredientTraitBackdrop.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                backdropImage = ingredientTraitBackdrop.AddComponent<Image>();
                backdropImage.color = new Color(0f, 0f, 0f, 0.24f);
            }

            if (!IsAlive(backdropImage))
            {
                backdropImage = ingredientTraitBackdrop.AddComponent<Image>();
                backdropImage.color = new Color(0f, 0f, 0f, 0.24f);
            }

            Button button = ingredientTraitBackdrop.GetComponent<Button>();
            if (!IsAlive(button))
            {
                button = ingredientTraitBackdrop.AddComponent<Button>();
            }

            button.transition = Selectable.Transition.None;
            button.targetGraphic = backdropImage;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(CollapseIngredientTraits);
            ingredientTraitBackdrop.SetActive(false);
        }

        private void EnsureIngredientTraitToggleButton()
        {
            if (!IsAlive(ingredientTraitToggleButton))
            {
                Transform existing = transform.Find("Ingredient Traits Toggle");
                if (IsAlive(existing))
                {
                    ingredientTraitToggleButton = existing.GetComponent<Button>();
                }
            }

            if (!IsAlive(ingredientTraitToggleButton))
            {
                ingredientTraitToggleButton = CreateAnchoredRuntimeButton(
                    transform,
                    "Ingredient Traits Toggle",
                    new Vector2(112f, 40f),
                    new Vector2(-24f, -24f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f));
            }

            TMP_Text label = ingredientTraitToggleButton.GetComponentInChildren<TMP_Text>();
            if (IsAlive(label))
            {
                label.text = "Traits";
                label.fontSize = 22;
            }

            ingredientTraitToggleButton.onClick.RemoveAllListeners();
            ingredientTraitToggleButton.onClick.AddListener(ExpandIngredientTraits);
            ingredientTraitToggleButton.gameObject.SetActive(false);
        }

        private void SelectRatio(RatioLevel ratio)
        {
            Action<RatioLevel> callback = pendingRatioSelection;
            pendingRatioSelection = null;
            if (ratioSelectionPanel != null)
            {
                ratioSelectionPanel.SetActive(false);
            }

            callback?.Invoke(ratio);
        }

        private void CloseRatioSelection()
        {
            pendingRatioSelection = null;
            if (ratioSelectionPanel != null)
            {
                ratioSelectionPanel.SetActive(false);
            }
        }

        private static string BuildDimensionFeedback(EvaluationResult result)
        {
            StringBuilder builder = new StringBuilder();
            foreach (DimensionEvaluation dimension in result.dimensions)
            {
                if (!string.IsNullOrWhiteSpace(dimension.feedback))
                {
                    builder.AppendLine();
                    builder.Append(dimension.mechanic).Append(": ").Append(dimension.feedback);
                }
            }

            return builder.ToString();
        }

        private static string BuildEvaluationFeedback(EvaluationResult result)
        {
            if (result == null)
            {
                return string.Empty;
            }

            if (result.judgement != JudgementResult.Correct)
            {
                return "Result: " + result.judgement + "\nUse the hint and try again.";
            }

            return "Result: Correct\n" + result.mainFeedback + BuildDimensionFeedback(result);
        }

        private static string BuildPanelEvaluationFeedback(EvaluationResult result, string permanentHint)
        {
            if (result == null)
            {
                return string.Empty;
            }

            if (result.judgement != JudgementResult.Correct)
            {
                return BuildEvaluationFeedback(result);
            }

            string message = "Result: Correct\nRecipe complete.";
            return string.IsNullOrWhiteSpace(permanentHint)
                ? message
                : message + "\nNew clue saved to Experiment Log.";
        }

        private static string BuildEvaluationHint(EvaluationResult result, HintResult hint, string permanentHint)
        {
            if (result == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            if (hint != null && !string.IsNullOrWhiteSpace(hint.text))
            {
                builder.Append(hint.text);
            }
            else if (result.judgement == JudgementResult.Correct)
            {
                builder.Append("Recipe complete.");
            }

            if (!string.IsNullOrWhiteSpace(permanentHint))
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(permanentHint);
            }

            return builder.ToString();
        }

        private void ScheduleResetPrompt(string baseText)
        {
            StopResetPrompt();
            resetPromptCoroutine = StartCoroutine(ShowResetPromptAfterDelay(baseText));
        }

        private IEnumerator ShowResetPromptAfterDelay(string baseText)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, resetPromptDelaySeconds));
            resetPromptCoroutine = null;

            if (attemptManager == null || !attemptManager.HasEvaluated || hintLabel == null)
            {
                yield break;
            }

            if (hintLabel.text != baseText)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(baseText))
            {
                ShowHint(resetRequiredHint);
            }
            else
            {
                ShowHint(baseText + "\n\n" + resetRequiredHint);
            }
        }

        private void StopResetPrompt()
        {
            if (resetPromptCoroutine != null)
            {
                StopCoroutine(resetPromptCoroutine);
                resetPromptCoroutine = null;
            }
        }

        private static string BuildPermanentHintText(IReadOnlyList<UnlockedClueRecord> clues)
        {
            if (clues == null || clues.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (UnlockedClueRecord clue in clues)
            {
                if (clue == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(clue.title))
                {
                    builder.Append("[").Append(clue.title).Append("] ");
                }

                builder.Append(clue.content);
                builder.AppendLine();
            }

            return builder.ToString().Trim();
        }

        private static string BuildIngredientTraitText(RecipeLevelData level)
        {
            if (level == null || level.availableIngredients == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Ingredient Traits");
            builder.AppendLine();
            IEnumerable<LevelIngredientProfile> profiles = level.ingredientProfiles != null && level.ingredientProfiles.Count > 0
                ? level.ingredientProfiles.Where(x => x != null && x.ingredient != null)
                    .GroupBy(x => x.ingredient)
                    .Select(x => x.First())
                : Enumerable.Empty<LevelIngredientProfile>();

            if (profiles.Any())
            {
                foreach (LevelIngredientProfile profile in profiles)
                {
                    string trait = !string.IsNullOrWhiteSpace(profile.levelTraitDescription)
                        ? profile.levelTraitDescription
                        : GetIngredientTraitFallback(profile.ingredient);
                    AppendIngredientTraitSection(builder, level, profile, trait);
                }

                return builder.ToString().Trim();
            }

            foreach (IngredientData ingredient in level.availableIngredients.Where(x => x != null))
            {
                AppendIngredientTraitSection(builder, level, level.GetProfile(ingredient), GetIngredientTraitFallback(ingredient), ingredient);
            }

            return builder.ToString().Trim();
        }

        private static void AppendIngredientTraitSection(
            StringBuilder builder,
            RecipeLevelData level,
            LevelIngredientProfile profile,
            string baseTrait,
            IngredientData fallbackIngredient = null)
        {
            IngredientData ingredient = profile != null && profile.ingredient != null
                ? profile.ingredient
                : fallbackIngredient;
            if (ingredient == null)
            {
                return;
            }

            builder.AppendLine(ingredient.DisplayName);
            if (!string.IsNullOrWhiteSpace(baseTrait))
            {
                builder.Append("- ").AppendLine(baseTrait.Trim());
            }

            List<string> clues = BuildLevelTraitClues(level, profile);
            foreach (string clue in clues)
            {
                builder.Append("- ").AppendLine(clue.Trim());
            }

            builder.AppendLine();
        }

        private static List<string> BuildLevelTraitClues(RecipeLevelData level, LevelIngredientProfile profile)
        {
            List<string> clues = new List<string>();
            if (level == null || level.enabledMechanics == null || profile == null || profile.ingredient == null)
            {
                return clues;
            }

            if (level.enabledMechanics.enableIngredientOrder && profile.targetOrderIndex >= 0)
            {
                clues.Add(BuildOrderTraitClue(level, profile));
            }

            if (level.enabledMechanics.enableRatio && profile.targetRatioLevel != RatioLevel.None)
            {
                clues.Add(BuildRatioTraitClue(profile.targetRatioLevel));
            }

            if (level.enabledMechanics.enableCombination && !string.IsNullOrWhiteSpace(profile.targetCombinationKey))
            {
                clues.Add(BuildCombinationTraitClue(level, profile));
            }

            if (level.enabledMechanics.enableForce)
            {
                clues.Add(BuildForceTraitClue(profile.targetForceLevel));
            }

            if (level.enabledMechanics.enableSpeed)
            {
                clues.Add(BuildSpeedTraitClue(profile.targetSpeedLevel));
            }

            return clues.Where(clue => !string.IsNullOrWhiteSpace(clue)).ToList();
        }

        private static string BuildOrderTraitClue(RecipeLevelData level, LevelIngredientProfile profile)
        {
            int count = level.correctIngredientOrder != null
                ? level.correctIngredientOrder.Count(x => x != null)
                : 0;
            if (profile.targetOrderIndex <= 0)
            {
                return "Its flavor needs time to form before the other ingredients arrive.";
            }

            if (count > 0 && profile.targetOrderIndex >= count - 1)
            {
                return "Its character is best preserved when it arrives near the end.";
            }

            return "It works best after the first flavor has formed, but before the final ingredient arrives.";
        }

        private static string BuildRatioTraitClue(RatioLevel ratio)
        {
            switch (ratio)
            {
                case RatioLevel.VeryLess:
                    return "Use the Very Small amount.";
                case RatioLevel.Less:
                    return "Use the Small amount.";
                case RatioLevel.Medium:
                case RatioLevel.SlightlyMore:
                    return "Use the Medium amount.";
                case RatioLevel.More:
                    return "Use the Large amount.";
                default:
                    return string.Empty;
            }
        }

        private static string BuildCombinationTraitClue(RecipeLevelData level, LevelIngredientProfile profile)
        {
            if (level != null && level.allowAnyPairPreparation)
            {
                return "It can prepare with any one spice, while the remaining spice is prepared separately.";
            }

            List<IngredientData> group = FindCombinationGroup(level, profile.ingredient);
            if (group.Count <= 1)
            {
                return "It keeps its shape better on its own.";
            }

            List<string> partners = group
                .Where(ingredient => ingredient != null && ingredient != profile.ingredient)
                .Select(ingredient => ingredient.DisplayName)
                .ToList();
            return partners.Count == 0
                ? "It can share a batch with a close flavor."
                : "It can blend with " + FormatNameList(partners) + ".";
        }

        private static List<IngredientData> FindCombinationGroup(RecipeLevelData level, IngredientData ingredient)
        {
            if (level == null || ingredient == null || level.correctCombinationPattern == null || level.correctCombinationPattern.groups == null)
            {
                return new List<IngredientData>();
            }

            CombinationGroup group = level.correctCombinationPattern.groups
                .FirstOrDefault(candidate => candidate != null && candidate.ingredients != null && candidate.ingredients.Contains(ingredient));
            return group != null
                ? group.ingredients.Where(x => x != null).ToList()
                : new List<IngredientData>();
        }

        private static string BuildForceTraitClue(ForceLevel force)
        {
            switch (force)
            {
                case ForceLevel.Light:
                    return "It opens with gentle pressure.";
                case ForceLevel.Heavy:
                    return "It needs firm pressure to come forward.";
                default:
                    return "Medium pressure keeps it balanced.";
            }
        }

        private static string BuildSpeedTraitClue(SpeedLevel speed)
        {
            switch (speed)
            {
                case SpeedLevel.Slow:
                    return "A slow rhythm brings out sweetness without extra bitterness.";
                case SpeedLevel.Fast:
                    return "A faster rhythm wakes it up.";
                default:
                    return "A steady rhythm keeps it balanced.";
            }
        }

        private static string FormatNameList(List<string> names)
        {
            if (names == null || names.Count == 0)
            {
                return "another ingredient";
            }

            if (names.Count == 1)
            {
                return names[0];
            }

            return string.Join(", ", names.Take(names.Count - 1)) + ", and " + names[names.Count - 1];
        }

        private static string GetIngredientTraitFallback(IngredientData ingredient)
        {
            if (ingredient == null)
            {
                return "Distinct flavor";
            }

            return !string.IsNullOrWhiteSpace(ingredient.aromaType)
                ? ingredient.aromaType
                : "Distinct flavor";
        }

        private static TMP_Text CreateRuntimeText(Transform parent, string name, Vector2 size, Vector2 position, TextAnchor anchor, int fontSize)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = ThemeFontProvider.GetTmpFont(fontSize);
            text.color = Color.black;
            text.alignment = TmpTextUtility.ToAlignment(anchor);
            text.fontSize = fontSize;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static Button CreateRuntimeButton(Transform parent, string name, Vector2 size, Vector2 position)
        {
            return CreateAnchoredRuntimeButton(parent, name, size, position, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }

        private static Button CreateAnchoredRuntimeButton(Transform parent, string name, Vector2 size, Vector2 position, Vector2 anchorPoint, Vector2 pivot)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorPoint;
            rect.anchorMax = anchorPoint;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.92f, 0.92f, 0.92f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateRuntimeText(buttonObject.transform, name + " Text", Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 14);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(34f, 10f);
            labelRect.offsetMax = new Vector2(-34f, -12f);
            labelRect.sizeDelta = Vector2.zero;
            return button;
        }

        private static Font GetRuntimeFont()
        {
            return ThemeFontProvider.GetFont(14);
        }

        private static bool IsAlive(UnityEngine.Object obj)
        {
            return obj != null;
        }
    }
}
