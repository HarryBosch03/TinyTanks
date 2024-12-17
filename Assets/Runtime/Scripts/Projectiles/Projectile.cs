using TinyTanks.Health;
using Unity.Netcode;
using UnityEngine;

namespace TinyTanks.Projectiles
{
    public class Projectile : MonoBehaviour
    {
        public const float RicochetDeviationAngleMax = 6f;
        
        public DamageInstance damage;
        public float startSpeed;
        public float maxAge;
        public float maxAngleBeforeRicochet = 60f;
        public float directHitAngle = 8f;
        public GameObject hitFx;
        public GameObject ricochetFx;

        private float age;

        [HideInInspector]
        public Vector3 position;
        [HideInInspector]
        public Vector3 velocity;

        public NetworkObject shooter;

        private void OnEnable()
        {
            position = transform.position;
        }

        private void Start()
        {
            velocity += transform.forward * startSpeed;
        }

        private void FixedUpdate()
        {
            var ray = new Ray(position, velocity);
            var ricochet = false;
            var playHitFx = true;
            
            if (Physics.Raycast(ray, out var hit, velocity.magnitude * Time.fixedDeltaTime * 1.01f))
            {
                var canBeDamaged = hit.collider.GetComponentInParent<ICanBeDamaged>();
                if (canBeDamaged != null)
                {
                    canBeDamaged.Damage(damage, new DamageSource(shooter, ray, hit), out var report);
                    if (report.didRicochet || !report.canPenetrate)
                    {
                        ricochet = true;
                        if (ricochetFx != null) Instantiate(ricochetFx, hit.point, Quaternion.LookRotation(hit.normal));
                        position = hit.point;
                        velocity = Vector3.Reflect(velocity, hit.normal) * 0.5f;

                        var devianceDirection = Quaternion.LookRotation(velocity.normalized);
                        devianceDirection *= Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                        devianceDirection *= Quaternion.Euler(Random.Range(-1f, 1f) * RicochetDeviationAngleMax, 0f, 0f);

                        velocity = (devianceDirection * Vector3.forward) * velocity.magnitude;
                        position -= velocity * Time.fixedDeltaTime * 0.99f;
                    }
                }

                if (!ricochet)
                {
                    if (playHitFx && hitFx != null) Instantiate(hitFx, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(gameObject);
                }
            }
            
            if (age > maxAge) Destroy(gameObject);
            
            position += velocity * Time.fixedDeltaTime;
            velocity += Physics.gravity * Time.fixedDeltaTime;
            
            age += Time.fixedDeltaTime;
        }

        private void LateUpdate()
        {
            transform.position = position + velocity * (Time.time - Time.fixedTime);
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }
}