using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Deplorable_Mountaineer.Code_Library {
    public class ValueBar : MonoBehaviour {
        [SerializeField] private Image backgroundBar;
        [SerializeField] private Image fillBar;
        [SerializeField] private Image amountBar;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, .5f);
        [SerializeField] private Color fillColor = new Color(.4f, 0, .25f, .75f);
        [SerializeField] private Color amountColor = new Color(.5f, 1, 0, .75f);

        [SerializeField, Range(0, 1)] private float initialAmount;

        private float _amount;

        [PublicAPI]
        public float Amount {
            get => _amount;
            set {
                if(!(Mathf.Abs(value - _amount) > Mathf.Epsilon)) return;
                _amount = Mathf.Clamp(value, 0, 1);
                OnValueChanged();
            }
        }

        private void OnValidate(){
            initialAmount = Mathf.Clamp(initialAmount, 0, 1);
            _amount = initialAmount;
            OnValueChanged();
            if(backgroundBar) backgroundBar.color = backgroundColor;
            if(fillBar) fillBar.color = fillColor;
            if(amountBar) amountBar.color = amountColor;
        }

        private void OnValueChanged(){
            if(!amountBar) return;
            Vector3 s = amountBar.rectTransform.localScale;
            s.x = Amount;
            amountBar.rectTransform.localScale = s;
        }
    }
}