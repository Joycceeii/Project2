using System.Collections;
using TMPro;
using UnityEngine;

namespace TheTasteReviver
{
    public sealed class MortarReactionPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float holdSeconds = 2f;

        private Coroutine displayRoutine;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        public void Show(string heading, string body)
        {
            if (label != null)
            {
                label.text = heading + "\n" + body;
            }

            if (displayRoutine != null)
            {
                StopCoroutine(displayRoutine);
            }

            gameObject.SetActive(true);
            displayRoutine = StartCoroutine(DisplayRoutine());
        }

        public void Hide()
        {
            if (displayRoutine != null)
            {
                StopCoroutine(displayRoutine);
                displayRoutine = null;
            }

            HideImmediate();
        }

        public void Configure(TMP_Text reactionLabel, CanvasGroup group, float duration)
        {
            label = reactionLabel;
            canvasGroup = group;
            holdSeconds = duration;
        }

        private IEnumerator DisplayRoutine()
        {
            const float fadeInSeconds = 0.18f;
            const float fadeOutSeconds = 0.38f;
            float elapsed = 0f;
            while (elapsed < fadeInSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInSeconds);
                }
                yield return null;
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, holdSeconds));
            elapsed = 0f;
            while (elapsed < fadeOutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutSeconds);
                }
                yield return null;
            }

            HideImmediate();
            displayRoutine = null;
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }
    }
}
