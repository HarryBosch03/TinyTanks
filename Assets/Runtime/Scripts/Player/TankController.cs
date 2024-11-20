using System;
using UnityEngine;

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
        public LayerMask groundCheckMask;
        public Vector3 groundCheckPoint;

        [Space]
        public Transform turretRotor;


        private Rigidbody body;
        private bool onGround;
        private RaycastHit groundHit;

        public float throttle { get; set; }
        public float steering { get; set; }
        public float turretRotation { get; set; }

        private void Awake() { body = GetComponent<Rigidbody>(); }

        private void FixedUpdate()
        {
            CheckForGround();
            MoveTank();
        }

        private void CheckForGround()
        {
            var corners = new[]
            {
                transform.TransformPoint(groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z),
                transform.TransformPoint(-groundCheckPoint.x, groundCheckPoint.y, groundCheckPoint.z),
                transform.TransformPoint(-groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z),
                transform.TransformPoint(groundCheckPoint.x, groundCheckPoint.y, -groundCheckPoint.z),
            };

            var hits = new RaycastHit?[4];
            for (var i = 0; i < 4; i++)
            {
                var ray = new Ray(corners[i] + Vector3.up, Vector3.down);
                if (Physics.Raycast(ray, out var hit, 2f, groundCheckMask)) hits[i] = hit;
                else hits[i] = null;
            }

            
            
            Debug.Log(onGround ? groundHit.collider.name : "null");
        }

        private void MoveTank()
        {
            if (onGround)
            {
                var localVelX = Vector3.Dot(transform.right, body.linearVelocity);
                var localVelZ = Vector3.Dot(transform.forward, body.linearVelocity);

                localVelX = 0f;
                localVelZ = Mathf.MoveTowards(localVelZ, throttle * maxSpeed, Time.deltaTime / Mathf.Max(Time.deltaTime, accelerationTime));

                body.angularVelocity = Vector3.MoveTowards(body.angularVelocity, Vector3.up * maxTurnSpeed * steering, Time.deltaTime / Mathf.Max(Time.deltaTime, turnAccelerationTime));

                body.linearVelocity = transform.right * localVelX + transform.forward * localVelZ + Vector3.Project(body.linearVelocity, transform.up);

                turretRotation %= 360f;
                turretRotor.localRotation = Quaternion.Euler(0f, turretRotation, 0f);
            }
        }

        private void Update() { turretRotor.localRotation = Quaternion.Euler(0f, turretRotation, 0f); }

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