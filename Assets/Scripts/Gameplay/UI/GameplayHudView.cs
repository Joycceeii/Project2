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
