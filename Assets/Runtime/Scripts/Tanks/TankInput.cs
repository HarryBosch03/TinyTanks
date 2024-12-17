using System;
using System.Collections.Generic;
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
        public ulong ownerId;

        private Vector2 cursorPosition;
        private Camera mainCamera;
        private CinemachineTankFollowCamera followCamera;

        public TankController tank { get; private set; }

        public static TankInput localPlayer => all.FirstOrDefault(e => e.IsOwner);
        public static List<TankInput> all { get; } = new List<TankInput>();

        public void TakeOver()
        {
            TakeOverServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void TakeOverServerRpc(ServerRpcParams rpcParams = default)
        {
            if (enabled && IsOwner) return;

            var clientId = rpcParams.Receive.SenderClientId;
            var existingPlayer = all.Find(e => e.NetworkObject.OwnerClientId == clientId);
            if (existingPlayer != null)
            {
                existingPlayer.NetworkObject.RemoveOwnership();
                existingPlayer.SetEnabledClientRpc(false);
            }

            NetworkObject.ChangeOwnership(clientId);
            SetEnabledClientRpc(true);
            tank.SetActiveViewer(true);
        }

        private void Awake()
        {
            tank = GetComponent<TankController>();
            followCamera = GetComponentInChildren<CinemachineTankFollowCamera>(true);
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            all.Add(this);
            if (IsServer) SetEnabledClientRpc(true);
        }

        private void OnDisable()
        {
            all.Remove(this);
            if (IsServer) SetEnabledClientRpc(false);
        }

        [ClientRpc]
        private void SetEnabledClientRpc(bool enabled)
        {
            this.enabled = enabled;
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
                    if (kb.cKey.wasPressedThisFrame) tank.ToggleSightZoom();

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

                var cameraRotation = tank.cameraRotation;
                var sensitivityScaling = Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                cameraRotation += cursorDelta * sensitivityScaling;

                cameraRotation.x %= 360f;
                cameraRotation.y = Mathf.Clamp(cameraRotation.y, -90f, 90f);

                var ray = new Ray(mainCamera.transform.position, Quaternion.Euler(-cameraRotation.y, cameraRotation.x, 0f) * Vector3.forward);
                tank.worldAimPosition = ray.GetPoint(1024f);
                var hits = Physics.RaycastAll(ray, 1024f).OrderBy(e => e.distance);
                foreach (var hit in hits)
                {
                    if (hit.collider.transform.IsChildOf(tank.transform)) continue;
                    tank.worldAimPosition = hit.point;
                    break;
                }

                followCamera.freeLookRotation = cameraRotation;
                cursorPosition = mainCamera.WorldToScreenPoint(tank.worldAimPosition);

                tank.cameraRotation = cameraRotation;
            }

            cursor.gameObject.SetActive(true);
            cursor.position = cursorPosition;
        }
    }
}