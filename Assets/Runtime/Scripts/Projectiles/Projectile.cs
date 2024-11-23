using System;
using UnityEngine;

namespace TinyTanks.Projectiles
{
    public class Projectile : MonoBehaviour
    {
        public float damage;
        public float startSpeed;
        public float maxAge;
        public GameObject hitFx;

        private float age;

        [HideInInspector]
        public Vector3 position;
        [HideInInspector]
        public Vector3 velocity;

        private void OnEnable()
        {
            position = transform.position;
        }

        private void Start()
        {
            velocity += transform.forward * startSpeed;
        }

        private void FixedUpdate()
        {
            var ray = new Ray(position, velocity);
            if (Physics.Raycast(ray, out var hit, velocity.magnitude * Time.fixedDeltaTime * 1.01f))
            {
                if (hitFx != null) Instantiate(hitFx, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(gameObject);
            }
            
            if (age > maxAge) Destroy(gameObject);
            
            position += velocity * Time.fixedDeltaTime;
            velocity += Physics.gravity * Time.fixedDeltaTime;
            
            age += Time.fixedDeltaTime;
        }

        private void LateUpdate()
        {
            transform.position = position + velocity * (Time.time - Time.fixedTime);
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }
}