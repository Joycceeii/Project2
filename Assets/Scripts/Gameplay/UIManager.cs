using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public Text levelLabel;
        public Text openingLevelTitleLabel;
        public Text currentOrderLabel;
        public Text currentRatioLabel;
        public Text currentSpeedLabel;
        public Text hintLabel;
        public Text ingredientTraitLabel;
        public Button ingredientTraitToggleButton;
        public GameObject ratioSelectionPanel;
        public Text ratioSelectionTitle;
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

        [Header("HUD Text Insets")]
        public Vector2 forceLabelTextInsetMin = new Vector2(58f, 13f);
        public Vector2 forceLabelTextInsetMax = new Vector2(-58f, -13f);
        public bool showOpeningLevelTitle = true;
        public float openingLevelTitleSeconds = 2.4f;

        private Action<RatioLevel> pendingRatioSelection;
        private Coroutine resetPromptCoroutine;
        private Coroutine openingLevelTitleCoroutine;
        private float lastEvaluationTime = -999f;
        private GameObject ingredientTraitPanel;
        private GameObject ingredientTraitBackdrop;
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
            NormalizeHudLayout();
            PanelBackgroundStyle.ApplyToNamedPanels(transform);
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
            ShowOpeningLevelTitle(level);

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

        private static void SetTextPanelVisible(Text label, bool visible)
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
                return attemptManager.RequiredBatchPowderSeconds;
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
                    ingredientTraitBackdrop.transform.SetAsLastSibling();
                }
            }

            if (IsAlive(ingredientTraitPanel))
            {
                ingredientTraitPanel.SetActive(expanded && hasText);
                if (expanded && hasText)
                {
                    ingredientTraitPanel.transform.SetAsLastSibling();
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
                Text label = button.GetComponentInChildren<Text>();
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
                ConfigureTextPanel(levelLabel, levelPanelLayout, new Vector2(0f, 1f), new Vector2(0f, 1f), TextAnchor.MiddleLeft);
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
                ConfigureForceLabelPanel(true);
            }
            else
            {
                StyleExistingHud();
                ConfigureForceLabelPanel(false);
            }

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
            ingredientTraitLabel.font = GetRuntimeFont();
            ingredientTraitLabel.fontStyle = FontStyle.Normal;
            ingredientTraitLabel.resizeTextForBestFit = true;
            ingredientTraitLabel.resizeTextMinSize = 17;
            ingredientTraitLabel.resizeTextMaxSize = 24;
            ingredientTraitLabel.alignment = TextAnchor.UpperLeft;
            ingredientTraitLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            ingredientTraitLabel.verticalOverflow = VerticalWrapMode.Truncate;

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

        private static void ConfigureTextPanel(Text label, HudPanelLayout layout, Vector2 anchor, Vector2 pivot, TextAnchor alignment)
        {
            ConfigureTextPanel(label, GetLayoutSize(layout), GetLayoutPosition(layout), anchor, pivot, alignment, GetLayoutFontSize(layout, 22));
        }

        private static void ConfigureTextPanel(Text label, Vector2 size, Vector2 position, Vector2 anchor, Vector2 pivot, TextAnchor alignment, int fontSize)
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

            label.alignment = alignment;
            label.fontSize = fontSize;
            label.font = GetRuntimeFont();
            label.fontStyle = alignment == TextAnchor.MiddleCenter ? FontStyle.Bold : FontStyle.Normal;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(14, fontSize - 8);
            label.resizeTextMaxSize = fontSize;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            ConfigureTextInset(label, alignment);
        }

        private static void ConfigureTextInset(Text label, TextAnchor alignment)
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

        private static void ConfigureButton(Button button, HudPanelLayout layout, Vector2 anchor, Vector2 pivot)
        {
            ConfigureButton(button, GetLayoutSize(layout), GetLayoutPosition(layout), anchor, pivot);
        }

        private static void ConfigureButton(Button button, Vector2 size, Vector2 position, Vector2 anchor, Vector2 pivot)
        {
            if (!IsAlive(button))
            {
                return;
            }

            ConfigureRect(button.transform as RectTransform, size, position, anchor, pivot);
            Text label = button.GetComponentInChildren<Text>(true);
            if (IsAlive(label))
            {
                label.fontSize = label.text != null && label.text.Length > 12 ? 22 : 24;
                label.font = GetRuntimeFont();
                label.fontStyle = FontStyle.Bold;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 16;
                label.resizeTextMaxSize = label.fontSize;
                label.alignment = TextAnchor.MiddleCenter;
                ConfigureTextInset(label, TextAnchor.MiddleCenter);
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
                openingLevelTitleLabel = existing.GetComponent<Text>();
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

        private static void ConfigureOpeningLevelTitlePanel(Text titleLabel)
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
                textRect.offsetMin = new Vector2(72f, 32f);
                textRect.offsetMax = new Vector2(-72f, -32f);
                textRect.sizeDelta = Vector2.zero;
            }

            titleLabel.font = GetRuntimeFont();
            titleLabel.fontStyle = FontStyle.Bold;
            titleLabel.resizeTextForBestFit = true;
            titleLabel.resizeTextMinSize = 18;
            titleLabel.resizeTextMaxSize = 34;
            titleLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleLabel.verticalOverflow = VerticalWrapMode.Overflow;
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
                openingLevelTitleLabel.resizeTextMaxSize = 42;
                panel.SetActive(true);
                yield return new WaitForSeconds(1.2f);
                panel.SetActive(false);
                yield return new WaitForSeconds(0.18f);
                openingLevelTitleLabel.resizeTextMaxSize = 34;
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

        private void ApplyPanelStyle(Text label)
        {
            if (!IsAlive(label))
            {
                return;
            }

            if (IsAlive(label.transform.parent))
            {
                PanelBackgroundStyle.Apply(label.transform.parent.GetComponent<Image>());
            }

            label.font = GetRuntimeFont();
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void ApplyButtonStyle(Button button)
        {
            if (!IsAlive(button))
            {
                return;
            }

            PanelBackgroundStyle.Apply(button.GetComponent<Image>());
            Text label = button.GetComponentInChildren<Text>(true);
            if (IsAlive(label))
            {
                label.font = GetRuntimeFont();
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
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
            Text label = panel.GetComponentInChildren<Text>(true);
            if (!IsAlive(label))
            {
                return;
            }

            label.font = GetRuntimeFont();
            label.fontSize = GetLayoutFontSize(forceLabelPanelLayout, 15);
            label.fontStyle = FontStyle.Normal;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = GetLayoutFontSize(forceLabelPanelLayout, 15);
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            if (!updateRectTransforms)
            {
                return;
            }

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

            Text text = button.GetComponentInChildren<Text>(true);
            if (!IsAlive(text))
            {
                text = CreateRuntimeText(button.transform, label + " Text", Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 14);
            }

            text.gameObject.SetActive(true);
            text.enabled = true;
            text.text = label;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = GetRuntimeFont();
            text.fontSize = label.Length > 12 ? 22 : 24;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = text.fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
                ingredientTraitPanel = ingredientTraitLabel.transform.parent != null
                    ? ingredientTraitLabel.transform.parent.gameObject
                    : ingredientTraitLabel.gameObject;
                PanelBackgroundStyle.Apply(ingredientTraitPanel.GetComponent<Image>(), 0.82f);
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
                ingredientTraitLabel = existing.GetComponent<Text>();
                ingredientTraitPanel = ingredientTraitLabel.transform.parent != null
                    ? ingredientTraitLabel.transform.parent.gameObject
                    : ingredientTraitLabel.gameObject;
                PanelBackgroundStyle.Apply(ingredientTraitPanel.GetComponent<Image>(), 0.82f);
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
            ingredientTraitLabel.alignment = TextAnchor.UpperLeft;
            ingredientTraitLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            ingredientTraitLabel.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform textRect = ingredientTraitLabel.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);
            textRect.sizeDelta = Vector2.zero;
            ingredientTraitLabel.text = "Ingredient Traits";
            ingredientTraitPanel = panel;
            EnsureIngredientTraitBackdrop();
            EnsureIngredientTraitToggleButton();
            SetIngredientTraitsExpanded(false);
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

            Text label = ingredientTraitToggleButton.GetComponentInChildren<Text>();
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
                    builder.Append("- ").Append(profile.ingredient.DisplayName).Append(": ");
                    builder.Append(BuildIngredientTraitLine(level, profile, trait));
                    builder.AppendLine();
                }

                return builder.ToString().Trim();
            }

            foreach (IngredientData ingredient in level.availableIngredients.Where(x => x != null))
            {
                builder.Append("- ").Append(ingredient.DisplayName).Append(": ");
                builder.Append(BuildIngredientTraitLine(level, level.GetProfile(ingredient), GetIngredientTraitFallback(ingredient)));
                builder.AppendLine();
            }

            return builder.ToString().Trim();
        }

        private static string BuildIngredientTraitLine(RecipeLevelData level, LevelIngredientProfile profile, string baseTrait)
        {
            List<string> clues = BuildLevelTraitClues(level, profile);
            if (clues.Count == 0)
            {
                return baseTrait;
            }

            return baseTrait.TrimEnd('.') + ". " + string.Join(" ", clues);
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
                return "It feels like a base note.";
            }

            if (count > 0 && profile.targetOrderIndex >= count - 1)
            {
                return "It reads best as a finishing note.";
            }

            return "It fits better after the base has formed.";
        }

        private static string BuildRatioTraitClue(RatioLevel ratio)
        {
            switch (ratio)
            {
                case RatioLevel.VeryLess:
                    return "Use it like a trace, not a main flavor.";
                case RatioLevel.Less:
                    return "Let it stay light in the mix.";
                case RatioLevel.SlightlyMore:
                    return "Give it a clear supporting role.";
                case RatioLevel.More:
                    return "Let it lead the flavor.";
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

        private static Text CreateRuntimeText(Transform parent, string name, Vector2 size, Vector2 position, TextAnchor anchor, int fontSize)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Text text = textObject.AddComponent<Text>();
            text.font = GetRuntimeFont();
            text.color = Color.black;
            text.alignment = anchor;
            text.fontSize = fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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

            Text label = CreateRuntimeText(buttonObject.transform, name + " Text", Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 14);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 0f);
            labelRect.offsetMax = new Vector2(-4f, 0f);
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
