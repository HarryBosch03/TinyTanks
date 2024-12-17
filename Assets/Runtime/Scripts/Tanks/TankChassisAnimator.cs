using System;
using UnityEngine;

namespace TinyTanks.Tanks
{
    public class TankChassisAnimator : MonoBehaviour
    {
        private TankController tank;

        private void Awake() { tank = GetComponentInParent<TankController>(); }
    }
}