using Unity.Netcode;

namespace TinyTanks.Health
{
    [System.Serializable]
    public struct DamageInstance : INetworkSerializable
    {
        public int damageClass;
        public int damageAmount;
        public float critAngle;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref damageAmount);
            serializer.SerializeValue(ref critAngle);
        }
    }
}