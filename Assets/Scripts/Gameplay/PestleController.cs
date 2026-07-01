using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public class PestleController : MonoBehaviour
    {
        public MortarArea mortarArea;
        public Camera interactionCamera;
        public Text speedLabel;
        public float slowThreshold = 360f;
        public float fastThreshold = 720f;

        public SpeedLevel CurrentSpeedLevel { get; private set; } = SpeedLevel.Medium;
        public float AverageMouseSpeed => trackedTime <= 0f ? 0f : totalMouseDistance / trackedTime;
        public float GrindDuration { get; private set; }

        private bool dragging;
        private Vector3 lastMousePosition;
        private float totalMouseDistance;
        private float trackedTime;
        private float dragPlaneY;

        private void Awake()
        {
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
                totalMouseDistance += delta;
                trackedTime += Time.deltaTime;
                GrindDuration += Time.deltaTime;
                CurrentSpeedLevel = SpeedToLevel(AverageMouseSpeed);
                RefreshLabel();
            }

            lastMousePosition = Input.mousePosition;
        }

        private void OnMouseUp()
        {
            dragging = false;
        }

        public void ResetTracking()
        {
            totalMouseDistance = 0f;
            trackedTime = 0f;
            GrindDuration = 0f;
            CurrentSpeedLevel = SpeedLevel.Medium;
            RefreshLabel();
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
