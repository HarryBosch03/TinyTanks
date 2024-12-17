using System;
using TinyTanks.Utility;
using UnityEngine;

namespace TinyTanks.Tanks
{
    [ExecuteAlways]
    [RequireComponent(typeof(ParticleSystem))]
    public class TrackSimulator : MonoBehaviour
    {
        public Wheel[] wheels = new Wheel[0];
        public int particleCount = 100;
        public Vector3 particleRotationOffset;
        public Vector3 driveWheelRotationOffset;
        public Vector3 driveWheelRotationAxis = Vector3.right;
        public float trackOffset = 0.006f;

        [Space]
        public float topSlack = 0.03f;
        public float slackJiggleAmplitude = 0.6f;
        public float slackJiggleFrequency = 2.5f;
        
        private TankController tankController;
        private ParticleSystem particles;
        private Vector3[] path;
        private ParticleSystem.Particle[] buffer;
        private float pathTotalLength;
        private float distanceTraveled;
        private float speed;

        private void Awake()
        {
            tankController = GetComponentInParent<TankController>();
            for (var i = 0; i < wheels.Length; i++)
            {
                wheels[i].restPosition = wheels[i].transform.localPosition;
            }
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying || tankController == null || tankController.body == null) return;
            
            var velocity = tankController.body.GetPointVelocity(transform.position);
            speed = Vector3.Dot(transform.forward, velocity);
            distanceTraveled += speed * Time.fixedDeltaTime;

            for (var i = 0; i < wheels.Length; i++)
            {
                var wheel = wheels[i];
                if (!wheel.doesContactGround) continue;

                wheel.transform.localPosition = wheel.restPosition;
                var ray = new Ray(wheel.transform.position, -transform.up);
                if (Physics.Raycast(ray, out var hit, wheel.radius))
                {
                    wheel.transform.position += Vector3.Project(hit.point + hit.normal * wheel.radius - wheel.transform.position, transform.up);
                }
            }
        }

        private void Update()
        {
            if (particles == null) particles = GetComponent<ParticleSystem>();
            var main = particles.main;
            main.maxParticles = particleCount;
            if (buffer == null || buffer.Length != particleCount) buffer = new ParticleSystem.Particle[particleCount];

            UpdatePath();
            
            var distanceCovered = distanceTraveled - trackOffset;
            distanceCovered = (distanceCovered % pathTotalLength + pathTotalLength) % pathTotalLength;
            var segmentSize = pathTotalLength / particleCount;
            var index = 0;

            var driveWheel = wheels[^1];
            driveWheel.transform.localRotation = Quaternion.AngleAxis(distanceTraveled / (Mathf.PI * 2f * driveWheel.radius) * 360f, driveWheelRotationAxis.normalized) * Quaternion.Euler(driveWheelRotationOffset);
            
            if (path.Length == 0) return;
            
            for (var i = 0; i < buffer.Length; i++)
            {
                ref var p = ref buffer[i];
                var last = path.IndexWrapped(index);
                var next = path.IndexWrapped(index + 1);
                var pathSegmentDistance = (next - last).magnitude;
                
                while (distanceCovered > pathSegmentDistance || pathSegmentDistance < 0.001f)
                {
                    distanceCovered -= pathSegmentDistance;
                    index++;
                    
                    last = path.IndexWrapped(index);
                    next = path.IndexWrapped(index + 1);
                    pathSegmentDistance = (next - last).magnitude;
                }
                
                p.remainingLifetime = 1f;
                p.startLifetime = main.startLifetime.Evaluate(Time.time);
                p.startSize = main.startSize.Evaluate(Time.time);
         
                var n0 = (path.IndexWrapped(index + 1) - path.IndexWrapped(index - 1)).normalized;
                var n1 = (path.IndexWrapped(index + 2) - path.IndexWrapped(index)).normalized;
                var normal = Vector3.Lerp(n0, n1, distanceCovered / pathSegmentDistance).normalized;
                
                p.position = last + (next - last).normalized * distanceCovered;
                p.rotation3D = (transform.rotation * Quaternion.AngleAxis(Vector3.SignedAngle(transform.forward, normal, transform.right), Vector3.right) * Quaternion.Euler(particleRotationOffset)).eulerAngles;
                distanceCovered += segmentSize;
            }
            
            particles.SetParticles(buffer);
        }

        private void OnDrawGizmos()
        {
            UpdatePath();
            Gizmos.color = Color.yellow;
            if (path != null || path.Length > 0)
            {
                for (var i = 0; i < path.Length - 1; i++)
                {
                    Gizmos.DrawLine(path[i], path[i + 1]);
                }
                Gizmos.DrawLine(path[^1], path[0]);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            for (var i = 0; i < wheels.Length; i++) if (wheels[i].transform == null) return;
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            for (var i = 0; i < wheels.Length; i++)
            {
                Gizmos.DrawSphere(wheels[i].transform.position, wheels[i].radius);
            }
        }

        private void UpdatePath()
        {
            for (var i = 0; i < wheels.Length; i++) if (wheels[i].transform == null) return;

            var length = wheels.Length * 8 + 16;
            if (path == null || path.Length != length) path = new Vector3[length];
            for (var i = 0; i < wheels.Length; i++)
            {
                var current = wheels[i];
                var next = wheels.IndexWrapped(i + 1);
                var last = wheels.IndexWrapped(i - 1);

                var t0 = (current.transform.position - last.transform.position).normalized;
                var t1 = (next.transform.position - current.transform.position).normalized;
                var n0 = Vector3.Cross(t0, transform.right).normalized;
                var n1 = Vector3.Cross(t1, transform.right).normalized;
                
                var angle = Vector3.SignedAngle(t0, t1, transform.right);
                
                for (var j = 0; j < 8; j++)
                {
                    var percent = j / 7f;
                    if (angle > 0f)
                    {
                        path[8 * i + j] = current.transform.position + Quaternion.AngleAxis(angle * percent, transform.right) * n0 * current.radius;
                    }
                    else
                    {
                        path[8 * i + j] = current.transform.position + (n0 + n1).normalized * current.radius;
                    }
                }
            }

            var indexOffset = wheels.Length * 8;
            for (var i = 0; i < 16; i++)
            {
                var start = path[indexOffset - 1];
                var end = path[0];
                var center = (start + end) / 2f - transform.up * topSlack;
                if (tankController != null) center = (start + end) / 2f - transform.up * (1f + Mathf.Sin(distanceTraveled * slackJiggleFrequency) * speed / tankController.maxFwdSpeed * slackJiggleAmplitude) * topSlack;

                var percent = i / 16f;
                path[indexOffset + i] = Vector3.Lerp(Vector3.Lerp(start, center, percent), Vector3.Lerp(center, end, percent), percent);
            }

            pathTotalLength = 0f;
            for (var i = 0; i < path.Length; i++)
            {
                var e0 = path[i];
                var e1 = path.IndexWrapped(i + 1);
                pathTotalLength += (e0 - e1).magnitude;
            }
        }

        [Serializable]
        public struct Wheel
        {
            public Transform transform;
            public float radius;
            public bool doesContactGround;

            [HideInInspector]
            public Vector3 restPosition;
        }
    }
}