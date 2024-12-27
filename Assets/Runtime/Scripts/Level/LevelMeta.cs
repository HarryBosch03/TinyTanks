using UnityEngine;

namespace TinyTanks.Level
{
    public class LevelMeta : MonoBehaviour
    {
        public float killPlane = -50;

        public Transform[] spawnPoints { get; private set; } = new Transform[0];
        public static LevelMeta instance { get; private set; }

        private void Awake()
        {
            var spawnPointParent = transform.Find("Spawn Points");
            if (spawnPointParent != null && spawnPointParent.childCount > 0)
            {
                spawnPoints = new Transform[spawnPointParent.childCount];
                for (var i = 0; i < spawnPoints.Length; i++)
                {
                    var spawnPoint = spawnPoints[i] = spawnPointParent.GetChild(i);
                    var ray = new Ray(spawnPoint.position + Vector3.up, Vector3.down * 256f);
                    if (Physics.Raycast(ray, out var hit, 256f))
                    {
                        spawnPoint.position = hit.point;
                    }
                }
            }
            else
            {
                spawnPoints = new[] { transform };
            }
        }

        private void OnEnable() { instance = this; }

        private void OnDisable()
        {
            if (instance == this) instance = null;
        }

        private void OnValidate()
        {
            if (transform.Find("Spawn Points") == null)
            {
                var spawnPointParent = new GameObject("Spawn Points");
                spawnPointParent.transform.SetParent(transform);
                spawnPointParent.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        private void OnDrawGizmos()
        {
            var spawnPointParent = transform.Find("Spawn Points");
            if (spawnPointParent != null)
            {
                Gizmos.color = Color.yellow;
                for (var i = 0; i < spawnPointParent.childCount; i++)
                {
                    var spawnPoint = spawnPointParent.GetChild(i);
                    Gizmos.matrix = Matrix4x4.TRS(spawnPoint.position, spawnPoint.rotation, new Vector3(1f, 0f, 1f));
                    Gizmos.DrawWireSphere(Vector3.zero, 4f);
                }
            }
        }
    }
}