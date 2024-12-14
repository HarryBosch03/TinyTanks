using Unity.Netcode;

namespace TinyTanks.Health
{
    [System.Serializable]
    public struct DamageInstance : INetworkSerializable
    {
        public int damageClass;
        public int spallCount;
        public float spallAngle;
        public float ricochetAngle;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref damageClass);
            serializer.SerializeValue(ref spallCount);
            serializer.SerializeValue(ref spallAngle);
            serializer.SerializeValue(ref ricochetAngle);
        }
    }
}