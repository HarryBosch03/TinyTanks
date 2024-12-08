using System;
using FishNet;
using FishNet.Managing;
using Unity.Multiplayer.Playmode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTanks.Network
{
    public class NetworkDebugUI : MonoBehaviour
    {
        private void Start()
        {
            var mpmTags = CurrentPlayer.ReadOnlyTags();
            var networkManager = InstanceFinder.NetworkManager;

            if (Array.Exists(mpmTags, e => e == "Host"))
            {
                StartHost();
            }
            else if (Array.Exists(mpmTags, e => e == "Client"))
            {
                StartClient();
            }
        }

        private void Update()
        {
            var serverManager = InstanceFinder.ServerManager;
            var clientManager = InstanceFinder.ClientManager;
            
            if (!serverManager.Started && !clientManager.Started)
            {
                var gp = Gamepad.current;
                if (gp != null && Application.isFocused)
                {
                    if (gp.rightShoulder.wasPressedThisFrame) StartHost();
                    if (gp.leftShoulder.wasPressedThisFrame) StartClient();
                }

                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.spaceKey.wasPressedThisFrame) StartHost();
                    if (kb.cKey.wasPressedThisFrame) StartClient();
                }
            }
        }

        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(20, 20, 150, Screen.height - 40)))
            {
                var serverManager = InstanceFinder.ServerManager;
                var clientManager = InstanceFinder.ClientManager;
                var timeManager = InstanceFinder.TimeManager;

                if (serverManager.Started)
                {
                    
                }
                else if (clientManager.Started)
                {
                    var ping = timeManager.RoundTripTime;
                    GUILayout.Label($"Ping: {ping}");
                }
                else if (!InstanceFinder.ClientManager.Started && !InstanceFinder.ServerManager.Started)
                {
                    if (GUILayout.Button("Start Host [RS]"))
                    {
                        StartHost();
                    }

                    if (GUILayout.Button("Start Client [LS]"))
                    {
                        StartClient();
                    }
                }
            }
        }

        private static void StartClient() { InstanceFinder.ClientManager.StartConnection("127.0.0.1"); }

        private static void StartHost()
        {
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection("127.0.0.1");
        }
    }
}