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
        public Vector3 velocity;

        private void Start()
        {
            velocity += transform.forward * startSpeed;
        }

        private void FixedUpdate()
        {
            var ray = new Ray(transform.position, velocity);
            if (Physics.Raycast(ray, out var hit, velocity.magnitude * Time.deltaTime * 1.01f))
            {
                if (hitFx != null) Instantiate(hitFx, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(gameObject);
            }
            
            if (age > maxAge) Destroy(gameObject);
            
            transform.position += velocity * Time.deltaTime;
            velocity += Physics.gravity * Time.deltaTime;
            
            age += Time.deltaTime;
        }
    }
}