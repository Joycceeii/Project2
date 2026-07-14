using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public class UIManager : MonoBehaviour
    {
        public RecipeAttemptManager attemptManager;
        public RecipeEvaluator evaluator;
        public HintManager hintManager;
        public ExperimentLogManager logManager;
        public LevelManager levelManager;

        public Text levelLabel;
        public Text currentIngredientsLabel;
        public Text currentOrderLabel;
        public Text currentRatioLabel;
        public Text currentSpeedLabel;
        public Text feedbackLabel;
        public Text hintLabel;
        public Text ingredientTraitLabel;
        public GameObject ratioSelectionPanel;
        public Text ratioSelectionTitle;
        public Button[] ratioSelectionButtons = new Button[4];
        public Button experimentLogButton;
        public Button processCheckButton;
        public Button evaluateButton;
        public Button resetAttemptButton;
        public Button nextLevelButton;
        public string experimentLogSceneName = "ExperimentLog";
        public bool autoUpdateLevelLabel;

        private Action<RatioLevel> pendingRatioSelection;
        private string pendingProcessCheckHint;

        public bool IsRatioSelectionOpen => ratioSelectionPanel != null && ratioSelectionPanel.activeSelf;

        private void Awake()
        {
            EnsureRatioSelectionPanel();
            EnsureExperimentLogButton();
            EnsureProcessCheckButton();
            EnsureActionButtons();
            EnsureIngredientTraitPanel();
        }

        public void EvaluateCurrentAttempt()
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

            EvaluationResult result = evaluator.Evaluate(attemptManager.currentLevel, attemptManager);
            HintResult hint = result.judgement == JudgementResult.Correct ? null : hintManager != null ? hintManager.GetNextHint(attemptManager.currentLevel, attemptManager, result) : null;
            string permanentHint = string.Empty;
            if (result.judgement == JudgementResult.Correct)
            {
                List<UnlockedClueRecord> unlockedClues = logManager != null ? logManager.UnlockClues(attemptManager.currentLevel) : null;
                permanentHint = BuildPermanentHintText(unlockedClues);
            }

            ShowFeedback(BuildEvaluationFeedback(result));
            ShowHint(BuildEvaluationHint(result, hint, permanentHint));
            logManager?.AddRecord(attemptManager, result, hint, pendingProcessCheckHint, permanentHint);
            pendingProcessCheckHint = null;
        }

        public void CheckGrindProcess()
        {
            if (IsRatioSelectionOpen)
            {
                ShowFeedback("Choose the ingredient ratio before checking the grind process.");
                return;
            }

            if (attemptManager == null || evaluator == null)
            {
                ShowFeedback("Process check is not ready.");
                return;
            }

            EvaluationResult result = evaluator.Evaluate(attemptManager.currentLevel, attemptManager);
            pendingProcessCheckHint = BuildProcessDiagnosticHint(attemptManager.currentLevel, result);
            ShowHint(pendingProcessCheckHint);
            ShowFeedback(BuildProcessCheckFeedback(result));
        }

        public void ResetAttempt()
        {
            pendingProcessCheckHint = null;
            attemptManager?.ResetAttempt();
            ShowFeedback(string.Empty);
            ShowHint(string.Empty);
        }

        public void NextLevel()
        {
            pendingProcessCheckHint = null;
            levelManager?.NextLevel();
        }

        public void OpenExperimentLog()
        {
            SceneManager.LoadScene(experimentLogSceneName);
        }

        public void ShowLevel(RecipeLevelData level)
        {
            ApplyLevelPanelVisibility(level);

            if (autoUpdateLevelLabel && levelLabel != null)
            {
                levelLabel.text = level == null ? "No Level" : level.levelID + " " + level.cityName + " - " + level.levelName + "\n" + level.levelIntro;
            }

            if (level != null && currentIngredientsLabel != null)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Available Ingredients:");
                foreach (IngredientData ingredient in level.availableIngredients.Where(x => x != null))
                {
                    builder.Append("- ").Append(ingredient.DisplayName);
                    builder.AppendLine();
                }

                currentIngredientsLabel.text = builder.ToString();
            }

            ShowIngredientTraits(level);
            pendingProcessCheckHint = null;
        }

        private void ApplyLevelPanelVisibility(RecipeLevelData level)
        {
            EnabledMechanics mechanics = level != null ? level.enabledMechanics : null;
            bool showIngredientPanel = mechanics == null
                || mechanics.enableIngredientSelection
                || mechanics.enableIngredientOrder
                || mechanics.enableRatio
                || mechanics.enableCombination;
            bool showOrderPanel = mechanics == null || mechanics.enableIngredientOrder;
            bool showRatioPanel = mechanics == null || mechanics.enableRatio;
            bool showSpeedPanel = mechanics == null || mechanics.enableSpeed;
            bool showForceControls = mechanics == null || mechanics.enableForce;
            bool showProcessCheck = mechanics == null
                || mechanics.enableForce
                || mechanics.enableSpeed
                || mechanics.enableGrindDuration;

            SetTextPanelVisible(currentIngredientsLabel, showIngredientPanel);
            SetTextPanelVisible(currentOrderLabel, showOrderPanel);
            SetTextPanelVisible(currentRatioLabel, showRatioPanel);
            SetTextPanelVisible(currentSpeedLabel, showSpeedPanel);
            SetNamedObjectVisible("Force Slider", showForceControls);
            SetNamedObjectVisible("Force Label Panel", showForceControls);
            SetButtonVisible(processCheckButton, showProcessCheck);
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

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (IsAlive(button))
            {
                button.gameObject.SetActive(visible);
            }
        }

        public void RefreshAttemptPanels(RecipeAttemptManager attempt)
        {
            if (attempt == null)
            {
                return;
            }

            if (currentIngredientsLabel != null)
            {
                currentIngredientsLabel.text = "Current Ingredients:\n" + string.Join(", ", attempt.IngredientAmounts.Where(x => x.ingredient != null).Select(x => x.ingredient.DisplayName));
            }

            if (currentOrderLabel != null)
            {
                currentOrderLabel.text = "Ingredient Order:\n" + string.Join(" -> ", attempt.IngredientOrder.Where(x => x != null).Select(x => x.DisplayName));
            }

            if (currentRatioLabel != null)
            {
                Dictionary<IngredientData, RatioLevel> ratio = attempt.CalculateRatioPattern(out bool ambiguous);
                string prefix = ambiguous ? "Ratio:\nRatio values are still close\n" : "Ratio:\n";
                currentRatioLabel.text = prefix + string.Join(" / ", ratio.Select(x => x.Key.DisplayName + "=" + GetRatioDisplayName(x.Value)));
            }

            if (currentSpeedLabel != null && attempt.pestleController != null)
            {
                currentSpeedLabel.text = "Current Grind Speed: " + attempt.pestleController.CurrentSpeedLevel;
            }
        }

        public void ShowFeedback(string text)
        {
            EnsureFeedbackLabel();
            if (feedbackLabel != null)
            {
                feedbackLabel.text = text;
            }
        }

        public void ShowHint(string text)
        {
            if (hintLabel != null)
            {
                hintLabel.text = text;
            }
        }

        public void ShowIngredientTraits(RecipeLevelData level)
        {
            EnsureIngredientTraitPanel();
            if (!IsAlive(ingredientTraitLabel))
            {
                return;
            }

            string text = BuildIngredientTraitText(level);
            Transform panel = ingredientTraitLabel.transform.parent;
            if (IsAlive(panel))
            {
                panel.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
            }

            ingredientTraitLabel.text = text;
        }

        public void ShowRatioSelection(IngredientData ingredient, IReadOnlyList<RatioLevel> options, Action<RatioLevel> onSelected)
        {
            EnsureRatioSelectionPanel();
            if (options == null || options.Count == 0)
            {
                return;
            }

            if (options.Count == 1)
            {
                onSelected?.Invoke(options[0]);
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

            ratioSelectionPanel.SetActive(true);
        }

        public static string GetRatioDisplayName(RatioLevel ratio)
        {
            switch (ratio)
            {
                case RatioLevel.VeryLess: return "Very Little";
                case RatioLevel.Less: return "Little";
                case RatioLevel.SlightlyMore: return "Much";
                case RatioLevel.More: return "Very Much";
                default: return "None";
            }
        }

        public void EnsureRatioSelectionPanel()
        {
            if (ratioSelectionPanel != null)
            {
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
            panelImage.color = new Color(1f, 1f, 1f, 0.94f);

            ratioSelectionTitle = CreateRuntimeText(ratioSelectionPanel.transform, "Ratio Selection Title", new Vector2(320f, 44f), new Vector2(0f, 72f), TextAnchor.MiddleCenter, 16);
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

        public void EnsureProcessCheckButton()
        {
            if (IsAlive(processCheckButton))
            {
                processCheckButton.onClick.RemoveAllListeners();
                processCheckButton.onClick.AddListener(CheckGrindProcess);
                return;
            }

            Transform existing = transform.Find("Check Grind Process");
            if (IsAlive(existing))
            {
                processCheckButton = existing.GetComponent<Button>();
                if (IsAlive(processCheckButton))
                {
                    processCheckButton.onClick.RemoveAllListeners();
                    processCheckButton.onClick.AddListener(CheckGrindProcess);
                    return;
                }
            }

            processCheckButton = CreateAnchoredRuntimeButton(transform, "Check Grind Process", new Vector2(172f, 36f), new Vector2(-24f, 64f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            processCheckButton.onClick.AddListener(CheckGrindProcess);
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
            nextLevelButton = BindButton(nextLevelButton, "Next Level", NextLevel);
            EnsureFeedbackPanelButton();
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
            text.fontSize = label.Length > 12 ? 16 : 18;
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
                image.color = Color.white;
                button.targetGraphic = image;
            }
        }

        private void EnsureFeedbackPanelButton()
        {
            EnsureFeedbackLabel();
            if (!IsAlive(feedbackLabel))
            {
                return;
            }

            Transform panel = feedbackLabel.transform.parent;
            if (!IsAlive(panel))
            {
                return;
            }

            Button button = panel.GetComponent<Button>();
            if (button == null)
            {
                button = panel.gameObject.AddComponent<Button>();
            }

            Image image = panel.GetComponent<Image>();
            if (IsAlive(image))
            {
                button.targetGraphic = image;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(EvaluateCurrentAttempt);
        }

        private void EnsureFeedbackLabel()
        {
            if (IsAlive(feedbackLabel))
            {
                return;
            }

            Transform feedback = transform.Find("Feedback Panel/Feedback Text");
            if (!IsAlive(feedback))
            {
                feedback = transform.Find("Feedback Panel/Feedback Text Text");
            }

            if (IsAlive(feedback))
            {
                feedbackLabel = feedback.GetComponent<Text>();
            }
        }

        public void EnsureIngredientTraitPanel()
        {
            if (IsAlive(ingredientTraitLabel))
            {
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
                return;
            }

            GameObject panel = new GameObject("Ingredient Traits Panel");
            panel.transform.SetParent(transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.sizeDelta = new Vector2(380f, 88f);
            panelRect.anchoredPosition = new Vector2(-24f, -244f);

            Image image = panel.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.82f);

            ingredientTraitLabel = CreateRuntimeText(panel.transform, "Ingredient Traits Text", Vector2.zero, Vector2.zero, TextAnchor.UpperLeft, 14);
            RectTransform textRect = ingredientTraitLabel.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);
            textRect.sizeDelta = Vector2.zero;
            ingredientTraitLabel.text = "Ingredient Traits";
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
                return "Result: " + result.judgement + "\nKeep experimenting with the hints.";
            }

            return "Result: Correct\n" + result.mainFeedback + BuildDimensionFeedback(result);
        }

        private static string BuildEvaluationHint(EvaluationResult result, HintResult hint, string permanentHint)
        {
            if (result == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("Completeness: ").Append(result.completenessScore).Append("%");

            if (hint != null && !string.IsNullOrWhiteSpace(hint.text))
            {
                builder.AppendLine();
                builder.Append(hint.text);
            }
            else if (result.judgement == JudgementResult.Correct)
            {
                builder.AppendLine();
                builder.Append("Restoration complete.");
            }

            if (!string.IsNullOrWhiteSpace(permanentHint))
            {
                builder.AppendLine();
                builder.Append(permanentHint);
            }

            return builder.ToString();
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
            foreach (IngredientData ingredient in level.availableIngredients.Where(x => x != null && !string.IsNullOrWhiteSpace(x.initialDescription)))
            {
                builder.Append(ingredient.DisplayName).Append(": ");
                builder.Append(ingredient.initialDescription);
                builder.AppendLine();
            }

            return builder.ToString().Trim();
        }

        private static string BuildProcessCheckFeedback(EvaluationResult result)
        {
            List<DimensionEvaluation> processDimensions = result.dimensions
                .Where(x => IsProcessMechanic(x.mechanic))
                .ToList();

            if (processDimensions.Count == 0)
            {
                return "This level does not check grinding process yet.";
            }

            bool allCorrect = processDimensions.All(x => x.isCorrect);
            float averageScore = processDimensions.Average(x => x.normalizedScore);
            StringBuilder builder = new StringBuilder();
            builder.Append("Grind Process: ");
            builder.Append(allCorrect ? "Correct" : averageScore >= 0.5f ? "Close" : "Wrong");

            foreach (DimensionEvaluation dimension in processDimensions)
            {
                builder.AppendLine();
                builder.Append(dimension.mechanic).Append(": ");
                builder.Append(dimension.isCorrect ? "Correct" : dimension.feedback);
            }

            return builder.ToString();
        }

        private static string BuildProcessDiagnosticHint(RecipeLevelData level, EvaluationResult result)
        {
            ProcessFeedbackRule rule = FindProcessFeedbackRule(level, result);
            if (rule != null && !string.IsNullOrWhiteSpace(rule.hintText))
            {
                return rule.hintText;
            }

            List<DimensionEvaluation> processDimensions = result.dimensions
                .Where(x => IsProcessMechanic(x.mechanic) && IsMechanicEnabled(level, x.mechanic))
                .ToList();

            if (processDimensions.Count == 0)
            {
                return "This level has no grinding-process diagnostic rule yet.";
            }

            DimensionEvaluation firstIssue = processDimensions.FirstOrDefault(x => !x.isCorrect);
            if (firstIssue == null)
            {
                return "The current grinding process is correct. Keep this process and check the recipe structure.";
            }

            return firstIssue.feedback;
        }

        private static ProcessFeedbackRule FindProcessFeedbackRule(RecipeLevelData level, EvaluationResult result)
        {
            if (level == null || level.processFeedbackRules == null || level.processFeedbackRules.Count == 0)
            {
                return null;
            }

            return level.processFeedbackRules
                .Where(rule => MatchesProcessFeedbackRule(level, rule, result))
                .OrderByDescending(rule => rule.priority)
                .FirstOrDefault();
        }

        private static bool MatchesProcessFeedbackRule(RecipeLevelData level, ProcessFeedbackRule rule, EvaluationResult result)
        {
            if (rule == null)
            {
                return false;
            }

            foreach (MechanicType mechanic in rule.requiredCorrect)
            {
                if (!IsMechanicEnabled(level, mechanic))
                {
                    return false;
                }

                if (!IsMechanicCorrect(result, mechanic))
                {
                    return false;
                }
            }

            foreach (MechanicType mechanic in rule.requiredIncorrect)
            {
                if (!IsMechanicEnabled(level, mechanic))
                {
                    return false;
                }

                DimensionEvaluation dimension = result.GetDimension(mechanic);
                if (dimension == null || dimension.isCorrect)
                {
                    return false;
                }
            }

            return rule.requiredCorrect.Count > 0 || rule.requiredIncorrect.Count > 0;
        }

        private static bool IsMechanicCorrect(EvaluationResult result, MechanicType mechanic)
        {
            DimensionEvaluation dimension = result.GetDimension(mechanic);
            return dimension != null && dimension.isCorrect;
        }

        private static bool IsMechanicEnabled(RecipeLevelData level, MechanicType mechanic)
        {
            return level != null && level.enabledMechanics != null && level.enabledMechanics.IsEnabled(mechanic);
        }

        private static bool IsProcessMechanic(MechanicType mechanic)
        {
            return mechanic == MechanicType.Force
                || mechanic == MechanicType.Speed
                || mechanic == MechanicType.GrindDuration;
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
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    return font;
                }
            }
            catch (ArgumentException)
            {
            }

            return Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial", "Helvetica" }, 14);
        }

        private static bool IsAlive(UnityEngine.Object obj)
        {
            return obj != null;
        }
    }
}
