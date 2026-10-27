using UnityEngine;

namespace TheTasteReviver
{
    [RequireComponent(typeof(Collider))]
    public class MortarArea : MonoBehaviour
    {
        [SerializeField] private Collider mortarCollider;
        [Tooltip("Drag a Scene Transform here to author the ingredient resting position inside the bowl.")]
        public Transform ingredientPlacementPoint;

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

        /// <summary>
        /// Checks the horizontal grinding footprint while ignoring the
        /// vertical placement of the authored pestle. The pestle can be
        /// positioned above the stone for visual alignment, so requiring its
        /// root to be inside the collider's Y volume would incorrectly block
        /// grinding.
        /// </summary>
        public bool ContainsGrindingPoint(Vector3 worldPoint)
        {
            if (mortarCollider == null)
            {
                return false;
            }

            Bounds bounds = mortarCollider.bounds;
            return worldPoint.x >= bounds.min.x
                && worldPoint.x <= bounds.max.x
                && worldPoint.z >= bounds.min.z
                && worldPoint.z <= bounds.max.z;
        }
    }
}
