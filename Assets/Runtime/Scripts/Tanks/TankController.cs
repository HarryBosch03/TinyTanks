using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Netcode;
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
        public float suspensionExtension;

        [Space]
        public LayerMask groundCheckMask;
        public Vector3 groundCheckPoint;
        public int wheelsPerTrack = 5;
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
        public float[] sightZoomLevels = { 1f, 2f };
        public float sightZoomTime = 0.3f;

        private bool onGround;
        private Vector2 turretVelocity;
        private int sightZoomLevelIndex;
        private float sightDefaultFov;

        public Rigidbody body { get; private set; }
        public TankModel model { get; private set; }
        public bool useSight { get; private set; }
        public Vector2 cameraRotation { get; set; }

        public float throttle { get; set; }
        public float steering { get; set; }
        public Vector2 turretRotation { get; private set; }
        public Vector2 turretTarget { get; private set; }
        public Vector3 worldAimPosition { get; set; }
        public bool isActiveViewer { get; private set; }
        public float sightZoom { get; set; }

        public static List<TankController> all = new List<TankController>();

        public Vector3[] leftTrackGroundSamples { get; private set; }
        public Vector3[] rightTrackGroundSamples { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;

            leftTrackGroundSamples = new Vector3[wheelsPerTrack];
            rightTrackGroundSamples = new Vector3[wheelsPerTrack];

            model = GetComponentInChildren<TankModel>();
            model.name = $"[SIM] {model.name}";

            sightCamera.transform.SetParent(model.gunPivot);
            
            leftTrackGroundSamples[0] = transform.TransformPoint(-groundCheckPoint.x, 0f, groundCheckPoint.z);
            leftTrackGroundSamples[1] = transform.TransformPoint(-groundCheckPoint.x, 0f, -groundCheckPoint.z);
            
            rightTrackGroundSamples[0] = transform.TransformPoint(groundCheckPoint.x, 0f, groundCheckPoint.z);
            rightTrackGroundSamples[1] = transform.TransformPoint(groundCheckPoint.x, 0f, -groundCheckPoint.z);
            
            SetActiveViewer(false);

            sightDefaultFov = sightCamera.Lens.FieldOfView;
            worldAimPosition = model.gunMuzzle.position + model.gunMuzzle.forward;
        }

        private void Start()
        {
            useSight = false;
        }

        private void OnEnable()
        {
            all.Add(this);
        }

        private void OnDisable() { all.Remove(this); }

        public void SetActive(bool isActive) => SetActiveRpc(isActive);

        [Rpc(SendTo.Everyone)]
        private void SetActiveRpc(bool isActive)
        {
            gameObject.SetActive(isActive);
        }
        
        public void SetActiveViewer(bool isActiveViewer)
        {
            this.isActiveViewer = isActiveViewer;
            hud.gameObject.SetActive(isActiveViewer);
            UpdateCameraStates();
        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                SendInputDataServerRpc(new InputData(this));
            }
            
            CheckIfOnGround();
            MoveTank();
            RotateTurret();
            AlignSight();

            body.AddForce(Physics.gravity, ForceMode.Acceleration);

            if (IsServer) SendNetworkStateClientRpc(new NetworkState(this));
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SendInputDataServerRpc(InputData input)
        {
            throttle = input.throttle;
            steering = input.steering;
            worldAimPosition = input.worldAimPosition;
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void SendNetworkStateClientRpc(NetworkState state)
        {
            transform.position = state.position;
            transform.rotation = state.rotation;

            body.linearVelocity = state.linearVelocity;
            body.angularVelocity = state.angularVelocity;

            turretRotation = state.turretRotation;
            turretVelocity = state.turretVelocity;
        }

        private void RotateTurret()
        {
            MoveTurret();

            model.turretMount.localRotation = Quaternion.Euler(0f, turretRotation.x, 0f);
            model.gunPivot.localRotation = Quaternion.Euler(-turretRotation.y, 0f, 0f);
        }

        public void SetUseSight(bool useSight)
        {
            this.useSight = useSight;
            if (useSight)
            {
                sightZoomLevelIndex = 0;
                sightCamera.Lens.FieldOfView = sightDefaultFov;
            }
            UpdateCameraStates();
        }
        
        public void ToggleSightZoom()
        {
            if (useSight) sightZoomLevelIndex++;
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
        }

        public void StartShooting(int weaponIndex)
        {
            if (weapons.Length > weaponIndex) weapons[weaponIndex].SetShooting(true);
        }

        public void StopShooting(int weaponIndex)
        {
            if (weapons.Length > weaponIndex) weapons[weaponIndex].SetShooting(false);
        }

        private void MoveTurret()
        {
            var worldVector = (worldAimPosition - model.gunPivot.position).normalized;
            var localVector = transform.InverseTransformDirection(worldVector);
            var rotation = Quaternion.LookRotation(localVector, Vector3.up);
            turretTarget = new Vector2(rotation.eulerAngles.y, -rotation.eulerAngles.x);

            var delta = new Vector2()
            {
                x = Mathf.DeltaAngle(turretRotation.x, turretTarget.x),
                y = Mathf.DeltaAngle(turretRotation.y, turretTarget.y),
            };

            turretRotation += turretVelocity * Time.fixedDeltaTime;
            turretVelocity += (delta * traverseSpeedSpring - turretVelocity * traverseSpeedDamping) * Time.fixedDeltaTime;
            turretVelocity.x = Mathf.Clamp(turretVelocity.x, -maxTraverseSpeed, maxTraverseSpeed);
            turretVelocity.y = Mathf.Clamp(turretVelocity.y, -maxTraverseSpeed, maxTraverseSpeed);

            turretRotation = ClampTurretRotation(turretRotation);

            MoveCoax();
        }

        private void MoveCoax()
        {
            var coax = model.coaxBarrel;
            coax.LookAt(worldAimPosition, transform.up);
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
            
            sampleTrack(-1, leftTrackGroundSamples);
            sampleTrack(1, rightTrackGroundSamples);

            void sampleTrack(int xSign, Vector3[] samples)
            {
                var start = transform.TransformPoint(groundCheckPoint.x * xSign, 0f, groundCheckPoint.z);
                var end = transform.TransformPoint(groundCheckPoint.x * xSign, 0f, -groundCheckPoint.z);
                
                for (var i = 0; i < samples.Length; i++)
                {
                    var percent = i / (samples.Length - 1f);
                    var point = Vector3.Lerp(start, end, percent);
                    var distance = groundCheckPoint.y + suspensionExtension;
                    var ray = new Ray(point + transform.up * groundCheckPoint.y, -transform.up);
                    var hits = Physics.RaycastAll(ray, distance, groundCheckMask).OrderBy(e => e.distance);
                    samples[i] = point;
                    foreach (var hit in hits)
                    {
                        if (hit.collider.transform.IsChildOf(transform)) continue;

                        onGround = true;

                        var velocity = body.GetPointVelocity(hit.point);
                        var force = hit.normal * ((distance - hit.distance) * suspensionSpring - Vector3.Dot(hit.normal, velocity) * suspensionDamping) * body.mass;
                        body.AddForceAtPosition(force, hit.point);

                        samples[i] = hit.point;
                        break;
                    }
                }
            }
        }

        private void MoveTank()
        {
            if (!onGround) return;

            var localVelX = Vector3.Dot(transform.right, body.linearVelocity);
            var localVelZ = Vector3.Dot(transform.forward, body.linearVelocity);

            var maxSpeed = throttle > 0f ? maxFwdSpeed : maxReverseSpeed;
            if (Mathf.Abs(throttle * maxSpeed) > Mathf.Abs(localVelZ))
            {
                var t = throttle > 0f ? Mathf.InverseLerp(0f, maxFwdSpeed, localVelZ) : Mathf.InverseLerp(0f, -maxReverseSpeed, localVelZ);
                var acceleration = maxSpeed / accelerationTime * accelerationCurve.Evaluate(t);

                localVelZ = Mathf.MoveTowards(localVelZ, throttle * maxSpeed, acceleration * Time.fixedDeltaTime);
            }
            else
            {
                localVelZ = Mathf.MoveTowards(localVelZ, throttle * maxSpeed, maxFwdSpeed * Time.fixedDeltaTime / brakeTime);
            }

            localVelX = (0f - localVelX) * xFriction;

            var localAngularVelocity = transform.InverseTransformVector(body.angularVelocity);
            localAngularVelocity.y += (maxTurnSpeed * steering - localAngularVelocity.y) * 2f * Time.fixedDeltaTime / turnAccelerationTime;
            body.AddTorque(transform.TransformVector(localAngularVelocity) - body.angularVelocity, ForceMode.VelocityChange);

            var newVelocity = transform.right * localVelX + transform.forward * localVelZ + Vector3.Project(body.linearVelocity, transform.up);
            newVelocity += Vector3.ProjectOnPlane(-Physics.gravity, transform.up) * Time.fixedDeltaTime;
            var force = (newVelocity - body.linearVelocity) / Time.fixedDeltaTime;

            body.AddForce(newVelocity - body.linearVelocity, ForceMode.VelocityChange);
            body.AddTorque((transform.forward * Vector3.Dot(force, transform.right) - transform.right * Vector3.Dot(force, transform.forward)) * moveTilt, ForceMode.Acceleration);
        }

        private void Update()
        {
            AlignSight();

            if (useSight)
            {
                sightZoomLevelIndex = (sightZoomLevelIndex % sightZoomLevels.Length + sightZoomLevels.Length) % sightZoomLevels.Length;
                var zoom = sightZoomLevels[sightZoomLevelIndex];
                var fov = Mathf.Atan(Mathf.Tan(sightDefaultFov * 0.5f * Mathf.Deg2Rad) / zoom) * 2f * Mathf.Rad2Deg;
                sightCamera.Lens.FieldOfView = Mathf.Lerp(sightCamera.Lens.FieldOfView, fov, 2f * Time.deltaTime / sightZoomTime);
                sightZoom = Mathf.Tan(sightDefaultFov * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(sightCamera.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);
            }
            else
            {
                sightZoom = 1f;
            }

            model.turretMount.localRotation = Quaternion.Euler(0f, turretRotation.x, 0f);
            model.gunPivot.localRotation = Quaternion.Euler(-turretRotation.y, 0f, 0f);
        }

        private void AlignSight()
        {
            sightCamera.transform.rotation = Quaternion.LookRotation(worldAimPosition - sightCamera.transform.position, transform.up);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;

            var start = new Vector3(groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z);
            var end = new Vector3(groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z);

            for (var i = 0; i <wheelsPerTrack; i++)
            {
                var percent = i / (wheelsPerTrack - 1f);
                var point = Vector3.Lerp(start, end, percent);
                Gizmos.DrawRay(point, Vector3.down * groundCheckPoint.y);
                Gizmos.DrawRay(new Vector3(-point.x, point.y, point.z), Vector3.down * groundCheckPoint.y);
            }
            
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        private void OnValidate()
        {
            wheelsPerTrack = Mathf.Max(wheelsPerTrack, 2);
            if (sightZoomLevels == null || sightZoomLevels.Length == 0) sightZoomLevels = new float[1];
            sightZoomLevels[0] = 1f;
            for (var i = 1; i < sightZoomLevels.Length; i++) sightZoomLevels[i] = Mathf.Max(sightZoomLevels[i], 1f);
        }

        public struct InputData : INetworkSerializable
        {
            public float throttle;
            public float steering;
            public Vector3 worldAimPosition;

            public InputData(TankController tank)
            {
                throttle = tank.throttle;
                steering = tank.steering;
                worldAimPosition = tank.worldAimPosition;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref throttle);
                serializer.SerializeValue(ref steering);
                serializer.SerializeValue(ref worldAimPosition);
            }
        }
        
        public struct NetworkState : INetworkSerializable
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 linearVelocity;
            public Vector3 angularVelocity;
            public Vector2 turretRotation;
            public Vector2 turretVelocity;

            public NetworkState(TankController tank)
            {
                position = tank.transform.position;
                rotation = tank.transform.rotation;

                linearVelocity = tank.body.linearVelocity;
                angularVelocity = tank.body.angularVelocity;

                turretRotation = tank.turretRotation;
                turretVelocity = tank.turretVelocity;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeValue(ref linearVelocity);
                serializer.SerializeValue(ref angularVelocity);
                serializer.SerializeValue(ref turretRotation);
                serializer.SerializeValue(ref turretVelocity);
            }
        }
    }
}