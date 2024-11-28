using System;
using UnityEngine;

namespace TinyTanks
{
    public class TankValidator : MonoBehaviour
    {
        public GameObject graphicsParent;
        public GameObject collisionParent;

        private void OnValidate()
        {
            if (graphicsParent != null)
            {
                foreach (var collider in graphicsParent.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }
            }
            
            if (collisionParent != null)
            {
                foreach (var renderer in collisionParent.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = false;
                }
            }
        }
    }
}