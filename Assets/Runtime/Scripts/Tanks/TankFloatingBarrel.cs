using System;
using System.Linq;
using UnityEngine;

namespace TinyTanks.Tanks
{
    public class TankFloatingBarrel : MonoBehaviour
    {
        public TankWeapon weapon;
        public AnimationCurve curve = new AnimationCurve
        {
#pragma warning disable CS0618 // Type or member is obsolete
            keys = new[]
            {
                new Keyframe
                {
                    time = 0.0f,
                    value = 0.0f,
                    inTangent = -24.25838279724121f,
                    outTangent = -24.25838279724121f,
                    tangentMode = 34,
                    weightedMode = 0,
                    inWeight = 0.3333333432674408f,
                    outWeight = 0.3333333432674408f
                },
                new Keyframe
                {
                    time = 0.041222862899303439f,
                    value = -1.0f,
                    inTangent = 0.0f,
                    outTangent = 3.07277774810791f,
                    tangentMode = 1,
                    weightedMode = 0,
                    inWeight = 0.3333333432674408f,
                    outWeight = 0.04723212122917175f
                },
                new Keyframe
                {
                    time = 1.0f,
                    value = 0.0f,
                    inTangent = 0.0f,
                    outTangent = 0.0f,
                    tangentMode = 136,
                    weightedMode = 0,
                    inWeight = 0.3333333432674408f,
                    outWeight = 0.3333333432674408f
                }
            }
#pragma warning restore CS0618 // Type or member is obsolete
        };
        public float duration = 0.6f;
        public float distance = 0.6f;
        public Vector3 axis = Vector3.forward;

        private float timer;
        private Vector3 basePosition;

        private void Awake()
        {
            basePosition = transform.localPosition;
        }

        private void OnEnable() { weapon.WeaponFiredEvent += OnWeaponFired; }

        private void OnDisable() { weapon.WeaponFiredEvent -= OnWeaponFired; }

        private void OnWeaponFired() { timer = 0f; }

        private void Update()
        {
            timer += Time.deltaTime;
            transform.localPosition = basePosition + axis.normalized * curve.Evaluate(timer / duration) * distance;
        }

        private void Reset()
        {
            var tank = GetComponentInParent<TankController>();
            weapon = tank.weapons.ElementAtOrDefault(0);
        }
    }
}