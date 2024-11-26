using TinyTanks.Tanks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTanks.UI
{
    public class WeaponTracker : MonoBehaviour
    {
        public TMP_Text text;
        public Image icon;
        public Image background;
        public AnimationCurve flashCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private Color baseColor;
        private float flashTime;

        public TankWeapon weapon { get; private set; }

        public void SetWeapon(TankWeapon weapon)
        {
            this.weapon = weapon;
            if (weapon != null)
            {
                gameObject.SetActive(true);
                text.text = weapon.displayName;
                if (weapon.icon != null) icon.sprite = weapon.icon;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        
        private void Awake()
        {
            baseColor = background.color;
            SetWeapon(null);
        }

        private void Update()
        {
            var alpha = 1f;

            var isWaiting = weapon.isReloading;
            var waitPercent = weapon.reloadPercent;
            
            if (isWaiting)
            {
                background.fillAmount = waitPercent;
                flashTime = 0f;
            }
            else
            {
                flashTime += Time.deltaTime;
                background.fillAmount = 1f;
                alpha = flashCurve.Evaluate(flashTime);
            }

            var newColor = baseColor;
            newColor.a *= alpha;
            background.color = newColor;
        }
    }
}