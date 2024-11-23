using FishNet;
using UnityEngine;

namespace TinyTanks.Network
{
    public class NetworkDebugUI : MonoBehaviour
    {
        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(20, 20, 150, Screen.height - 40)))
            {
                if (GUILayout.Button("Start Host"))
                {
                    InstanceFinder.ServerManager.StartConnection();
                    InstanceFinder.ClientManager.StartConnection("127.0.0.1");
                }

                if (GUILayout.Button("Start Client"))
                {
                    InstanceFinder.ClientManager.StartConnection("127.0.0.1");
                }
            }
        }
    }
}