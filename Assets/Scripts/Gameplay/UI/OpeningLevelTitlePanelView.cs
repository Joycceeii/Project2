using System.Collections;
using TMPro;
using UnityEngine;

namespace TheTasteReviver
{
    public sealed class OpeningLevelTitlePanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        private Coroutine displayRoutine;

        public TMP_Text Label => titleLabel;

        public void Show(MonoBehaviour coroutineOwner, string text, float seconds)
        {
            if (titleLabel != null)
            {
                titleLabel.text = text ?? string.Empty;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (displayRoutine != null)
            {
                coroutineOwner.StopCoroutine(displayRoutine);
            }

            displayRoutine = coroutineOwner.StartCoroutine(HideAfter(seconds));
        }

        public void Hide(MonoBehaviour coroutineOwner)
        {
            if (displayRoutine != null && coroutineOwner != null)
            {
                coroutineOwner.StopCoroutine(displayRoutine);
                displayRoutine = null;
            }

            gameObject.SetActive(false);
        }

        public void Configure(TMP_Text label)
        {
            titleLabel = label;
        }

        private IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, seconds));
            gameObject.SetActive(false);
            displayRoutine = null;
        }
    }
}
