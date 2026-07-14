using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public class ForceSliderController : MonoBehaviour
    {
        public Slider forceSlider;
        public Text forceLabel;
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

        private void OnSliderChanged(float value)
        {
            CurrentForceLevel = ValueToForce(value);
            if (forceLabel != null)
            {
                forceLabel.text = "Current Force: " + CurrentForceLevel;
            }
        }

        public static ForceLevel ValueToForce(float value)
        {
            if (value <= 0.25f) return ForceLevel.Light;
            if (value <= 0.5f) return ForceLevel.Medium;
            if (value <= 0.75f) return ForceLevel.MediumHigh;
            return ForceLevel.Heavy;
        }
    }
}
