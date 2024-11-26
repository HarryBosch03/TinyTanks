using FishNet.Object.Prediction;

namespace TinyTanks.Network
{
    public struct BlankReconcileData : IReconcileData
    {
        private uint tick;

        public uint GetTick() => tick;
        public void SetTick(uint value) => tick = value;
        public void Dispose() { }
    }
}