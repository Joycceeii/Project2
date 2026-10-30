using UnityEngine;

namespace TheTasteReviver
{
    [ExecuteAlways]
    public class GameplayBackground : MonoBehaviour
    {
        public Camera targetCamera;
        public Texture backgroundTexture;

        public static void Ensure(Camera camera)
        {
            if (camera == null) return;
            const string objectName = "Lingnan Kitchen Background";
            var existing = camera.transform.Find(objectName);
            if (existing != null) return;
            var material = Resources.Load<Material>("Backgrounds/LingnanKitchen");
            if (material == null || material.mainTexture == null)
            {
                Debug.LogWarning("Kitchen background material or texture is missing.");
                return;
            }
            var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.name = objectName;
            background.transform.SetParent(camera.transform, false);
            var collider = background.GetComponent<Collider>();
            // Disable immediately so the background never participates in pointer raycasts.
            collider.enabled = false;
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
            var renderer = background.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var view = background.AddComponent<GameplayBackground>();
            view.targetCamera = camera;
            view.backgroundTexture = material.mainTexture;
            view.FitToCamera();
        }

        private void LateUpdate()
        {
            FitToCamera();
        }

        private void OnEnable()
        {
            FitToCamera();
        }

        public void FitToCamera()
        {
            if (targetCamera == null || backgroundTexture == null) return;
            float distance = Mathf.Max(targetCamera.nearClipPlane + 0.1f, targetCamera.farClipPlane - 1f);
            float height = targetCamera.orthographic
                ? targetCamera.orthographicSize * 2f
                : 2f * distance * Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float width = height * targetCamera.aspect;
            float imageAspect = (float)backgroundTexture.width / backgroundTexture.height;
            // Cover the viewport without stretching the artwork.
            if (width / height > imageAspect) height = width / imageAspect;
            else width = height * imageAspect;
            transform.localPosition = new Vector3(0f, 0f, distance);
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(width, height, 1f);
        }
    }
}
