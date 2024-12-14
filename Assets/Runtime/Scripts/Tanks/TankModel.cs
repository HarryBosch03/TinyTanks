using System;
using UnityEngine;

namespace TinyTanks.Tanks
{
    public class TankModel : MonoBehaviour
    {
        public TankModel parentModel;
        
        public Transform body;
        public Transform turret;
        
        [Space]
        public Transform turretMount;
        public Transform gunPivot;
        public Transform gunMuzzle;
        public Transform gunBarrel;
        public Transform coaxMuzzle;
        public Transform leftTrack;
        public Transform rightTrack;

        private void Awake()
        {
            turret.SetParent(turretMount);
            turret.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void Update()
        {
            if (parentModel != null)
            {
                turretMount.localRotation = parentModel.turretMount.localRotation;
                gunPivot.localRotation = parentModel.gunPivot.localRotation;
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (body == null && transform.childCount > 0) body = transform.GetChild(0); 
                if (turret == null && transform.childCount > 1) turret = transform.GetChild(1); 
                
                if (body != null)
                {
                    if (turretMount == null) turretMount = body.Find("TurretMount");
                    if (leftTrack == null) leftTrack = body.Find("Track.L");
                    if (rightTrack == null) rightTrack = body.Find("Track.R");
                }

                if (turret != null)
                {
                    if (gunPivot == null) gunPivot = turret.Find("GunPivot");
                    if (gunMuzzle == null) gunMuzzle = turret.Find("GunPivot/GunBarrel/GunMuzzle");
                    if (coaxMuzzle == null) coaxMuzzle = turret.Find("GunPivot/CoaxBarrel/CoaxMuzzle");
                    if (gunBarrel == null) gunBarrel = turret.Find("GunPivot/GunBarrel");
                }

                if (turret != null && turretMount != null)
                {
                    turret.SetPositionAndRotation(turretMount.position, turretMount.rotation);
                }
            }
        }
    }
}