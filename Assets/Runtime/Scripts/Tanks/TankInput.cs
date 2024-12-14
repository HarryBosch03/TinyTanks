using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTanks.Tanks
{
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TankController))]
    public class TankInput : MonoBehaviour
    {
        public int controllerIndex;
        public float mouseCameraSensitivity;
        public float mouseTraverseSensitivity = 0.01f;
        public float gamepadSensitivity;
        public RectTransform cursor;

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

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            if (Application.isFocused)
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

                    cursorDelta = Mouse.current.delta.ReadValue();
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

                var aimPosition = mainCamera.transform.position + tank.worldAimVector * 1024f;
                var targetCursorPosition = (Vector2)mainCamera.WorldToScreenPoint(aimPosition);
                targetCursorPosition += cursorDelta;
                targetCursorPosition.x = Mathf.Clamp(targetCursorPosition.x, 0, Screen.width);
                targetCursorPosition.y = Mathf.Clamp(targetCursorPosition.y, 0, Screen.height);

                var currentCursorPosition = (Vector2)mainCamera.WorldToScreenPoint(mainCamera.transform.position + tank.model.gunPivot.forward * 1024f);
                currentCursorPosition.x = Mathf.Clamp(currentCursorPosition.x, 0, Screen.width);
                currentCursorPosition.y = Mathf.Clamp(currentCursorPosition.y, 0, Screen.height);
                
                if (tank.useSight)
                {
                    var screenSize = new Vector2(Screen.width, Screen.height) / 2f;
                    targetCursorPosition = Vector2.ClampMagnitude((targetCursorPosition - currentCursorPosition) / screenSize, 0.5f) * screenSize + currentCursorPosition;
                }

                cursorPosition = targetCursorPosition;
                
                var ray = mainCamera.ScreenPointToRay(targetCursorPosition);
                aimPosition = ray.GetPoint(50f);
                tank.worldAimVector = (aimPosition - mainCamera.transform.position).normalized;

                var camOrientation = Quaternion.LookRotation(tank.worldAimVector, Vector3.up).eulerAngles;
                followCamera.freeLookRotation = new Vector2(camOrientation.y, -camOrientation.x);
            }

            cursor.gameObject.SetActive(true);
            cursor.position = cursorPosition;
        }
    }
}