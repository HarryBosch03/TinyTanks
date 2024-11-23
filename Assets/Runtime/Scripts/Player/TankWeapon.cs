using System;
using TinyTanks.Projectiles;
using UnityEngine;

namespace TinyTanks.Player
{
    public class TankWeapon : MonoBehaviour
    {
        public string displayName;
        public Transform muzzle;
        public Projectile projectile;
        public float fireDelay;
        public float projectileSpeed;
        public float recoilForce;
        public bool automatic;

        private bool shooting;
        private Rigidbody body;
        private float reloadTimer;

        public event Action WeaponFiredEvent;
        
        public bool isReloading => reloadTimer > 0f;
        public float reloadPercent => 1f - reloadTimer / fireDelay;

        private void Awake()
        {
            body = GetComponentInParent<Rigidbody>();
            if (string.IsNullOrEmpty(displayName)) displayName = name;
        }
        
        public void StartShooting()
        {
            shooting = true;
        }

        public void StopShooting()
        {
            shooting = false;
        }

        private void FixedUpdate()
        {
            if (reloadTimer > 0f) reloadTimer -= Time.deltaTime;

            if (shooting && reloadTimer <= 0f)
            {
                var instance = Instantiate(projectile, muzzle.position, muzzle.rotation);

                instance.startSpeed = projectileSpeed;
            
                instance.velocity += body.GetPointVelocity(muzzle.position);
                reloadTimer = fireDelay;

                body.AddForceAtPosition(-muzzle.forward * recoilForce, muzzle.position, ForceMode.VelocityChange);
                WeaponFiredEvent?.Invoke();
                
                if (!automatic) shooting = false;
            }
        }
        
        public Vector3 PredictProjectileArc()
        {
            var position = muzzle.position;
            var velocity = muzzle.forward * projectileSpeed;

            var maxTime = 5f;
            var deltaTime = 0.1f;
            
            for (var t = 0f; t < maxTime; t += deltaTime)
            {
                if (Physics.Linecast(position, position + velocity * deltaTime, out var hit))
                {
                    Debug.DrawLine(position, hit.point, Color.red);
                    return hit.point;
                }
                else
                {
                    Debug.DrawLine(position, position + velocity * deltaTime, Color.red);
                }
                
                position += velocity * deltaTime;
                velocity += Physics.gravity * deltaTime;
            }

            return muzzle.position + muzzle.forward * 500f;
        }
    }
}