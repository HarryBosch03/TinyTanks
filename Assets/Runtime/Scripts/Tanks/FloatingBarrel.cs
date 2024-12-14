using System;
using UnityEngine;

namespace TinyTanks.Tanks
{
    [RequireComponent(typeof(TankWeapon))]
    public class FloatingBarrel : MonoBehaviour
    {
        public AnimationCurve animation;
        public float duration = 0.4f;
        public float distance = 0f;
        
        private TankWeapon weapon;
        private TankController tank;

        private float animationTimer;
        private Vector3 restPosition;

        private void Awake()
        {
            tank = GetComponentInParent<TankController>();
            weapon = GetComponent<TankWeapon>();
        }

        private void Start()
        {
            restPosition = tank.model.gunBarrel.localPosition;
        }

        private void OnEnable()
        {
            weapon.WeaponFiredEvent += OnWeaponFired;
            animationTimer = duration;
        }

        private void OnDisable()
        {
            weapon.WeaponFiredEvent -= OnWeaponFired;
        }

        private void Update()
        {
            tank.model.gunBarrel.localPosition = restPosition + Vector3.forward * animation.Evaluate(animationTimer / duration) * distance;
            if (animationTimer < duration) animationTimer += Time.deltaTime;
        }

        private void OnWeaponFired()
        {
            animationTimer = 0f;
        }
    }
}