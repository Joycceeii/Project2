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
        public HudPanelLayout ratioSelectionPanelLayout = new HudPanelLayout(420f, 260f, 0f, 0f);

        [Header("Traits Overlay")]
        [Range(0f, 1f)]
        public float ingredientTraitBackdropAlpha = 0.34f;

        [Header("HUD Text Insets")]
        public Vector2 hudTextInsetMin = new Vector2(90f, 46f);
        public Vector2 hudTextInsetMax = new Vector2(-90f, -66f);
        public Vector2 buttonTextInsetMin = new Vector2(46f, 12f);
        public Vector2 buttonTextInsetMax = new Vector2(-46f, -14f);
        public Vector2 forceLabelTextInsetMin = new Vector2(58f, 13f);
        public Vector2 forceLabelTextInsetMax = new Vector2(-58f, -13f);

        [Header("Opening Level Title")]
        public bool showOpeningLevelTitle = true;
        public float openingLevelTitleSeconds = 2.5f;

        [Header("Story Intro")]
        public bool showStoryIntroPanels = true;
        public Color storyIntroBackdropColor = new Color(0.93f, 0.88f, 0.78f, 1f);
        public Vector2 storyIntroFrameSize = new Vector2(920f, 620f);
        public Vector2 storyIntroFramePosition = new Vector2(0f, 82f);
        public Vector2 storyIntroImageSize = new Vector2(1040f, 650f);
        public Vector2 storyIntroImagePosition = new Vector2(0f, 42f);
        public Vector2 storyIntroTextSize = new Vector2(820f, 122f);
        public Vector2 storyIntroTextPosition = new Vector2(0f, -248f);
        public Vector2 storyIntroButtonSize = new Vector2(220f, 58f);
        public Vector2 storyIntroButtonPosition = new Vector2(0f, -328f);

        [Header("Ingredient Hover Tooltip")]
        public GameObject ingredientTooltipPanel;
        public TMP_Text ingredientTooltipLabel;
        public Vector2 ingredientTooltipCursorOffset = new Vector2(24f, -22f);

        [Header("Mortar Reaction")]
        public Vector2 mortarReactionSize = new Vector2(620f, 207f);
        public Vector2 mortarReactionPosition = new Vector2(0f, 126f);
        public float mortarReactionHoldSeconds = 2f;
        public float mortarReactionCooldownSeconds = 3f;
        public float mortarReactionMinimumStateSeconds = 0.85f;

        private Action<RatioLevel> pendingRatioSelection;
        private Coroutine resetPromptCoroutine;
        private Coroutine openingLevelTitleCoroutine;
        private Coroutine experimentLogPulseCoroutine;
        private Coroutine mortarReactionCoroutine;
        private float lastEvaluationTime = -999f;
        private GameObject ingredientTraitPanel;
        private GameObject ingredientTraitBackdrop;
        private bool ingredientTraitsExpanded;
        private ScrollRect ingredientTraitScrollRect;
        private RectTransform ingredientTraitViewportRect;
        private RectTransform ingredientTooltipRect;
        private UnityEngine.Object ingredientTooltipOwner;
        private GameObject mortarReactionPanel;
        private TMP_Text mortarReactionLabel;
        private CanvasGroup mortarReactionCanvasGroup;
        private GameObject storyIntroPanel;
        private Image storyIntroBackdropImage;
        private Image storyIntroImage;
        private TMP_Text storyIntroText;
        private TMP_Text storyIntroButtonText;
        private readonly List<StoryIntroPage> pendingStoryIntroPages = new List<StoryIntroPage>();
        private int currentStoryIntroIndex;
        private string lastMortarReactionKey = string.Empty;
        private float lastMortarReactionTime = -999f;
        private const string ChallengeStartTitleLine = "Tutorial complete. Challenge levels begin now.";
        private const string TutorialCompleteTitle = "Tutorial Complete";
        private const string ChallengeStartSubtitle = "Challenge levels begin now.";
        private const string ExperimentLogDefaultLabel = "Experiment Log";
        private const string ExperimentLogReviewLabel = "Review Log";
        private const string ExperimentLogNewClueLabel = "Log: New Clue";
        private static readonly Color GraphiteTextColor = new Color(0.16f, 0.14f, 0.12f, 1f);
        private static readonly Color ColoredPencilTextColor = new Color(0.98f, 0.91f, 0.8f, 1f);
        private static readonly Color CrayonTextColor = new Color(0.22f, 0.075f, 0.025f, 1f);
        private static readonly Color DryBrushTextColor = new Color(1f, 0.94f, 0.82f, 1f);
        private static readonly Color MilitaryGreenTextColor = new Color(0.96f, 0.92f, 0.8f, 1f);
        private static readonly Color OliveGreenTextColor = new Color(0.13f, 0.12f, 0.035f, 1f);
        private static readonly Color SlateBlueTextColor = new Color(0.96f, 0.93f, 0.84f, 1f);
        private static readonly Color OchreYellowTextColor = new Color(0.2f, 0.105f, 0.025f, 1f);
        private static readonly Color PowderBlueTextColor = new Color(0.075f, 0.12f, 0.16f, 1f);
        private static readonly Color PaleYellowTextColor = new Color(0.24f, 0.16f, 0.055f, 1f);
#if UNITY_EDITOR
        private bool layoutUpdateQueued;
#endif
        private static readonly HashSet<string> autoShownIngredientTraitLevels = new HashSet<string>();
        private static readonly HashSet<string> shownStoryIntroPageKeys = new HashSet<string>();

        private class StoryIntroPage
        {
            public string key;
            public string resourcePath;
            public string storyText;

            public StoryIntroPage(string key, string resourcePath, string storyText)
            {
                this.key = key;
                this.resourcePath = resourcePath;
                this.storyText = storyText;
            }
        }

        public bool IsRatioSelectionOpen => ratioSelectionPanel != null && ratioSelectionPanel.activeSelf;

        private void Awake()
        {
            EnsureRatioSelectionPanel();
            EnsureExperimentLogButton();
            EnsureActionButtons();
            EnsureIngredientTraitPanel();
            EnsureOpeningLevelTitlePanel();
            EnsureIngredientTooltip();
            EnsureMortarReactionPanel();
            EnsureStoryIntroPanel();
            NormalizeHudLayout();
            PanelBackgroundStyle.ApplyToNamedPanels(transform);
            ApplyDistinctBackgroundStyles();
        }

        private void LateUpdate()
        {
            if (ingredientTraitsExpanded)
            {
                HideIngredientTooltip(null);
                return;
            }

            if (IsAlive(ingredientTooltipPanel) && ingredientTooltipPanel.activeSelf)
            {
                PositionIngredientTooltip();
            }
        }

        public void ShowIngredientTooltip(IReadOnlyList<IngredientData> ingredients, UnityEngine.Object owner)
        {
            if (ingredientTraitsExpanded
                || (IsAlive(ingredientTraitPanel) && ingredientTraitPanel.activeInHierarchy))
            {
                HideIngredientTooltip(null);
                return;
            }

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

        public void UpdateMortarReaction(SpeedLevel observedSpeed, float stableStateSeconds)
        {
            if (stableStateSeconds < Mathf.Max(0.1f, mortarReactionMinimumStateSeconds)
                || attemptManager == null
                || attemptManager.HasEvaluated
                || attemptManager.currentLevel == null)
            {
                return;
            }

            RecipeLevelData level = attemptManager.currentLevel;
            GrindingBatch batch = attemptManager.GetCurrentBatch();
            if (batch == null || batch.ingredientsInBatch == null || batch.ingredientsInBatch.Count == 0)
            {
                return;
            }

            IngredientData batchIngredient = batch.ingredientsInBatch.FirstOrDefault(x => x != null);
            if (batchIngredient == null)
            {
                return;
            }

            if (TryGetBatchTargetSpeed(level, batch, out SpeedLevel targetSpeed, out IngredientData speedIngredient)
                && observedSpeed != targetSpeed)
            {
                IngredientData ingredient = speedIngredient != null ? speedIngredient : batchIngredient;
                bool tooFast = (int)observedSpeed > (int)targetSpeed;
                ShowMortarReaction(
                    "speed-" + ingredient.ingredientID + "-" + (tooFast ? "fast" : "slow"),
                    tooFast ? "AROMA WARNING" : "FAINT AROMA",
                    BuildSpeedReactionText(ingredient, tooFast));
                return;
            }

            ForceLevel observedForce = attemptManager.forceController != null
                ? attemptManager.forceController.CurrentForceLevel
                : ForceLevel.Medium;
            if (TryGetBatchTargetForce(level, batch, out ForceLevel targetForce, out IngredientData forceIngredient)
                && observedForce != targetForce)
            {
                IngredientData ingredient = forceIngredient != null ? forceIngredient : batchIngredient;
                bool tooHeavy = (int)observedForce > (int)targetForce;
                ShowMortarReaction(
                    "force-" + ingredient.ingredientID + "-" + (tooHeavy ? "heavy" : "light"),
                    tooHeavy ? "TEXTURE WARNING" : "FAINT RESPONSE",
                    BuildForceReactionText(ingredient, tooHeavy));
            }
        }

        private static bool TryGetBatchTargetSpeed(RecipeLevelData level, GrindingBatch batch, out SpeedLevel target, out IngredientData ingredient)
        {
            target = SpeedLevel.Medium;
            ingredient = null;
            if (level == null || level.enabledMechanics == null || !level.enabledMechanics.enableSpeed)
            {
                return false;
            }

            IReadOnlyList<LevelIngredientProfile> profiles = level.GetProfilesForMechanic(MechanicType.Speed);
            LevelIngredientProfile profile = profiles
                .FirstOrDefault(candidate => candidate != null
                    && candidate.ingredient != null
                    && batch.ingredientsInBatch.Contains(candidate.ingredient));
            if (profiles.Count > 0 && profile == null)
            {
                return false;
            }

            target = profile != null ? profile.targetSpeedLevel : level.targetSpeedLevel;
            ingredient = profile != null ? profile.ingredient : batch.ingredientsInBatch.FirstOrDefault(x => x != null);
            return true;
        }

        private static bool TryGetBatchTargetForce(RecipeLevelData level, GrindingBatch batch, out ForceLevel target, out IngredientData ingredient)
        {
            target = ForceLevel.Medium;
            ingredient = null;
            if (level == null || level.enabledMechanics == null || !level.enabledMechanics.enableForce)
            {
                return false;
            }

            IReadOnlyList<LevelIngredientProfile> profiles = level.GetProfilesForMechanic(MechanicType.Force);
            LevelIngredientProfile profile = profiles
                .FirstOrDefault(candidate => candidate != null
                    && candidate.ingredient != null
                    && batch.ingredientsInBatch.Contains(candidate.ingredient));
            if (profiles.Count > 0 && profile == null)
            {
                return false;
            }

            target = profile != null ? profile.targetForceLevel : level.targetForceLevel;
            ingredient = profile != null ? profile.ingredient : batch.ingredientsInBatch.FirstOrDefault(x => x != null);
            return true;
        }

        private static string BuildSpeedReactionText(IngredientData ingredient, bool tooFast)
        {
            if (ingredient != null && ingredient.ingredientID == "TeaLeaf")
            {
                return tooFast
                    ? "The tea leaves are warming too quickly."
                    : "Only a faint fragrance is rising from the tea leaves.";
            }

            string subject = GetSensorySubject(ingredient);
            return tooFast
                ? "Heat is building around " + subject + " too quickly."
                : "Only a faint aroma is rising from " + subject + ".";
        }

        private static string BuildForceReactionText(IngredientData ingredient, bool tooHeavy)
        {
            string subject = GetSensorySubject(ingredient);
            return tooHeavy
                ? "The aroma from " + subject + " is becoming harsh under the pressure."
                : "Very little aroma is being released from " + subject + ".";
        }

        private static string GetSensorySubject(IngredientData ingredient)
        {
            if (ingredient == null)
            {
                return "the ingredients";
            }

            switch (ingredient.ingredientID)
            {
                case "Rice": return "the rice grains";
                case "TeaLeaf": return "the tea leaves";
                case "RedBean": return "the red beans";
                case "RockSugar": return "the rock sugar crystals";
                case "Peanut": return "the peanuts";
                default: return "the " + ingredient.DisplayName.ToLowerInvariant();
            }
        }

        private void EnsureMortarReactionPanel()
        {
            if (IsAlive(mortarReactionPanel) && IsAlive(mortarReactionLabel))
            {
                return;
            }

            Transform existing = transform.Find("Mortar Reaction");
            if (IsAlive(existing))
            {
                mortarReactionPanel = existing.gameObject;
                mortarReactionLabel = existing.GetComponentInChildren<TMP_Text>(true);
                mortarReactionCanvasGroup = existing.GetComponent<CanvasGroup>();
                if (!IsAlive(mortarReactionCanvasGroup))
                {
                    mortarReactionCanvasGroup = mortarReactionPanel.AddComponent<CanvasGroup>();
                }
                PanelBackgroundStyle.Apply(
                    mortarReactionPanel.GetComponent<Image>(),
                    PanelBackgroundKind.DryBrushBrown);
                mortarReactionPanel.SetActive(false);
                return;
            }

            mortarReactionPanel = new GameObject("Mortar Reaction");
            mortarReactionPanel.transform.SetParent(transform, false);
            RectTransform panelRect = mortarReactionPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = mortarReactionSize;
            panelRect.anchoredPosition = mortarReactionPosition;

            Image image = mortarReactionPanel.AddComponent<Image>();
            PanelBackgroundStyle.Apply(image, PanelBackgroundKind.DryBrushBrown);
            image.raycastTarget = false;

            mortarReactionCanvasGroup = mortarReactionPanel.AddComponent<CanvasGroup>();
            mortarReactionCanvasGroup.alpha = 0f;
            mortarReactionCanvasGroup.blocksRaycasts = false;
            mortarReactionCanvasGroup.interactable = false;

            mortarReactionLabel = CreateRuntimeText(
                mortarReactionPanel.transform,
                "Mortar Reaction Text",
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleCenter,
                23);
            RectTransform textRect = mortarReactionLabel.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(94f, 60f);
            textRect.offsetMax = new Vector2(-94f, -60f);
            mortarReactionLabel.color = new Color(1f, 0.94f, 0.82f, 1f);
            mortarReactionLabel.alignment = TextAlignmentOptions.Center;
            mortarReactionLabel.fontStyle = FontStyles.Normal;
            mortarReactionLabel.enableAutoSizing = true;
            mortarReactionLabel.fontSizeMin = 16f;
            mortarReactionLabel.fontSizeMax = 23f;
            mortarReactionLabel.richText = true;
            mortarReactionLabel.textWrappingMode = TextWrappingModes.Normal;
            mortarReactionLabel.overflowMode = TextOverflowModes.Truncate;
            mortarReactionLabel.raycastTarget = false;
            mortarReactionPanel.SetActive(false);
        }

        private void ShowMortarReaction(string key, string heading, string body)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            bool sameRecentReaction = key == lastMortarReactionKey
                && Time.unscaledTime - lastMortarReactionTime < Mathf.Max(0.1f, mortarReactionCooldownSeconds);
            if (sameRecentReaction)
            {
                return;
            }

            EnsureMortarReactionPanel();
            if (!IsAlive(mortarReactionPanel) || !IsAlive(mortarReactionLabel))
            {
                return;
            }

            lastMortarReactionKey = key;
            lastMortarReactionTime = Time.unscaledTime;
            mortarReactionLabel.text = "<size=16><color=#D9A441><b>" + heading + "</b></color></size>\n" + body;
            mortarReactionPanel.SetActive(true);
            mortarReactionPanel.transform.SetAsLastSibling();

            if (mortarReactionCoroutine != null)
            {
                StopCoroutine(mortarReactionCoroutine);
            }

            mortarReactionCoroutine = StartCoroutine(ShowMortarReactionRoutine());
        }

        private IEnumerator ShowMortarReactionRoutine()
        {
            const float fadeInSeconds = 0.18f;
            const float fadeOutSeconds = 0.38f;
            float elapsed = 0f;
            while (elapsed < fadeInSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (IsAlive(mortarReactionCanvasGroup))
                {
                    mortarReactionCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInSeconds);
                }
                yield return null;
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, mortarReactionHoldSeconds));
            elapsed = 0f;
            while (elapsed < fadeOutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (IsAlive(mortarReactionCanvasGroup))
                {
                    mortarReactionCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutSeconds);
                }
                yield return null;
            }

            if (IsAlive(mortarReactionPanel))
            {
                mortarReactionPanel.SetActive(false);
            }
            mortarReactionCoroutine = null;
        }

        private void HideMortarReaction()
        {
            if (mortarReactionCoroutine != null)
            {
                StopCoroutine(mortarReactionCoroutine);
                mortarReactionCoroutine = null;
            }

            if (IsAlive(mortarReactionPanel))
            {
                mortarReactionPanel.SetActive(false);
            }

            if (IsAlive(mortarReactionCanvasGroup))
            {
                mortarReactionCanvasGroup.alpha = 0f;
            }

            lastMortarReactionKey = string.Empty;
            lastMortarReactionTime = -999f;
        }

        private void EnsureIngredientTooltip()
        {
            if (IsAlive(ingredientTooltipPanel) && IsAlive(ingredientTooltipLabel))
            {
                ingredientTooltipRect = ingredientTooltipPanel.transform as RectTransform;
                ingredientTooltipPanel.SetActive(false);
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
            ingredientTooltipRect.sizeDelta = new Vector2(380f, 170f);

            Image image = ingredientTooltipPanel.AddComponent<Image>();
            PanelBackgroundStyle.Apply(image, 0.97f);
            image.raycastTarget = false;

            ingredientTooltipLabel = CreateRuntimeText(
                ingredientTooltipPanel.transform,
                "Ingredient Hover Tooltip Text",
                Vector2.zero,
                Vector2.zero,
                TextAnchor.UpperLeft,
                18);
            RectTransform labelRect = ingredientTooltipLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(56f, 34f);
            labelRect.offsetMax = new Vector2(-56f, -40f);
            ingredientTooltipLabel.fontStyle = FontStyles.Normal;
            ingredientTooltipLabel.enableAutoSizing = true;
            ingredientTooltipLabel.fontSizeMin = 14f;
            ingredientTooltipLabel.fontSizeMax = 18f;
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
            return "<b>" + displayName + "</b>";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying
                || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                || layoutUpdateQueued)
            {
                return;
            }

            layoutUpdateQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                layoutUpdateQueued = false;
                if (this == null)
                {
                    return;
                }

                if (driveHudLayoutFromInspector)
                {
                    NormalizeHudLayout();
                }
                else
                {
                    PanelBackgroundStyle.ApplyToNamedPanels(transform);
                    ApplyDistinctBackgroundStyles();
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
            HideMortarReaction();
            ShowFeedback(string.Empty);
            RecipeLevelData level = attemptManager != null ? attemptManager.currentLevel : null;
            ShowHint(BuildLevelStartGuidance(level));
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
            Scene experimentLogScene = SceneManager.GetSceneByName(experimentLogSceneName);
            if (experimentLogScene.IsValid() && experimentLogScene.isLoaded)
            {
                return;
            }

            HideIngredientTooltip(null);
            SceneManager.LoadScene(experimentLogSceneName, LoadSceneMode.Additive);
        }

        public void ResumeFromExperimentLog()
        {
            HideOpeningLevelTitle();
            RefreshAttemptPanels(attemptManager);
            RecipeLevelData level = attemptManager != null ? attemptManager.currentLevel : null;
            ShowHint(BuildLevelStartGuidance(level, true));
        }

        public void ShowLevel(RecipeLevelData level)
        {
            HideMortarReaction();
            ApplyLevelPanelVisibility(level);

            if (levelLabel != null)
            {
                levelLabel.text = BuildCollapsedLevelTitle(level);
            }
            bool returnedFromExperimentLog = GameSceneReturnState.ConsumeSkipOpeningLevelTitle();
            if (returnedFromExperimentLog)
            {
                HideOpeningLevelTitle();
            }
            else
            {
                ShowOpeningLevelTitle(level);
            }

            RefreshAttemptPanels(attemptManager);
            ShowHint(BuildLevelStartGuidance(level, returnedFromExperimentLog));

            ShowIngredientTraits(level);
            if (!returnedFromExperimentLog)
            {
                TryShowStoryIntro(level);
            }
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

            if (result != null && result.judgement != JudgementResult.Correct)
            {
                string reminder = BuildExperimentLogReminder(attemptManager != null ? attemptManager.currentLevel : null, false);
                if (!string.IsNullOrWhiteSpace(reminder))
                {
                    displayedText += "\n\n" + reminder;
                    SetExperimentLogAttention(ExperimentLogReviewLabel);
                }
            }
            else if (!string.IsNullOrWhiteSpace(permanentHint))
            {
                SetExperimentLogAttention(ExperimentLogNewClueLabel);
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

        private string BuildLevelStartGuidance(RecipeLevelData level, bool returnedFromExperimentLog = false)
        {
            SetExperimentLogAttention(null);
            string text = BuildLevelStartHint(level);
            if (returnedFromExperimentLog)
            {
                return text + "\nExperiment Log reviewed. Apply one saved note at a time, then test the recipe.";
            }

            string reminder = BuildExperimentLogReminder(level, true);
            if (!string.IsNullOrWhiteSpace(reminder))
            {
                SetExperimentLogAttention(ExperimentLogReviewLabel);
                return text + "\n" + reminder;
            }

            if (level != null && level.levelID == "L01")
            {
                return text + "\nCorrect recipes save reusable clues in the Experiment Log for later levels.";
            }

            return text;
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

        private static string BuildExperimentLogReminder(RecipeLevelData level, bool beforeAttempt)
        {
            List<string> ingredientNames = GetRelevantExperimentLogIngredientNames(level);
            if (ingredientNames.Count == 0)
            {
                return beforeAttempt
                    ? string.Empty
                    : "Open the Experiment Log to review this attempt before changing the recipe.";
            }

            string names = FormatList(ingredientNames);
            return beforeAttempt
                ? "Experiment Log reminder: review your saved notes for " + names + " before your first attempt."
                : "Still unsure? Open the Experiment Log and compare this attempt with your saved notes for " + names + ".";
        }

        private static List<string> GetRelevantExperimentLogIngredientNames(RecipeLevelData level)
        {
            if (level == null || level.availableIngredients == null)
            {
                return new List<string>();
            }

            HashSet<string> clueIngredientIds = new HashSet<string>(
                ExperimentLogManager.UnlockedClues
                    .Where(clue => clue != null && clue.relatedIngredientIDs != null)
                    .SelectMany(clue => clue.relatedIngredientIDs)
                    .Where(id => !string.IsNullOrWhiteSpace(id)));

            return level.availableIngredients
                .Where(ingredient => ingredient != null
                    && !string.IsNullOrWhiteSpace(ingredient.ingredientID)
                    && clueIngredientIds.Contains(ingredient.ingredientID))
                .Select(ingredient => ingredient.DisplayName)
                .Distinct()
                .ToList();
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
            ingredientTraitsExpanded = expanded && hasText;

            if (ingredientTraitsExpanded)
            {
                HideIngredientTooltip(null);
            }

            if (IsAlive(ingredientTraitBackdrop))
            {
                ingredientTraitBackdrop.SetActive(ingredientTraitsExpanded);
                if (ingredientTraitsExpanded)
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
                ingredientTraitPanel.SetActive(ingredientTraitsExpanded);
                if (ingredientTraitsExpanded)
                {
                    ingredientTraitPanel.transform.SetAsLastSibling();

                    EnsureIngredientTraitScrollArea();
                    if (IsAlive(ingredientTraitScrollRect))
                    {
                        Canvas.ForceUpdateCanvases();
                        RefreshIngredientTraitScrollContent();
                        ingredientTraitScrollRect.StopMovement();
                        ingredientTraitScrollRect.verticalNormalizedPosition = 1f;
                    }
                }
            }

            if (IsAlive(ingredientTraitToggleButton))
            {
                ingredientTraitToggleButton.gameObject.SetActive(!ingredientTraitsExpanded && hasText);
                if (!ingredientTraitsExpanded && hasText)
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
                StyleRatioSelectionContents();
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
            PanelBackgroundStyle.Apply(panelImage, PanelBackgroundKind.SlateBlue, 0.94f);

            ratioSelectionTitle = CreateRuntimeText(ratioSelectionPanel.transform, "Ratio Selection Title", new Vector2(320f, 44f), new Vector2(0f, 72f), TextAnchor.MiddleCenter, 22);
            ratioSelectionButtons = new Button[4];

            for (int i = 0; i < ratioSelectionButtons.Length; i++)
            {
                float x = i % 2 == 0 ? -88f : 88f;
                float y = i < 2 ? 12f : -52f;
                ratioSelectionButtons[i] = CreateRuntimeButton(ratioSelectionPanel.transform, "Ratio Option " + (i + 1), new Vector2(150f, 46f), new Vector2(x, y));
            }

            StyleRatioSelectionContents();

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
                EnsureButtonLabel(experimentLogButton, ExperimentLogDefaultLabel);
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
                    EnsureButtonLabel(experimentLogButton, ExperimentLogDefaultLabel);
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

        private void SetExperimentLogAttention(string label)
        {
            if (!IsAlive(experimentLogButton))
            {
                return;
            }

            if (experimentLogPulseCoroutine != null)
            {
                StopCoroutine(experimentLogPulseCoroutine);
                experimentLogPulseCoroutine = null;
            }

            RectTransform buttonRect = experimentLogButton.transform as RectTransform;
            if (IsAlive(buttonRect))
            {
                buttonRect.localScale = Vector3.one;
            }

            bool needsAttention = !string.IsNullOrWhiteSpace(label);
            EnsureButtonLabel(experimentLogButton, needsAttention ? label : ExperimentLogDefaultLabel);
            ApplyButtonStyle(experimentLogButton);

            Image image = experimentLogButton.GetComponent<Image>();
            if (needsAttention && IsAlive(image))
            {
                image.color = Color.Lerp(image.color, new Color(0.98f, 0.78f, 0.34f, 1f), 0.42f);
            }

            if (needsAttention && Application.isPlaying && isActiveAndEnabled)
            {
                experimentLogPulseCoroutine = StartCoroutine(PulseExperimentLogButton());
            }
        }

        private IEnumerator PulseExperimentLogButton()
        {
            RectTransform buttonRect = IsAlive(experimentLogButton)
                ? experimentLogButton.transform as RectTransform
                : null;
            if (!IsAlive(buttonRect))
            {
                experimentLogPulseCoroutine = null;
                yield break;
            }

            const float duration = 1.4f;
            float elapsed = 0f;
            while (elapsed < duration && IsAlive(buttonRect))
            {
                elapsed += Time.unscaledDeltaTime;
                float pulse = Mathf.Sin(elapsed / duration * Mathf.PI * 4f);
                buttonRect.localScale = Vector3.one * (1f + Mathf.Max(0f, pulse) * 0.045f);
                yield return null;
            }

            if (IsAlive(buttonRect))
            {
                buttonRect.localScale = Vector3.one;
            }

            experimentLogPulseCoroutine = null;
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
            ApplyDistinctBackgroundStyles();
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
                if (!string.Equals(objectName, "Ratio Selection Panel", StringComparison.OrdinalIgnoreCase))
                {
                    PanelBackgroundStyle.Apply(target.GetComponent<Image>());
                }
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
                ConfigureOpeningLevelTitlePanel(openingLevelTitleLabel, false);
                openingLevelTitleLabel.transform.parent.gameObject.SetActive(false);
                return;
            }

            Transform existing = transform.Find("Opening Level Title Panel/Opening Level Title Text");
            if (IsAlive(existing))
            {
                openingLevelTitleLabel = existing.GetComponent<TMP_Text>();
                if (IsAlive(openingLevelTitleLabel))
                {
                    ConfigureOpeningLevelTitlePanel(openingLevelTitleLabel, false);
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
            panelRect.sizeDelta = new Vector2(1120f, 300f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panel.AddComponent<Image>();
            PanelBackgroundStyle.Apply(panelImage, PanelBackgroundKind.PaleYellow, 0.96f);

            openingLevelTitleLabel = CreateRuntimeText(panel.transform, "Opening Level Title Text", Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 34);
            ConfigureOpeningLevelTitlePanel(openingLevelTitleLabel, true);
            panel.SetActive(false);
        }

        private void ConfigureOpeningLevelTitlePanel(TMP_Text titleLabel, bool applyFallbackLayout)
        {
            if (!IsAlive(titleLabel) || !applyFallbackLayout)
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
                PanelBackgroundStyle.Apply(panelImage, PanelBackgroundKind.PaleYellow, 0.96f);
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

            const int titleFontSize = 34;
            titleLabel.font = ThemeFontProvider.GetTmpFont(titleFontSize);
            titleLabel.color = PaleYellowTextColor;
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.enableAutoSizing = false;
            titleLabel.fontSize = titleFontSize;
            titleLabel.fontSizeMin = titleFontSize;
            titleLabel.fontSizeMax = titleFontSize;
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

            if (IsExperimentLogPracticeLevel(level))
            {
                return "Log Practice";
            }

            return TryGetLevelNumber(level, out int number) ? "Level " + number : level.levelID;
        }

        private static string BuildFullLevelTitle(RecipeLevelData level)
        {
            if (level == null)
            {
                return "No Level";
            }

            return string.IsNullOrWhiteSpace(level.levelName)
                ? level.levelID
                : level.levelName.Trim();
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
                panel.SetActive(true);
                yield return new WaitForSecondsRealtime(Mathf.Max(1.5f, openingLevelTitleSeconds * 0.5f));
                panel.SetActive(false);
                yield return new WaitForSecondsRealtime(0.18f);
            }

            openingLevelTitleLabel.text = BuildFullLevelTitle(level);
            panel.SetActive(true);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, openingLevelTitleSeconds));
            panel.SetActive(false);
            openingLevelTitleCoroutine = null;
        }

        private static bool IsTutorialCompleteTitleLevel(RecipeLevelData level)
        {
            return level != null && string.Equals(level.levelID, "L07", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExperimentLogPracticeLevel(RecipeLevelData level)
        {
            return level != null
                && !string.IsNullOrWhiteSpace(level.levelID)
                && level.levelID.IndexOf("Log_Practice", StringComparison.OrdinalIgnoreCase) >= 0;
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

            PanelBackgroundKind backgroundKind = PanelBackgroundKind.Default;
            if (button.gameObject.name == "Evaluate")
            {
                backgroundKind = PanelBackgroundKind.ColoredPencilRed;
            }
            else if (button.gameObject.name == "Next Level")
            {
                backgroundKind = PanelBackgroundKind.OchreYellow;
            }
            else if (button.gameObject.name == "New Batch"
                || button.gameObject.name.StartsWith("Ratio Option", StringComparison.Ordinal))
            {
                backgroundKind = PanelBackgroundKind.OliveGreen;
            }
            else if (button.gameObject.name == "Experiment Log")
            {
                backgroundKind = PanelBackgroundKind.MilitaryGreen;
            }
            else if (button.gameObject.name == "Ingredient Traits Toggle")
            {
                backgroundKind = PanelBackgroundKind.OliveGreen;
            }

            PanelBackgroundStyle.Apply(button.GetComponent<Image>(), backgroundKind);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (IsAlive(label))
            {
                label.font = ThemeFontProvider.GetTmpFont();
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = GetTextColor(backgroundKind);
            }
        }

        private void ApplyDistinctBackgroundStyles()
        {
            ApplyTextPanelBackground(levelLabel, PanelBackgroundKind.OchreYellow);
            ApplyTextPanelBackground(openingLevelTitleLabel, PanelBackgroundKind.PaleYellow);
            ApplyTextPanelBackground(currentOrderLabel, PanelBackgroundKind.MilitaryGreen);
            ApplyTextPanelBackground(currentRatioLabel, PanelBackgroundKind.OliveGreen);
            ApplyTextPanelBackground(currentSpeedLabel, PanelBackgroundKind.SlateBlue);
            ApplyTextPanelBackground(hintLabel, PanelBackgroundKind.SlateBlue);
            ApplyNamedPanelBackground("Force Slider", PanelBackgroundKind.PowderBlue);
            ApplyNamedPanelBackground("Force Label Panel", PanelBackgroundKind.PowderBlue);
            ApplyButtonStyle(experimentLogButton);
            ApplyButtonStyle(evaluateButton);
            ApplyButtonStyle(resetAttemptButton);
            ApplyButtonStyle(newBatchButton);
            ApplyButtonStyle(nextLevelButton);
            ApplyButtonStyle(ingredientTraitToggleButton);
            StyleRatioSelectionContents();

            if (IsAlive(levelLabel))
            {
                levelLabel.color = OchreYellowTextColor;
            }
            if (IsAlive(openingLevelTitleLabel))
            {
                openingLevelTitleLabel.color = PaleYellowTextColor;
            }
            SetTextColor(currentOrderLabel, MilitaryGreenTextColor);
            SetTextColor(currentSpeedLabel, SlateBlueTextColor);
            SetTextColor(hintLabel, SlateBlueTextColor);

            if (IsAlive(mortarReactionPanel))
            {
                PanelBackgroundStyle.Apply(
                    mortarReactionPanel.GetComponent<Image>(),
                    PanelBackgroundKind.DryBrushBrown);
            }
            if (IsAlive(mortarReactionLabel))
            {
                mortarReactionLabel.color = DryBrushTextColor;
            }
        }

        private void StyleRatioSelectionContents()
        {
            SetTextColor(ratioSelectionTitle, Color.black);
            if (ratioSelectionButtons == null)
            {
                return;
            }

            foreach (Button button in ratioSelectionButtons)
            {
                ApplyButtonStyle(button);
                SetTextColor(button != null ? button.GetComponentInChildren<TMP_Text>(true) : null, Color.white);
            }
        }

        private static void SetTextColor(TMP_Text label, Color color)
        {
            if (IsAlive(label))
            {
                label.color = color;
            }
        }

        private static Color GetTextColor(PanelBackgroundKind kind)
        {
            switch (kind)
            {
                case PanelBackgroundKind.CrayonOrange:
                    return CrayonTextColor;
                case PanelBackgroundKind.ColoredPencilRed:
                    return ColoredPencilTextColor;
                case PanelBackgroundKind.MilitaryGreen:
                    return MilitaryGreenTextColor;
                case PanelBackgroundKind.OliveGreen:
                    return OliveGreenTextColor;
                case PanelBackgroundKind.SlateBlue:
                    return SlateBlueTextColor;
                case PanelBackgroundKind.OchreYellow:
                    return OchreYellowTextColor;
                case PanelBackgroundKind.PowderBlue:
                    return PowderBlueTextColor;
                case PanelBackgroundKind.PaleYellow:
                    return PaleYellowTextColor;
                case PanelBackgroundKind.DryBrushBrown:
                    return DryBrushTextColor;
                default:
                    return GraphiteTextColor;
            }
        }

        private void ApplyNamedPanelBackground(string objectName, PanelBackgroundKind kind)
        {
            Transform target = transform.Find(objectName);
            if (IsAlive(target))
            {
                PanelBackgroundStyle.Apply(target.GetComponent<Image>(), kind);
                SetTextColor(target.GetComponentInChildren<TMP_Text>(true), GetTextColor(kind));
            }
        }

        private static void ApplyTextPanelBackground(TMP_Text label, PanelBackgroundKind kind)
        {
            if (IsAlive(label) && IsAlive(label.transform.parent))
            {
                PanelBackgroundStyle.Apply(label.transform.parent.GetComponent<Image>(), kind);
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
                    while (panelTransform.parent != null
                        && panelTransform.name != "Ingredient Traits Expanded Panel"
                        && panelTransform.name != "Ingredient Traits Panel")
                    {
                        panelTransform = panelTransform.parent;
                    }

                    ingredientTraitPanel = panelTransform.gameObject;
                }

                EnsureIngredientTraitBackdrop();
                EnsureIngredientTraitToggleButton();
                EnsureIngredientTraitScrollArea();
                return;
            }

            Transform existing = transform.Find("Ingredient Traits Expanded Panel/Ingredient Traits Text");
            if (!IsAlive(existing))
            {
                existing = transform.Find("Ingredient Traits Panel/Ingredient Traits Text");
            }
            if (!IsAlive(existing))
            {
                return;
            }

            ingredientTraitLabel = existing.GetComponent<TMP_Text>();
            if (!IsAlive(ingredientTraitLabel))
            {
                return;
            }

            ingredientTraitPanel = ingredientTraitLabel.transform.parent != null
                ? ingredientTraitLabel.transform.parent.gameObject
                : ingredientTraitLabel.gameObject;
            EnsureIngredientTraitBackdrop();
            EnsureIngredientTraitToggleButton();
            EnsureIngredientTraitScrollArea();
        }

        private void EnsureIngredientTraitScrollArea()
        {
            if (!IsAlive(ingredientTraitPanel) || !IsAlive(ingredientTraitLabel))
            {
                return;
            }

            RectTransform panelRect = ingredientTraitPanel.GetComponent<RectTransform>();
            if (!IsAlive(panelRect))
            {
                return;
            }

            ingredientTraitScrollRect = ingredientTraitPanel.GetComponent<ScrollRect>();
            if (!IsAlive(ingredientTraitScrollRect))
            {
                ingredientTraitScrollRect = ingredientTraitPanel.AddComponent<ScrollRect>();
            }

            Transform viewport = ingredientTraitPanel.transform.Find("Ingredient Traits Viewport");
            if (!IsAlive(viewport))
            {
                GameObject viewportObject = new GameObject("Ingredient Traits Viewport");
                viewportObject.transform.SetParent(ingredientTraitPanel.transform, false);
                viewport = viewportObject.transform;
            }

            ingredientTraitViewportRect = viewport.GetComponent<RectTransform>();
            if (!IsAlive(ingredientTraitViewportRect))
            {
                ingredientTraitViewportRect = viewport.gameObject.AddComponent<RectTransform>();
            }

            ingredientTraitViewportRect.anchorMin = Vector2.zero;
            ingredientTraitViewportRect.anchorMax = Vector2.one;
            ingredientTraitViewportRect.pivot = new Vector2(0.5f, 0.5f);
            ingredientTraitViewportRect.offsetMin = new Vector2(72f, 52f);
            ingredientTraitViewportRect.offsetMax = new Vector2(-72f, -52f);
            ingredientTraitViewportRect.localRotation = Quaternion.identity;
            ingredientTraitViewportRect.localScale = Vector3.one;

            Image viewportImage = viewport.GetComponent<Image>();
            if (!IsAlive(viewportImage))
            {
                viewportImage = viewport.gameObject.AddComponent<Image>();
            }
            viewportImage.sprite = null;
            viewportImage.type = Image.Type.Simple;
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            RectMask2D mask = viewport.GetComponent<RectMask2D>();
            if (!IsAlive(mask))
            {
                viewport.gameObject.AddComponent<RectMask2D>();
            }

            RectTransform labelRect = ingredientTraitLabel.GetComponent<RectTransform>();
            if (IsAlive(labelRect) && ingredientTraitLabel.transform.parent != viewport)
            {
                ingredientTraitLabel.transform.SetParent(viewport, false);
            }

            if (IsAlive(labelRect))
            {
                labelRect.anchorMin = new Vector2(0f, 1f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.pivot = new Vector2(0.5f, 1f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.offsetMin = new Vector2(0f, labelRect.offsetMin.y);
                labelRect.offsetMax = new Vector2(0f, labelRect.offsetMax.y);
                labelRect.localRotation = Quaternion.identity;
                labelRect.localScale = Vector3.one;
            }

            ingredientTraitLabel.alignment = TextAlignmentOptions.TopLeft;
            ingredientTraitLabel.textWrappingMode = TextWrappingModes.Normal;
            ingredientTraitLabel.overflowMode = TextOverflowModes.Overflow;

            ingredientTraitScrollRect.viewport = ingredientTraitViewportRect;
            ingredientTraitScrollRect.content = labelRect;
            ingredientTraitScrollRect.horizontal = false;
            ingredientTraitScrollRect.vertical = true;
            ingredientTraitScrollRect.movementType = ScrollRect.MovementType.Clamped;
            ingredientTraitScrollRect.inertia = true;
            ingredientTraitScrollRect.decelerationRate = 0.12f;
            ingredientTraitScrollRect.scrollSensitivity = 42f;
        }

        private void RefreshIngredientTraitScrollContent()
        {
            if (!IsAlive(ingredientTraitLabel) || !IsAlive(ingredientTraitViewportRect))
            {
                return;
            }

            RectTransform labelRect = ingredientTraitLabel.GetComponent<RectTransform>();
            if (!IsAlive(labelRect))
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(labelRect);
            float viewportHeight = Mathf.Max(1f, ingredientTraitViewportRect.rect.height);
            float preferredHeight = Mathf.Max(viewportHeight, ingredientTraitLabel.preferredHeight + 12f);
            labelRect.sizeDelta = new Vector2(0f, preferredHeight);
            labelRect.anchoredPosition = Vector2.zero;
            LayoutRebuilder.ForceRebuildLayoutImmediate(labelRect);
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
                : message + "\nNew clue saved.";
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
                clues.Add(IsExperimentLogPracticeLevel(level)
                    ? "The force answer must be recovered from an earlier Experiment Log clue."
                    : BuildForceTraitClue(profile.targetForceLevel));
            }

            if (level.enabledMechanics.enableSpeed)
            {
                clues.Add(IsExperimentLogPracticeLevel(level)
                    ? "The speed answer must be recovered from the saved Tea Leaf clue."
                    : BuildSpeedTraitClue(profile.targetSpeedLevel));
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
                return "It feels like the base, so it should arrive before the brighter finishing flavors.";
            }

            if (count > 0 && profile.targetOrderIndex >= count - 1)
            {
                return "It feels like the finishing touch that rounds out the recipe.";
            }

            return "It works best after a base is present, but before the final finish.";
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

        private void EnsureStoryIntroPanel()
        {
            if (IsAlive(storyIntroPanel))
            {
                storyIntroPanel.transform.SetParent(GetStoryIntroParent(), false);
                storyIntroPanel.transform.SetAsLastSibling();
                storyIntroPanel.SetActive(false);
                return;
            }

            storyIntroPanel = new GameObject("Story Intro Panel");
            storyIntroPanel.transform.SetParent(GetStoryIntroParent(), false);
            storyIntroPanel.transform.SetAsLastSibling();

            RectTransform rootRect = storyIntroPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            storyIntroBackdropImage = storyIntroPanel.AddComponent<Image>();
            storyIntroBackdropImage.color = storyIntroBackdropColor;

            CanvasGroup canvasGroup = storyIntroPanel.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            GameObject imageFrame = new GameObject("Story Image Frame");
            imageFrame.transform.SetParent(storyIntroPanel.transform, false);
            RectTransform imageFrameRect = imageFrame.AddComponent<RectTransform>();
            imageFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageFrameRect.pivot = new Vector2(0.5f, 0.5f);
            imageFrameRect.sizeDelta = storyIntroFrameSize;
            imageFrameRect.anchoredPosition = storyIntroFramePosition;
            Image imageFrameBackground = imageFrame.AddComponent<Image>();
            PanelBackgroundStyle.Apply(imageFrameBackground, PanelBackgroundKind.PaleCream, 0.98f);

            GameObject imageObject = new GameObject("Story Image");
            imageObject.transform.SetParent(imageFrame.transform, false);
            RectTransform imageRect = imageObject.AddComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.sizeDelta = storyIntroImageSize;
            imageRect.anchoredPosition = storyIntroImagePosition;
            storyIntroImage = imageObject.AddComponent<Image>();
            storyIntroImage.color = Color.white;
            storyIntroImage.preserveAspect = true;
            storyIntroImage.raycastTarget = false;

            storyIntroText = CreateRuntimeText(
                imageFrame.transform,
                "Story Intro Text",
                storyIntroTextSize,
                storyIntroTextPosition,
                TextAnchor.UpperCenter,
                22);
            storyIntroText.color = GraphiteTextColor;
            storyIntroText.lineSpacing = 7f;
            storyIntroText.overflowMode = TextOverflowModes.Ellipsis;

            Button continueButton = CreateRuntimeButton(storyIntroPanel.transform, "Story Continue Button", storyIntroButtonSize, storyIntroButtonPosition);
            PanelBackgroundStyle.Apply(continueButton.GetComponent<Image>(), PanelBackgroundKind.OchreYellow, 0.98f);
            storyIntroButtonText = continueButton.GetComponentInChildren<TMP_Text>();
            if (IsAlive(storyIntroButtonText))
            {
                storyIntroButtonText.text = "Continue";
                storyIntroButtonText.color = OchreYellowTextColor;
                storyIntroButtonText.fontSize = 20f;
                storyIntroButtonText.fontStyle = FontStyles.Bold;
            }
            continueButton.onClick.AddListener(AdvanceStoryIntro);

            storyIntroPanel.SetActive(false);
        }

        private void TryShowStoryIntro(RecipeLevelData level)
        {
            if (!showStoryIntroPanels || level == null)
            {
                return;
            }

            pendingStoryIntroPages.Clear();
            currentStoryIntroIndex = 0;

            foreach (StoryIntroPage page in BuildStoryIntroPagesForLevel(level))
            {
                if (page == null || string.IsNullOrWhiteSpace(page.key) || shownStoryIntroPageKeys.Contains(page.key))
                {
                    continue;
                }

                Texture2D texture = Resources.Load<Texture2D>(page.resourcePath);
                if (texture == null)
                {
                    continue;
                }

                pendingStoryIntroPages.Add(page);
            }

            if (pendingStoryIntroPages.Count == 0)
            {
                return;
            }

            ShowStoryIntroPage(0);
        }

        private Transform GetStoryIntroParent()
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            return parentCanvas != null ? parentCanvas.transform : transform;
        }

        private IEnumerable<StoryIntroPage> BuildStoryIntroPagesForLevel(RecipeLevelData level)
        {
            string levelID = level.levelID ?? string.Empty;
            switch (levelID)
            {
                case "L01":
                    yield return new StoryIntroPage(
                        "Opening_Guangdong_Map",
                        "Story/Opening_Guangdong_Map",
                        "Grandma once remembered recipes by scent, not by scale or clock.\nBut the old recipe book has blurred with time.\nShe places her first marker on the Guangdong map and begins to restore the tastes hidden inside her memories.");
                    yield return new StoryIntroPage(
                        "Level01_Rice_Start",
                        "Story/Level01_Rice_Start",
                        "The first stop begins on an old arcade street.\nIn her hand is a small bag of rice, and in the recipe book only one warning remains: rice can turn burnt under the wrong pressure.\nShe starts with the simplest flavor: a clean grain memory.");
                    break;
                case "L02":
                    yield return new StoryIntroPage(
                        "Level02_Tea_Start",
                        "Story/Level02_Tea_Start",
                        "After the rice returns as soft powder, a brittle tea leaf slips from the next page.\nTea is different. Its bitterness arrives first, and sweetness follows only with patience.\nGrandma sits by the window and listens for the rhythm of the leaves.");
                    break;
            }

            // Add future transition images here, for example:
            // if (levelID == "L03") yield return new StoryIntroPage("Level03_Start", "Story/Level03_Start", "...");
        }

        private void ShowStoryIntroPage(int index)
        {
            if (!IsAlive(storyIntroPanel) || index < 0 || index >= pendingStoryIntroPages.Count)
            {
                CloseStoryIntro();
                return;
            }

            currentStoryIntroIndex = index;
            StoryIntroPage page = pendingStoryIntroPages[index];
            Texture2D texture = Resources.Load<Texture2D>(page.resourcePath);
            if (texture == null)
            {
                AdvanceStoryIntro();
                return;
            }

            storyIntroPanel.transform.SetParent(GetStoryIntroParent(), false);
            storyIntroPanel.transform.SetAsLastSibling();
            storyIntroPanel.SetActive(true);

            if (IsAlive(storyIntroBackdropImage))
            {
                storyIntroBackdropImage.color = storyIntroBackdropColor;
            }

            if (IsAlive(storyIntroImage))
            {
                storyIntroImage.sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            if (IsAlive(storyIntroText))
            {
                storyIntroText.text = page.storyText;
            }

            if (IsAlive(storyIntroButtonText))
            {
                storyIntroButtonText.text = index >= pendingStoryIntroPages.Count - 1 ? "Begin" : "Continue";
            }
        }

        private void AdvanceStoryIntro()
        {
            if (pendingStoryIntroPages.Count == 0)
            {
                CloseStoryIntro();
                return;
            }

            StoryIntroPage page = pendingStoryIntroPages[currentStoryIntroIndex];
            if (page != null && !string.IsNullOrWhiteSpace(page.key))
            {
                shownStoryIntroPageKeys.Add(page.key);
            }

            int nextIndex = currentStoryIntroIndex + 1;
            if (nextIndex >= pendingStoryIntroPages.Count)
            {
                CloseStoryIntro();
                return;
            }

            ShowStoryIntroPage(nextIndex);
        }

        private void CloseStoryIntro()
        {
            pendingStoryIntroPages.Clear();
            currentStoryIntroIndex = 0;
            if (IsAlive(storyIntroPanel))
            {
                storyIntroPanel.SetActive(false);
            }
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
            text.color = GraphiteTextColor;
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
