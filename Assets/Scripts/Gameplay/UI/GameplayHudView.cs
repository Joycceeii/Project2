using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    /// <summary>
    /// Owns the always-visible gameplay HUD. Layout and child references live on
    /// the HUD prefab; UIManager only coordinates the actions exposed here.
    /// </summary>
    public sealed class GameplayHudView : MonoBehaviour
    {
        [Header("Readouts")]
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text currentOrderLabel;
        [SerializeField] private TMP_Text currentRatioLabel;
        [SerializeField] private TMP_Text currentSpeedLabel;
        [SerializeField] private TMP_Text hintLabel;

        [Header("Actions")]
        [SerializeField] private Button experimentLogButton;
        [SerializeField] private Button evaluateButton;
        [SerializeField] private Button resetAttemptButton;
        [SerializeField] private Button newBatchButton;
        [SerializeField] private Button nextLevelButton;

        [Header("Dynamic States")]
        [SerializeField] private Color experimentLogAttentionColor = new Color(0.98f, 0.78f, 0.34f, 1f);
        [SerializeField, Range(0f, 1f)] private float experimentLogAttentionBlend = 0.42f;
        [SerializeField] private float experimentLogPulseScale = 1.08f;
        [SerializeField] private float experimentLogPulseSeconds = 0.55f;

        private Coroutine experimentLogPulseCoroutine;
        private Color experimentLogBaseColor = Color.white;
        private Vector3 experimentLogBaseScale = Vector3.one;
        private bool experimentLogVisualStateCaptured;

        public TMP_Text LevelLabel => levelLabel;
        public TMP_Text CurrentOrderLabel => currentOrderLabel;
        public TMP_Text CurrentRatioLabel => currentRatioLabel;
        public TMP_Text CurrentSpeedLabel => currentSpeedLabel;
        public TMP_Text HintLabel => hintLabel;
        public Button ExperimentLogButton => experimentLogButton;
        public Button EvaluateButton => evaluateButton;
        public Button ResetAttemptButton => resetAttemptButton;
        public Button NewBatchButton => newBatchButton;
        public Button NextLevelButton => nextLevelButton;

        private void Awake()
        {
            CaptureExperimentLogVisualState();
        }

        private void OnDisable()
        {
            StopExperimentLogPulse();
        }

        public void Bind(UIManager coordinator)
        {
            Bind(experimentLogButton, coordinator.OpenExperimentLog);
            Bind(evaluateButton, coordinator.EvaluateCurrentAttempt);
            Bind(resetAttemptButton, coordinator.ResetAttempt);
            Bind(newBatchButton, coordinator.StartNewBatch);
            Bind(nextLevelButton, coordinator.NextLevel);
        }

        public void Configure(
            TMP_Text level,
            TMP_Text order,
            TMP_Text ratio,
            TMP_Text speed,
            TMP_Text hint,
            Button experimentLog,
            Button evaluate,
            Button reset,
            Button newBatch,
            Button nextLevel)
        {
            levelLabel = level;
            currentOrderLabel = order;
            currentRatioLabel = ratio;
            currentSpeedLabel = speed;
            hintLabel = hint;
            experimentLogButton = experimentLog;
            evaluateButton = evaluate;
            resetAttemptButton = reset;
            newBatchButton = newBatch;
            nextLevelButton = nextLevel;
            experimentLogVisualStateCaptured = false;
            CaptureExperimentLogVisualState();
        }

        public void SetExperimentLogAttention(string label, string defaultLabel)
        {
            if (experimentLogButton == null)
            {
                return;
            }

            CaptureExperimentLogVisualState();
            StopExperimentLogPulse();

            bool needsAttention = !string.IsNullOrWhiteSpace(label);
            TMP_Text buttonLabel = experimentLogButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
            {
                buttonLabel.text = needsAttention ? label : defaultLabel;
            }

            Image image = experimentLogButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = needsAttention
                    ? Color.Lerp(experimentLogBaseColor, experimentLogAttentionColor, experimentLogAttentionBlend)
                    : experimentLogBaseColor;
            }

            if (needsAttention && Application.isPlaying && isActiveAndEnabled)
            {
                experimentLogPulseCoroutine = StartCoroutine(PulseExperimentLogButton());
            }
        }

        private void CaptureExperimentLogVisualState()
        {
            if (experimentLogVisualStateCaptured || experimentLogButton == null)
            {
                return;
            }

            Image image = experimentLogButton.GetComponent<Image>();
            if (image != null)
            {
                experimentLogBaseColor = image.color;
            }

            experimentLogBaseScale = experimentLogButton.transform.localScale;
            experimentLogVisualStateCaptured = true;
        }

        private void StopExperimentLogPulse()
        {
            if (experimentLogPulseCoroutine != null)
            {
                StopCoroutine(experimentLogPulseCoroutine);
                experimentLogPulseCoroutine = null;
            }

            if (experimentLogButton != null && experimentLogVisualStateCaptured)
            {
                experimentLogButton.transform.localScale = experimentLogBaseScale;
            }
        }

        private IEnumerator PulseExperimentLogButton()
        {
            Transform target = experimentLogButton.transform;
            float duration = Mathf.Max(0.1f, experimentLogPulseSeconds);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                target.localScale = experimentLogBaseScale * Mathf.Lerp(1f, experimentLogPulseScale, wave);
                yield return null;
            }

            target.localScale = experimentLogBaseScale;
            experimentLogPulseCoroutine = null;
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
