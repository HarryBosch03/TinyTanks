using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTanks.Player
{
    [RequireComponent(typeof(TankController))]
    public class TankInput : MonoBehaviour
    {
        public int controllerIndex;
        public float turnSensitivity;
        public float stickSmoothing;

        private TankController tank;
        private Vector2 lastRightStickInput;
        private float smoothedRightRotationInput;

        private void Awake() { tank = GetComponent<TankController>(); }

        private void Update()
        {
            var gp = Gamepad.all.ElementAtOrDefault(controllerIndex);
            if (gp != null)
            {
                var trackLeft = gp.leftTrigger.ReadValue() - gp.leftShoulder.ReadValue();
                var trackRight = gp.rightTrigger.ReadValue() - gp.rightShoulder.ReadValue();

                var stickInput = gp.rightStick.ReadValue();

                tank.throttle = (trackLeft + trackRight) * 0.5f;
                tank.steering = (trackLeft - trackRight) * 0.5f;

                var rightRotationInput = Vector3.Cross(stickInput, lastRightStickInput).z * turnSensitivity;
                smoothedRightRotationInput = Mathf.Lerp(smoothedRightRotationInput, rightRotationInput, Time.deltaTime / Mathf.Max(Time.deltaTime, stickSmoothing));
                tank.turretRotation += smoothedRightRotationInput;

                lastRightStickInput = stickInput;
            }
        }
    }
}