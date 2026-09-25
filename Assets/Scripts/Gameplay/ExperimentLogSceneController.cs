using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public class ExperimentLogSceneController : MonoBehaviour
    {
        public string testLevelSceneName = "TasteRestorerTestLevel";
        public Canvas canvas;
        public ScrollRect scrollRect;
        public TMP_Text logText;
        public Button backButton;
        public Button refreshButton;
        public Camera sceneCamera;
        public Image background;
        public TMP_Text titleText;
        public TMP_Text guidanceText;
        public Button ingredientButtonTemplate;
        [SerializeField] private RectTransform ingredientListContent;
        [SerializeField] private GameObject detailBackdrop;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private ScrollRect detailScrollRect;
        private const string ReturnToGameButtonName = "Return To Game";

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureSceneObjects();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureSceneObjects();
            Refresh();
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureSceneObjects();
            Refresh();
        }

        public void EnsureSceneObjects()
        {
            if (!IsControllerSceneLoaded())
            {
                return;
            }

            ClearDestroyedReferences();
            EnsureEventSystem();
            EnsureCamera();
            EnsureCanvas();
            BindButtons();
        }

        public void Refresh()
        {
            if (!IsControllerSceneLoaded())
            {
                return;
            }

            BuildIngredientButtons();
            Canvas.ForceUpdateCanvases();

            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void BackToTestLevel()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            GameSceneReturnState.MarkReturningFromExperimentLog();
            SceneManager.LoadScene(testLevelSceneName);
        }

        private void EnsureCanvas()
        {
            if (!IsControllerSceneLoaded())
            {
                return;
            }

            if (canvas == null)
            {
                canvas = FindInControllerScene<Canvas>();
            }

            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Experiment Log Canvas");
                SceneManager.MoveGameObjectToScene(canvasObject, gameObject.scene);
                canvas = canvasObject.AddComponent<Canvas>();
                canvasObject.AddComponent<GraphicRaycaster>();
                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1366f, 768f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            canvas.gameObject.SetActive(true);
            canvas.enabled = true;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            raycaster.enabled = true;

            RectTransform canvasRect = EnsureComponent<RectTransform>(canvas.transform);
            Transform root = canvas.transform;

            if (!IsAlive(background))
            {
                background = EnsureImage(root, "Background");
                RectTransform backgroundRect = IsAlive(background) ? EnsureComponent<RectTransform>(background.transform) : null;
                StretchToParent(backgroundRect);
                PanelBackgroundStyle.Apply(background);
            }
            if (IsAlive(background)) background.transform.SetAsFirstSibling();

            if (!IsAlive(titleText))
            {
                titleText = EnsureText(root, "Experiment Log Title", TextAnchor.MiddleCenter, 30, FontStyle.Bold);
                RectTransform titleRect = IsAlive(titleText) ? EnsureComponent<RectTransform>(titleText.transform) : null;
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.offsetMin = new Vector2(96f, -82f);
                titleRect.offsetMax = new Vector2(-96f, -24f);
            }

            if (IsAlive(titleText))
            {
                titleText.text = "Experiment Log";
            }

            if (!IsAlive(guidanceText))
            {
                guidanceText = EnsureText(root, "Experiment Log Guidance", TextAnchor.MiddleCenter, 16, FontStyle.Normal);
                RectTransform guidanceRect = IsAlive(guidanceText) ? EnsureComponent<RectTransform>(guidanceText.transform) : null;
                guidanceRect.anchorMin = new Vector2(0f, 1f);
                guidanceRect.anchorMax = new Vector2(1f, 1f);
                guidanceRect.pivot = new Vector2(0.5f, 1f);
                guidanceRect.offsetMin = new Vector2(120f, -126f);
                guidanceRect.offsetMax = new Vector2(-120f, -82f);
            }

            if (IsAlive(guidanceText))
            {
                guidanceText.text = "Select an ingredient to compare saved clues with notes from earlier attempts.";
            }

            EnsureScrollView(root);

            if (backButton == null)
            {
                backButton = FindButton(root, ReturnToGameButtonName);
            }

            if (refreshButton == null)
            {
                refreshButton = FindButton(root, "Refresh Log");
            }

            if (canvasRect != null)
            {
                canvasRect.localScale = Vector3.one;
            }
        }

        private void EnsureScrollView(Transform root)
        {
            Image scrollImage = IsAlive(scrollRect) ? scrollRect.GetComponent<Image>() : null;
            bool createdScrollView = !IsAlive(scrollImage);
            if (createdScrollView) scrollImage = EnsureImage(root, "Experiment Log Scroll View");
            if (!IsAlive(scrollImage))
            {
                return;
            }

            // The page background is the only decorative frame in this view.
            scrollImage.sprite = null;
            scrollImage.type = Image.Type.Simple;
            scrollImage.color = Color.clear;
            scrollImage.raycastTarget = false;
            RectTransform scrollRectTransform = EnsureComponent<RectTransform>(scrollImage.transform);
            if (!IsAlive(scrollRectTransform))
            {
                return;
            }

            if (createdScrollView)
            {
                scrollRectTransform.anchorMin = new Vector2(0f, 0f);
                scrollRectTransform.anchorMax = new Vector2(1f, 1f);
                scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
                scrollRectTransform.offsetMin = new Vector2(72f, 92f);
                scrollRectTransform.offsetMax = new Vector2(-72f, -132f);
            }

            scrollRect = EnsureComponent<ScrollRect>(scrollImage.transform);
            if (!IsAlive(scrollRect))
            {
                return;
            }

            Transform viewportTransform = IsAlive(scrollRect.viewport)
                ? scrollRect.viewport.transform
                : EnsureChild(scrollImage.transform, "Viewport");
            RectTransform viewportRect = EnsureRectTransform(viewportTransform);
            if (createdScrollView) StretchToParent(viewportRect);

            Image viewportImage = EnsureComponent<Image>(viewportTransform);
            if (!IsAlive(viewportImage))
            {
                return;
            }

            viewportImage.sprite = null;
            viewportImage.type = Image.Type.Simple;
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;

            RectMask2D mask = EnsureComponent<RectMask2D>(viewportTransform);
            if (!IsAlive(mask))
            {
                return;
            }

            Transform contentTransform = IsAlive(ingredientListContent)
                ? ingredientListContent.transform
                : EnsureChild(viewportTransform, "Content");
            contentTransform.gameObject.SetActive(true);
            RectTransform contentRect = EnsureRectTransform(contentTransform);
            if (!IsAlive(contentRect))
            {
                return;
            }

            if (createdScrollView)
            {
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0f, 720f);
            }
            ingredientListContent = contentRect;

            bool createdLogText = !IsAlive(logText);
            if (createdLogText) logText = EnsureText(contentTransform, "Empty Message", TextAnchor.MiddleCenter, 20, FontStyle.Normal);
            if (!IsAlive(logText))
            {
                return;
            }

            if (createdLogText)
            {
                logText.font = ThemeFontProvider.GetTmpFont(18);
                logText.fontSize = 18;
                logText.fontStyle = FontStyles.Normal;
                logText.color = new Color(0.12f, 0.1f, 0.08f, 1f);
                logText.alignment = TextAlignmentOptions.TopLeft;
                logText.textWrappingMode = TextWrappingModes.Normal;
                logText.overflowMode = TextOverflowModes.Overflow;
                logText.raycastTarget = false;
            }

            RectTransform textRect = EnsureRectTransform(logText.transform);
            if (!IsAlive(textRect))
            {
                return;
            }

            if (createdLogText)
            {
                textRect.anchorMin = new Vector2(0f, 1f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.pivot = new Vector2(0.5f, 1f);
                textRect.anchoredPosition = Vector2.zero;
                textRect.sizeDelta = new Vector2(-48f, 720f);
            }

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;

            EnsureDetailPanel(root);
        }

        private void BindButtons()
        {
            if (backButton == null && canvas != null)
            {
                backButton = FindButton(canvas.transform, ReturnToGameButtonName);
            }

            if (backButton != null)
            {
                backButton.gameObject.SetActive(true);
                backButton.interactable = true;
                backButton.onClick.RemoveListener(BackToTestLevel);
                backButton.onClick.AddListener(BackToTestLevel);
                backButton.transform.SetAsLastSibling();
            }

            if (refreshButton != null)
            {
                refreshButton.gameObject.SetActive(true);
                refreshButton.interactable = true;
                refreshButton.onClick.RemoveListener(Refresh);
                refreshButton.onClick.AddListener(Refresh);
            }
        }

        private static Button FindButton(Transform root, string name)
        {
            if (!IsAlive(root) || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            Transform target = root.Find(name);
            if (!IsAlive(target))
            {
                Button[] buttons = root.GetComponentsInChildren<Button>(true);
                foreach (Button button in buttons)
                {
                    if (IsAlive(button) && button.gameObject.name == name)
                    {
                        return button;
                    }
                }

                return null;
            }

            return target.GetComponent<Button>();
        }

        private void BuildIngredientButtons()
        {
            if (!IsAlive(ingredientListContent))
            {
                EnsureSceneObjects();
            }

            if (!IsAlive(ingredientListContent))
            {
                return;
            }

            ingredientListContent.gameObject.SetActive(true);
            if (IsAlive(ingredientButtonTemplate))
            {
                ingredientButtonTemplate.gameObject.SetActive(false);
            }

            ClearIngredientButtons(
                ingredientListContent,
                logText != null ? logText.transform : null,
                ingredientButtonTemplate != null ? ingredientButtonTemplate.transform : null);
            List<IngredientLogEntry> entries = ExperimentLogManager.BuildIngredientLogEntries();
            float buttonHeight = IsAlive(ingredientButtonTemplate)
                ? ingredientButtonTemplate.GetComponent<RectTransform>().rect.height
                : 44f;
            float rowStep = buttonHeight + 10f;
            float contentHeight = Mathf.Max(720f, entries.Count * rowStep + 48f);
            ingredientListContent.sizeDelta = new Vector2(0f, contentHeight);

            if (IsAlive(logText))
            {
                bool isEmpty = entries.Count == 0;
                logText.enabled = isEmpty;
                logText.text = isEmpty
                    ? "No ingredients discovered yet.\nComplete a recipe correctly to unlock its ingredient traits."
                    : string.Empty;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                IngredientLogEntry entry = entries[i];
                Button button = CreateIngredientButton(ingredientListContent, entry.ingredientName, i * rowStep, buttonHeight);
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ShowIngredientDetail(entry));
            }
        }

        private static void ClearIngredientButtons(RectTransform parent, params Transform[] preservedChildren)
        {
            if (!IsAlive(parent))
            {
                return;
            }

            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in parent)
            {
                if (child != null && !preservedChildren.Contains(child))
                {
                    children.Add(child.gameObject);
                }
            }

            foreach (GameObject child in children)
            {
                UnityEngine.Object.Destroy(child);
            }
        }

        private Button CreateIngredientButton(Transform parent, string labelText, float verticalOffset, float height)
        {
            if (IsAlive(ingredientButtonTemplate))
            {
                Button templateInstance = Instantiate(ingredientButtonTemplate, parent, false);
                templateInstance.gameObject.name = "Ingredient Tag - " + labelText;
                templateInstance.gameObject.SetActive(true);

                RectTransform instanceRect = templateInstance.GetComponent<RectTransform>();
                if (IsAlive(instanceRect))
                {
                    instanceRect.anchoredPosition = new Vector2(
                        ingredientButtonTemplate.GetComponent<RectTransform>().anchoredPosition.x,
                        ingredientButtonTemplate.GetComponent<RectTransform>().anchoredPosition.y - verticalOffset);
                }

                TMP_Text instanceLabel = templateInstance.GetComponentInChildren<TMP_Text>(true);
                if (IsAlive(instanceLabel))
                {
                    instanceLabel.text = labelText;
                }

                return templateInstance;
            }

            Transform transform = CreateChild(parent, "Ingredient Tag - " + labelText);
            RectTransform rect = EnsureRectTransform(transform);
            if (!IsAlive(rect))
            {
                return null;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -16f - verticalOffset);
            rect.sizeDelta = new Vector2(360f, height);

            Image image = EnsureComponent<Image>(transform);
            if (!IsAlive(image))
            {
                return null;
            }
            PanelBackgroundStyle.Apply(image, PanelBackgroundKind.PaleYellow, 1f);

            Button button = EnsureComponent<Button>(transform);
            if (!IsAlive(button))
            {
                return null;
            }
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 0.88f, 0.78f, 1f);
            colors.pressedColor = new Color(0.68f, 0.76f, 0.64f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TMP_Text label = EnsureText(transform, "Label", TextAnchor.MiddleLeft, 18, FontStyle.Normal);
            if (IsAlive(label))
            {
                RectTransform labelRect = EnsureRectTransform(label.transform);
                StretchToParent(labelRect);
                labelRect.offsetMin = new Vector2(16f, 0f);
                labelRect.offsetMax = new Vector2(-16f, 0f);
                label.text = labelText;
                label.color = new Color(0.13f, 0.11f, 0.08f, 1f);
                label.enableAutoSizing = true;
                label.fontSizeMin = 13f;
                label.fontSizeMax = 18f;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Truncate;
                label.raycastTarget = false;
            }

            return button;
        }

        private void EnsureDetailPanel(Transform root)
        {
            if (!IsAlive(root))
            {
                return;
            }

            if (IsAlive(detailBackdrop))
            {
                if (IsAlive(detailText))
                {
                    ContentSizeFitter existingFitter = detailText.GetComponent<ContentSizeFitter>();
                    if (IsAlive(existingFitter))
                    {
                        existingFitter.enabled = false;
                    }
                }

                Button existingBackdropButton = detailBackdrop.GetComponent<Button>();
                if (IsAlive(existingBackdropButton))
                {
                    existingBackdropButton.onClick.RemoveAllListeners();
                    existingBackdropButton.onClick.AddListener(HideIngredientDetail);
                }
                detailBackdrop.SetActive(false);
                return;
            }

            Image backdropImage = EnsureImage(root, "Ingredient Detail Backdrop");
            detailBackdrop = backdropImage != null ? backdropImage.gameObject : null;
            if (!IsAlive(detailBackdrop))
            {
                return;
            }

            RectTransform backdropRect = EnsureRectTransform(detailBackdrop.transform);
            StretchToParent(backdropRect);
            backdropImage.color = new Color(0f, 0f, 0f, 0.28f);

            Button backdropButton = EnsureComponent<Button>(detailBackdrop.transform);
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.targetGraphic = backdropImage;
            backdropButton.onClick.RemoveAllListeners();
            backdropButton.onClick.AddListener(HideIngredientDetail);

            Image panelImage = EnsureImage(detailBackdrop.transform, "Ingredient Detail Panel");
            if (!IsAlive(panelImage))
            {
                return;
            }
            RectTransform panelRect = EnsureRectTransform(panelImage.transform);
            if (IsAlive(panelRect))
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(720f, 520f);
            }

            panelImage.sprite = null;
            panelImage.type = Image.Type.Simple;
            panelImage.color = new Color(0.97f, 0.94f, 0.86f, 0.98f);

            Button panelButton = EnsureComponent<Button>(panelImage.transform);
            panelButton.transition = Selectable.Transition.None;
            panelButton.targetGraphic = panelImage;
            panelButton.onClick.RemoveAllListeners();

            detailScrollRect = EnsureComponent<ScrollRect>(panelImage.transform);
            detailScrollRect.horizontal = false;
            detailScrollRect.vertical = true;
            detailScrollRect.movementType = ScrollRect.MovementType.Clamped;
            detailScrollRect.inertia = true;
            detailScrollRect.decelerationRate = 0.12f;
            detailScrollRect.scrollSensitivity = 42f;

            Image viewportImage = EnsureImage(panelImage.transform, "Ingredient Detail Viewport");
            if (!IsAlive(viewportImage))
            {
                return;
            }

            RectTransform viewportRect = EnsureRectTransform(viewportImage.transform);
            if (IsAlive(viewportRect))
            {
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.pivot = new Vector2(0.5f, 0.5f);
                viewportRect.offsetMin = new Vector2(44f, 36f);
                viewportRect.offsetMax = new Vector2(-44f, -36f);
            }

            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;
            EnsureComponent<RectMask2D>(viewportImage.transform);

            detailText = EnsureText(viewportImage.transform, "Ingredient Detail Text", TextAnchor.UpperLeft, 17, FontStyle.Normal);
            if (!IsAlive(detailText))
            {
                return;
            }
            RectTransform textRect = EnsureRectTransform(detailText.transform);
            if (IsAlive(textRect))
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = Vector2.zero;
                textRect.sizeDelta = new Vector2(-24f, -24f);
            }

            detailText.color = new Color(0.12f, 0.1f, 0.08f, 1f);
            detailText.textWrappingMode = TextWrappingModes.Normal;
            detailText.overflowMode = TextOverflowModes.Overflow;
            detailText.margin = new Vector4(4f, 4f, 12f, 12f);
            detailText.raycastTarget = true;

            detailScrollRect.viewport = viewportRect;
            detailScrollRect.content = textRect;
            detailBackdrop.SetActive(false);
        }

        private void ShowIngredientDetail(IngredientLogEntry entry)
        {
            EnsureDetailPanel(canvas != null ? canvas.transform : null);
            if (entry == null || detailText == null || detailBackdrop == null)
            {
                return;
            }

            detailText.text = BuildIngredientDetailText(entry);
            detailBackdrop.SetActive(true);
            detailBackdrop.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            RectTransform textRect = detailText.rectTransform;
            if (textRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
            }

            if (detailScrollRect != null)
            {
                detailScrollRect.StopMovement();
                detailScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void HideIngredientDetail()
        {
            if (detailBackdrop != null)
            {
                detailBackdrop.SetActive(false);
            }
        }

        private static string BuildIngredientDetailText(IngredientLogEntry entry)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(entry.ingredientName);
            builder.AppendLine();
            builder.AppendLine("Ingredient Traits");
            if (!string.IsNullOrWhiteSpace(entry.aromaType))
            {
                builder.AppendLine("Aroma: " + entry.aromaType);
            }
            builder.AppendLine(string.IsNullOrWhiteSpace(entry.traitDescription) ? "No traits recorded yet." : entry.traitDescription);

            if (entry.levelNotes.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Experiment Notes");
                foreach (ExperimentIngredientEntry note in entry.levelNotes)
                {
                    builder.AppendLine("[" + note.levelID + " " + note.levelName + "]");
                    List<string> parts = new List<string>();
                    if (note.checkedRatio) parts.Add(FormatCheckedNote("Amount", note.ratio, note.ratioStatus));
                    if (note.checkedOrder) parts.Add(FormatCheckedNote("Order", note.order, note.orderStatus));
                    if (note.checkedForce) parts.Add(FormatCheckedNote("Force", note.force, note.forceStatus));
                    if (note.checkedSpeed) parts.Add(FormatCheckedNote("Speed", note.speed, note.speedStatus));
                    if (note.checkedCombination) parts.Add(FormatCheckedNote("Batch", note.combination, note.combinationStatus));
                    if (!string.IsNullOrWhiteSpace(note.grindDuration)) parts.Add("Grinding Time: " + note.grindDuration);

                    if (parts.Count == 0)
                    {
                        builder.AppendLine("- Used in this recipe.");
                    }
                    else
                    {
                        foreach (string part in parts)
                        {
                            builder.AppendLine("- " + part);
                        }
                    }

                    builder.AppendLine();
                }
            }

            if (entry.clues.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Unlocked Clues");
                for (int i = 0; i < entry.clues.Count; i++)
                {
                    UnlockedClueRecord clue = entry.clues[i];
                    builder.AppendLine("Clue " + (i + 1) + ": " + clue.content);
                }
            }

            return builder.ToString().Trim();
        }

        private static string FormatCheckedNote(string label, string value, string status)
        {
            string normalizedStatus;
            if (string.Equals(status, "Correct", StringComparison.OrdinalIgnoreCase))
            {
                normalizedStatus = "CORRECT";
            }
            else if (string.Equals(status, "Incorrect", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Needs Work", StringComparison.OrdinalIgnoreCase))
            {
                normalizedStatus = "INCORRECT";
            }
            else
            {
                normalizedStatus = "NOT CHECKED";
            }

            return label + ": " + value + " [" + normalizedStatus + "]";
        }

        private void EnsureCamera()
        {
            if (!IsControllerSceneLoaded())
            {
                return;
            }

            if (sceneCamera == null)
            {
                sceneCamera = FindInControllerScene<Camera>();
            }

            if (sceneCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                SceneManager.MoveGameObjectToScene(cameraObject, gameObject.scene);
                sceneCamera = cameraObject.AddComponent<Camera>();
            }

            if (Camera.main == null || Camera.main == sceneCamera)
            {
                sceneCamera.gameObject.tag = "MainCamera";
            }

            sceneCamera.gameObject.SetActive(true);
            sceneCamera.enabled = true;
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.94f, 0.91f, 0.84f, 1f);
            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = 5f;
            sceneCamera.transform.position = new Vector3(0f, 0f, -10f);
            sceneCamera.transform.rotation = Quaternion.identity;

            if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
            {
                sceneCamera.gameObject.AddComponent<AudioListener>();
            }
        }

        private void EnsureEventSystem()
        {
            if (!IsControllerSceneLoaded())
            {
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                EventSystem existing = FindInControllerScene<EventSystem>();
                if (existing != null)
                {
                    existing.gameObject.SetActive(true);
                    return;
                }
            }

            GameObject eventSystem = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystem, gameObject.scene);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private T FindInControllerScene<T>() where T : Component
        {
            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private bool IsControllerSceneLoaded()
        {
            Scene scene = gameObject.scene;
            return scene.IsValid() && scene.isLoaded;
        }

        private void ClearDestroyedReferences()
        {
            if (canvas == null)
            {
                canvas = null;
            }

            if (scrollRect == null)
            {
                scrollRect = null;
            }

            if (logText == null)
            {
                logText = null;
            }

            if (backButton == null)
            {
                backButton = null;
            }

            if (refreshButton == null)
            {
                refreshButton = null;
            }

            if (sceneCamera == null)
            {
                sceneCamera = null;
            }
        }

        private static Image EnsureImage(Transform parent, string name)
        {
            Transform child = EnsureChild(parent, name);
            GameObject childObject = GetLiveGameObject(child);
            if (!IsAlive(childObject))
            {
                child = CreateChild(parent, name);
                childObject = GetLiveGameObject(child);
            }

            RectTransform rect = EnsureRectTransform(child);
            Image image = null;

            try
            {
                image = child.GetComponent<Image>();
            }
            catch (MissingReferenceException)
            {
                child = CreateChild(parent, name);
                childObject = GetLiveGameObject(child);
                rect = EnsureRectTransform(child);
            }

            if (!IsAlive(image))
            {
                childObject = GetLiveGameObject(child);
                if (!IsAlive(childObject))
                {
                    child = CreateChild(parent, name);
                    childObject = GetLiveGameObject(child);
                    rect = EnsureRectTransform(child);
                }

                if (!IsAlive(childObject))
                {
                    return null;
                }

                image = childObject.AddComponent<Image>();
            }

            if (IsAlive(rect))
            {
                rect.localScale = Vector3.one;
            }

            return image;
        }

        private static TMP_Text EnsureText(Transform parent, string name, TextAnchor anchor, int fontSize, FontStyle fontStyle)
        {
            Transform child = EnsureChild(parent, name);
            GameObject childObject = GetLiveGameObject(child);
            if (!IsAlive(childObject))
            {
                child = CreateChild(parent, name);
                childObject = GetLiveGameObject(child);
            }

            RectTransform rect = EnsureRectTransform(child);
            TMP_Text text = null;

            try
            {
                text = child.GetComponent<TMP_Text>();
            }
            catch (MissingReferenceException)
            {
                child = CreateChild(parent, name);
                childObject = GetLiveGameObject(child);
                rect = EnsureRectTransform(child);
            }

            if (!IsAlive(text))
            {
                childObject = GetLiveGameObject(child);
                if (!IsAlive(childObject))
                {
                    child = CreateChild(parent, name);
                    childObject = GetLiveGameObject(child);
                    rect = EnsureRectTransform(child);
                }

                if (!IsAlive(childObject))
                {
                    return null;
                }

                text = childObject.AddComponent<TextMeshProUGUI>();
            }

            if (IsAlive(rect))
            {
                rect.localScale = Vector3.one;
            }

            text.font = ThemeFontProvider.GetTmpFont(fontSize);
            text.alignment = TmpTextUtility.ToAlignment(anchor);
            text.fontSize = fontSize;
            text.fontStyle = TmpTextUtility.ToFontStyle(fontStyle);
            return text;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            if (!IsAlive(parent))
            {
                return null;
            }

            Transform child = null;
            try
            {
                child = parent.Find(name);
            }
            catch (MissingReferenceException)
            {
                child = null;
            }

            if (IsAlive(child))
            {
                return child;
            }

            return CreateChild(parent, name);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            if (!IsAlive(parent))
            {
                return null;
            }

            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private static RectTransform EnsureRectTransform(Transform transform)
        {
            if (!IsAlive(transform))
            {
                return null;
            }

            RectTransform rect = null;
            try
            {
                rect = transform.GetComponent<RectTransform>();
            }
            catch (MissingReferenceException)
            {
                return null;
            }

            if (IsAlive(rect))
            {
                return rect;
            }

            GameObject target = GetLiveGameObject(transform);
            return IsAlive(target) ? target.AddComponent<RectTransform>() : null;
        }

        private static T EnsureComponent<T>(Transform transform) where T : Component
        {
            if (!IsAlive(transform))
            {
                return null;
            }

            T component = null;
            try
            {
                component = transform.GetComponent<T>();
            }
            catch (MissingReferenceException)
            {
                return null;
            }

            if (IsAlive(component))
            {
                return component;
            }

            GameObject target = GetLiveGameObject(transform);
            return IsAlive(target) ? target.AddComponent<T>() : null;
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (!IsAlive(rect))
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static Font GetRuntimeFont()
        {
            return ThemeFontProvider.GetFont(16);
        }

        private static bool IsAlive(UnityEngine.Object obj)
        {
            return obj != null;
        }

        private static GameObject GetLiveGameObject(Component component)
        {
            if (!IsAlive(component))
            {
                return null;
            }

            try
            {
                return component.gameObject;
            }
            catch (MissingReferenceException)
            {
                return null;
            }
        }
    }
}
