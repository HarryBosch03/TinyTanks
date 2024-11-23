using System;
using UnityEngine;

namespace TinyTanks.Tanks
{
    public class TankChassisAnimator : MonoBehaviour
    {
        public Vector2 movementLeanAmplitude = new Vector2(-3f, 0f);
        public float movementLeanSmoothing = 0.5f;
        
        [Space]
        public Transform chassis;
        public Transform leftTrack;
        public Transform rightTrack;
        
        private TankController tank;
        private Vector2 smoothedMovementLean;

        private void Awake()
        {
            tank = GetComponentInParent<TankController>();
        }

        private void Update()
        {
            var movementLean = new Vector2()
            {
                x = Vector3.Dot(tank.transform.forward, tank.body.linearVelocity),
                y = Vector3.Dot(tank.transform.up, tank.body.angularVelocity),
            };
            smoothedMovementLean = Vector2.Lerp(smoothedMovementLean, movementLean, Time.deltaTime / Mathf.Max(Time.deltaTime, movementLeanSmoothing));

            chassis.localRotation = Quaternion.Euler(smoothedMovementLean.x * movementLeanAmplitude.x, 0f, smoothedMovementLean.y * movementLeanAmplitude.y);

            UpdateTrack(leftTrack, tank.leftTrackGroundSamples);
            UpdateTrack(rightTrack, tank.rightTrackGroundSamples);
        }

        private void UpdateTrack(Transform track, Vector3[] samples)
        {
            var center = (samples[0] + samples[^1]) * 0.5f;
            var direction = (samples[0] - samples[^1]).normalized;

            track.position = center;
            track.rotation = Quaternion.LookRotation(direction, tank.transform.up);
        }
    }
}