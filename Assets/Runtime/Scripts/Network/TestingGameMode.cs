using System;
using System.Collections.Generic;
using TinyTanks.Tanks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTanks.Network
{
    public class TestingGameMode : MonoBehaviour
    {
        public TankInput tankPrefab;
        public CinemachineCamera spectatorCamera;
        public Canvas respawnCanvas;

        public List<TankInput> players { get; } = new List<TankInput>();
        public TankInput localPlayer { get; private set; }

        public void RespawnPlayer() => RespawnPlayer(-1);
        public void RespawnPlayer(int controllerIndex) => RespawnPlayerRpc(controllerIndex);

        private void Awake() { respawnCanvas.gameObject.SetActive(false); }

        private void OnEnable()
        {
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

        private void RespawnPlayerRpc(int controllerIndex)
        {
            var tank = GetTankControllerForConnection();
            if (tank == null)
            {
                var instance = Instantiate(tankPrefab);

                players.Add(instance);
                SetLocalPlayerRpc(instance, controllerIndex);
            }
            else
            {
                tank.tank.SetActive(true);
            }

            NotifyRespawnRpc();
        }

        private void NotifyRespawnRpc()
        {
            respawnCanvas.gameObject.SetActive(false);
            localPlayer.tank.SetActiveViewer(true);
        }

        private void SetLocalPlayerRpc(TankInput instance, int controllerIndex)
        {
            localPlayer = instance;
            localPlayer.controllerIndex = controllerIndex;
        }

        private TankInput GetTankControllerForConnection()
        {
            foreach (var player in players)
            {
                return player;
            }

            return null;
        }
    }
}