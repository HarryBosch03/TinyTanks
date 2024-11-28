using System;
using UnityEngine;

namespace TinyTanks.Tanks
{
    public class TankChassisAnimator : MonoBehaviour
    {
        public Vector2 movementLeanAmplitude = new Vector2(-3f, 0f);
        public float movementLeanSmoothing = 0.5f;
        
        [Space]
        public Transform leftTrack;
        public Transform rightTrack;
        public float trackSmoothing = 0.5f;
        
        private TankController tank;
        private Vector2 smoothedMovementLean;

        private Vector2 smoothedTrackPositionLeft;
        private Vector2 smoothedTrackPositionRight;
        private float smoothedTrackRotationLeft;
        private float smoothedTrackRotationRight;

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

            UpdateTrack(leftTrack, tank.leftTrackGroundSamples, ref smoothedTrackPositionLeft, ref smoothedTrackRotationLeft);
            UpdateTrack(rightTrack, tank.rightTrackGroundSamples, ref smoothedTrackPositionRight, ref smoothedTrackRotationLeft);
        }

        private void UpdateTrack(Transform track, Vector3[] samples, ref Vector2 smoothedTrackPosition, ref float smoothedTrackRotation)
        {
            var center = (samples[0] + samples[^1]) * 0.5f;
            var direction = (samples[0] - samples[^1]).normalized;

            var localPosition = tank.transform.InverseTransformPoint(center);
            var localRotation = Quaternion.Inverse(tank.transform.rotation) * Quaternion.LookRotation(direction, tank.transform.up);

            var trackPosition = new Vector2(localPosition.y, localPosition.z);
            var trackRotation = localRotation.eulerAngles.x;

            smoothedTrackPosition = Vector2.Lerp(smoothedTrackPosition, trackPosition, Time.deltaTime / Mathf.Max(Time.deltaTime, trackSmoothing));
            smoothedTrackRotation = Mathf.LerpAngle(smoothedTrackRotation, trackRotation, Time.deltaTime / Mathf.Max(Time.deltaTime, trackSmoothing));
            
            track.localPosition = new Vector3(0f, smoothedTrackPosition.x, smoothedTrackPosition.y);
            track.localEulerAngles = new Vector3(smoothedTrackRotation, 0f, 0f);
        }
    }
}