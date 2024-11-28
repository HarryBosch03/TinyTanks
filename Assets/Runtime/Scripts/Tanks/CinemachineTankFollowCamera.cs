using Unity.Cinemachine;
using UnityEngine;

namespace TinyTanks.Tanks
{
    [AddComponentMenu("Cinemachine/Procedural/Position Control/Tank Follow Camera")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    public class CinemachineTankFollowCamera : CinemachineComponentBase
    {
        public TankController target;
        public Vector3 offset;
        public bool followTankRotation;
        
        [HideInInspector]
        public float rotationOffset;
        
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            var targetTransform = target.NetworkObject.GetGraphicalObject();
            if (targetTransform == null) targetTransform = target.transform;
            
            var angle = rotationOffset;
            if (followTankRotation)
            {
                var fwd = Vector3.ProjectOnPlane(targetTransform.forward, Vector3.up).normalized;
                angle += Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            }

            var orientation = Quaternion.Euler(Vector3.up * angle);
            
            curState.RawPosition = targetTransform.position + orientation * offset;
            curState.RawOrientation = orientation;
        }

        public override bool IsValid => target != null;
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Body;
    }
}