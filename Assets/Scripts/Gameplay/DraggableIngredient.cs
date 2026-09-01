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
        public float dragLiftHeight = 0.55f;
        public float mortarDropHeight = 0.2f;
        public float groundVisualLift = 0.1f;
        public float groundVisualScale = 0.8f;

        private Vector3 startPosition;
        private bool dragging;
        private float dragPlaneY;
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private GameObject groundVisualInstance;
        private bool hasBeenGround;

        public bool IsInMortar { get; private set; }

        private void Awake()
        {
            RemoveRedundantLodModels(transform);
            NormalizeRuntimeVisualSettings();
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
            ReturnHome(false);
        }

        public void ReturnHome(bool keepGroundState)
        {
            dragging = false;
            IsInMortar = false;
            if (!keepGroundState)
            {
                ClearGroundState();
                hasBeenGround = false;
            }

            transform.position = startPosition;
            dragPlaneY = startPosition.y;
            if (keepGroundState && hasBeenGround)
            {
                EnsureGroundVisual();
            }
        }

        public void ShowGroundState()
        {
            if (!IsInMortar)
            {
                return;
            }

            hasBeenGround = true;
            EnsureGroundVisual();
        }

        private void EnsureGroundVisual()
        {
            if (ingredientData == null || ingredientData.groundPrefab == null || groundVisualInstance != null)
            {
                return;
            }

            HideCurrentRenderers();
            groundVisualInstance = Instantiate(ingredientData.groundPrefab, transform);
            groundVisualInstance.name = "Ground Visual";
            RemoveRedundantLodModels(groundVisualInstance.transform);
            groundVisualInstance.transform.localPosition = Vector3.zero;
            groundVisualInstance.transform.localRotation = Quaternion.identity;
            groundVisualInstance.transform.localScale = Vector3.one * Mathf.Clamp(groundVisualScale, 0.25f, 1.2f);
            FitChildRenderersToAnchor(groundVisualInstance.transform, groundVisualLift);

            foreach (Collider collider in groundVisualInstance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        private void OnMouseDown()
        {
            dragging = true;
            dragPlaneY = transform.position.y + Mathf.Max(0f, dragLiftHeight);
            transform.position = new Vector3(transform.position.x, dragPlaneY, transform.position.z);
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
            bool droppedInMortar = mortarArea != null && mortarArea.ContainsWorldPoint(GetMortarCheckPoint());
            bool accepted = droppedInMortar && attemptManager != null && attemptManager.TryAddIngredient(ingredientData);
            IsInMortar = accepted;
            if (accepted && mortarArea != null)
            {
                MoveIntoMortar();
            }
            else
            {
                transform.position = startPosition;
            }

            dragPlaneY = transform.position.y;
        }

        private Vector3 GetMortarCheckPoint()
        {
            if (mortarArea == null)
            {
                return transform.position;
            }

            return new Vector3(transform.position.x, mortarArea.transform.position.y, transform.position.z);
        }

        private void NormalizeRuntimeVisualSettings()
        {
            mortarDropHeight = Mathf.Clamp(mortarDropHeight, 0.18f, 0.22f);
            groundVisualLift = Mathf.Clamp(groundVisualLift, 0.08f, 0.14f);
            groundVisualScale = Mathf.Clamp(groundVisualScale, 0.72f, 0.9f);
        }

        private void MoveIntoMortar()
        {
            Collider mortarCollider = mortarArea.GetComponent<Collider>();
            Vector3 center = IsAlive(mortarCollider) ? mortarCollider.bounds.center : mortarArea.transform.position;
            float visibleY = mortarArea.transform.position.y + mortarDropHeight;
            if (IsAlive(mortarCollider))
            {
                visibleY = Mathf.Max(visibleY, mortarCollider.bounds.max.y + 0.015f);
            }

            transform.position = new Vector3(center.x, visibleY, center.z);
            FitSelfRenderersToWorldAnchor(new Vector3(center.x, visibleY, center.z));
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

        private void FitSelfRenderersToWorldAnchor(Vector3 worldAnchor)
        {
            Bounds bounds;
            if (!TryCalculateWorldBounds(transform, out bounds) || bounds.size.sqrMagnitude <= 0.0001f)
            {
                transform.position = worldAnchor;
                return;
            }

            Vector3 offset = new Vector3(
                worldAnchor.x - bounds.center.x,
                worldAnchor.y - bounds.min.y,
                worldAnchor.z - bounds.center.z);
            transform.position += offset;
        }

        private static bool TryCalculateWorldBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                bounds = new Bounds(Vector3.zero, Vector3.zero);
                return false;
            }

            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                if (IsAlive(renderer))
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return true;
        }

        private static void FitChildRenderersToAnchor(Transform child, float lift)
        {
            if (!IsAlive(child) || child.parent == null)
            {
                return;
            }

            Bounds bounds;
            if (!TryCalculateParentLocalBounds(child.parent, child, out bounds) || bounds.size.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 offset = new Vector3(-bounds.center.x, Mathf.Max(0f, lift) - bounds.min.y, -bounds.center.z);
            child.localPosition += offset;
        }

        private static bool TryCalculateParentLocalBounds(Transform parent, Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                bounds = new Bounds(Vector3.zero, Vector3.zero);
                return false;
            }

            bounds = new Bounds(parent.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                if (!IsAlive(renderer))
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                bounds.Encapsulate(parent.InverseTransformPoint(worldBounds.min));
                bounds.Encapsulate(parent.InverseTransformPoint(worldBounds.max));
            }

            return true;
        }

        private static void RemoveRedundantLodModels(Transform root)
        {
            if (!IsAlive(root))
            {
                return;
            }

            Transform lodRoot = FindChildRecursive(root, "lod");
            if (!IsAlive(lodRoot))
            {
                return;
            }

            Transform keep = lodRoot.Find("model_LOD0");
            List<GameObject> remove = new List<GameObject>();
            foreach (Transform child in lodRoot)
            {
                if (!IsAlive(child) || !child.name.StartsWith("model_LOD"))
                {
                    continue;
                }

                if (IsAlive(keep) && child == keep)
                {
                    child.gameObject.SetActive(true);
                    continue;
                }

                remove.Add(child.gameObject);
            }

            foreach (GameObject target in remove)
            {
                if (IsAlive(target))
                {
                    target.SetActive(false);
                    DestroyObject(target);
                }
            }
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (!IsAlive(root))
            {
                return null;
            }

            foreach (Transform child in root)
            {
                if (!IsAlive(child))
                {
                    continue;
                }

                if (child.name == childName)
                {
                    return child;
                }

                Transform match = FindChildRecursive(child, childName);
                if (IsAlive(match))
                {
                    return match;
                }
            }

            return null;
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
            if (!IsAlive(target))
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

        private static bool IsAlive(Object obj)
        {
            return obj != null;
        }
    }
}
