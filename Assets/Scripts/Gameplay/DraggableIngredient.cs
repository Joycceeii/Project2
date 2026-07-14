using UnityEngine;

namespace TheTasteReviver
{
    public class DraggableIngredient : MonoBehaviour
    {
        public IngredientData ingredientData;
        public RecipeAttemptManager attemptManager;
        public MortarArea mortarArea;
        public Camera interactionCamera;

        private Vector3 startPosition;
        private bool dragging;
        private float dragPlaneY;

        private void Awake()
        {
            ResetHomePosition();
            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }

            dragPlaneY = transform.position.y;
        }

        public void ResetHomePosition()
        {
            startPosition = transform.position;
            dragPlaneY = transform.position.y;
        }

        private void OnMouseDown()
        {
            dragging = true;
            dragPlaneY = transform.position.y;
        }

        private void OnMouseDrag()
        {
            if (!dragging || interactionCamera == null)
            {
                return;
            }

            if (TryGetMouseWorldPoint(out Vector3 worldPoint))
            {
                transform.position = worldPoint;
            }
        }

        private void OnMouseUp()
        {
            dragging = false;
            bool droppedInMortar = mortarArea != null && mortarArea.ContainsWorldPoint(transform.position);
            bool accepted = droppedInMortar && attemptManager != null && attemptManager.TryAddIngredient(ingredientData);
            transform.position = accepted && mortarArea != null ? mortarArea.transform.position + Vector3.up * 0.35f : startPosition;
        }

        private bool TryGetMouseWorldPoint(out Vector3 worldPoint)
        {
            Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, new Vector3(0f, dragPlaneY, 0f));
            if (plane.Raycast(ray, out float distance))
            {
                worldPoint = ray.GetPoint(distance);
                return true;
            }

            worldPoint = transform.position;
            return false;
        }
    }
}
