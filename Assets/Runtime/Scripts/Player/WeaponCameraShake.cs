using System;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TinyTanks.Player
{
    [RequireComponent(typeof(TankWeapon))]
    public class WeaponCameraShake : MonoBehaviour
    {
        public CinemachineCamera target;

        [Space]
        public float recoilImpulse;
        public float recoilSpring;
        public float recoilDamping;

        [Space]
        public Vector3 translationAxis;
        public Vector3 rotationAxis;

        private TankWeapon weapon;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        
        private float position;
        private float velocity;

        private void Awake()
        {
            weapon = GetComponent<TankWeapon>();
            basePosition = target.transform.localPosition;
            baseRotation = target.transform.localRotation;
        }

        private void OnEnable()
        {
            weapon.WeaponFiredEvent += OnWeaponFired;
        }

        private void OnDisable()
        {
            weapon.WeaponFiredEvent -= OnWeaponFired;
        }

        private void OnWeaponFired()
        {
            velocity += recoilImpulse;
        }

        private void FixedUpdate()
        {
            var force = -position * recoilSpring - velocity * recoilDamping;
            
            position += velocity * Time.deltaTime;
            velocity += force * Time.deltaTime;

            if (weapon.enabled)
            {
                target.transform.localPosition = basePosition + translationAxis * position;
                target.transform.localRotation = baseRotation * Quaternion.AngleAxis(rotationAxis.magnitude * position, rotationAxis.normalized);
            }
        }
    }
}