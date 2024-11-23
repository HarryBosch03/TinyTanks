using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTanks.Player
{
    [RequireComponent(typeof(TankController))]
    public class TankInput : MonoBehaviour
    {
        public int controllerIndex;
        public Vector2 normalTurnSensitivity = new Vector2(1f, 8f);
        public Vector2 scopedTurnedSensitivity = new Vector2(2f, 2f);
        public float stickSmoothing;

        private TankController tank;
        private Vector2 lastLeftStickInput;
        private Vector2 lastRightStickInput;
        private float smoothedLeftRotationInput;
        private float smoothedRightRotationInput;

        private void Awake() { tank = GetComponent<TankController>(); }

        private void Update()
        {
            var gp = Gamepad.all.ElementAtOrDefault(controllerIndex);
            if (gp != null)
            {
                var trackLeft = gp.leftTrigger.ReadValue();
                var trackRight = gp.rightTrigger.ReadValue();

                var leftStickInput = gp.leftStick.ReadValue();
                var rightStickInput = gp.rightStick.ReadValue();

                tank.throttle = (trackLeft + trackRight) * 0.5f;
                tank.steering = (trackLeft - trackRight) * 0.5f;

                var sensitivity = tank.useSight ? scopedTurnedSensitivity : normalTurnSensitivity;
                
                var leftRotationInput = Vector3.Cross(leftStickInput, lastLeftStickInput).z * sensitivity.x;
                smoothedLeftRotationInput = Mathf.Lerp(smoothedLeftRotationInput, leftRotationInput, Time.deltaTime / Mathf.Max(Time.deltaTime, stickSmoothing));
                
                var rightRotationInput = Vector3.Cross(rightStickInput, lastRightStickInput).z * sensitivity.y;
                smoothedRightRotationInput = Mathf.Lerp(smoothedRightRotationInput, rightRotationInput, Time.deltaTime / Mathf.Max(Time.deltaTime, stickSmoothing));

                tank.turretRotation += new Vector2(smoothedRightRotationInput, smoothedLeftRotationInput);

                lastRightStickInput = rightStickInput;
                lastLeftStickInput = leftStickInput;

                if (gp.leftShoulder.wasPressedThisFrame) tank.SetUseSight(!tank.useSight);
                
                if (gp.rightShoulder.wasPressedThisFrame) tank.StartShooting();
                if (gp.rightShoulder.wasReleasedThisFrame) tank.StopShooting();

                if (gp.dpad.down.wasPressedThisFrame) tank.SetStabs(!tank.stabsEnabled);
                if (gp.dpad.up.wasPressedThisFrame) tank.ChangeWeapon();
            }
        }
    }
}