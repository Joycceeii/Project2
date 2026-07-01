using UnityEngine;

namespace TheTasteReviver
{
    [RequireComponent(typeof(Collider))]
    public class MortarArea : MonoBehaviour
    {
        [SerializeField] private Collider mortarCollider;

        private void Awake()
        {
            if (mortarCollider == null)
            {
                mortarCollider = GetComponent<Collider>();
            }
        }

        public bool ContainsWorldPoint(Vector3 worldPoint)
        {
            if (mortarCollider == null)
            {
                return false;
            }

            Vector3 closest = mortarCollider.ClosestPoint(worldPoint);
            return (closest - worldPoint).sqrMagnitude < 0.0025f || mortarCollider.bounds.Contains(worldPoint);
        }
    }
}
