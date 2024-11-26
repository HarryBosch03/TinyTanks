using UnityEngine;

namespace TinyTanks.Tanks
{
    public class CopyPoseTree : MonoBehaviour
    {
        public Transform[] sourceList;
        public Transform[] destList;

        private void LateUpdate()
        {
            for (var i = 0; i < destList.Length; i++)
            {
                var source = sourceList[i];
                var dest = destList[i];

                dest.localPosition = source.localPosition;
                dest.localRotation = source.localRotation;
            }
        }
    }
}