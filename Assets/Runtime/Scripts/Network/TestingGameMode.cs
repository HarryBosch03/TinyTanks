using System;
using System.Collections.Generic;
using TinyTanks.Tanks;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTanks.Network
{
    public class TestingGameMode : NetworkBehaviour
    {
        public TankInput tankPrefab;
        public CinemachineCamera spectatorCamera;
        public Canvas respawnCanvas;

        public List<TankInput> players { get; } = new List<TankInput>();
        public TankInput localPlayer { get; private set; }

        public void RespawnPlayer() => RespawnPlayer(-1);
        public void RespawnPlayer(int controllerIndex) => RespawnPlayerServerRpc(controllerIndex);

        private void Awake() { respawnCanvas.gameObject.SetActive(false); }

        private void OnEnable() { ShowRespawnScreen(true); }

        private void ShowRespawnScreen(bool show) { respawnCanvas.gameObject.SetActive(show); }

        private void Update()
        {
            if (respawnCanvas.gameObject.activeSelf)
            {
                for (var i = 0; i < Gamepad.all.Count; i++)
                {
                    var gp = Gamepad.all[i];
                    if (gp != null && gp.buttonSouth.wasPressedThisFrame)
                    {
                        RespawnPlayer(i);
                    }
                }

                var kb = Keyboard.current;
                if (kb.spaceKey.wasPressedThisFrame)
                {
                    RespawnPlayer(-1);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RespawnPlayerServerRpc(int controllerIndex, ServerRpcParams rpcParams = default)
        {
            var replyParams = new ClientRpcParams()
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { rpcParams.Receive.SenderClientId }
                }
            };
            
            var tank = GetTankControllerForConnection(rpcParams.Receive.SenderClientId);
            if (tank == null)
            {
                var instance = Instantiate(tankPrefab);
                instance.NetworkObject.SpawnWithOwnership(rpcParams.Receive.SenderClientId, true);

                players.Add(instance);
                SetLocalPlayerClientRpc(instance.NetworkObject, controllerIndex, replyParams);
            }
            else
            {
                tank.tank.SetActive(true);
            }

            NotifyRespawnClientRpc(replyParams);
        }

        [ClientRpc]
        private void NotifyRespawnClientRpc(ClientRpcParams sendParams = default)
        {
            respawnCanvas.gameObject.SetActive(false);
            localPlayer.tank.SetActiveViewer(true);
        }

        [ClientRpc]
        private void SetLocalPlayerClientRpc(NetworkObjectReference reference, int controllerIndex, ClientRpcParams target = default)
        {
            reference.TryGet(out var networkObject);
            localPlayer = networkObject.GetComponent<TankInput>();
            localPlayer.controllerIndex = controllerIndex;
        }

        private TankInput GetTankControllerForConnection(ulong clientId)
        {
            foreach (var player in players)
            {
                if (clientId == player.OwnerClientId) return player;
            }

            return null;
        }
    }
}