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
        public TankController tankPrefab;
        public CinemachineCamera spectatorCamera;
        public Canvas respawnCanvas;
        
        private NetworkManager netManager;

        public List<TankController> players { get; } = new List<TankController>();
        public TankController localPlayer { get; private set; }
        
        public void RespawnPlayer() => RespawnPlayerRpc();

        private void Awake()
        {
            respawnCanvas.gameObject.SetActive(false);
        }

        public override void OnStartNetwork()
        {
            netManager = InstanceFinder.NetworkManager;
            ShowRespawnScreen(true);
        }

        private void ShowRespawnScreen(bool show)
        {
            respawnCanvas.gameObject.SetActive(show);
        }

        private void Update()
        {
            if (respawnCanvas.gameObject.activeSelf)
            {
                var gp = Gamepad.current;
                if (gp != null && gp.buttonSouth.wasPressedThisFrame)
                {
                    RespawnPlayer();
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RespawnPlayerRpc(NetworkConnection connection = null)
        {
            var tank = GetTankControllerForConnection(connection);
            if (tank == null)
            {
                var instance = Instantiate(tankPrefab);
                Spawn(instance.gameObject, connection);
                
                players.Add(instance);
                SetLocalPlayer(connection, instance);
            }
            else
            {
                tank.SetActive(true);
            }

            NotifyRespawnRpc(connection);
        }
        
        [TargetRpc]
        private void NotifyRespawnRpc(NetworkConnection connection)
        {
            respawnCanvas.gameObject.SetActive(false);
            localPlayer.SetActiveViewer(true);
        }

        [TargetRpc]
        private void SetLocalPlayer(NetworkConnection connection, TankController instance)
        {
            if (instance.Owner != LocalConnection) return;
            localPlayer = instance;
        }

        private TankController GetTankControllerForConnection(NetworkConnection connection)
        {
            foreach (var player in players)
            {
                if (player.Owner == connection) return player;
            }

            return null;
        }
    }
}