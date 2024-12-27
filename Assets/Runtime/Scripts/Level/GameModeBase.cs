using System;
using System.Collections.Generic;
using System.Linq;
using TinyTanks.Tanks;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace TinyTanks.Level
{
    public class GameModeBase : NetworkBehaviour
    {
        public TankInput tankPrefab;
        public CinemachineCamera spectatorCamera;
        public Canvas respawnCanvas;

        public List<PlayerData> players { get; } = new List<PlayerData>();
        public TankInput localPlayer { get; private set; }
        
        public static GameModeBase instance { get; private set; }

        public void RespawnPlayer() => RespawnPlayer(-1);
        public void RespawnPlayer(int controllerIndex) => RespawnPlayerServerRpc(controllerIndex);

        private void Awake()
        {
            respawnCanvas.gameObject.SetActive(false);
            var players = FindObjectsByType<TankInput>(FindObjectsSortMode.None);
            foreach (var player in players) this.players.Add(new PlayerData(player, this.players.Count));
        }

        private void OnEnable()
        {
            ShowRespawnScreen(true);
            instance = this;
        }

        private void OnDisable()
        {
            if (instance == this) instance = null;
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

        private void FixedUpdate()
        {
            if (!IsServer) return;

            CheckPlayersAgainstBoundary();
            OnFixedUpdate();
        }

        protected virtual void OnFixedUpdate() { }

        private void CheckPlayersAgainstBoundary()
        {
            var boundary = LevelMeta.instance;
            if (boundary == null) return;

            foreach (var player in players)
            {
                if (player.tankInput.transform.position.y < boundary.killPlane)
                {
                    player.tankInput.tank.SetIsDestroyed(true);
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
                tank = new PlayerData(Instantiate(tankPrefab), players.Count);
                tank.tankInput.NetworkObject.SpawnWithOwnership(rpcParams.Receive.SenderClientId, true);

                players.Add(tank);
                SetControllingTank(tank.tankInput, rpcParams.Receive.SenderClientId, controllerIndex);
            }
            else
            {
                tank.tankInput.tank.SetIsDestroyed(false);
            }

            var spawnPoint = FindBestSpawnPoint(tank);
            tank.tankInput.tank.body.position = spawnPoint.position;
            tank.tankInput.tank.body.rotation = spawnPoint.rotation;
            tank.tankInput.tank.body.linearVelocity = Vector3.zero;
            tank.tankInput.tank.body.angularVelocity = Vector3.zero;
            
            NotifyRespawnClientRpc(replyParams);
        }

        private Transform FindBestSpawnPoint(PlayerData tank)
        {
            var levelMeta = FindFirstObjectByType<LevelMeta>();
            if (levelMeta == null) return transform;

            var bestSpawnPoint = (Transform)null;
            var bestScore = 0f;
            for (var i = 0; i < levelMeta.spawnPoints.Length; i++)
            {
                var spawnPoint = levelMeta.spawnPoints[i];
                var score = float.MaxValue;
                for (var j = 0; j < players.Count; j++)
                {
                    var player = players[j];
                    if (player.team == tank.team || player.tankInput.tank.isDestroyed) continue;
                    score = Mathf.Min(score, (player.tankInput.transform.position - spawnPoint.position).sqrMagnitude);
                }

                if (score > bestScore)
                {
                    bestSpawnPoint = spawnPoint;
                    bestScore = score;
                }
            }

            if (bestSpawnPoint == null)
            {
                bestSpawnPoint = levelMeta.spawnPoints[Random.Range(0, levelMeta.spawnPoints.Length)];
            }
            
            return bestSpawnPoint;
        }

        [ClientRpc]
        private void NotifyRespawnClientRpc(ClientRpcParams sendParams = default)
        {
            respawnCanvas.gameObject.SetActive(false);
            localPlayer.tank.SetActiveViewer(true);
        }

        [ClientRpc]
        private void SetLocalPlayerClientRpc(NetworkBehaviourReference tankInputRef, int controllerIndex, ClientRpcParams target = default)
        {
            tankInputRef.TryGet<TankInput>(out var tankInput);
            localPlayer = tankInput;
            tankInput.controllerIndex = controllerIndex;
            tankInput.tank.SetActiveViewer(true);
        }

        private PlayerData GetTankControllerForConnection(ulong clientId)
        {
            foreach (var player in players)
            {
                if (clientId == player.tankInput.OwnerClientId) return player;
            }

            return null;
        }

        public void SetControllingTank(TankInput tankInput, ulong clientId, int controllerIndex)
        {
            if (tankInput.NetworkObject.OwnerClientId != clientId) tankInput.NetworkObject.ChangeOwnership(clientId);
            SetLocalPlayerClientRpc(tankInput, controllerIndex, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            });
        }

        public class PlayerData
        {
            public TankInput tankInput;
            public int team;

            public PlayerData(TankInput tankInput, int team)
            {
                this.tankInput = tankInput;
                this.team = team;
            }
        }
    }
}