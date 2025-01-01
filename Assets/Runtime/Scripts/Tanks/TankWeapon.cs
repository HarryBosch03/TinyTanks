using System;
using TinyTanks.Health;
using TinyTanks.Projectiles;
using TinyTanks.Utility;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TinyTanks.Tanks
{
    public class TankWeapon : NetworkBehaviour
    {
        public string displayName;
        public Sprite icon;
        public Projectile projectile;
        public float fireRate = 600f;
        public DamageInstance damage;
        public float projectileSpeed;
        public float recoilForce;
        public bool automatic;
        public int beltSize;
        public float reloadTime;
        public float spreadAngle;

        [Space]
        public ParticleSystem fireFx;

        private Rigidbody body;
        private TankController tank;
        private float reloadTimer;
        private readonly NetworkVariable<int> beltLeft = new NetworkVariable<int>();
        private readonly NetworkVariable<int> totalShotsFired = new NetworkVariable<int>();

        public event Action WeaponFiredEvent;

        public Transform muzzle { get; private set; }
        public bool shooting { get; private set; }
        public bool isReloading => reloadTimer > 0f;
        public float currentReloadDuration { get; private set; }
        public float reloadPercent => 1f - reloadTimer / currentReloadDuration;

        private void Awake()
        {
            body = GetComponentInParent<Rigidbody>();
            tank = GetComponentInParent<TankController>();
            if (string.IsNullOrEmpty(displayName)) displayName = name;
            beltLeft.Value = beltSize;
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

            if (fireFx != null)
            {
                fireFx.transform.SetParent(muzzle);
                fireFx.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        public void SetShooting(bool shooting)
        {
            if (IsOwner) SetShootingServerRpc(shooting);
        }

        [Rpc(SendTo.Everyone)]
        private void SetShootingServerRpc(bool shooting)
        {
            this.shooting = shooting;
        }

        private void FixedUpdate()
        {
            if (!tank.canShoot || tank.isDestroyed)
            {
                shooting = false;
            }
            else
            {
                if (shooting && !isReloading && beltLeft.Value > 0)
                {
                    var rand = new System.Random(totalShotsFired.Value);
                    
                    var a0 = rand.NextFloat(0f, 360f);
                    var a1 = rand.NextFloat(-spreadAngle, spreadAngle) * 0.5f;

                    var orientation = muzzle.rotation * Quaternion.Euler(0f, 0f, a0) * Quaternion.Euler(a1, 0f, 0f);
                    var instance = Instantiate(projectile, muzzle.position, orientation);
    
                    instance.shooter = tank.NetworkObject;
                    instance.damage = damage;
                    instance.startSpeed = projectileSpeed;
    
                    instance.velocity += body.GetPointVelocity(muzzle.position);
                    WeaponFiredEvent?.Invoke();
    
                    beltLeft.Value--;

                    if (beltLeft.Value <= 0)
                    {
                        currentReloadDuration = reloadTime;
                        beltLeft.Value = 0;
                    }
                    else
                    {
                        currentReloadDuration = 60f / fireRate;
                    }
                    reloadTimer = currentReloadDuration;
                    tank.body.AddForceAtPosition(-muzzle.forward * recoilForce, muzzle.position, ForceMode.VelocityChange);
    
                    if (fireFx != null) fireFx.Play(true);
                    if (!automatic) shooting = false;

                    totalShotsFired.Value++;
                }

                if (reloadTimer > 0)
                {   
                    reloadTimer -= Time.fixedDeltaTime;
                }
                else if (beltLeft.Value == 0)
                {
                    beltLeft.Value = beltSize;
                }
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

        private void OnValidate()
        {
            beltSize = Mathf.Max(1, beltSize);
        }
    }
}