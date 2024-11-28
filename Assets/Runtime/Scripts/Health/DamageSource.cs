using FishNet.Object;
using UnityEngine;

namespace TinyTanks.Health
{
    public struct DamageSource
    {
        public NetworkObject invoker;
        public bool critical;

        public DamageSource(NetworkObject invoker, Vector3 direction, Vector3 normal, bool critical)
        {
            this.invoker = invoker;
            this.critical = critical;
        }
    }
}