using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public class PestleController : MonoBehaviour
    {
        public MortarArea mortarArea;
        public Camera interactionCamera;
        public Text speedLabel;
        public UIManager uiManager;
        public float slowThreshold = 520f;
        public float fastThreshold = 980f;
        public float speedResponseTime = 0.45f;
        public float speedDeadZone = 2.5f;
        public float speedHysteresis = 90f;
        public float maxInstantSpeed = 1400f;
        public float groundVisualDelaySeconds = 1.2f;
        [Range(0f, 1f)] public float groundVisualGateRatio = 0.35f;
        public bool expandClickHitbox = true;
        public Vector3 clickHitboxPadding = new Vector3(0.45f, 0.28f, 0.45f);
        public Vector3 minimumClickHitboxSize = new Vector3(0.9f, 0.45f, 0.9f);

        public SpeedLevel CurrentSpeedLevel { get; private set; } = SpeedLevel.Medium;
        public SpeedLevel EvaluatedSpeedLevel => hasSpeedSample && speedSampleSeconds > 0.05f
            ? ClassifySpeed(speedSampleTotal / speedSampleSeconds)
            : CurrentSpeedLevel;
        public float AverageMouseSpeed => currentMouseSpeed;
        public float GrindDuration { get; private set; }

        private bool dragging;
        private Vector3 lastMousePosition;
        private float currentMouseSpeed;
        private bool hasSpeedSample;
        private float speedSampleSeconds;
        private float speedSampleTotal;
        private float dragPlaneY;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Awake()
        {
            ConfigureClickHitbox();
            startPosition = transform.position;
            startRotation = transform.rotation;
            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }

            dragPlaneY = transform.position.y;
            RefreshLabel();
        }

        private void ConfigureClickHitbox()
        {
            if (!expandClickHitbox)
            {
                return;
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                if (collider != null && collider.transform != transform)
                {
                    collider.enabled = false;
                }
            }

            Bounds bounds = CalculateLocalBounds();
            BoxCollider hitbox = GetComponent<BoxCollider>();
            if (hitbox == null)
            {
                hitbox = gameObject.AddComponent<BoxCollider>();
            }

            if (hitbox == null)
            {
                return;
            }

            hitbox.isTrigger = false;
            hitbox.enabled = true;

            if (bounds.size.sqrMagnitude <= 0.0001f)
            {
                hitbox.center = Vector3.zero;
                hitbox.size = minimumClickHitboxSize;
                return;
            }

            hitbox.center = bounds.center;
            hitbox.size = new Vector3(
                Mathf.Max(bounds.size.x + clickHitboxPadding.x, minimumClickHitboxSize.x),
                Mathf.Max(bounds.size.y + clickHitboxPadding.y, minimumClickHitboxSize.y),
                Mathf.Max(bounds.size.z + clickHitboxPadding.z, minimumClickHitboxSize.z));
        }

        private Bounds CalculateLocalBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Bounds bounds = new Bounds(transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                bounds.Encapsulate(transform.InverseTransformPoint(worldBounds.min));
                bounds.Encapsulate(transform.InverseTransformPoint(worldBounds.max));
            }

            return bounds;
        }

        private void OnMouseDown()
        {
            dragging = true;
            lastMousePosition = Input.mousePosition;
            dragPlaneY = transform.position.y;
        }

        private void OnMouseDrag()
        {
            if (!dragging)
            {
                return;
            }

            if (TryGetMouseWorldPoint(out Vector3 worldPoint))
            {
                transform.position = worldPoint;
            }

            bool validGrinding = mortarArea != null && mortarArea.ContainsWorldPoint(transform.position);
            if (validGrinding)
            {
                float delta = Vector3.Distance(Input.mousePosition, lastMousePosition);
                GrindDuration += Time.deltaTime;
                UpdateCurrentSpeed(delta);
                RecordSpeedSample(Time.deltaTime);
                CurrentSpeedLevel = SpeedToLevel(currentMouseSpeed);
                RefreshLabel();
                if (ShouldShowGroundVisuals())
                {
                    uiManager?.attemptManager?.ShowGroundVisualsForCurrentBatch();
                }

                uiManager?.TryAutoEvaluateAfterGrinding();
            }

            lastMousePosition = Input.mousePosition;
        }

        private void OnMouseUp()
        {
            dragging = false;
            if (hasSpeedSample)
            {
                uiManager?.ShowStepFeedback(MechanicType.Speed);
            }
        }

        public void ResetTracking()
        {
            dragging = false;
            currentMouseSpeed = 0f;
            hasSpeedSample = false;
            ResetSpeedAveraging();
            GrindDuration = 0f;
            CurrentSpeedLevel = SpeedLevel.Medium;
            RefreshLabel();
        }

        public void ResetSpeedAveraging()
        {
            speedSampleSeconds = 0f;
            speedSampleTotal = 0f;
        }

        public void ResetToDefault()
        {
            ResetTracking();
            transform.position = startPosition;
            transform.rotation = startRotation;
            dragPlaneY = transform.position.y;
        }

        private void UpdateCurrentSpeed(float mouseDelta)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            float instantSpeed = mouseDelta <= speedDeadZone
                ? 0f
                : Mathf.Min(mouseDelta / deltaTime, maxInstantSpeed);
            if (!hasSpeedSample)
            {
                currentMouseSpeed = instantSpeed;
                hasSpeedSample = true;
                return;
            }

            float response = Mathf.Max(0.01f, speedResponseTime);
            float blend = 1f - Mathf.Exp(-deltaTime / response);
            currentMouseSpeed = Mathf.Lerp(currentMouseSpeed, instantSpeed, blend);
        }

        private void RecordSpeedSample(float deltaTime)
        {
            if (!hasSpeedSample)
            {
                return;
            }

            speedSampleSeconds += Mathf.Max(0f, deltaTime);
            speedSampleTotal += currentMouseSpeed * Mathf.Max(0f, deltaTime);
        }

        private SpeedLevel SpeedToLevel(float speed)
        {
            if (CurrentSpeedLevel == SpeedLevel.Slow && speed <= slowThreshold + speedHysteresis)
            {
                return SpeedLevel.Slow;
            }

            if (CurrentSpeedLevel == SpeedLevel.Fast && speed >= fastThreshold - speedHysteresis)
            {
                return SpeedLevel.Fast;
            }

            if (speed <= slowThreshold) return SpeedLevel.Slow;
            if (speed >= fastThreshold) return SpeedLevel.Fast;
            return SpeedLevel.Medium;
        }

        private SpeedLevel ClassifySpeed(float speed)
        {
            if (speed <= slowThreshold) return SpeedLevel.Slow;
            if (speed >= fastThreshold) return SpeedLevel.Fast;
            return SpeedLevel.Medium;
        }

        private void RefreshLabel()
        {
            if (speedLabel != null)
            {
                speedLabel.text = "Current Grind Speed: " + CurrentSpeedLevel;
            }
        }

        private bool ShouldShowGroundVisuals()
        {
            float requiredSeconds = Mathf.Max(0f, groundVisualDelaySeconds);
            RecipeLevelData level = uiManager != null && uiManager.attemptManager != null
                ? uiManager.attemptManager.currentLevel
                : null;
            if (level != null)
            {
                requiredSeconds = Mathf.Max(requiredSeconds, Mathf.Max(0f, level.minGrindDuration) * groundVisualGateRatio);
            }

            return GrindDuration >= requiredSeconds;
        }

        private bool TryGetMouseWorldPoint(out Vector3 worldPoint)
        {
            if (interactionCamera == null)
            {
                worldPoint = transform.position;
                return false;
            }

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
