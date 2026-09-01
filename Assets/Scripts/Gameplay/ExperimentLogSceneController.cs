using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public Text logText;
        public Button backButton;
        public Button refreshButton;
        public Camera sceneCamera;
        private const string ReturnToGameButtonName = "Return To Game";
        private RectTransform ingredientListContent;
        private GameObject detailBackdrop;
        private Text detailText;

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

            Image background = EnsureImage(root, "Background");
            RectTransform backgroundRect = IsAlive(background) ? EnsureComponent<RectTransform>(background.transform) : null;
            StretchToParent(backgroundRect);
            if (IsAlive(background))
            {
                background.color = new Color(0.94f, 0.91f, 0.84f, 1f);
                background.transform.SetAsFirstSibling();
            }

            Text title = EnsureText(root, "Experiment Log Title", TextAnchor.MiddleCenter, 30, FontStyle.Bold);
            RectTransform titleRect = IsAlive(title) ? EnsureComponent<RectTransform>(title.transform) : null;
            if (IsAlive(titleRect))
            {
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.offsetMin = new Vector2(96f, -82f);
                titleRect.offsetMax = new Vector2(-96f, -24f);
            }

            if (IsAlive(title))
            {
                title.text = "Experiment Log";
                title.color = new Color(0.12f, 0.1f, 0.08f, 1f);
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
            Image scrollImage = EnsureImage(root, "Experiment Log Scroll View");
            if (!IsAlive(scrollImage))
            {
                return;
            }

            scrollImage.color = new Color(1f, 0.98f, 0.92f, 1f);
            RectTransform scrollRectTransform = EnsureComponent<RectTransform>(scrollImage.transform);
            if (!IsAlive(scrollRectTransform))
            {
                return;
            }

            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.offsetMin = new Vector2(72f, 92f);
            scrollRectTransform.offsetMax = new Vector2(-72f, -100f);

            scrollRect = EnsureComponent<ScrollRect>(scrollImage.transform);
            if (!IsAlive(scrollRect))
            {
                return;
            }

            Transform viewportTransform = EnsureChild(scrollImage.transform, "Viewport");
            RectTransform viewportRect = EnsureRectTransform(viewportTransform);
            StretchToParent(viewportRect);

            Image viewportImage = EnsureComponent<Image>(viewportTransform);
            if (!IsAlive(viewportImage))
            {
                return;
            }

            viewportImage.color = new Color(1f, 1f, 1f, 0.08f);

            Mask mask = EnsureComponent<Mask>(viewportTransform);
            if (!IsAlive(mask))
            {
                return;
            }

            mask.showMaskGraphic = false;

            Transform contentTransform = EnsureChild(viewportTransform, "Content");
            contentTransform.gameObject.SetActive(true);
            RectTransform contentRect = EnsureRectTransform(contentTransform);
            if (!IsAlive(contentRect))
            {
                return;
            }

            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 720f);
            ingredientListContent = contentRect;

            logText = EnsureComponent<Text>(contentTransform);
            if (!IsAlive(logText))
            {
                return;
            }

            logText.font = GetRuntimeFont();
            logText.fontSize = 18;
            logText.fontStyle = FontStyle.Normal;
            logText.color = new Color(0.12f, 0.1f, 0.08f, 1f);
            logText.alignment = TextAnchor.UpperLeft;
            logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            logText.verticalOverflow = VerticalWrapMode.Overflow;
            logText.raycastTarget = false;
            logText.enabled = false;

            ContentSizeFitter fitter = EnsureComponent<ContentSizeFitter>(contentTransform);
            if (!IsAlive(fitter))
            {
                return;
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            RectTransform textRect = EnsureComponent<RectTransform>(logText.transform);
            if (!IsAlive(textRect))
            {
                return;
            }

            textRect.offsetMin = new Vector2(24f, 0f);
            textRect.offsetMax = new Vector2(-24f, 0f);

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
            ClearChildrenExceptText(ingredientListContent);
            List<IngredientLogEntry> entries = ExperimentLogManager.BuildIngredientLogEntries();
            const float buttonWidth = 190f;
            const float buttonHeight = 42f;
            const float gapX = 14f;
            const float gapY = 14f;
            const int columns = 5;
            for (int i = 0; i < entries.Count; i++)
            {
                IngredientLogEntry entry = entries[i];
                int row = i / columns;
                int column = i % columns;
                Button button = CreateIngredientButton(ingredientListContent, entry.ingredientName, new Vector2(column * (buttonWidth + gapX), -row * (buttonHeight + gapY)), new Vector2(buttonWidth, buttonHeight));
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ShowIngredientDetail(entry));
            }

            int rows = Mathf.CeilToInt(entries.Count / (float)columns);
            ingredientListContent.sizeDelta = new Vector2(0f, Mathf.Max(720f, rows * (buttonHeight + gapY) + 48f));
        }

        private static void ClearChildrenExceptText(RectTransform parent)
        {
            if (!IsAlive(parent))
            {
                return;
            }

            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in parent)
            {
                if (child != null)
                {
                    children.Add(child.gameObject);
                }
            }

            foreach (GameObject child in children)
            {
                UnityEngine.Object.Destroy(child);
            }
        }

        private static Button CreateIngredientButton(Transform parent, string labelText, Vector2 position, Vector2 size)
        {
            Transform transform = CreateChild(parent, "Ingredient Tag - " + labelText);
            RectTransform rect = EnsureRectTransform(transform);
            if (!IsAlive(rect))
            {
                return null;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f + position.x, -24f + position.y);
            rect.sizeDelta = size;

            Image image = EnsureComponent<Image>(transform);
            if (!IsAlive(image))
            {
                return null;
            }
            image.color = new Color(0.98f, 0.95f, 0.88f, 1f);

            Button button = EnsureComponent<Button>(transform);
            if (!IsAlive(button))
            {
                return null;
            }
            button.targetGraphic = image;

            Text label = EnsureText(transform, "Label", TextAnchor.MiddleCenter, 15, FontStyle.Bold);
            if (!IsAlive(label))
            {
                return button;
            }
            RectTransform labelRect = EnsureRectTransform(label.transform);
            StretchToParent(labelRect);
            label.text = labelText;
            label.color = new Color(0.16f, 0.12f, 0.08f, 1f);
            label.raycastTarget = false;
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

            panelImage.color = new Color(1f, 0.98f, 0.92f, 1f);

            detailText = EnsureText(panelImage.transform, "Ingredient Detail Text", TextAnchor.UpperLeft, 17, FontStyle.Normal);
            if (!IsAlive(detailText))
            {
                return;
            }
            RectTransform textRect = EnsureRectTransform(detailText.transform);
            if (IsAlive(textRect))
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(28f, 24f);
                textRect.offsetMax = new Vector2(-28f, -24f);
                textRect.sizeDelta = Vector2.zero;
            }

            detailText.color = new Color(0.12f, 0.1f, 0.08f, 1f);
            detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailText.verticalOverflow = VerticalWrapMode.Overflow;
            detailText.raycastTarget = false;
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
                    builder.AppendLine(parts.Count == 0 ? "Used in this recipe." : string.Join(" | ", parts));
                    builder.AppendLine();
                }
            }

            if (entry.clues.Count > 0)
            {
                builder.AppendLine("Unlocked Clues");
                foreach (UnlockedClueRecord clue in entry.clues)
                {
                    builder.AppendLine("- " + clue.content);
                }
            }

            return builder.ToString().Trim();
        }

        private static string FormatCheckedNote(string label, string value, string status)
        {
            return label + ": " + value + " [" + (string.IsNullOrWhiteSpace(status) ? "Not Checked" : status) + "]";
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

        private static Text EnsureText(Transform parent, string name, TextAnchor anchor, int fontSize, FontStyle fontStyle)
        {
            Transform child = EnsureChild(parent, name);
            GameObject childObject = GetLiveGameObject(child);
            if (!IsAlive(childObject))
            {
                child = CreateChild(parent, name);
                childObject = GetLiveGameObject(child);
            }

            RectTransform rect = EnsureRectTransform(child);
            Text text = null;

            try
            {
                text = child.GetComponent<Text>();
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

                text = childObject.AddComponent<Text>();
            }

            if (IsAlive(rect))
            {
                rect.localScale = Vector3.one;
            }

            text.font = GetRuntimeFont();
            text.alignment = anchor;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
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

            return Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial", "Helvetica" }, 16);
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
