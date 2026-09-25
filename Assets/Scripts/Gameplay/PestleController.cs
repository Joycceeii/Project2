using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public class PestleController : MonoBehaviour
    {
        public MortarArea mortarArea;
        public Camera interactionCamera;
        public TMP_Text speedLabel;
        public UIManager uiManager;
        [Header("Speed Calibration")]
        public bool applyPlayerFriendlySpeedCalibration = true;
        public float slowThreshold = 470f;
        public float fastThreshold = 1370f;
        public float speedResponseTime = 0.22f;
        [Tooltip("A speed level must be held this long before it is used for evaluation.")]
        public float speedHoldSeconds = 0.3f;
        public float speedDeadZone = 2.5f;
        public float speedHysteresis = 90f;
        public float maxInstantSpeed = 2400f;
        [Header("Grinding Completion")]
        [Tooltip("The pestle must be moving at least this many screen pixels per frame before the motion counts as grinding.")]
        public float minimumGrindingMouseDelta = 1f;
        [HideInInspector]
        [Tooltip("Fallback only when no RecipeAttemptManager is connected. Use RecipeAttemptManager.batchPowderSeconds for gameplay tuning.")]
        public float groundVisualDelaySeconds = 1.5f;
        public bool expandClickHitbox = true;
        public Vector3 clickHitboxPadding = new Vector3(0.45f, 0.28f, 0.45f);
        public Vector3 minimumClickHitboxSize = new Vector3(0.9f, 0.45f, 0.9f);

        public SpeedLevel CurrentSpeedLevel { get; private set; } = SpeedLevel.Medium;
        public SpeedLevel EvaluatedSpeedLevel => hasStableSpeedLevel ? stableSpeedLevel : CurrentSpeedLevel;
        public bool HasEvaluatedSpeedLevel => hasStableSpeedLevel;
        public bool HasSpeedReading => hasSpeedSample && GrindDuration > 0f;
        public float CurrentSpeedHoldSeconds => candidateSpeedSeconds;
        public float RequiredSpeedHoldSeconds => Mathf.Max(0f, speedHoldSeconds);
        public float RequiredGrindingSeconds => Mathf.Max(0f, groundVisualDelaySeconds);
        public float RequiredBatchPowderSeconds => Mathf.Max(0.5f, RequiredGrindingSeconds);
        public float AverageMouseSpeed => currentMouseSpeed;
        public float GrindDuration { get; private set; }

        private bool dragging;
        private Vector3 lastMousePosition;
        private float currentMouseSpeed;
        private bool hasSpeedSample;
        private SpeedLevel candidateSpeedLevel = SpeedLevel.Medium;
        private float candidateSpeedSeconds;
        private SpeedLevel stableSpeedLevel = SpeedLevel.Medium;
        private bool hasStableSpeedLevel;
        private float dragPlaneY;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Awake()
        {
            ApplyPlayerFriendlySpeedCalibration();
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

        private void ApplyPlayerFriendlySpeedCalibration()
        {
            if (!applyPlayerFriendlySpeedCalibration)
            {
                return;
            }

            slowThreshold = Mathf.Clamp(slowThreshold, 320f, 560f);
            fastThreshold = Mathf.Clamp(fastThreshold, 1300f, 1900f);
            maxInstantSpeed = Mathf.Max(maxInstantSpeed, fastThreshold + 450f);
            speedResponseTime = Mathf.Clamp(speedResponseTime, 0.12f, 0.35f);
            speedHoldSeconds = Mathf.Clamp(speedHoldSeconds, 0.15f, 1.2f);
            minimumGrindingMouseDelta = Mathf.Clamp(minimumGrindingMouseDelta, 0.5f, 24f);
            groundVisualDelaySeconds = Mathf.Clamp(groundVisualDelaySeconds, 1.2f, 6f);
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
                UpdateCurrentSpeed(delta);
                CurrentSpeedLevel = SpeedToLevel(currentMouseSpeed);
                if (IsEffectiveGrindingMotion(delta))
                {
                    GrindDuration += Time.deltaTime;
                    UpdateEvaluatedSpeedLevel(CurrentSpeedLevel, Time.deltaTime);
                    RefreshLabel();
                    uiManager?.UpdateMortarReaction(CurrentSpeedLevel, CurrentSpeedHoldSeconds);
                    if (ShouldShowGroundVisualsForCurrentBatch())
                    {
                        uiManager?.attemptManager?.ShowGroundVisualsForCurrentBatch();
                    }

                    uiManager?.TryAutoEvaluateAfterGrinding();
                }
                else
                {
                    RefreshLabel();
                }
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
            ResetSpeedEvaluation();
            GrindDuration = 0f;
            CurrentSpeedLevel = SpeedLevel.Medium;
            RefreshLabel();
        }

        public void ResetSpeedAveraging()
        {
            currentMouseSpeed = 0f;
            hasSpeedSample = false;
            CurrentSpeedLevel = SpeedLevel.Medium;
            ResetSpeedEvaluation();
            RefreshLabel();
        }

        public void ResetSpeedEvaluation()
        {
            candidateSpeedLevel = SpeedLevel.Medium;
            candidateSpeedSeconds = 0f;
            stableSpeedLevel = SpeedLevel.Medium;
            hasStableSpeedLevel = false;
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

        private void UpdateEvaluatedSpeedLevel(SpeedLevel observedLevel, float deltaTime)
        {
            if (!hasSpeedSample)
            {
                return;
            }

            if (observedLevel != candidateSpeedLevel)
            {
                candidateSpeedLevel = observedLevel;
                candidateSpeedSeconds = 0f;
            }

            candidateSpeedSeconds += Mathf.Max(0f, deltaTime);
            if (candidateSpeedSeconds >= Mathf.Max(0f, speedHoldSeconds))
            {
                if (!hasStableSpeedLevel || (int)candidateSpeedLevel > (int)stableSpeedLevel)
                {
                    stableSpeedLevel = candidateSpeedLevel;
                }

                hasStableSpeedLevel = true;
            }
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

        private void RefreshLabel()
        {
            if (speedLabel != null)
            {
                speedLabel.text = HasSpeedReading ? "Current Grind Speed: " + CurrentSpeedLevel : string.Empty;
            }
        }

        private bool ShouldShowGroundVisualsForCurrentBatch()
        {
            if (uiManager != null && uiManager.attemptManager != null)
            {
                return uiManager.attemptManager.HasCurrentBatchReachedPowderTime();
            }

            return GrindDuration >= RequiredGrindingSeconds;
        }

        private bool IsEffectiveGrindingMotion(float mouseDelta)
        {
            return mouseDelta >= Mathf.Max(0.05f, minimumGrindingMouseDelta);
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
