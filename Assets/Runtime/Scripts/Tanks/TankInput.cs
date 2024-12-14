using System;
using System.Linq;
using Unity.Netcode;
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
        public float mouseCameraSensitivity;
        public float mouseTraverseSensitivity = 0.01f;
        public float gamepadSensitivity;
        public RectTransform cursor;

        private Vector2 cameraRotation;
        private Vector2 cursorPosition;
        private Camera mainCamera;
        private CinemachineTankFollowCamera followCamera;

        public TankController tank { get; private set; }

        private void Awake()
        {
            tank = GetComponent<TankController>();
            followCamera = GetComponentInChildren<CinemachineTankFollowCamera>();
            mainCamera = Camera.main;
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void Update()
        {
            if (Application.isFocused && IsOwner)
            {
                var cursorDelta = Vector2.zero;
                var gp = Gamepad.all.ElementAtOrDefault(controllerIndex);
                
                if (controllerIndex == -1)
                {
                    var kb = Keyboard.current;
                    var m = Mouse.current;

                    tank.throttle = kb.wKey.ReadValue() - kb.sKey.ReadValue();
                    tank.steering = kb.dKey.ReadValue() - kb.aKey.ReadValue();

                    if (m.leftButton.wasPressedThisFrame) tank.StartShooting(0);
                    if (m.leftButton.wasReleasedThisFrame) tank.StopShooting(0);

                    if (m.rightButton.wasPressedThisFrame) tank.StartShooting(1);
                    if (m.rightButton.wasReleasedThisFrame) tank.StopShooting(1);

                    if (kb.leftShiftKey.wasPressedThisFrame) tank.SetUseSight(!tank.useSight);

                    cursorDelta = Mouse.current.delta.ReadValue() * mouseCameraSensitivity;
                }
                else if (gp != null)
                {
                    cursorDelta = gp.rightStick.ReadValue() * gamepadSensitivity * Time.deltaTime;
                    tank.throttle = gp.leftStick.y.ReadValue();
                    tank.steering = gp.leftStick.x.ReadValue();

                    if (gp.rightShoulder.wasPressedThisFrame) tank.StartShooting(0);
                    if (gp.rightShoulder.wasReleasedThisFrame) tank.StopShooting(0);

                    if (gp.leftShoulder.wasPressedThisFrame) tank.StartShooting(1);
                    if (gp.leftShoulder.wasReleasedThisFrame) tank.StopShooting(1);

                    if (gp.buttonSouth.wasPressedThisFrame) tank.SetUseSight(!tank.useSight);
                }

                var sensitivityScaling = Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                cameraRotation += cursorDelta * sensitivityScaling;

                cameraRotation.x %= 360f;
                cameraRotation.y = Mathf.Clamp(cameraRotation.y, -90f, 90f);
                
                tank.worldAimVector = Quaternion.Euler(-cameraRotation.y, cameraRotation.x, 0f) * Vector3.forward;
                followCamera.freeLookRotation = cameraRotation;

                cursorPosition = mainCamera.WorldToScreenPoint(mainCamera.transform.position + tank.worldAimVector);
            }

            cursor.gameObject.SetActive(true);
            cursor.position = cursorPosition;
        }
    }
}