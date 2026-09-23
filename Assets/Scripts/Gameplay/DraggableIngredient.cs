using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheTasteReviver
{
    public class DraggableIngredient : MonoBehaviour
    {
        public IngredientData ingredientData;
        public List<IngredientData> representedIngredients = new List<IngredientData>();
        public RecipeAttemptManager attemptManager;
        public MortarArea mortarArea;
        public Camera interactionCamera;
        public GameObject sourcePrefab;
        public float dragLiftHeight = 0.55f;
        public float mortarDragClearance = 0.08f;
        public float mortarDropHeight = 0.2f;
        public float groundVisualLift = 0.1f;
        public float mortarGroundVisualLift = 0.04f;
        public float homeGroundVisualLift = 0.22f;
        public float groundVisualScale = 0.8f;
        public float groundVisualFootprint = 0.7f;
        public float groundVisualMaxHeight = 0.14f;

        private Vector3 startPosition;
        private bool dragging;
        private float dragPlaneY;
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private GameObject groundVisualInstance;
        private bool groundVisualSizeNormalized;
        private bool hasBeenGround;
        private UIManager hoverUIManager;

        public bool IsInMortar { get; private set; }
        public bool HasBeenGround => hasBeenGround;

        private void Awake()
        {
            DisableLodGroups(transform);
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
                RestoreOriginalRenderers();
            }

            transform.position = startPosition;
            dragPlaneY = startPosition.y;
            if (keepGroundState && hasBeenGround)
            {
                EnsureGroundVisual();
            }
        }

        public void ReturnHomeAsGround()
        {
            hasBeenGround = true;
            ReturnHome(true);
            RaiseGroundVisual(homeGroundVisualLift);
        }

        public void HideAtHome()
        {
            ReturnHome(false);
            gameObject.SetActive(false);
        }

        public void ShowGroundState()
        {
            if (!IsInMortar)
            {
                return;
            }

            hasBeenGround = true;
            EnsureGroundVisual();
            AlignGroundVisualToMortarSurface();
        }

        private void EnsureGroundVisual()
        {
            if (ingredientData == null || ingredientData.groundPrefab == null)
            {
                return;
            }

            HideCurrentRenderers();
            if (groundVisualInstance != null)
            {
                groundVisualInstance.SetActive(true);
                SetGroundVisualRenderersEnabled(true);
                NormalizeGroundVisualSize();
                RaiseGroundVisual(groundVisualLift);
                return;
            }

            groundVisualInstance = Instantiate(ingredientData.groundPrefab, transform);
            groundVisualSizeNormalized = false;
            groundVisualInstance.name = "Ground Visual";
            DisableLodGroups(groundVisualInstance.transform);
            RemoveRedundantLodModels(groundVisualInstance.transform);
            groundVisualInstance.transform.localPosition = Vector3.zero;
            groundVisualInstance.transform.localRotation = Quaternion.identity;
            groundVisualInstance.transform.localScale = Vector3.one * Mathf.Clamp(groundVisualScale, 0.25f, 1.2f);
            NormalizeGroundVisualSize();
            FitChildRenderersToAnchor(groundVisualInstance.transform, groundVisualLift);
            SetGroundVisualRenderersEnabled(true);

            foreach (Collider collider in groundVisualInstance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        private void RaiseGroundVisual(float lift)
        {
            if (groundVisualInstance == null)
            {
                return;
            }

            groundVisualInstance.transform.localPosition = Vector3.zero;
            FitChildRenderersToAnchor(groundVisualInstance.transform, Mathf.Max(0f, lift));
            SetGroundVisualRenderersEnabled(true);
        }

        private void NormalizeGroundVisualSize()
        {
            if (groundVisualInstance == null || groundVisualSizeNormalized)
            {
                return;
            }

            Bounds bounds;
            if (!TryCalculateWorldBounds(groundVisualInstance.transform, out bounds) || bounds.size.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            float height = bounds.size.y;
            if (footprint <= 0.0001f || height <= 0.0001f)
            {
                return;
            }

            float targetFootprint = Mathf.Max(0.1f, groundVisualFootprint);
            float targetHeight = Mathf.Max(0.02f, groundVisualMaxHeight);
            Vector3 axisScale = new Vector3(
                Mathf.Clamp(targetFootprint / Mathf.Max(bounds.size.x, 0.0001f), 0.05f, 4f),
                Mathf.Clamp(targetHeight / height, 0.05f, 4f),
                Mathf.Clamp(targetFootprint / Mathf.Max(bounds.size.z, 0.0001f), 0.05f, 4f));
            groundVisualInstance.transform.localScale = Vector3.Scale(groundVisualInstance.transform.localScale, axisScale);

            groundVisualSizeNormalized = true;
        }

        private void AlignGroundVisualToMortarSurface()
        {
            MortarArea activeMortarArea = GetActiveMortarArea();
            if (groundVisualInstance == null || activeMortarArea == null)
            {
                return;
            }

            Collider mortarCollider = activeMortarArea.GetComponent<Collider>();
            Vector3 target = IsAlive(mortarCollider) ? mortarCollider.bounds.center : activeMortarArea.transform.position;
            float surfaceY = IsAlive(mortarCollider) ? mortarCollider.bounds.max.y : activeMortarArea.transform.position.y + mortarDropHeight;
            AlignGroundVisualToWorldAnchor(new Vector3(target.x, surfaceY + Mathf.Max(0f, mortarGroundVisualLift), target.z));
        }

        private void AlignGroundVisualToWorldAnchor(Vector3 worldAnchor)
        {
            if (groundVisualInstance == null)
            {
                return;
            }

            SetGroundVisualRenderersEnabled(true);
            Bounds bounds;
            if (!TryCalculateWorldBounds(groundVisualInstance.transform, out bounds) || bounds.size.sqrMagnitude <= 0.0001f)
            {
                groundVisualInstance.transform.position = worldAnchor;
                return;
            }

            Vector3 offset = new Vector3(
                worldAnchor.x - bounds.center.x,
                worldAnchor.y - bounds.min.y,
                worldAnchor.z - bounds.center.z);
            groundVisualInstance.transform.position += offset;
        }

        private void SetGroundVisualRenderersEnabled(bool enabled)
        {
            if (groundVisualInstance == null)
            {
                return;
            }

            foreach (Renderer renderer in groundVisualInstance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }

        private void OnMouseDown()
        {
            HideHoverTooltip();
            if (IsInMortar && attemptManager != null)
            {
                attemptManager.RemoveIngredientsFromCurrentBatch(GetRepresentedIngredients());
                IsInMortar = false;
            }

            dragging = true;
            dragPlaneY = GetDragPlaneHeight(GetActiveMortarArea());
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
            MortarArea activeMortarArea = GetActiveMortarArea();
            bool droppedInMortar = activeMortarArea != null && activeMortarArea.ContainsWorldPoint(GetMortarCheckPoint(activeMortarArea));
            bool accepted = droppedInMortar && attemptManager != null && attemptManager.TryAddIngredients(GetRepresentedIngredients());
            IsInMortar = accepted;
            if (accepted && activeMortarArea != null)
            {
                MoveIntoMortar(activeMortarArea);
            }
            else
            {
                transform.position = startPosition;
            }

            dragPlaneY = transform.position.y;
        }

        private IReadOnlyList<IngredientData> GetRepresentedIngredients()
        {
            if (representedIngredients != null && representedIngredients.Any(x => x != null))
            {
                return representedIngredients.Where(x => x != null).Distinct().ToList();
            }

            return ingredientData != null ? new[] { ingredientData } : System.Array.Empty<IngredientData>();
        }

        private Vector3 GetMortarCheckPoint(MortarArea activeMortarArea)
        {
            if (activeMortarArea == null)
            {
                return transform.position;
            }

            return new Vector3(transform.position.x, activeMortarArea.transform.position.y, transform.position.z);
        }

        private MortarArea GetActiveMortarArea()
        {
            MortarArea attemptMortarArea = attemptManager != null
                && attemptManager.pestleController != null
                ? attemptManager.pestleController.mortarArea
                : null;
            return IsAlive(attemptMortarArea) ? attemptMortarArea : mortarArea;
        }

        private float GetDragPlaneHeight(MortarArea activeMortarArea)
        {
            float targetY = transform.position.y + Mathf.Max(0f, dragLiftHeight);
            if (!IsAlive(activeMortarArea))
            {
                return targetY;
            }

            Collider mortarCollider = activeMortarArea.GetComponent<Collider>();
            if (!IsAlive(mortarCollider))
            {
                return targetY;
            }

            float minimumVisualY = mortarCollider.bounds.max.y + Mathf.Max(0f, mortarDragClearance);
            if (TryCalculateWorldBounds(transform, out Bounds visualBounds) && visualBounds.size.sqrMagnitude > 0.0001f)
            {
                float liftNeeded = minimumVisualY - visualBounds.min.y;
                targetY = Mathf.Max(targetY, transform.position.y + Mathf.Max(0f, liftNeeded));
            }
            else
            {
                targetY = Mathf.Max(targetY, minimumVisualY);
            }

            return targetY;
        }

        private void NormalizeRuntimeVisualSettings()
        {
            if (mortarDragClearance <= 0f)
            {
                mortarDragClearance = 0.08f;
            }

            mortarDragClearance = Mathf.Clamp(mortarDragClearance, 0.03f, 0.2f);
            mortarDropHeight = Mathf.Clamp(mortarDropHeight, 0.18f, 0.22f);
            groundVisualLift = Mathf.Clamp(groundVisualLift, 0.08f, 0.14f);
            mortarGroundVisualLift = Mathf.Clamp(mortarGroundVisualLift, 0.02f, 0.12f);
            homeGroundVisualLift = Mathf.Clamp(homeGroundVisualLift, 0.18f, 0.32f);
            groundVisualScale = Mathf.Clamp(groundVisualScale, 0.72f, 0.9f);
            groundVisualFootprint = Mathf.Clamp(groundVisualFootprint, 0.35f, 1.2f);
            groundVisualMaxHeight = Mathf.Clamp(groundVisualMaxHeight, 0.06f, 0.28f);
        }

        private void MoveIntoMortar(MortarArea activeMortarArea)
        {
            Collider mortarCollider = activeMortarArea.GetComponent<Collider>();
            Vector3 center = IsAlive(mortarCollider) ? mortarCollider.bounds.center : activeMortarArea.transform.position;
            float visibleY = activeMortarArea.transform.position.y + mortarDropHeight;
            if (IsAlive(mortarCollider))
            {
                visibleY = Mathf.Max(visibleY, mortarCollider.bounds.max.y + 0.015f);
            }

            transform.position = new Vector3(center.x, visibleY, center.z);
            FitSelfRenderersToWorldAnchor(new Vector3(center.x, visibleY, center.z));
        }

        private void HideCurrentRenderers()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if ((groundVisualInstance != null && renderer.transform.IsChildOf(groundVisualInstance.transform))
                    || IsChildOfNamed(renderer.transform, "Ground Visual"))
                {
                    continue;
                }

                if (renderer.enabled)
                {
                    renderer.enabled = false;
                    if (!hiddenRenderers.Contains(renderer))
                    {
                        hiddenRenderers.Add(renderer);
                    }
                }
            }
        }

        private void OnMouseEnter()
        {
            if (dragging)
            {
                return;
            }

            hoverUIManager = attemptManager != null ? attemptManager.uiManager : null;
            if (hoverUIManager == null)
            {
                hoverUIManager = FindFirstObjectByType<UIManager>();
            }

            hoverUIManager?.ShowIngredientTooltip(GetRepresentedIngredients(), this);
        }

        private void OnMouseExit()
        {
            HideHoverTooltip();
        }

        private void OnDisable()
        {
            HideHoverTooltip();
        }

        private void HideHoverTooltip()
        {
            hoverUIManager?.HideIngredientTooltip(this);
            hoverUIManager = null;
        }

        private void ClearGroundState()
        {
            if (groundVisualInstance != null)
            {
                DestroyObject(groundVisualInstance);
                groundVisualInstance = null;
                groundVisualSizeNormalized = false;
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

        private void RestoreOriginalRenderers()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                if (groundVisualInstance != null && renderer.transform.IsChildOf(groundVisualInstance.transform))
                {
                    continue;
                }

                renderer.enabled = true;
            }
        }

        private static bool IsChildOfNamed(Transform child, string objectName)
        {
            Transform current = child;
            while (current != null)
            {
                if (current.name == objectName)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
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

            bool hasBounds = false;
            bounds = new Bounds(root.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                if (IsVisibleRenderer(renderer))
                {
                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return hasBounds;
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

            bool hasBounds = false;
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                if (!IsVisibleRenderer(renderer))
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                Vector3 localMin = parent.InverseTransformPoint(worldBounds.min);
                Vector3 localMax = parent.InverseTransformPoint(worldBounds.max);
                if (!hasBounds)
                {
                    bounds = new Bounds((localMin + localMax) * 0.5f, Vector3.zero);
                    hasBounds = true;
                }

                bounds.Encapsulate(localMin);
                bounds.Encapsulate(localMax);
            }

            return hasBounds;
        }

        private static bool IsVisibleRenderer(Renderer renderer)
        {
            return IsAlive(renderer) && renderer.enabled && renderer.gameObject.activeInHierarchy;
        }

        private static void DisableLodGroups(Transform root)
        {
            if (!IsAlive(root))
            {
                return;
            }

            foreach (LODGroup lodGroup in root.GetComponentsInChildren<LODGroup>(true))
            {
                if (lodGroup != null)
                {
                    lodGroup.ForceLOD(0);
                    lodGroup.enabled = false;
                }
            }
        }

        private static void RemoveRedundantLodModels(Transform root)
        {
            if (!IsAlive(root))
            {
                return;
            }

            List<Transform> lodRoots = new List<Transform>();
            FindChildrenRecursive(root, "lod", lodRoots);
            foreach (Transform lodRoot in lodRoots)
            {
                if (!IsAlive(lodRoot))
                {
                    continue;
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

        private static void FindChildrenRecursive(Transform root, string childName, List<Transform> matches)
        {
            if (!IsAlive(root))
            {
                return;
            }

            foreach (Transform child in root)
            {
                if (!IsAlive(child))
                {
                    continue;
                }

                if (child.name == childName)
                {
                    matches.Add(child);
                }

                FindChildrenRecursive(child, childName, matches);
            }
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
