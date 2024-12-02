using System;
using FishNet.Object;
using TinyTanks.Health;
using UnityEngine;

namespace TinyTanks.Projectiles
{
    public class Projectile : MonoBehaviour
    {
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
                    if (!report.canPenetrate)
                    {
                        playHitFx = false;
                        if (ricochetFx != null) Instantiate(ricochetFx, hit.point, Quaternion.LookRotation(hit.normal));
                    }
                    else if (report.didRicochet)
                    {
                        ricochet = true;
                        if (ricochetFx != null) Instantiate(ricochetFx, hit.point, Quaternion.LookRotation(hit.normal));
                        position = report.exitRay.origin;
                        velocity = report.exitRay.direction * velocity.magnitude;
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