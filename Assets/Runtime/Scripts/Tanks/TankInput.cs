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
    public class TankInput : NetworkBehaviour
    {
        public int controllerIndex;
        public float mouseSensitivity;

        public TankController tank { get; private set; }

        private void Awake() { tank = GetComponent<TankController>(); }

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
            if (Application.isFocused && IsOwner)
            {
                var gp = Gamepad.all.ElementAtOrDefault(controllerIndex);
                if (controllerIndex == -1)
                {
                    var kb = Keyboard.current;
                    var m = Mouse.current;

                    if (m.middleButton.isPressed) tank.turretTraverse += m.delta.ReadValue() * mouseSensitivity;
                    else tank.turretTraverse = Vector2.zero;

                    tank.throttle = kb.wKey.ReadValue() - kb.sKey.ReadValue();
                    tank.steering = kb.dKey.ReadValue() - kb.aKey.ReadValue();

                    if (m.leftButton.wasPressedThisFrame) tank.StartShooting(0);
                    if (m.leftButton.wasReleasedThisFrame) tank.StopShooting(0);
                    
                    if (m.rightButton.wasReleasedThisFrame) tank.StopShooting(1);
                    if (m.rightButton.wasReleasedThisFrame) tank.StopShooting(1);

                    if (kb.zKey.wasPressedThisFrame) tank.SetStabs(!tank.stabsEnabled);
                    if (kb.leftShiftKey.wasPressedThisFrame) tank.SetUseSight(!tank.useSight);
                }
                else if (gp != null)
                {
                    tank.turretTraverse = gp.rightStick.ReadValue();
                    tank.throttle = gp.leftStick.y.ReadValue();
                    tank.steering = gp.leftStick.x.ReadValue();

                    if (gp.rightShoulder.wasPressedThisFrame) tank.StartShooting(0);
                    if (gp.rightShoulder.wasReleasedThisFrame) tank.StopShooting(0);

                    if (gp.leftShoulder.wasPressedThisFrame) tank.StartShooting(1);
                    if (gp.leftShoulder.wasReleasedThisFrame) tank.StopShooting(1);

                    if (gp.dpad.down.wasPressedThisFrame) tank.SetStabs(!tank.stabsEnabled);
                    if (gp.buttonSouth.wasPressedThisFrame) tank.SetUseSight(!tank.useSight);
                }
            }
        }
    }
}