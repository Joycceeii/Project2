using UnityEngine;

namespace TheTasteReviver
{
    public class IngredientVisualState : MonoBehaviour
    {
        [Tooltip("Authored visual shown before grinding. Adjust this child Transform to tune its size and placement.")]
        public GameObject beforeGrinding;
        [Tooltip("Authored visual shown after grinding. Adjust this child Transform to tune its size and placement.")]
        public GameObject afterGrinding;

        public bool HasAfterGrindingVisual => afterGrinding != null;
        public GameObject AfterGrindingVisual => afterGrinding;
        public bool HasGroundDisplayVisual => afterGrinding != null || beforeGrinding != null;
        public GameObject GroundDisplayVisual => afterGrinding != null ? afterGrinding : beforeGrinding;

        private void Awake()
        {
            ShowBeforeGrinding();
        }

        public void ShowBeforeGrinding()
        {
            if (beforeGrinding != null)
            {
                beforeGrinding.SetActive(true);
            }

            if (afterGrinding != null)
            {
                afterGrinding.SetActive(false);
            }
        }

        public void ShowAfterGrinding()
        {
            // Some ingredients (for example salt, scallion, and the lotus-leaf
            // wrapper) intentionally do not have a separate ground model. In
            // that case their authored before-grinding visual is also the safe
            // ground/result display instead of falling back to legacy data.
            if (afterGrinding == null)
            {
                if (beforeGrinding != null)
                {
                    beforeGrinding.SetActive(true);
                }

                return;
            }

            if (beforeGrinding != null)
            {
                beforeGrinding.SetActive(false);
            }

            afterGrinding.SetActive(true);
        }
    }
}
