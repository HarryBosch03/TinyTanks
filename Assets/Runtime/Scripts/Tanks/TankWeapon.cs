using System;
using FishNet;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using TinyTanks.Network;
using TinyTanks.Projectiles;
using UnityEngine;

namespace TinyTanks.Tanks
{
    public class TankWeapon : MonoBehaviour
    {
        public string displayName;
        public Transform muzzle;
        public Sprite icon;
        public Projectile projectile;
        public float fireDelay;
        public float projectileSpeed;
        public float recoilForce;
        public bool automatic;

        [Space]
        public ParticleSystem fireFx;

        private Rigidbody body;
        private float startReloadTime;

        public event Action WeaponFiredEvent;
        
        public bool shooting { get; private set; }
        public TimeManager timeManager => InstanceFinder.TimeManager;
        public float serverTime => (float)timeManager.TicksToTime(timeManager.Tick);
        public float reloadTimer => startReloadTime + fireDelay - serverTime;
        public bool isReloading => reloadTimer > 0f;
        public float reloadPercent => 1f - reloadTimer / fireDelay;

        private void Awake()
        {
            body = GetComponentInParent<Rigidbody>();
            if (string.IsNullOrEmpty(displayName)) displayName = name;
        }

        public void SetShooting(bool shooting)
        {
            this.shooting = shooting;
        }

        private void FixedUpdate()
        {
            if (shooting && reloadTimer <= 0f)
            {
                var instance = Instantiate(projectile, muzzle.position, muzzle.rotation);

                instance.startSpeed = projectileSpeed;
            
                instance.velocity += body.GetPointVelocity(muzzle.position);
                startReloadTime = serverTime;

                body.AddForceAtPosition(-muzzle.forward * recoilForce, muzzle.position, ForceMode.VelocityChange);
                WeaponFiredEvent?.Invoke();

                if (fireFx != null) fireFx.Play(true);
                
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