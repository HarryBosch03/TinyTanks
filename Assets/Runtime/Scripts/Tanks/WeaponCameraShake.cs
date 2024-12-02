using Unity.Cinemachine;
using UnityEngine;

namespace TinyTanks.Tanks
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
        
        private float position;
        private float velocity;

        private void Awake()
        {
            weapon = GetComponent<TankWeapon>();
            basePosition = target.transform.localPosition;
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

        private void LateUpdate()
        {
            var force = -position * recoilSpring - velocity * recoilDamping;
            
            position += velocity * Time.deltaTime;
            velocity += force * Time.deltaTime;

            if (weapon.enabled)
            {
                target.transform.localPosition = basePosition + translationAxis * position;
                target.transform.rotation = weapon.muzzle.rotation * Quaternion.AngleAxis(rotationAxis.magnitude * position, rotationAxis.normalized);
            }
        }
    }
}