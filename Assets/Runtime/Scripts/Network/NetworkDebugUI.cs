using System;
using FishNet;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTanks.Network
{
    public class NetworkDebugUI : MonoBehaviour
    {
        private void Update()
        {
            var gp = Gamepad.current;
            if (gp != null && Application.isFocused && !InstanceFinder.ClientManager.Started && !InstanceFinder.ServerManager.Started)
            {
                if (gp.rightShoulder.wasPressedThisFrame)
                {
                    StartHost();
                }
                
                if (gp.leftShoulder.wasPressedThisFrame)
                {
                    StartClient();
                }
            }
        }

        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(20, 20, 150, Screen.height - 40)))
            {
                if (!InstanceFinder.ClientManager.Started && !InstanceFinder.ServerManager.Started)
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