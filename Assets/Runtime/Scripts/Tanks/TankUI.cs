using System.Linq;
using TinyTanks.UI;
using TMPro;
using UnityEngine;

namespace TinyTanks.Tanks
{
    public class TankUI : MonoBehaviour
    {
        public RectTransform alignmentBody;
        public RectTransform alignmentTurret;
        public TMP_Text infoText;
        
        private WeaponTracker[] weaponTrackers;
        private TankController tank;
        private Camera mainCamera;

        private void Awake()
        {
            tank = GetComponentInParent<TankController>();
            weaponTrackers = GetComponentsInChildren<WeaponTracker>(true);
            mainCamera = Camera.main;
        }

        private void Start()
        {
            for (var i = 0; i < weaponTrackers.Length; i++)
            {
                weaponTrackers[i].SetWeapon(tank.weapons.ElementAtOrDefault(i));
            }
        }

        private void Update()
        {
            var turretAngle = Vector3.SignedAngle(tank.transform.forward, tank.visualModel.turretMount.forward, tank.transform.up);
            var cameraAngle = Vector3.SignedAngle(tank.transform.forward, mainCamera.transform.forward, tank.transform.up);

            alignmentBody.rotation = Quaternion.Euler(0f, 0f, cameraAngle);
            alignmentTurret.rotation = Quaternion.Euler(0f, 0f, cameraAngle - turretAngle);

            var fwdSpeedKmph = Mathf.Abs(Vector3.Dot(tank.body.linearVelocity, tank.transform.forward)) * 3.6f;
            infoText.text = $"{fwdSpeedKmph:0}km/h\nStabs: {(tank.stabsEnabled? "On" : "Off")}";
        }
    }
}