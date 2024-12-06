using System;
using FishNet;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using TinyTanks.Health;
using TinyTanks.Projectiles;
using UnityEngine;

namespace TinyTanks.Tanks
{
    [DefaultExecutionOrder(-50)]
    public class TankWeapon : NetworkBehaviour
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

        public event Action WeaponFiredEvent;

        public Transform muzzle { get; private set; }
        public bool shooting { get; private set; }
        public TimeManager timeManager => InstanceFinder.TimeManager;
        public float reloadTimer { get; private set; }
        public bool isReloading => reloadTimer > 0f;
        public float reloadPercent => 1f - reloadTimer / fireDelay;

        private void Awake()
        {
            body = GetComponentInParent<Rigidbody>();
            tank = GetComponentInParent<TankController>();
            if (string.IsNullOrEmpty(displayName)) displayName = name;

        }

        public override void OnStartNetwork()
        {
            TimeManager.OnTick += OnTick;
        }

        public override void OnStopNetwork()
        {
            TimeManager.OnTick -= OnTick;
        }

        private void OnTick()
        {
            RunInputs(CreateReplicateData());
            CreateReconcile();
        }

        public override void CreateReconcile()
        {
            var data = new ReconcileData();
            data.reloadTimer = reloadTimer;

            ReconcileState(data);
        }

        [Reconcile]
        private void ReconcileState(ReconcileData data, Channel channel = Channel.Unreliable)
        {
            reloadTimer = data.reloadTimer;
        }

        private ReplicateData CreateReplicateData()
        {
            var data = new ReplicateData()
            {
                shooting = shooting,
            };

            return data;
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

        public void SetShooting(bool shooting)
        {
            this.shooting = shooting;
        }

        [Replicate]
        private void RunInputs(ReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            Debug.Log($"Frame: {data.GetTick()} || 1");
            
            if (data.shooting && reloadTimer <= 0f)
            {
                if (state == ReplicateState.CurrentCreated)
                {
                    var instance = Instantiate(projectile, muzzle.position, muzzle.rotation);
                
                    instance.shooter = tank.NetworkObject;
                    instance.damage = damage;
                    instance.startSpeed = projectileSpeed;
                
                    instance.velocity += body.GetPointVelocity(muzzle.position);
                    WeaponFiredEvent?.Invoke();
                }
                reloadTimer = fireDelay;

                tank.predictionBody.AddForceAtPosition(-muzzle.forward * recoilForce, muzzle.position, ForceMode.VelocityChange);
                
                if (fireFx != null && !(tank.isActiveViewer && tank.sightCamera)) fireFx.Play(true);
                
                if (!automatic) shooting = false;
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
        
        
        public struct ReconcileData : IReconcileData
        {
            public float reloadTimer;

            private uint tick;

            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
            public void Dispose() { }
        }

        public struct ReplicateData : IReplicateData
        {
            public bool shooting;

            private uint tick;

            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
            public void Dispose() { }
        }
    }
}