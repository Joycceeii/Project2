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
        public Text logText;

        private void Awake()
        {
            EnsureEventSystem();
            EnsureCanvas();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (logText != null)
            {
                logText.text = ExperimentLogManager.BuildFullLogText();
            }
        }

        public void BackToTestLevel()
        {
            SceneManager.LoadScene(testLevelSceneName);
        }

        private void EnsureCanvas()
        {
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            Transform parent;
            if (existingCanvas == null)
            {
                GameObject canvasObject = new GameObject("Experiment Log Canvas");
                existingCanvas = canvasObject.AddComponent<Canvas>();
                existingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1366f, 768f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObject.AddComponent<GraphicRaycaster>();
                parent = canvasObject.transform;
            }
            else
            {
                parent = existingCanvas.transform;
            }

            Text title = CreateText(parent, "Experiment Log Title", new Vector2(900f, 54f), new Vector2(0f, -36f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), TextAnchor.MiddleCenter, 28);
            title.text = "Experiment Log";

            logText = CreateText(parent, "Experiment Log Records", new Vector2(1040f, 560f), new Vector2(0f, -112f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), TextAnchor.UpperLeft, 18);
            logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            logText.verticalOverflow = VerticalWrapMode.Overflow;

            Button backButton = CreateButton(parent, "Back To Test Level", new Vector2(180f, 44f), new Vector2(24f, 24f), Vector2.zero, Vector2.zero);
            backButton.onClick.AddListener(BackToTestLevel);
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static Text CreateText(Transform parent, string name, Vector2 size, Vector2 position, Vector2 anchorPoint, Vector2 pivot, TextAnchor anchor, int fontSize)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorPoint;
            rect.anchorMax = anchorPoint;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Text text = textObject.AddComponent<Text>();
            text.font = GetRuntimeFont();
            text.color = Color.black;
            text.alignment = anchor;
            text.fontSize = fontSize;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 size, Vector2 position, Vector2 anchorPoint, Vector2 pivot)
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
            image.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text label = CreateText(buttonObject.transform, name + " Text", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, 16);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 0f);
            labelRect.offsetMax = new Vector2(-6f, 0f);
            labelRect.sizeDelta = Vector2.zero;
            label.text = name;
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

            return Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial", "Helvetica" }, 16);
        }
    }
}
