using TinyTanks.Health;
using UnityEngine;

namespace TinyTanks.CDM
{
    public class CdmTester : MonoBehaviour
    {
        public DamageInstance damage;

        private void Awake()
        {
            if (Application.isPlaying) Destroy(gameObject);
        }

        private void OnDrawGizmos()
        {
            var ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out var hit))
            {
                var cdm = hit.collider.GetComponentInParent<CdmController>();
                if (cdm != null)
                {
                    cdm.Damage(damage, new DamageSource(null, ray, hit), out var report);
                    report.DrawGizmos();
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(transform.position, hit.point);
                }
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, transform.forward * 1024f);
            }
        }
    }
}