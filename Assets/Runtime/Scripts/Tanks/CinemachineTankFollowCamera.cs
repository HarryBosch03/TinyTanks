using System;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace TinyTanks.Tanks
{
    [AddComponentMenu("Cinemachine/Procedural/Position Control/Tank Follow Camera")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    public class CinemachineTankFollowCamera : CinemachineComponentBase
    {
        public TankController target;
        public float cameraDistance;
        public float orbitHeight;
        public Vector2 verticalClamp;
        public float fovLerpTime = 1f;

        [HideInInspector]
        public Vector2 freeLookRotation = new Vector2(-90f, 90f);

        [HideInInspector]
        public float enabledTime;

        protected override void OnEnable() { enabledTime = 0f; }

        private void Update()
        {
            enabledTime += Time.deltaTime; 
        }

        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            var orientation = Quaternion.Euler(-freeLookRotation.y, freeLookRotation.x, 0f);
            freeLookRotation.y = Mathf.Clamp(freeLookRotation.y, verticalClamp.x, verticalClamp.y);
            var clampedOrientation = Quaternion.Euler(-freeLookRotation.y, freeLookRotation.x, 0f);
            
            curState.RawPosition = target.transform.position + target.transform.up * orbitHeight + clampedOrientation * Vector3.back * cameraDistance;
            curState.RawOrientation = orientation;

            DoCollision(ref curState);

            var t = enabledTime / fovLerpTime;
            t = -1f / (10f * t + 1f) + 1f;
            if (Application.isPlaying && float.IsFinite(t)) curState.Lens.FieldOfView = Mathf.Lerp(curState.Lens.FieldOfView * 0.5f, curState.Lens.FieldOfView, t);
        }

        private void DoCollision(ref CameraState curState)
        {
            var start = target.transform.position + target.transform.up * orbitHeight;
            var end = curState.RawPosition;
            var radius = curState.Lens.NearClipPlane / Mathf.Cos(curState.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);

            var ray = new Ray(start, end - start);
            var hits = Physics.SphereCastAll(ray, radius, (end - start).magnitude).OrderBy(e => e.distance);
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(target.transform)) continue;
                curState.RawPosition = hit.point + hit.normal * radius;
                return;
            }
        }

        public override bool IsValid => target != null;
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Body;
    }
}