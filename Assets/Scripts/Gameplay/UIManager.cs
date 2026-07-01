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
        public GameObject ratioSelectionPanel;
        public Text ratioSelectionTitle;
        public Button[] ratioSelectionButtons = new Button[4];
        public Button experimentLogButton;
        public string experimentLogSceneName = "ExperimentLog";
        public bool autoUpdateLevelLabel;

        private Action<RatioLevel> pendingRatioSelection;

        public bool IsRatioSelectionOpen => ratioSelectionPanel != null && ratioSelectionPanel.activeSelf;

        private void Awake()
        {
            EnsureRatioSelectionPanel();
            EnsureExperimentLogButton();
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
            HintResult hint = hintManager != null ? hintManager.GetNextHint(attemptManager.currentLevel, attemptManager, result) : null;
            ShowFeedback("Completeness: " + result.completenessScore + "%\n" + result.mainFeedback + BuildDimensionFeedback(result));
            ShowHint(hint != null ? hint.text : string.Empty);
            logManager?.AddRecord(attemptManager, result, hint);
        }

        public void ResetAttempt()
        {
            attemptManager?.ResetAttempt();
        }

        public void NextLevel()
        {
            levelManager?.NextLevel();
        }

        public void OpenExperimentLog()
        {
            SceneManager.LoadScene(experimentLogSceneName);
        }

        public void ShowLevel(RecipeLevelData level)
        {
            if (autoUpdateLevelLabel && levelLabel != null)
            {
                levelLabel.text = level == null ? "No Level" : level.levelID + " " + level.levelName + "\n" + level.cityName;
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
                currentIngredientsLabel.text = "Current Ingredients:\n" + string.Join(", ", attempt.IngredientAmounts.Where(x => x.ingredient != null).Select(x => x.ingredient.DisplayName + " x" + x.amount));
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
                if (button == null)
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
                if (label != null)
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
            if (experimentLogButton != null)
            {
                return;
            }

            Transform existing = transform.Find("Experiment Log");
            if (existing != null)
            {
                experimentLogButton = existing.GetComponent<Button>();
                if (experimentLogButton != null)
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
    }
}
