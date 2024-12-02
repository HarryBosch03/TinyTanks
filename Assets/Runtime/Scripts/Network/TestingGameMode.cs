using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using TinyTanks.Tanks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTanks.Network
{
    public class TestingGameMode : NetworkBehaviour
    {
        public TankInput tankPrefab;
        public CinemachineCamera spectatorCamera;
        public Canvas respawnCanvas;

        private NetworkManager netManager;

        public List<TankInput> players { get; } = new List<TankInput>();
        public TankInput localPlayer { get; private set; }

        public void RespawnPlayer() => RespawnPlayer(-1);
        public void RespawnPlayer(int controllerIndex) => RespawnPlayerRpc(controllerIndex);

        private void Awake() { respawnCanvas.gameObject.SetActive(false); }

        public override void OnStartNetwork()
        {
            netManager = InstanceFinder.NetworkManager;
            ShowRespawnScreen(true);
        }

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
        private void RespawnPlayerRpc(int controllerIndex, NetworkConnection connection = null)
        {
            var tank = GetTankControllerForConnection(connection);
            if (tank == null)
            {
                var instance = Instantiate(tankPrefab);
                Spawn(instance.gameObject, connection);

                players.Add(instance);
                SetLocalPlayerRpc(connection, instance, controllerIndex);
            }
            else
            {
                tank.tank.SetActive(true);
            }

            NotifyRespawnRpc(connection);
        }

        [TargetRpc]
        private void NotifyRespawnRpc(NetworkConnection connection)
        {
            respawnCanvas.gameObject.SetActive(false);
            localPlayer.tank.SetActiveViewer(true);
        }

        [TargetRpc]
        private void SetLocalPlayerRpc(NetworkConnection connection, TankInput instance, int controllerIndex)
        {
            if (instance.Owner != LocalConnection) return;
            localPlayer = instance;
            localPlayer.controllerIndex = controllerIndex;
        }

        private TankInput GetTankControllerForConnection(NetworkConnection connection)
        {
            foreach (var player in players)
            {
                if (player.Owner == connection) return player;
            }

            return null;
        }
    }
}