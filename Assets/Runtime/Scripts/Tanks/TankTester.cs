using System.Linq;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTanks.Tanks
{
    [RequireComponent(typeof(TankController))]
    public class TankTester : NetworkBehaviour
    {
        public bool spin;
        
        private TankController tank;
        
        private void Awake() { tank = GetComponent<TankController>(); }

        private void Update()
        {
            if (IsOwner)
            {
                var gp = Gamepad.current;
                if (gp != null)
                {
                    if (gp.buttonSouth.wasPressedThisFrame) spin = !spin;
                }
                
                tank.steering = spin ? 1f : 0f;
                tank.SetStabs(true);
            }
        }
    }
}