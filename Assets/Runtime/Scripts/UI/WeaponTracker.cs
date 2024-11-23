using TinyTanks.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTanks.UI
{
    public class WeaponTracker : MonoBehaviour
    {
        public TMP_Text text;
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
                alpha = flashCurve.Evaluate(flashTime);
            }

            var newColor = baseColor;
            newColor.a *= alpha;
            background.color = newColor;
        }
    }
}