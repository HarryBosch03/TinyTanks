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
        public Transform turretRotor;

        private Rigidbody body;

        public float throttle { get; set; }
        public float steering { get; set; }
        public float turretRotation { get; set; }
        
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            var localVelX = Vector3.Dot(transform.right, body.linearVelocity);
            var localVelZ = Vector3.Dot(transform.forward, body.linearVelocity);

            localVelX = 0f;
            localVelZ = Mathf.MoveTowards(localVelZ, throttle * maxSpeed, Time.deltaTime / Mathf.Max(Time.deltaTime, accelerationTime));

            body.angularVelocity = Vector3.MoveTowards(body.angularVelocity, Vector3.up * maxTurnSpeed * steering, Time.deltaTime /  Mathf.Max(Time.deltaTime, turnAccelerationTime));
            
            body.linearVelocity = transform.right * localVelX + transform.forward * localVelZ + Vector3.Project(body.linearVelocity, transform.up);

            turretRotation %= 360f;
            turretRotor.localRotation = Quaternion.Euler(0f, turretRotation, 0f);
        }
    }
}
