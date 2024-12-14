using System;
using TinyTanks.Health;
using TinyTanks.Projectiles;
using UnityEngine;

namespace TinyTanks.Tanks
{
    public class TankWeapon : MonoBehaviour
    {
        public string displayName;
        public Sprite icon;
        public Projectile projectile;
        public float fireDelay;
        public DamageInstance damage;
        public float projectileSpeed;
        public float recoilForce;
        public bool automatic;

        [Space]
        public ParticleSystem fireFx;

        private Rigidbody body;
        private TankController tank;
        private float reloadTimer;

        public event Action WeaponFiredEvent;

        public Transform muzzle { get; private set; }
        public bool shooting { get; private set; }
        public bool isReloading => reloadTimer > 0f;
        public float reloadPercent => 1f - reloadTimer / fireDelay;

        private void Awake()
        {
            body = GetComponentInParent<Rigidbody>();
            tank = GetComponentInParent<TankController>();
            if (string.IsNullOrEmpty(displayName)) displayName = name;
        }

        private void Start()
        {
            var index = Array.IndexOf(tank.weapons, this);
            muzzle = index switch
            {
                0 => tank.model.gunMuzzle,
                1 => tank.model.coaxMuzzle,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public void SetShooting(bool shooting) { this.shooting = shooting; }

        private void FixedUpdate()
        {
            if (shooting && !isReloading)
            {
                var instance = Instantiate(projectile, muzzle.position, muzzle.rotation);

                instance.shooter = tank.gameObject;
                instance.damage = damage;
                instance.startSpeed = projectileSpeed;

                instance.velocity += body.GetPointVelocity(muzzle.position);
                WeaponFiredEvent?.Invoke();

                reloadTimer = fireDelay;
                //tank.predictionBody.AddForceAtPosition(-muzzle.forward * recoilForce, muzzle.position, ForceMode.VelocityChange);

                if (fireFx != null && !(tank.isActiveViewer && tank.sightCamera)) fireFx.Play(true);
            }

            reloadTimer -= Time.fixedDeltaTime;
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