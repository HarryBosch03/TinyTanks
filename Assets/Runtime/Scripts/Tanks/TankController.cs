using System;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using Unity.Cinemachine;
using UnityEngine;

namespace TinyTanks.Tanks
{
    [RequireComponent(typeof(Rigidbody))]
    public class TankController : NetworkBehaviour
    {
        public float maxSpeed;
        public float accelerationTime;
        [Range(0f, 1f)]
        public float xFriction = 0.8f;
        [Range(0f, 1f)]
        public float zFriction = 0.8f;
        public float moveTilt;

        [Space]
        public float maxTurnSpeed;
        public float turnAccelerationTime;

        [Space]
        public float suspensionSpring = 30f;
        public float suspensionDamping = 3f;
        public float suspensionOffset = 0f;

        [Space]
        public LayerMask groundCheckMask;
        public Vector3 groundCheckPoint;

        [Space]
        public Transform turretAzimuthRotor;
        public Transform turretAltitudeRotor;
        public bool limitTurretX;
        public Vector2 turretLimitX;
        public Vector2 turretLimitY;

        [Space]
        public TankWeapon[] weapons;

        [Space]
        public CinemachineFollow followCamera;
        public Vector3 followCameraOffset = new Vector3(0f, 0.8f, -2f);
        public CinemachineCamera sightCamera;

        private PredictionRigidbody predictionBody;
        
        private bool onGround;
        private Vector3 worldAimVector;
        private float trackSpeed;

        public Rigidbody body { get; private set; }
        public bool stabsEnabled { get; private set; }
        public bool useSight { get; private set; }
        public float freeLookRotation { get; set; }

        public float throttle { get; set; }
        public float steering { get; set; }
        public Vector2 turretRotation { get; private set; }
        public Vector2 turretDelta { get; set; }
        public bool isActiveViewer { get; private set; }

        public Vector3[] leftTrackGroundSamples { get; } = new Vector3[2];
        public Vector3[] rightTrackGroundSamples { get; } = new Vector3[2];

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            predictionBody = ObjectCaches<PredictionRigidbody>.Retrieve();
            predictionBody.Initialize(body);
        }

        private void OnDestroy()
        {
            ObjectCaches<PredictionRigidbody>.StoreAndDefault(ref predictionBody);
        }

        private void Start()
        {
            SetUseSight(false);
            SetStabs(true);
        }

        public override void OnStartNetwork()
        {
            TimeManager.OnTick += OnTick;
            TimeManager.OnPostTick += OnPostTick;

            SetIsActiveViewer(Owner.IsLocalClient);
            worldAimVector = transform.forward;
        }

        private void SetIsActiveViewer(bool isActiveViewer)
        {
            this.isActiveViewer = isActiveViewer;
            UpdateCameraStates();
        }

        public override void OnStopNetwork()
        {
            TimeManager.OnTick -= OnTick;
            TimeManager.OnPostTick -= OnPostTick;
        }

        private void OnTick() { RunInputs(CreateReplicateData()); }

        private ReplicateData CreateReplicateData()
        {
            if (!IsOwner) return default;

            var data = new ReplicateData();

            data.throttle = throttle;
            data.steering = steering;
            data.stabsEnabled = stabsEnabled;
            data.useSight = useSight;
            data.turretDelta = turretDelta;
            if (weapons.Length > 0) data.weapon0Shooting = weapons[0].shooting;
            if (weapons.Length > 1) data.weapon1Shooting = weapons[1].shooting;

            turretDelta = Vector2.zero;

            return data;
        }

        [Replicate]
        private void RunInputs(ReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            if (!IsOwner)
            {
                if (data.stabsEnabled != stabsEnabled) SetStabs(data.stabsEnabled);
                if (data.useSight != useSight) SetUseSight(data.useSight);
                if (weapons.Length > 0 && data.weapon0Shooting != weapons[0].shooting) weapons[0].SetShooting(data.weapon0Shooting);
                if (weapons.Length > 1 && data.weapon1Shooting != weapons[1].shooting) weapons[1].SetShooting(data.weapon1Shooting);
            }

            CheckIfOnGround();
            MoveTank(data);
            RotateTurret(data);
            
            predictionBody.Simulate();
        }

        private void OnPostTick() { CreateReconcile(); }

        public override void CreateReconcile()
        {
            var data = new ReconcileData();

            data.predictionBody = predictionBody;
            data.turretRotation = turretRotation;
            data.worldAimVector = worldAimVector;
            data.trackSpeed = trackSpeed;

            ReconcileState(data);
        }

        [Reconcile]
        private void ReconcileState(ReconcileData data, Channel channel = Channel.Unreliable)
        {
            predictionBody.Reconcile(data.predictionBody);
            turretRotation = data.turretRotation;
            worldAimVector = data.worldAimVector;
            trackSpeed = data.trackSpeed;
        }

        public void SetStabs(bool stabsEnabled) { this.stabsEnabled = stabsEnabled; }

        public void SetUseSight(bool useSight)
        {
            this.useSight = useSight;
            UpdateCameraStates();
        }

        private void UpdateCameraStates()
        {
            if (isActiveViewer)
            {
                followCamera.gameObject.SetActive(!useSight);
                sightCamera.gameObject.SetActive(useSight);
            }
            else
            {
                followCamera.gameObject.SetActive(false);
                sightCamera.gameObject.SetActive(false);
            }

            if (!followCamera.gameObject.activeSelf) freeLookRotation = 0f;
        }

        public void StartShooting(int weaponIndex)
        {
            if (weapons.Length > weaponIndex) weapons[weaponIndex].SetShooting(true);
        }

        public void StopShooting(int weaponIndex)
        {
            if (weapons.Length > weaponIndex) weapons[weaponIndex].SetShooting(false);
        }

        public Vector3 PredictProjectileArc() { return weapons.Length > 0 ? weapons[0].PredictProjectileArc() : turretAltitudeRotor.position + turretAltitudeRotor.forward * 1024f; }

        private (Vector2 turretRotation, Vector3 worldAimVector) GetTurretRotation(Vector2 turretDelta)
        {
            var turretRotation = this.turretRotation;
            var worldAimVector = this.worldAimVector;

            var refTransform = NetworkObject.GetGraphicalObject();
            if (refTransform == null) refTransform = transform;
            
            if (stabsEnabled)
            {
                worldAimVector = (Quaternion.Euler(turretAzimuthRotor.right * -turretDelta.y + transform.up * turretDelta.x) * worldAimVector).normalized;
                var localVector = refTransform.InverseTransformDirection(worldAimVector);
                var rotation = Quaternion.LookRotation(localVector, Vector3.up);
                turretRotation = new Vector2(rotation.eulerAngles.y, -rotation.eulerAngles.x);
            }
            else
            {
                turretRotation += turretDelta;
            }

            turretRotation = ClampTurretRotation(turretRotation);
            worldAimVector = refTransform.rotation * Quaternion.Euler(-turretRotation.y, turretRotation.x, 0f) * Vector3.forward;

            return (turretRotation, worldAimVector);
        }

        private void RotateTurret(ReplicateData data)
        {
            (turretRotation, worldAimVector) = GetTurretRotation(data.turretDelta);

            if (turretAzimuthRotor != null) turretAzimuthRotor.localRotation = Quaternion.Euler(0f, turretRotation.x, 0f);
            if (turretAltitudeRotor != null) turretAltitudeRotor.localRotation = Quaternion.Euler(-turretRotation.y, 0f, 0f);
        }

        private Vector2 ClampTurretRotation(Vector2 turretRotation)
        {
            turretRotation.x = ((turretRotation.x + 180f) % 360f + 360f) % 360f - 180f;
            turretRotation.y = ((turretRotation.y + 180f) % 360f + 360f) % 360f - 180f;

            if (limitTurretX) turretRotation.x = Mathf.Clamp(turretRotation.x, turretLimitX.x, turretLimitX.y);
            turretRotation.y = Mathf.Clamp(turretRotation.y, turretLimitY.x, turretLimitY.y);

            return turretRotation;
        }

        private void CheckIfOnGround()
        {
            onGround = false;

            sampleTrack(new[]
            {
                transform.TransformPoint(-groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z),
                transform.TransformPoint(-groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z),
            }, leftTrackGroundSamples);

            sampleTrack(new[]
            {
                transform.TransformPoint(groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z),
                transform.TransformPoint(groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z),
            }, rightTrackGroundSamples);

            void sampleTrack(Vector3[] points, Vector3[] samples)
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var point = points[i] + Vector3.up * suspensionOffset;
                    var distance = 0.2f;
                    var ray = new Ray(point + transform.up * distance, -transform.up);
                    if (Physics.Raycast(ray, out var hit, distance, groundCheckMask))
                    {
                        onGround = true;

                        var velocity = body.GetPointVelocity(hit.point);
                        var force = ((hit.point - point) * suspensionSpring - velocity * suspensionDamping) * body.mass;
                        force = Vector3.Project(force, hit.normal);
                        predictionBody.AddForceAtPosition(force, hit.point);

                        samples[i] = hit.point;
                    }
                    else
                    {
                        samples[i] = point;
                    }
                }
            }
        }

        private void MoveTank(ReplicateData data)
        {
            if (!onGround) return;

            var localVelX = Vector3.Dot(transform.right, body.linearVelocity);
            var localVelZ = Vector3.Dot(transform.forward, body.linearVelocity);

            var torque = (data.throttle * maxSpeed - trackSpeed) * 2f / Mathf.Max(Time.fixedDeltaTime, accelerationTime);
            trackSpeed += torque * Time.fixedDeltaTime;

            localVelX = (0f - localVelX) * xFriction;
            localVelZ += (trackSpeed - localVelZ) * zFriction;

            var localAngularVelocity = transform.InverseTransformVector(body.angularVelocity);
            localAngularVelocity.y += (maxTurnSpeed * data.steering - localAngularVelocity.y) * 2f * Time.fixedDeltaTime / turnAccelerationTime;
            body.angularVelocity = transform.TransformVector(localAngularVelocity);

            var newVelocity = transform.right * localVelX + transform.forward * localVelZ + Vector3.Project(body.linearVelocity, transform.up);
            newVelocity += Vector3.ProjectOnPlane(-Physics.gravity, transform.up) * Time.fixedDeltaTime;
            var force = (newVelocity - body.linearVelocity) / Time.fixedDeltaTime;

            body.linearVelocity = newVelocity;
            body.angularVelocity += (transform.forward * Vector3.Dot(force, transform.right) - transform.right * Vector3.Dot(force, transform.forward)) * moveTilt * Time.fixedDeltaTime;
        }

        private void Update()
        {
            AlignSight();

            freeLookRotation %= 360f;
            followCamera.FollowOffset = Quaternion.Euler(0f, freeLookRotation, 0f) * followCameraOffset;

            var turretRotation = GetTurretRotation(turretDelta).turretRotation;
            if (turretAzimuthRotor != null) turretAzimuthRotor.localRotation = Quaternion.Euler(0f, turretRotation.x, 0f);
            if (turretAltitudeRotor != null) turretAltitudeRotor.localRotation = Quaternion.Euler(-turretRotation.y, 0f, 0f);
        }

        private void AlignSight()
        {
            if (weapons.Length == 0)
            {
                sightCamera.transform.localRotation = Quaternion.identity;
                return;
            }

            const float maxRange = 1024f;
            var ray = new Ray(weapons[0].muzzle.position, weapons[0].muzzle.forward);
            var point = ray.GetPoint(maxRange);
            if (Physics.Raycast(ray, out var hit, maxRange))
            {
                point = hit.point;
            }

            sightCamera.transform.LookAt(point);
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;

            Gizmos.DrawRay(new Vector3(groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z), Vector3.up);
            Gizmos.DrawRay(new Vector3(-groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z), Vector3.up);
            Gizmos.DrawRay(new Vector3(-groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z), Vector3.up);
            Gizmos.DrawRay(new Vector3(groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z), Vector3.up);
        }

        public struct ReconcileData : IReconcileData
        {
            public PredictionRigidbody predictionBody;
            public Vector2 turretRotation;
            public Vector3 worldAimVector;
            public float trackSpeed;

            private uint tick;

            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
            public void Dispose() { }
        }

        public struct ReplicateData : IReplicateData
        {
            public float throttle;
            public float steering;

            public Vector2 turretDelta;

            public bool stabsEnabled;
            public bool useSight;
            public bool weapon0Shooting;
            public bool weapon1Shooting;

            private uint tick;

            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
            public void Dispose() { }
        }
    }
}