using System;
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

            if (logText == null)
            {
                return;
            }

            logText.text = ExperimentLogManager.BuildFullLogText();
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

            backButton = EnsureButton(root, "Back To Test Level", new Vector2(196f, 44f), new Vector2(24f, 24f), Vector2.zero, Vector2.zero);
            refreshButton = EnsureButton(root, "Refresh Log", new Vector2(140f, 44f), new Vector2(-24f, 24f), Vector2.one, Vector2.one);

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

            ContentSizeFitter fitter = EnsureComponent<ContentSizeFitter>(contentTransform);
            if (!IsAlive(fitter))
            {
                return;
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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
        }

        private void BindButtons()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(BackToTestLevel);
                backButton.onClick.AddListener(BackToTestLevel);
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveListener(Refresh);
                refreshButton.onClick.AddListener(Refresh);
            }
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

                if (Camera.main == null)
                {
                    cameraObject.tag = "MainCamera";
                }
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
                return;
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

        private static Button EnsureButton(Transform parent, string name, Vector2 size, Vector2 position, Vector2 anchorPoint, Vector2 pivot)
        {
            Image image = EnsureImage(parent, name);
            if (!IsAlive(image))
            {
                return null;
            }

            RectTransform rect = EnsureComponent<RectTransform>(image.transform);
            if (!IsAlive(rect))
            {
                return null;
            }

            rect.anchorMin = anchorPoint;
            rect.anchorMax = anchorPoint;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            image.color = new Color(0.29f, 0.35f, 0.28f, 1f);

            Button button = EnsureComponent<Button>(image.transform);
            if (!IsAlive(button))
            {
                return null;
            }
            button.targetGraphic = image;

            Text label = EnsureText(image.transform, name + " Text", TextAnchor.MiddleCenter, 16, FontStyle.Bold);
            if (!IsAlive(label))
            {
                return button;
            }

            RectTransform labelRect = EnsureComponent<RectTransform>(label.transform);
            StretchToParent(labelRect);
            if (IsAlive(labelRect))
            {
                labelRect.offsetMin = new Vector2(8f, 0f);
                labelRect.offsetMax = new Vector2(-8f, 0f);
            }

            label.text = name;
            label.color = Color.white;
            label.raycastTarget = false;
            return button;
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

            GameObject obj = new GameObject(name);
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
