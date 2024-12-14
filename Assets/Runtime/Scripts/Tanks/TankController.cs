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
        private Vector2 turretVelocity;

        public float targetingRange { get; private set; }
        public Rigidbody body { get; private set; }
        public TankModel model { get; private set; }
        public bool useSight { get; private set; }

        public float throttle { get; set; }
        public float steering { get; set; }
        public Vector2 turretRotation { get; private set; }
        public Vector2 turretTarget { get; private set; }
        public Vector3 worldAimVector { get; set; }
        public bool isActiveViewer { get; private set; }

        public static List<TankController> all = new List<TankController>();

        public Vector3[] leftTrackGroundSamples { get; } = new Vector3[2];
        public Vector3[] rightTrackGroundSamples { get; } = new Vector3[2];

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;

            model = GetComponentInChildren<TankModel>();
            model.name = $"[SIM] {model.name}";

            sightCamera.transform.SetParent(model.gunPivot);
            
            leftTrackGroundSamples[0] = transform.TransformPoint(-groundCheckPoint.x, 0f, groundCheckPoint.z);
            leftTrackGroundSamples[1] = transform.TransformPoint(-groundCheckPoint.x, 0f, -groundCheckPoint.z);
            
            rightTrackGroundSamples[0] = transform.TransformPoint(groundCheckPoint.x, 0f, groundCheckPoint.z);
            rightTrackGroundSamples[1] = transform.TransformPoint(groundCheckPoint.x, 0f, -groundCheckPoint.z);
            
            SetActiveViewer(false);
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
            
            if (useSight != changeUseSight)
            {
                useSight = changeUseSight;
                UpdateCameraStates();
            }

            CheckIfOnGround();
            MoveTank();
            RotateTurret();
            AlignSight(worldAimVector);

            body.AddForce(Physics.gravity, ForceMode.Acceleration);

            if (IsServer) SendNetworkStateClientRpc(new NetworkState(this));
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SendInputDataServerRpc(InputData input)
        {
            throttle = input.throttle;
            steering = input.steering;
            worldAimVector = input.worldAimVector;
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

        private void MoveTurret()
        {
            var ray = new Ray(sightCamera.transform.position, sightCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit, 1024))
            {
                targetingRange = Mathf.Max(hit.distance, 5f);
            }
            else
            {
                targetingRange = 1024f;
            }

            var aimPoint = sightCamera.transform.position + sightCamera.transform.forward * targetingRange;
            var worldVector = (aimPoint - model.gunPivot.position).normalized;
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
                transform.TransformPoint(-groundCheckPoint.x, 0f, groundCheckPoint.z),
                transform.TransformPoint(-groundCheckPoint.x, 0f, -groundCheckPoint.z),
            }, leftTrackGroundSamples);

            sampleTrack(new[]
            {
                transform.TransformPoint(groundCheckPoint.x, 0f, groundCheckPoint.z),
                transform.TransformPoint(groundCheckPoint.x, 0f, -groundCheckPoint.z),
            }, rightTrackGroundSamples);

            void sampleTrack(Vector3[] points, Vector3[] samples)
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    var distance = groundCheckPoint.y;
                    var ray = new Ray(point + transform.up * distance, -transform.up);
                    var hits = Physics.RaycastAll(ray, distance, groundCheckMask).OrderBy(e => e.distance);
                    samples[i] = point;
                    foreach (var hit in hits)
                    {
                        if (hit.collider.transform.IsChildOf(transform)) continue;

                        onGround = true;

                        var velocity = body.GetPointVelocity(hit.point);
                        var force = ((hit.point - point) * suspensionSpring - velocity * suspensionDamping) * body.mass;
                        force = Vector3.Project(force, hit.normal);
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
            AlignSight(worldAimVector);

            model.turretMount.localRotation = Quaternion.Euler(0f, turretRotation.x, 0f);
            model.gunPivot.localRotation = Quaternion.Euler(-turretRotation.y, 0f, 0f);
        }

        private void AlignSight(Vector3 worldAimVector)
        {
            sightCamera.transform.rotation = Quaternion.LookRotation(worldAimVector, transform.up);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;

            Gizmos.DrawRay(new Vector3(groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z), Vector3.down * groundCheckPoint.y);
            Gizmos.DrawRay(new Vector3(-groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z), Vector3.down * groundCheckPoint.y);
            Gizmos.DrawRay(new Vector3(-groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z), Vector3.down * groundCheckPoint.y);
            Gizmos.DrawRay(new Vector3(groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z), Vector3.down * groundCheckPoint.y);

            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        public struct InputData : INetworkSerializable
        {
            public float throttle;
            public float steering;
            public Vector3 worldAimVector;

            public InputData(TankController tank)
            {
                throttle = tank.throttle;
                steering = tank.steering;
                worldAimVector = tank.worldAimVector;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref throttle);
                serializer.SerializeValue(ref steering);
                serializer.SerializeValue(ref worldAimVector);
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