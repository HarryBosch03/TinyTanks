using UnityEngine;

namespace TinyTanks.Health
{
    public struct DamageSource
    {
        public GameObject invoker;
        public Vector3 origin;
        public Vector3 direction;
        public Vector3 hitPoint;
        public Vector3 hitNormal;

        public DamageSource(GameObject invoker, Ray ray, RaycastHit hit)
        {
            this.invoker = invoker;
            
            origin = ray.origin;
            direction = ray.direction;
            hitPoint = hit.point;
            hitNormal = hit.normal;
        }
    }
}