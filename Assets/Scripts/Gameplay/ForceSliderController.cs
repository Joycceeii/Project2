using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public class ForceSliderController : MonoBehaviour
    {
        public Slider forceSlider;
        public Text forceLabel;
        public UIManager uiManager;
        public ForceLevel CurrentForceLevel { get; private set; } = ForceLevel.Medium;

        private void Awake()
        {
            TryInitialize();
        }

        private void OnEnable()
        {
            TryInitialize();
        }

        private void Start()
        {
            TryInitialize();
        }

        private void OnValidate()
        {
            TryInitialize();
        }

        private void TryInitialize()
        {
            if (forceSlider != null)
            {
                forceSlider.onValueChanged.RemoveListener(OnSliderChanged);
                forceSlider.onValueChanged.AddListener(OnSliderChanged);

                forceSlider.minValue = 0f;
                forceSlider.maxValue = 1f;
                OnSliderChanged(forceSlider.value);
            }
        }

        public void Bind(Slider slider, Text label)
        {
            forceSlider = slider;
            forceLabel = label;
            TryInitialize();
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
                forceLabel.text = "Current Force: " + CurrentForceLevel;
            }
        }

        private void OnSliderChanged(float value)
        {
            ForceLevel previous = CurrentForceLevel;
            CurrentForceLevel = ValueToForce(value);
            if (forceLabel != null)
            {
                forceLabel.text = "Current Force: " + CurrentForceLevel;
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
    }
}
