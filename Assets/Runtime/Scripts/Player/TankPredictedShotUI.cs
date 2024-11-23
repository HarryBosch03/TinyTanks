using System;
using UnityEngine;

namespace TinyTanks.Player
{
    [DefaultExecutionOrder(300)]
    [RequireComponent(typeof(TankController))]
    public class TankPredictedShotUI : MonoBehaviour
    {
        public RectTransform target;

        private Camera mainCamera;
        private TankController tank;

        private void Awake()
        {
            mainCamera = Camera.main;
            tank = GetComponent<TankController>();
        }
        
        private void LateUpdate()
        {
            target.anchoredPosition = mainCamera.WorldToScreenPoint(tank.PredictProjectileArc());
        }
    }
}