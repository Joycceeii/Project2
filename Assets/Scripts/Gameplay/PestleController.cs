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
        public float slowThreshold = 360f;
        public float fastThreshold = 720f;
        public float speedResponseTime = 0.25f;

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
                uiManager?.attemptManager?.ShowGroundVisualsForCurrentBatch();
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
            float instantSpeed = mouseDelta / deltaTime;
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
