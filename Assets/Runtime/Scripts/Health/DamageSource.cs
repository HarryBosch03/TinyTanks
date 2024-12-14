using Unity.Netcode;
using UnityEngine;

namespace TinyTanks.Health
{
    public struct DamageSource : INetworkSerializable
    {
        public NetworkObjectReference invoker;
        public Vector3 origin;
        public Vector3 direction;
        public Vector3 hitPoint;
        public Vector3 hitNormal;

        public DamageSource(NetworkObject invoker, Ray ray, RaycastHit hit)
        {
            this.invoker = invoker;
            
            origin = ray.origin;
            direction = ray.direction;
            hitPoint = hit.point;
            hitNormal = hit.normal;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref invoker);
            serializer.SerializeValue(ref origin);
            serializer.SerializeValue(ref direction);
            serializer.SerializeValue(ref hitPoint);
            serializer.SerializeValue(ref hitNormal);
        }
    }
}