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
        
        [HideInInspector]
        public Vector2 freeLookRotation;
        
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            var orientation = Quaternion.Euler(-freeLookRotation.y, freeLookRotation.x, 0f);
            
            curState.RawPosition = target.transform.position + orientation * offset;
            curState.RawOrientation = orientation;
        }

        public override bool IsValid => target != null;
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Body;
    }
}