using System;
using System.Collections.Generic;
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
        public float maxFwdSpeed = 12f;
        public float maxReverseSpeed = 4f;
        public float accelerationTime = 1.5f;
        public AnimationCurve accelerationCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        public float brakeTime = 1f;
        [Range(0f, 1f)]
        public float xFriction = 0.4f;
        public float moveTilt = 0.2f;

        [Space]
        public float maxTraverseSpeed;
        public float traverseSpeedSpring;
        public float traverseSpeedDamping;

        [Space]
        public float maxTurnSpeed;
        public float turnAccelerationTime;

        [Space]
        public float suspensionSpring = 30f;
        public float suspensionDamping = 3f;

        [Space]
        public LayerMask groundCheckMask;
        public Vector3 groundCheckPoint;
        public Canvas hud;

        [Space]
        public bool limitTurretX;
        public Vector2 turretLimitX;
        public Vector2 turretLimitY;

        [Space]
        public Bounds bounds;

        [Space]
        public TankWeapon[] weapons;

        [Space]
        public CinemachineTankFollowCamera followCamera;
        public CinemachineCamera sightCamera;

        private bool changeUseSight;
        private bool onGround;
        private Vector2 traverseVelocity;
        private ReplicateData lastValidReplicateData;

        public PredictionRigidbody predictionBody;
        
        public Rigidbody body { get; private set; }
        public TankModel model { get; private set; }
        public bool useSight { get; private set; }
        public Vector3 sightAimPoint { get; set; }
        
        public float throttle { get; set; }
        public float steering { get; set; }
        public Vector2 turretRotation { get; private set; }
        public Vector3 worldAimVector { get; set; }
        public bool isActiveViewer { get; private set; }
        
        public static List<TankController> all = new List<TankController>();

        public Vector3[] leftTrackGroundSamples { get; } = new Vector3[2];
        public Vector3[] rightTrackGroundSamples { get; } = new Vector3[2];

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            predictionBody = ObjectCaches<PredictionRigidbody>.Retrieve();
            predictionBody.Initialize(body);
            body.useGravity = false;
            
            model = GetComponentInChildren<TankModel>();
            sightCamera.transform.SetParent(model.gunPivot);
            
            SetActiveViewer(false);
        }

        private void OnDestroy()
        {
            var body = predictionBody;
            ObjectCaches<PredictionRigidbody>.StoreAndDefault(ref body);
            predictionBody = body;
        }

        private void Start()
        {
            useSight = false;
            changeUseSight = false;
        }

        private void OnEnable()
        {
            all.Add(this);
            worldAimVector = transform.forward;
        }

        private void OnDisable()
        {
            all.Remove(this);
        }

        [Server]
        public void SetActive(bool isActive)
        {
            SetActiveRpc(isActive);
        }

        [ObserversRpc(RunLocally = true, ExcludeOwner = false, ExcludeServer = false)]
        private void SetActiveRpc(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public override void OnStartNetwork()
        {
            TimeManager.OnTick += OnTick;
            TimeManager.OnPostTick += OnPostTick;
        }

        public void SetActiveViewer(bool isActiveViewer)
        {
            this.isActiveViewer = isActiveViewer;
            hud.gameObject.SetActive(isActiveViewer);
            UpdateCameraStates();
        }

        public override void OnStopNetwork()
        {
            TimeManager.OnTick -= OnTick;
            TimeManager.OnPostTick -= OnPostTick;
        }

        private void OnTick()
        {
            model.transform.position = transform.position;
            model.transform.rotation = transform.rotation;
            
            RunInputs(CreateReplicateData());
        }

        private ReplicateData CreateReplicateData()
        {
            if (!HasAuthority) return default;

            var data = new ReplicateData();

            data.throttle = throttle;
            data.steering = steering;
            data.changeUseSight = changeUseSight;
            data.worldAimVector = worldAimVector;

            return data;
        }

        [Replicate]
        private void RunInputs(ReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            if (state.IsFuture())
            {
                data = lastValidReplicateData;
            }
            else if (state == ReplicateState.ReplayedCreated)
            {
                lastValidReplicateData = data;
            }
            
            if (useSight != data.changeUseSight)
            {
                useSight = data.changeUseSight;
                UpdateCameraStates();
            }
            
            CheckIfOnGround();
            MoveTank(data);
            RotateTurret(data);

            predictionBody.AddForce(Physics.gravity, ForceMode.Acceleration);
            predictionBody.Simulate();
        }

        private void RotateTurret(ReplicateData data)
        {
            MoveTurret(data);

            model.turretMount.localRotation = Quaternion.Euler(0f, turretRotation.x, 0f);
            model.gunPivot.localRotation = Quaternion.Euler(-turretRotation.y, 0f, 0f);
        }

        private void OnPostTick() { CreateReconcile(); }

        public override void CreateReconcile()
        {
            var data = new ReconcileData();

            data.predictionBody = predictionBody;
            data.turretRotation = turretRotation;
            data.useSight = useSight;
            
            ReconcileState(data);
        }

        [Reconcile]
        private void ReconcileState(ReconcileData data, Channel channel = Channel.Unreliable)
        {
            predictionBody.Reconcile(data.predictionBody);
            turretRotation = data.turretRotation;
            if (useSight != data.useSight)
            {
                useSight = data.useSight;
                UpdateCameraStates();
            }
        }

        public void SetUseSight(bool useSight) => changeUseSight = useSight;

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
        }

        public void StartShooting(int weaponIndex)
        {
            if (weapons.Length > weaponIndex) weapons[weaponIndex].SetShooting(true);
        }

        public void StopShooting(int weaponIndex)
        {
            if (weapons.Length > weaponIndex) weapons[weaponIndex].SetShooting(false);
        }

        private void MoveTurret(ReplicateData data)
        {
            var localVector = transform.InverseTransformDirection(data.worldAimVector);
            var rotation = Quaternion.LookRotation(localVector, Vector3.up);
            var target = new Vector2(rotation.eulerAngles.y, -rotation.eulerAngles.x);
            
            var delta = new Vector2()
            {
                x = Mathf.DeltaAngle(turretRotation.x, target.x),
                y = Mathf.DeltaAngle(turretRotation.y, target.y),
            };

            turretRotation += traverseVelocity * Time.fixedDeltaTime;
            traverseVelocity += (delta * traverseSpeedSpring - traverseVelocity * traverseSpeedDamping) * Time.fixedDeltaTime;
            traverseVelocity.x = Mathf.Clamp(traverseVelocity.x, -maxTraverseSpeed, maxTraverseSpeed);
            traverseVelocity.y = Mathf.Clamp(traverseVelocity.y, -maxTraverseSpeed, maxTraverseSpeed);
            
            turretRotation = ClampTurretRotation(turretRotation);
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
                    var point = points[i];
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

            var maxSpeed = data.throttle > 0f ? maxFwdSpeed : maxReverseSpeed;
            if (Mathf.Abs(data.throttle * maxSpeed) > Mathf.Abs(localVelZ))
            {
                var t = data.throttle > 0f ? Mathf.InverseLerp(0f, maxFwdSpeed, localVelZ) : Mathf.InverseLerp(0f, -maxReverseSpeed, localVelZ);
                var acceleration = maxSpeed / accelerationTime * accelerationCurve.Evaluate(t);
                
                localVelZ = Mathf.MoveTowards(localVelZ, data.throttle * maxSpeed, acceleration * Time.fixedDeltaTime);
            }
            else
            {
                localVelZ = Mathf.MoveTowards(localVelZ, data.throttle * maxSpeed, maxFwdSpeed * Time.fixedDeltaTime / brakeTime);
            }
            
            localVelX = (0f - localVelX) * xFriction;

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

            model.turretMount.localRotation = Quaternion.Euler(0f, turretRotation.x, 0f);
            model.gunPivot.localRotation = Quaternion.Euler(-turretRotation.y, 0f, 0f);
        }

        private void LateUpdate()
        {
            var graphicalObject = NetworkObject.GetGraphicalObject();
            if (graphicalObject == null) graphicalObject = transform;
            model.transform.position = graphicalObject.position;
            model.transform.rotation = graphicalObject.rotation;
        }

        private void AlignSight()
        {
            if (weapons.Length == 0)
            {
                sightCamera.transform.localRotation = Quaternion.identity;
                return;
            }
            else
            {
                sightCamera.transform.rotation = Quaternion.LookRotation(worldAimVector, transform.up);
            }

            const float maxRange = 1024f;
            var ray = new Ray(weapons[0].muzzle.position, weapons[0].muzzle.forward);
            var point = ray.GetPoint(maxRange);
            if (Physics.Raycast(ray, out var hit, maxRange))
            {
                point = hit.point;
            }

            sightAimPoint = point;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;

            Gizmos.DrawRay(new Vector3(groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z), Vector3.up);
            Gizmos.DrawRay(new Vector3(-groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z), Vector3.up);
            Gizmos.DrawRay(new Vector3(-groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z), Vector3.up);
            Gizmos.DrawRay(new Vector3(groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z), Vector3.up);

            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        public struct ReconcileData : IReconcileData
        {
            public PredictionRigidbody predictionBody;
            public Vector2 turretRotation;
            public Vector2 traverseVelocity;
            
            public bool useSight;

            private uint tick;

            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
            public void Dispose() { }
        }

        public struct ReplicateData : IReplicateData
        {
            public float throttle;
            public float steering;
            public bool changeUseSight;

            public Vector3 worldAimVector;

            private uint tick;

            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
            public void Dispose() { }
        }
    }
}