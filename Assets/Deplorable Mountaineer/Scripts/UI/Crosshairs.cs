using System;
using UnityEngine;
using UnityEngine.UI;

namespace Deplorable_Mountaineer.UI {
    public class Crosshairs : MonoBehaviour {
        [SerializeField] private Image primaryCrosshairs;
        [SerializeField] private Image secondaryCrosshairs;
        [SerializeField] private string primaryStyle = "Hitscan";
        [SerializeField] private string secondaryStyle = "Select";
        [SerializeField] private int primarySize = 64;
        [SerializeField] private int secondarySize = 64;
        [SerializeField] private Mode initialPrimaryMode = Mode.Inactive;
        [SerializeField] private Mode initialSecondaryMode = Mode.Inactive;
        [SerializeField] private Color inactiveColor = new Color(.5f, .5f, .5f, .5f);
        [SerializeField] private Color enemyColor = new Color(1, 0, 0, .75f);
        [SerializeField] private Color shootableColor = new Color(0, 1, 0, .75f);
        [SerializeField] private Color interactableColor = new Color(0, 0, 1, .75f);
        [SerializeField] private CrosshairStyle[] styles;

        private Mode _primaryMode;
        private Mode _secondaryMode;
        private int _primarySize;
        private int _secondarySize;
        private string _primaryStyle;
        private string _secondaryStyle;
        private float _zoomMultiplier = 1;

        public float ZoomMultiplier {
            get => _zoomMultiplier;
            set {
                _zoomMultiplier = value;
                UpdateSize();
            }
        }

        public string PrimaryStyle {
            get => _primaryStyle;
            set {
                _primaryStyle = value;
                SetStyle(primaryCrosshairs, _primaryStyle);
            }
        }

        public string SecondaryStyle {
            get => _secondaryStyle;
            set {
                _secondaryStyle = value;
                SetStyle(secondaryCrosshairs, _secondaryStyle);
            }
        }

        public int PrimarySize {
            get => _primarySize;
            set {
                _primarySize = value;
                UpdateSize();
            }
        }

        public int SecondarySize {
            get => _secondarySize;
            set {
                _secondarySize = value;
                UpdateSize();
            }
        }

        public Mode PrimaryMode {
            get => _primaryMode;
            set {
                _primaryMode = value;
                SetMode(primaryCrosshairs, _primaryMode);
            }
        }

        public Mode SecondaryMode {
            get => _secondaryMode;
            set {
                _secondaryMode = value;
                SetMode(secondaryCrosshairs, _secondaryMode);
            }
        }

        private void OnValidate(){
            PrimaryMode = initialPrimaryMode;
            SecondaryMode = initialSecondaryMode;
            _primarySize = primarySize;
            _secondarySize = secondarySize;
            PrimaryStyle = primaryStyle;
            SecondaryStyle = secondaryStyle;
        }

        private void Start(){
            PrimaryMode = initialPrimaryMode;
            SecondaryMode = initialSecondaryMode;
            _primarySize = primarySize;
            _secondarySize = secondarySize;
            PrimaryStyle = primaryStyle;
            SecondaryStyle = secondaryStyle;
            UpdateSize();
        }

        private void SetMode(Image image, Mode mode){
            image.color = mode switch {
                Mode.None => new Color(0, 0, 0, 0),
                Mode.Inactive => inactiveColor,
                Mode.Enemy => enemyColor,
                Mode.Shootable => shootableColor,
                Mode.Interactable => interactableColor,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        private void SetStyle(Image image, string style){
            image.sprite = GetSprite(style);
        }

        private Sprite GetSprite(string style){
            foreach(CrosshairStyle s in styles){
                if(s.name == style) return s.sprite;
            }

            return null;
        }

        private void UpdateSize(){
            primaryCrosshairs.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, PrimarySize*ZoomMultiplier*ZoomMultiplier);
            primaryCrosshairs.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, PrimarySize*ZoomMultiplier*ZoomMultiplier);
            secondaryCrosshairs.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, SecondarySize);
            secondaryCrosshairs.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, SecondarySize);
        }

        [Serializable]
        public class CrosshairStyle {
            public string name;
            public Sprite sprite;
        }

        public enum Mode {
            None,
            Inactive,
            Enemy,
            Shootable,
            Interactable
        }
    }
}