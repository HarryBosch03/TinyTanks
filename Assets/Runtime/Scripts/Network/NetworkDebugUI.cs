using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTanks.Network
{
    public class NetworkDebugUI : MonoBehaviour
    {
        private NetworkManager netManager;

        private void Awake() { netManager = GetComponent<NetworkManager>(); }

        private void Update()
        {
            if (!netManager.IsServer && !netManager.IsClient)
            {
                var kb = Keyboard.current;
                if (kb.spaceKey.wasPressedThisFrame || kb.hKey.wasPressedThisFrame) netManager.StartHost();
                if (kb.cKey.wasPressedThisFrame) netManager.StartClient();
            }
        }

        private void OnGUI()
        {
            if (!netManager.IsServer && !netManager.IsClient)
            {
                using (new GUILayout.AreaScope(new Rect(20f, 20f, 200f, Screen.height - 40f)))
                {
                    if (GUILayout.Button("Start Host")) netManager.StartHost();
                    if (GUILayout.Button("Start Client")) netManager.StartClient();
                }
            }
        }
    }
}