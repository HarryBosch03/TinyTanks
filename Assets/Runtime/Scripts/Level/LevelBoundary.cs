using TinyTanks.Utility;
using UnityEngine;

namespace TinyTanks.Level
{
    public class LevelBoundary : MonoBehaviour
    {
        public float killPlane = -50;
        public Vector2[] border = new Vector2[0];

        public static LevelBoundary instance { get; private set; }

        private void OnEnable() { instance = this; }

        private void OnDisable()
        {
            if (instance == this) instance = null;
        }

        public bool IsInsideBoundary(Vector3 point)
        {
            if (border.Length < 3) return true;
            
            var count = 0;
            
            for (var i = 0; i < border.Length; i++)
            {
                var p0 = border[i];
                var p1 = border.IndexWrapped(i + 1);
                
                if ((point.z > Mathf.Min(p0.y, p1.y))
                    && (point.z <= Mathf.Max(p0.y, p1.y))
                    && (point.x <= Mathf.Max(p0.x, p1.x)))
                {
                    var xIntersect = (point.z - p0.y)
                                     * (p1.x - p0.x)
                                     / (p1.y - p0.y)
                                     + p0.x;

                    if (p0.x == p1.x || point.x <= xIntersect)
                    {
                        count++;
                    }
                }
            }

            return count % 2 == 0;

            Vector2 remap(Vector3 p) => new Vector2(p.x, p.z);
        }

        private void OnDrawGizmos()
        {
            if (border.Length > 1)
            {
                Gizmos.matrix = Matrix4x4.Rotate(Quaternion.Euler(90f, 0f, 0f));
                for (var i = 0; i < border.Length - 1; i++)
                {
                    Gizmos.DrawLine(border[i], border[i + 1]);
                }

                Gizmos.DrawLine(border[^1], border[0]);
            }
        }
    }
}