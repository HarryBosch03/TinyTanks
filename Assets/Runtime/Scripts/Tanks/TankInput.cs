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
        private int controllerIndex = -1;

        public float mouseCameraSensitivity;
        public float traverseSensitivity = 0.01f;
        public float gamepadSensitivity;
        public RectTransform cursor;
        public ulong ownerId;

        private Vector2 cursorPosition;
        private Camera mainCamera;
        private CinemachineTankFollowCamera followCamera;

        public ulong? controllingId { get; private set; } = null;

        public TankController tank { get; private set; }

        public static TankInput localPlayer { get; private set; }
        public static List<TankInput> all { get; } = new List<TankInput>();

        [ServerRpc(RequireOwnership = false)]
        public void TakeOverServerRpc(ulong? controllingId)
        {
            var existingPlayer = all.FirstOrDefault(e => e.controllingId.HasValue && e.controllingId == controllingId);
            if (existingPlayer != null)
            {
                existingPlayer.SetControllingIdRpc(null);
            }

            SetControllingIdRpc(controllingId);
        }

        [Rpc(SendTo.Everyone)]
        private void SetControllingIdRpc(ulong? controllingId)
        {
            this.controllingId = controllingId;
            if (IsServer) NetworkObject.ChangeOwnership(controllingId ?? 0);

            var isLocalPlayer = controllingId.HasValue && NetworkManager.LocalClientId == controllingId.Value;
            if (isLocalPlayer) localPlayer = this;
            tank.SetActiveViewer(isLocalPlayer);
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
            followCamera.transform.SetParent(null);
        }

        private void OnDisable()
        {
            all.Remove(this);
            if (IsServer) SetEnabledClientRpc(false);
            followCamera.transform.SetParent(transform);
        }

        [ClientRpc]
        private void SetEnabledClientRpc(bool enabled) { this.enabled = enabled; }

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
            if (Application.isFocused && controllingId.HasValue && controllingId.Value == NetworkManager.LocalClientId)
            {
                var map = InputSystem.actions.FindActionMap("Tank");
                tank.throttle = map.FindAction("Throttle").ReadValue<float>();
                tank.steering = map.FindAction("Steering").ReadValue<float>();

                if (map.FindAction("Shoot Primary").WasPressedThisFrame()) tank.StartShooting(0);
                if (map.FindAction("Shoot Primary").WasReleasedThisFrame()) tank.StopShooting(0);

                if (map.FindAction("Shoot Coax").WasPressedThisFrame()) tank.StartShooting(1);
                if (map.FindAction("Shoot Coax").WasReleasedThisFrame()) tank.StopShooting(1);

                if (map.FindAction("Toggle Sight").WasPerformedThisFrame()) tank.SetUseSight(!tank.useSight);
                if (map.FindAction("Toggle Sight Zoom").WasPerformedThisFrame()) tank.ToggleSightZoom();

                var traverseTurret = map.FindAction("Traverse Turret").IsPressed();
                var cursorDelta = Mouse.current.delta.ReadValue() * mouseCameraSensitivity;

                var cameraRotation = tank.cameraRotation;
                var sensitivityScaling = Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                cameraRotation += cursorDelta * sensitivityScaling;

                cameraRotation.x %= 360f;
                cameraRotation.y = Mathf.Clamp(cameraRotation.y, tank.cameraFreeLookClamp.x, tank.cameraFreeLookClamp.y);

                if (traverseTurret)
                {
                    tank.traverseInput += cursorDelta * traverseSensitivity;
                    tank.worldAimDirection = mainCamera.transform.forward;
                }
                else
                {
                    tank.traverseInput = Vector2.zero;
                }

                followCamera.freeLookRotation = cameraRotation;
                tank.cameraRotation = cameraRotation;
            }
        }
    }
}