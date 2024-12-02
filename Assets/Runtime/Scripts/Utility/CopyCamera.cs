using System;
using UnityEngine;

namespace TinyTanks.Utility
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class CopyCamera : MonoBehaviour
    {
        public Camera source;

        private Camera dest;

        private void Awake()
        {
            dest = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            dest.fieldOfView = source.fieldOfView;
            dest.nearClipPlane = source.nearClipPlane;
            dest.farClipPlane = source.farClipPlane;
        }

        private void Reset()
        {
            source = transform.parent.GetComponentInParent<Camera>();
        }
    }
}