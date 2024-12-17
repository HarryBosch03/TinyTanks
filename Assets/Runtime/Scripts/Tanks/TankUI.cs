using System.Linq;
using System.Text;
using TinyTanks.CDM;
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
        private CdmController cdmController;
        private Camera mainCamera;

        private void Awake()
        {
            tank = GetComponentInParent<TankController>();
            cdmController = tank.GetComponentInChildren<CdmController>();
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
            var turretAngle = Vector3.SignedAngle(tank.transform.forward, tank.model.turretMount.forward, tank.transform.up);
            var cameraAngle = Vector3.SignedAngle(tank.transform.forward, mainCamera.transform.forward, tank.transform.up);

            alignmentBody.rotation = Quaternion.Euler(0f, 0f, cameraAngle);
            alignmentTurret.rotation = Quaternion.Euler(0f, 0f, cameraAngle - turretAngle);

            var fwdSpeedKmph = Mathf.Abs(Vector3.Dot(tank.body.linearVelocity, tank.transform.forward)) * 3.6f;
            var info = new StringBuilder();
            info.AppendLine($"{fwdSpeedKmph:0}km/h");

            info.Append("<color=#FF0000>");
            foreach (var component in cdmController.EnumerateComponents())
            {
                if (component.destroyed) info.AppendLine($"{component.displayName} {(component.isFlesh ? "Dead" : "Destroyed")}");
            }
            info.Append("</color>");

            infoText.text = info.ToString();
        }
    }
}