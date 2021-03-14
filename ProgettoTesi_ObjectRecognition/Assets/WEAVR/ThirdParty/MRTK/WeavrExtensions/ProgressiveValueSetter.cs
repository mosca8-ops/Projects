#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.InteractionUI
{
    using TXT.WEAVR.Core;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    [RequireComponent(typeof(Button))]
    public class ProgressiveValueSetter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Default title for this component")]
        public string defaultTitle;
        public float maxValue = 5.0f;
        public float value = 1.0f;
        public float timeToEnableEdit = 1.0f;

        [Header("Components")]
        public Image imageComponent;
        public Text titleText;
        public Text valueText;

        public event OnValueChanged<ProgressiveValueSetter, float> ValueChanged;

        private Button _buttonComponent;
        private bool _isHovered = false;
        private bool _changed = false;
        private float _timeToEnable;
        private Color _delayTextColor;

        /// <summary>
        /// The title of this component
        /// </summary>
        public string Title {
            get {
                return titleText != null ? titleText.text : null;
            }
            set {
                if (titleText != null)
                {
                    titleText.text = value;
                }
            }
        }

        private void OnValidate()
        {
            Title = defaultTitle;
        }

        // Use this for initialization
        void Start()
        {
            _buttonComponent = GetComponent<Button>();
            _timeToEnable = timeToEnableEdit;
            if (imageComponent == null)
            {
                imageComponent = GetComponentInChildren<Image>();
            }
            if (valueText == null)
            {
                valueText = GetComponentInChildren<Text>();
            }
            _delayTextColor = valueText.color;
            _delayTextColor.a = 0;
            valueText.color = _delayTextColor;

            imageComponent.fillAmount = value / maxValue;
        }

        // Update is called once per frame
        void Update()
        {
            if (_isHovered || _timeToEnable < timeToEnableEdit)
            {
                _timeToEnable = Mathf.MoveTowards(_timeToEnable, _isHovered ? 0 : timeToEnableEdit, Time.deltaTime);

                if (_timeToEnable == 0 && _isHovered && value <= maxValue)
                {
                    value = _changed ? value + Time.deltaTime : 0;
                    valueText.text = string.Format("{0:0.00} s", value);
                    imageComponent.fillAmount = value / maxValue;
                    ChangeDelayTextTransparencyTo(0.8f);
                    _changed = true;
                }
            }
            else if (_delayTextColor.a > 0)
            {
                ChangeDelayTextTransparencyTo(0);
                _changed = false;
            }
        }

        private void ChangeDelayTextTransparencyTo(float newTransparency)
        {
            _delayTextColor.a = Mathf.MoveTowards(_delayTextColor.a, newTransparency, Time.deltaTime * 0.5f);
            valueText.color = _delayTextColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (_changed && ValueChanged != null)
            {
                ValueChanged(this, value);
            }
        }
    }
}
#endif
