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

        public SpeedLevel CurrentSpeedLevel { get; private set; } = SpeedLevel.Medium;
        public float AverageMouseSpeed => currentMouseSpeed;
        public float GrindDuration { get; private set; }

        private bool dragging;
        private Vector3 lastMousePosition;
        private float currentMouseSpeed;
        private bool hasSpeedSample;
        private float dragPlaneY;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }

            dragPlaneY = transform.position.y;
            RefreshLabel();
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
            GrindDuration = 0f;
            CurrentSpeedLevel = SpeedLevel.Medium;
            RefreshLabel();
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
