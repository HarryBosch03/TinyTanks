using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyTanks.Health
{
    public struct DamageSource : INetworkSerializable
    {
        public NetworkObjectReference invoker;
        public Vector3 origin;
        public Vector3 direction;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public float impactAngle;

        public DamageSource(NetworkObject invoker, Ray ray, RaycastHit hit, float impactAngle)
        {
            this.invoker = invoker;
            
            origin = ray.origin;
            direction = ray.direction;
            
            hitPoint = hit.point;
            hitNormal = hit.normal;
            
            this.impactAngle = impactAngle;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref invoker);
            serializer.SerializeValue(ref origin);
            serializer.SerializeValue(ref direction);
            serializer.SerializeValue(ref hitPoint);
            serializer.SerializeValue(ref hitNormal);
            serializer.SerializeValue(ref impactAngle);
        }
    }
}