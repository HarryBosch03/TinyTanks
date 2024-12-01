using UnityEngine;

namespace TinyTanks.CDM
{
    public class CdmBox : MonoBehaviour, ICdmShape
    {
        public Vector3 offset;
        public Vector3 size = Vector3.one;

        private Bounds privateBounds;
        
        public Bounds bounds => privateBounds;

        private void Update() { RecalculateBounds(); }

        private void RecalculateBounds()
        {
            privateBounds = new Bounds(transform.TransformPoint(offset), Vector3.zero);
            privateBounds.Encapsulate(transform.TransformPoint(offset + new Vector3(size.x, -size.y, size.z) / 2f));
            privateBounds.Encapsulate(transform.TransformPoint(offset + new Vector3(-size.x, -size.y, size.z) / 2f));
            privateBounds.Encapsulate(transform.TransformPoint(offset + new Vector3(-size.x, -size.y, -size.z) / 2f));
            privateBounds.Encapsulate(transform.TransformPoint(offset + new Vector3(size.x, -size.y, -size.z) / 2f));
            privateBounds.Encapsulate(transform.TransformPoint(offset + new Vector3(size.x, size.y, size.z) / 2f));
            privateBounds.Encapsulate(transform.TransformPoint(offset + new Vector3(-size.x, size.y, size.z) / 2f));
            privateBounds.Encapsulate(transform.TransformPoint(offset + new Vector3(-size.x, size.y, -size.z) / 2f));
            privateBounds.Encapsulate(transform.TransformPoint(offset + new Vector3(size.x, size.y, -size.z) / 2f));
        }

        public bool DoesIntersect(Ray ray, out float enter)
        {
            enter = float.MaxValue;
            var localRay = new Ray(transform.InverseTransformPoint(ray.origin), transform.InverseTransformDirection(ray.direction));
            if (!new Bounds(offset, size).IntersectRay(localRay, out var localEnter)) return false;

            var localHit = localRay.GetPoint(localEnter);
            enter = Vector3.Dot(transform.TransformPoint(localHit) - ray.origin, ray.direction);
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(offset, size);

            RecalculateBounds();
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}