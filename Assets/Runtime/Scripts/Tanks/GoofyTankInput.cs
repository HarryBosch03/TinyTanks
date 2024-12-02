using System;
using System.Linq;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTanks.Tanks
{
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TankController))]
    public class GoofyTankInput : NetworkBehaviour
    {
        public int controllerIndex;
        public Vector2 normalTurnSensitivity = new Vector2(1f, 8f);
        public Vector2 scopedTurnedSensitivity = new Vector2(2f, 2f);
        public float stickSmoothing;

        private Vector2 lastLeftStickInput;
        private Vector2 lastRightStickInput;
        private float smoothedLeftRotationInput;
        private float smoothedRightRotationInput;
        
        public TankController tank { get; private set; }

        private void Awake()
        {
            tank = GetComponent<TankController>();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (Owner == null)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            var gp = Gamepad.all.ElementAtOrDefault(controllerIndex);
            if (gp != null && Application.isFocused && IsOwner)
            {
                var leftStickInput = gp.leftStick.ReadValue();
                var rightStickInput = gp.rightStick.ReadValue();

                var sensitivity = tank.useSight ? scopedTurnedSensitivity : normalTurnSensitivity;
                ProcessSickInput(leftStickInput, ref smoothedLeftRotationInput, ref lastLeftStickInput, sensitivity.x);
                ProcessSickInput(rightStickInput, ref smoothedRightRotationInput, ref lastRightStickInput, sensitivity.y);

                if (tank.useSight)
                {
                    tank.throttle = 0f;
                    tank.steering = 0f;
                    tank.turretTraverse += new Vector2(smoothedRightRotationInput, smoothedLeftRotationInput);
                    
                    if (gp.rightShoulder.wasPressedThisFrame) tank.StartShooting(0);
                    if (gp.rightShoulder.wasReleasedThisFrame) tank.StopShooting(0);
                    
                    if (gp.leftShoulder.wasPressedThisFrame) tank.StartShooting(1);
                    if (gp.leftShoulder.wasReleasedThisFrame) tank.StopShooting(1);
                }
                else
                {
                    var trackLeft = gp.leftTrigger.ReadValue() - gp.leftShoulder.ReadValue();
                    var trackRight = gp.rightTrigger.ReadValue() - gp.rightShoulder.ReadValue();

                    tank.throttle = (trackLeft + trackRight) * 0.5f;
                    tank.steering = (trackLeft - trackRight) * 0.5f;

                    tank.freeLookRotation += smoothedRightRotationInput;
                }

                if (gp.dpad.down.wasPressedThisFrame) tank.SetStabs(!tank.stabsEnabled);
                if (gp.buttonSouth.wasPressedThisFrame) tank.SetUseSight(!tank.useSight);
            }
        }

        private void ProcessSickInput(Vector2 input, ref float smoothedPosition, ref Vector2 lastStickInput, float sensitivity)
        {
            input = input.magnitude > 0.75f ? input : Vector2.zero;
            var leftRotationInput = Vector3.Cross(input, lastStickInput).z * sensitivity;
            smoothedPosition = Mathf.Lerp(smoothedPosition, leftRotationInput, Time.deltaTime / Mathf.Max(Time.deltaTime, stickSmoothing));
            
            lastStickInput = input;
        }
    }
}