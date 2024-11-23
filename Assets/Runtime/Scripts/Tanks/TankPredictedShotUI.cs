using UnityEngine;

namespace TinyTanks.Tanks
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
            if (tank.isActiveViewer && !tank.useSight)
            {
                target.gameObject.SetActive(true);
                target.anchoredPosition = mainCamera.WorldToScreenPoint(tank.PredictProjectileArc());
            }
            else
            {
                target.gameObject.SetActive(false);
            }
        }
    }
}