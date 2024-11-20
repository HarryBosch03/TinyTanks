using System;
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

        private TankController tank;
        private Vector2 lastStickInput;

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

                tank.turretRotation += Vector3.Cross(stickInput, lastStickInput).z * turnSensitivity;

                lastStickInput = stickInput;
            }
        }
    }
}