using System;
using System.Linq;
using TinyTanks.UI;
using TMPro;
using UnityEngine;

namespace TinyTanks.Player
{
    public class TankUI : MonoBehaviour
    {
        private WeaponTracker[] weaponTrackers;
        private TankController tank;

        private void Awake()
        {
            tank = GetComponentInParent<TankController>();
            weaponTrackers = GetComponentsInChildren<WeaponTracker>(true);
        }

        private void OnEnable()
        {
            tank.ChangeWeaponEvent += OnWeaponChanged;
        }

        private void OnDisable()
        {
            tank.ChangeWeaponEvent -= OnWeaponChanged;
        }

        private void OnWeaponChanged()
        {
            var currentTracker = (WeaponTracker)null;
            
            for (var i = 0; i < weaponTrackers.Length; i++)
            {
                var tracker = weaponTrackers[i];
                if (tracker.weapon == tank.currentWeapon)
                {
                    currentTracker = tracker;
                    continue;
                }

                tracker.transform.SetSiblingIndex(i);
                tracker.transform.localScale = Vector3.one * 0.5f;
            }
            
            if (currentTracker != null)
            {
                currentTracker.transform.SetAsFirstSibling();
                currentTracker.transform.localScale = Vector3.one;
            }
        }

        private void Start()
        {
            for (var i = 0; i < weaponTrackers.Length; i++)
            {
                weaponTrackers[i].SetWeapon(tank.weapons.ElementAtOrDefault(i));
            }
            OnWeaponChanged();
        }
    }
}