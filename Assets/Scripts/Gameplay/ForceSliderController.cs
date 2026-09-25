using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public class ForceSliderController : MonoBehaviour
    {
        public Slider forceSlider;
        public TMP_Text forceLabel;
        public UIManager uiManager;
        [Header("Slider Style")]
        [Tooltip("Off lets you adjust the slider fill, handle, and label RectTransforms directly in the Scene view.")]
        public bool driveSliderStyleFromInspector = false;
        public Vector2 fillAreaAnchorMin = new Vector2(0f, 0.36f);
        public Vector2 fillAreaAnchorMax = new Vector2(1f, 0.64f);
        public Vector2 fillAreaOffsetMin = new Vector2(48f, 0f);
        public Vector2 fillAreaOffsetMax = new Vector2(-48f, 0f);
        public Vector2 handleSize = new Vector2(34f, 56f);
        public Vector2 handleAreaOffsetMin = new Vector2(38f, -2f);
        public Vector2 handleAreaOffsetMax = new Vector2(-38f, 2f);
        public Color fillColor = Color.white;
        public Color handleColor = Color.white;
        public Vector2 labelTextInsetMin = new Vector2(58f, 13f);
        public Vector2 labelTextInsetMax = new Vector2(-58f, -13f);
        public Vector2 labelPanelSize = new Vector2(390f, 52f);
        public Vector2 labelPanelPosition = new Vector2(56f, 282f);
        public int labelFontSize = 15;
        public ForceLevel CurrentForceLevel { get; private set; } = ForceLevel.Medium;

#if UNITY_EDITOR
        private bool editorStyleQueued;
#endif

        private void Awake()
        {
            TryInitialize(false);
        }

        private void OnEnable()
        {
            TryInitialize(false);
            QueueStyleSlider();
        }

        private void Start()
        {
            TryInitialize(true);
        }

        private void OnValidate()
        {
            TryInitialize(false);
            QueueStyleSlider();
        }

        private void TryInitialize(bool applyStyle)
        {
            if (forceSlider != null)
            {
                forceSlider.onValueChanged.RemoveListener(OnSliderChanged);
                forceSlider.onValueChanged.AddListener(OnSliderChanged);

                forceSlider.minValue = 0f;
                forceSlider.maxValue = 1f;
                ApplySliderColors();
                if (applyStyle && driveSliderStyleFromInspector)
                {
                    StyleSlider();
                    StyleLabel();
                }

                OnSliderChanged(forceSlider.value);
            }
        }

        public void Bind(Slider slider, TMP_Text label)
        {
            forceSlider = slider;
            forceLabel = label;
            TryInitialize(false);
            QueueStyleSlider();
        }

        public void SetValue(float value)
        {
            if (forceSlider != null)
            {
                forceSlider.value = Mathf.Clamp01(value);
            }
            else
            {
                OnSliderChanged(value);
            }
        }

        public void ResetToDefault()
        {
            float defaultValue = 0.5f;
            if (forceSlider != null)
            {
                forceSlider.SetValueWithoutNotify(defaultValue);
            }

            CurrentForceLevel = ForceLevel.Medium;
            if (forceLabel != null)
            {
                forceLabel.text = FormatForceLabel(CurrentForceLevel);
            }
        }

        private void OnSliderChanged(float value)
        {
            ForceLevel previous = CurrentForceLevel;
            CurrentForceLevel = ValueToForce(value);
            if (forceLabel != null)
            {
                forceLabel.text = FormatForceLabel(CurrentForceLevel);
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (previous != CurrentForceLevel)
            {
                uiManager?.ShowStepFeedback(MechanicType.Force);
            }
        }

        public static ForceLevel ValueToForce(float value)
        {
            if (value < 0.34f) return ForceLevel.Light;
            if (value < 0.67f) return ForceLevel.Medium;
            return ForceLevel.Heavy;
        }

        private void StyleSlider()
        {
            if (forceSlider == null)
            {
                return;
            }

            if (forceSlider.fillRect != null)
            {
                RectTransform fillArea = forceSlider.fillRect.parent as RectTransform;
                if (fillArea != null)
                {
                    fillArea.anchorMin = fillAreaAnchorMin;
                    fillArea.anchorMax = fillAreaAnchorMax;
                    fillArea.offsetMin = fillAreaOffsetMin;
                    fillArea.offsetMax = fillAreaOffsetMax;
                }
            }

            if (forceSlider.handleRect != null)
            {
                forceSlider.handleRect.sizeDelta = handleSize;
                Image handle = forceSlider.handleRect.GetComponent<Image>();
                if (handle != null)
                {
                    forceSlider.targetGraphic = handle;
                }

                RectTransform handleArea = forceSlider.handleRect.parent as RectTransform;
                if (handleArea != null)
                {
                    handleArea.anchorMin = Vector2.zero;
                    handleArea.anchorMax = Vector2.one;
                    handleArea.offsetMin = handleAreaOffsetMin;
                    handleArea.offsetMax = handleAreaOffsetMax;
                }
            }

            ApplySliderColors();
        }

        private void ApplySliderColors()
        {
            if (forceSlider == null)
            {
                return;
            }

            PanelBackgroundStyle.Apply(
                forceSlider.GetComponent<Image>(),
                PanelBackgroundKind.PowderBlue,
                0.9f);

            if (forceSlider.fillRect != null)
            {
                Image fill = forceSlider.fillRect.GetComponent<Image>();
                if (fill != null)
                {
                    PanelBackgroundStyle.Apply(fill, PanelBackgroundKind.PaleCream);
                    fill.color = fillColor;
                }
            }

            if (forceSlider.handleRect != null)
            {
                Image handle = forceSlider.handleRect.GetComponent<Image>();
                if (handle != null)
                {
                    PanelBackgroundStyle.Apply(handle, PanelBackgroundKind.PaleCream);
                    handle.color = handleColor;
                    forceSlider.targetGraphic = handle;
                }
            }
        }

        private void QueueStyleSlider()
        {
            if (forceSlider == null || !driveSliderStyleFromInspector)
            {
                return;
            }

#if UNITY_EDITOR
            if (editorStyleQueued)
            {
                return;
            }

            editorStyleQueued = true;
            UnityEditor.EditorApplication.delayCall += ApplyQueuedEditorStyle;
#endif
        }

#if UNITY_EDITOR
        private void ApplyQueuedEditorStyle()
        {
            editorStyleQueued = false;
            if (this == null || forceSlider == null)
            {
                return;
            }

            StyleSlider();
            StyleLabel();
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        private void StyleLabel()
        {
            if (forceLabel == null)
            {
                return;
            }

            HudPanelLayout managerLabelLayout = uiManager != null ? uiManager.forceLabelPanelLayout : null;
            bool updateLabelRectTransforms = uiManager == null || uiManager.driveHudLayoutFromInspector;
            Vector2 insetMin = uiManager != null ? uiManager.forceLabelTextInsetMin : labelTextInsetMin;
            Vector2 insetMax = uiManager != null ? uiManager.forceLabelTextInsetMax : labelTextInsetMax;
            Vector2 panelSize = managerLabelLayout != null ? managerLabelLayout.size : labelPanelSize;
            Vector2 panelPosition = managerLabelLayout != null ? managerLabelLayout.position : labelPanelPosition;
            int fontSize = managerLabelLayout != null && managerLabelLayout.fontSize > 0 ? managerLabelLayout.fontSize : labelFontSize;

            forceLabel.font = ThemeFontProvider.GetTmpFont(14);
            forceLabel.fontSize = fontSize;
            forceLabel.fontStyle = FontStyles.Normal;
            forceLabel.enableAutoSizing = true;
            forceLabel.fontSizeMin = 10;
            forceLabel.fontSizeMax = fontSize;
            forceLabel.alignment = TextAlignmentOptions.Center;
            forceLabel.textWrappingMode = TextWrappingModes.Normal;
            forceLabel.overflowMode = TextOverflowModes.Truncate;

            RectTransform labelRect = forceLabel.GetComponent<RectTransform>();
            if (updateLabelRectTransforms && labelRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.offsetMin = insetMin;
                labelRect.offsetMax = insetMax;
                labelRect.localRotation = Quaternion.identity;
                labelRect.localScale = Vector3.one;
            }

            RectTransform panelRect = forceLabel.transform.parent as RectTransform;
            if (updateLabelRectTransforms && panelRect != null)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.zero;
                panelRect.pivot = Vector2.zero;
                panelRect.sizeDelta = panelSize;
                panelRect.anchoredPosition = panelPosition;
                panelRect.localRotation = Quaternion.identity;
                panelRect.localScale = Vector3.one;
            }

            Image panelImage = forceLabel.transform.parent != null
                ? forceLabel.transform.parent.GetComponent<Image>()
                : null;
            PanelBackgroundStyle.Apply(panelImage, PanelBackgroundKind.PowderBlue, 0.9f);
            forceLabel.color = new Color(0.075f, 0.12f, 0.16f, 1f);
        }

        private static string FormatForceLabel(ForceLevel force)
        {
            return "Force: " + force;
        }
    }
}
