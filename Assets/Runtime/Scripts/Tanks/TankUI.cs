using System.Linq;
using System.Text;
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
            var tankForward = tank.transform.forward;
            var turretForward = tank.model.turretMount.forward;
            var cameraForward = mainCamera.transform.forward;
            var normal = tank.transform.up;

            tankForward = Vector3.ProjectOnPlane(tankForward, normal).normalized;
            turretForward = Vector3.ProjectOnPlane(turretForward, normal).normalized;
            cameraForward = Vector3.ProjectOnPlane(cameraForward, normal).normalized;
            
            var turretAngle = Vector3.SignedAngle(tankForward, turretForward, tank.transform.up);
            var cameraAngle = Vector3.SignedAngle(tankForward, cameraForward, tank.transform.up);

            alignmentBody.rotation = Quaternion.Euler(0f, 0f, cameraAngle);
            alignmentTurret.rotation = Quaternion.Euler(0f, 0f, cameraAngle - turretAngle);

            var fwdSpeedKmph = Mathf.Abs(Vector3.Dot(tank.body.linearVelocity, tank.transform.forward)) * 3.6f;
            var info = new StringBuilder();
            info.AppendLine($"{fwdSpeedKmph:0}km/h");

            infoText.text = info.ToString();
        }
    }
}