using System.Collections.Generic;
using UnityEngine;

namespace TheTasteReviver
{
    public class DraggableIngredient : MonoBehaviour
    {
        public IngredientData ingredientData;
        public RecipeAttemptManager attemptManager;
        public MortarArea mortarArea;
        public Camera interactionCamera;
        public GameObject sourcePrefab;

        private Vector3 startPosition;
        private bool dragging;
        private float dragPlaneY;
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private GameObject groundVisualInstance;

        public bool IsInMortar { get; private set; }

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

        public void ReturnHome()
        {
            dragging = false;
            IsInMortar = false;
            ClearGroundState();
            transform.position = startPosition;
            dragPlaneY = startPosition.y;
        }

        public void ShowGroundState()
        {
            if (!IsInMortar || ingredientData == null || ingredientData.groundPrefab == null || groundVisualInstance != null)
            {
                return;
            }

            HideCurrentRenderers();
            groundVisualInstance = Instantiate(ingredientData.groundPrefab, transform);
            groundVisualInstance.name = "Ground Visual";
            groundVisualInstance.transform.localPosition = Vector3.zero;
            groundVisualInstance.transform.localRotation = Quaternion.identity;
            groundVisualInstance.transform.localScale = Vector3.one;

            foreach (Collider collider in groundVisualInstance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
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
            IsInMortar = accepted;
            transform.position = accepted && mortarArea != null ? mortarArea.transform.position + Vector3.up * 0.35f : startPosition;
        }

        private void HideCurrentRenderers()
        {
            hiddenRenderers.Clear();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.enabled)
                {
                    renderer.enabled = false;
                    hiddenRenderers.Add(renderer);
                }
            }
        }

        private void ClearGroundState()
        {
            if (groundVisualInstance != null)
            {
                DestroyObject(groundVisualInstance);
                groundVisualInstance = null;
            }

            foreach (Renderer renderer in hiddenRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            hiddenRenderers.Clear();
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

        private static void DestroyObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
