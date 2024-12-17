using UnityEngine;

namespace TinyTanks.Utility
{
    public class Reparent : MonoBehaviour
    {
        public Transform parent;

        private void Awake()
        {
            transform.SetParent(parent);
        }
    }
}