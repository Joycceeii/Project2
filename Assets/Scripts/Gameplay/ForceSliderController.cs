using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public class ForceSliderController : MonoBehaviour
    {
        public Slider forceSlider;
        public Text forceLabel;
        public ForceLevel CurrentForceLevel { get; private set; } = ForceLevel.Medium;
        private bool initialized;

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
                initialized = true;

                forceSlider.minValue = 0f;
                forceSlider.maxValue = 1f;
                OnSliderChanged(forceSlider.value);
            }
        }

        public void Bind(Slider slider, Text label)
        {
            forceSlider = slider;
            forceLabel = label;
            initialized = false;
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
            if (value <= 0.33f) return ForceLevel.Light;
            if (value <= 0.66f) return ForceLevel.Medium;
            return ForceLevel.Heavy;
        }
    }
}
