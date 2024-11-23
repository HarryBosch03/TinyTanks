using System;
using UnityEngine;
using Unity.Cinemachine;

namespace TinyTanks.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class TankController : MonoBehaviour
    {
        public float maxSpeed;
        public float accelerationTime;

        [Space]
        public float maxTurnSpeed;
        public float turnAccelerationTime;

        [Space]
        public float groundSpring;
        public float groundDamping;

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
        public TankWeapon currentWeapon;
        public TankWeapon[] weapons;
        
        [Space]
        public CinemachineCamera followCamera;
        public CinemachineCamera sightCamera;

        private Camera mainCamera;
        private Rigidbody body;
        private bool onGround;
        private Quaternion lastOrientation;

        public event Action ChangeWeaponEvent;
        
        public bool stabsEnabled { get; private set; }
        public bool useSight { get; private set; }

        public float throttle { get; set; }
        public float steering { get; set; }
        public Vector2 turretRotation { get; set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            mainCamera = Camera.main;
        }

        private void Start()
        {
            SetUseSight(false);
            SetStabs(true);
        }

        public void SetStabs(bool stabsEnabled)
        {
            this.stabsEnabled = stabsEnabled;
        }
        
        public void SetUseSight(bool useSight)
        {
            this.useSight = useSight;

            followCamera.gameObject.SetActive(!useSight);
            sightCamera.gameObject.SetActive(useSight);
        }

        public void StartShooting()
        {
            if (currentWeapon != null) currentWeapon.StartShooting();
        }

        public void StopShooting()
        {
            if (currentWeapon != null) currentWeapon.StopShooting();
        }

        public void ChangeWeapon()
        {
            var index = Array.IndexOf(weapons, currentWeapon);
            var c = weapons.Length;
            index = ((index + 1) % c + c) % c;

            if (currentWeapon != null) currentWeapon.StopShooting();
            
            currentWeapon = weapons[index];
            
            ChangeWeaponEvent?.Invoke();
        }
        
        private void FixedUpdate()
        {
            CheckIfOnGround();
            MoveTank();
            RotateTurret();
        }

        public Vector3 PredictProjectileArc()
        {
            return currentWeapon != null ? currentWeapon.PredictProjectileArc() : turretAltitudeRotor.position + turretAltitudeRotor.forward * 1000f;
        }

        private void RotateTurret()
        {
            ApplyStabs();
            var turretRotation = this.turretRotation;

            turretRotation.x = ((turretRotation.x + 180f) % 360f + 360f) % 360f - 180f;
            turretRotation.y = ((turretRotation.y + 180f) % 360f + 360f) % 360f - 180f;
            
            if (limitTurretX) turretRotation.x = Mathf.Clamp(turretRotation.x, turretLimitX.x, turretLimitX.y);
            turretRotation.y = Mathf.Clamp(turretRotation.y, turretLimitY.x, turretLimitY.y);

            if (turretAzimuthRotor != null) turretAzimuthRotor.localRotation = Quaternion.Euler(0f, turretRotation.x, 0f);
            if (turretAltitudeRotor != null) turretAltitudeRotor.localRotation = Quaternion.Euler(-turretRotation.y, 0f, 0f);
            
            this.turretRotation = turretRotation;
        }

        private void ApplyStabs()
        {
            if (stabsEnabled)
            {
                var stabsOrientation = lastOrientation * Quaternion.Euler(-turretRotation.y, turretRotation.x, 0f);
                stabsOrientation = Quaternion.Inverse(transform.rotation) * stabsOrientation;
                turretRotation = new Vector2(stabsOrientation.eulerAngles.y, -stabsOrientation.eulerAngles.x);
            }
            lastOrientation = transform.rotation;
        }

        private void CheckIfOnGround()
        {
            var points = new[]
            {
                transform.TransformPoint(groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z),
                transform.TransformPoint(-groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z),
                transform.TransformPoint(-groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z),
                transform.TransformPoint(groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z),
            };

            onGround = false;
            for (var i = 0; i < points.Length; i++)
            {
                var point = points[i];
                var ray = new Ray(point + transform.up, -transform.up);
                if (Physics.Raycast(ray, out var hit, 1f, groundCheckMask))
                {
                    onGround = true;

                    var velocity = body.GetPointVelocity(hit.point);
                    var force = ((hit.point - point) * groundSpring - velocity * groundDamping) * body.mass;
                    body.AddForceAtPosition(Vector3.Project(force, hit.normal), hit.point);
                }
            }
        }

        private void MoveTank()
        {
            if (!onGround) return;

            var localVelX = Vector3.Dot(transform.right, body.linearVelocity);
            var localVelZ = Vector3.Dot(transform.forward, body.linearVelocity);

            localVelX = 0f;
            localVelZ = Mathf.MoveTowards(localVelZ, throttle * maxSpeed, Time.deltaTime / Mathf.Max(Time.deltaTime, accelerationTime));

            var localAngularVelocity = transform.InverseTransformVector(body.angularVelocity);
            localAngularVelocity.y = Mathf.MoveTowards(localAngularVelocity.y, maxTurnSpeed * steering, Time.deltaTime / Mathf.Max(Time.deltaTime, turnAccelerationTime));
            body.angularVelocity = transform.TransformVector(localAngularVelocity);
            
            var newVelocity = transform.right * localVelX + transform.forward * localVelZ + Vector3.Project(body.linearVelocity, transform.up);
            body.linearVelocity = newVelocity;
        }

        private void Update()
        {
            RotateTurret(); 
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
    }
}