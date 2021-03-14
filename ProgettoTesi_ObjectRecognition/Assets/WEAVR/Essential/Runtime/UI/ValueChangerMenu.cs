    using System;
    using System.Collections;
    using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.UI;
using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

namespace TXT.WEAVR.Interaction
{
    [AddComponentMenu("WEAVR/UI/Value Changer Menu")]
    public class ValueChangerMenu : AbstractMenu3D
    {
        #region [  STATIC PART  ]
        private static ValueChangerMenu _instance;
        /// <summary>
        /// Gets the instance of the value change menu
        /// </summary>
        public static ValueChangerMenu Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<ValueChangerMenu>();
                    if (_instance == null) {
                        foreach (var rootObject in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) {
                            _instance = rootObject.GetComponentInChildren<ValueChangerMenu>(true);
                            if (_instance != null) {
                                break;
                            }
                        }
                    }
                    if (_instance != null) {
                        _instance.Start();
                    }
                    else {
                        Debug.LogErrorFormat("[{0}]: Unable to find gameobject with such component", typeof(ValueChangerMenu).Name);
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Shows the value change popup with specified options
        /// </summary>
        /// <param name="point">The 3D point where to show the popup</param>
        /// <param name="adaptScale">Whether to change the scale of the canvas based on bounding boxes</param>
        /// <param name="fieldName">The field whose value will be changed</param>
        /// <param name="getValue">The function to get the value</param>
        /// <param name="minValue">The lower bound of the value</param>
        /// <param name="maxValue">The upper bound of the value</param>
        /// <param name="stepSize">The size of the step</param>
        /// <param name="setCallback">The callback which updates the value to the field</param>
        /// <returns>The popup instance</returns>
        public static ValueChangerMenu Show(Transform point, bool adaptScale, string fieldName, Func<float> getValue, float minValue, float maxValue, float stepSize, Action<float> setCallback)
        {
            Instance.gameObject.SetActive(true);
            Instance.ShowPopup(point, adaptScale, false, fieldName, getValue, minValue, maxValue, Mathf.Abs(stepSize), setCallback);
            return Instance;
        }

        /// <summary>
        /// Shows the value change popup with specified options
        /// </summary>
        /// <param name="point">The 3D point where to show the popup</param>
        /// <param name="adaptScale">Whether to change the scale of the canvas based on bounding boxes</param>
        /// <param name="fieldName">The field whose value will be changed</param>
        /// <param name="value">The initial value</param>
        /// <param name="minValue">The lower bound of the value</param>
        /// <param name="maxValue">The upper bound of the value</param>
        /// <param name="stepSize">The size of the step</param>
        /// <param name="setCallback">The callback which updates the value to the field</param>
        /// <returns>The popup instance</returns>
        public static ValueChangerMenu Show(Transform point, bool adaptScale, string fieldName, float value, float minValue, float maxValue, float stepSize, Action<float> setCallback) {
            Instance.gameObject.SetActive(true);
            Instance.ShowPopup(point, adaptScale, false, fieldName, value, minValue, maxValue, Mathf.Abs(stepSize), setCallback, true);
            return Instance;
        }

        /// <summary>
        /// Shows the value change popup with specified options
        /// </summary>
        /// <param name="point">The 3D point where to show the popup</param>
        /// <param name="adaptScale">Whether to change the scale of the canvas based on bounding boxes</param>
        /// <param name="fieldName">The field whose value will be changed</param>
        /// <param name="value">The initial value</param>
        /// <param name="stepSize">The size of the step</param>
        /// <param name="setCallback">The callback which updates the value to the field</param>
        /// <returns>The popup instance</returns>
        public static ValueChangerMenu Show(Transform point, bool adaptScale, string fieldName, float value, float stepSize, Action<float> setCallback) {
            Instance.gameObject.SetActive(true);
            Instance.ShowPopup(point, adaptScale, false, fieldName, value, float.MinValue, float.MaxValue, Mathf.Abs(stepSize), setCallback, true);
            return Instance;
        }

        /// <summary>
        /// Shows the value change popup with specified options
        /// </summary>
        /// <param name="point">The 3D point where to show the popup</param>
        /// <param name="adaptScale">Whether to change the scale of the canvas based on bounding boxes</param>
        /// <param name="fieldName">The field whose value will be changed</param>
        /// <param name="value">The initial value</param>
        /// <param name="setCallback">The callback which updates the value to the field</param>
        /// <returns>The popup instance</returns>
        public static ValueChangerMenu Show(Transform point, bool adaptScale, string fieldName, float value, Action<float> setCallback) {
            Instance.gameObject.SetActive(true);
            Instance.ShowPopup(point, adaptScale, false, fieldName, value, float.MinValue, float.MaxValue, 1.0f, setCallback, true);
            return Instance;
        }

        #endregion

        [Header("Parameters")]
        public int valueTextLength = 7;
        [Tooltip("Update time in seconds")]
        public float updateCycle = 0.2f;

        [Header("Components")]
        [SerializeField]
        [Common.HiddenBy(nameof(m_fieldTextElement), hiddenWhenTrue: true)]
        [Draggable]
        private Text _fieldText;
        [SerializeField]
        [Common.HiddenBy(nameof(_fieldText), hiddenWhenTrue: true)]
        [Draggable]
        [Common.Type(typeof(ITextComponent))]
        private Component m_fieldTextElement;
        [SerializeField]
        [Common.HiddenBy(nameof(m_valueTextElement), hiddenWhenTrue: true)]
        [Draggable]
        private Text _valueText;
        [SerializeField]
        [Common.HiddenBy(nameof(_valueText), hiddenWhenTrue: true)]
        [Draggable]
        [Common.Type(typeof(ITextComponent))]
        private Component m_valueTextElement;
        [SerializeField]
        [Draggable]
        private GameObject _controlsPanel;
        [SerializeField]
        [Draggable]
        private Button _plusButton;
        [SerializeField]
        [Draggable]
        private Button _minusButton;
        [SerializeField]
        [Draggable]
        private Slider _slider;

        private float _stepSize;
        
        private Action<float> m_setter;
        private Func<float> m_getter;

        private float _minValue;
        private float _maxValue;
        private float _value;

        private float _updateCounter = 0;
        
        private Color _defaultValueTextColor;

        // Automatic part
        private float _targetValue;
        private float _autoStepSize;
        private bool _isAuto;
        private bool m_isPoling;

        public Color ValueColor {
            get => (m_valueTextElement as ITextComponent)?.Color ?? _valueText.color;
            set 
            {
                if (m_valueTextElement is ITextComponent tElem)
                {
                    tElem.Color = value == Color.clear ? _defaultValueTextColor : value;
                }
                else
                {
                    _valueText.color = value == Color.clear ? _defaultValueTextColor : value;
                }
            }
        }

        public string ValueText
        {
            get => (m_valueTextElement as ITextComponent)?.Text ?? _valueText.text;
            set
            {
                if(m_valueTextElement is ITextComponent tElem)
                {
                    tElem.Text = value;
                }
                else
                {
                    _valueText.text = value;
                }
            }
        }

        public string FieldText
        {
            get => (m_fieldTextElement as ITextComponent)?.Text ?? _valueText.text;
            set
            {
                if (m_fieldTextElement is ITextComponent tElem)
                {
                    tElem.Text = value;
                }
                else
                {
                    _fieldText.text = value;
                }
            }
        }

        protected virtual void Reset()
        {
            m_fieldTextElement = GetComponentInChildren<ITextComponent>(true) as Component;
            if(m_fieldTextElement == null)
            {
                _fieldText = GetComponentInChildren<Text>();
            }
            m_valueTextElement = GetComponentsInChildren<ITextComponent>(true)
                                .FirstOrDefault(t => t is Component tc 
                                                  && tc != m_fieldTextElement 
                                                  && tc != _fieldText) as Component;
            if(m_valueTextElement == null)
            {
                _valueText = GetComponentsInChildren<Text>(true)
                                .FirstOrDefault(t => t != m_fieldTextElement
                                                  && t != _fieldText);
            }
        }

        protected override void OnValidate() {
            base.OnValidate();

            if(_minusButton == null) {
                _minusButton = GetComponentInChildren<Button>();
            }
            if(_plusButton == null) {
                foreach (var button in GetComponentsInChildren<Button>()) {
                    if (button != _minusButton) {
                        _plusButton = button;
                        break;
                    }
                }
            }
            if(_slider == null) {
                _slider = GetComponentInChildren<Slider>();
            }
        }

        protected override void Clear() {
            base.Clear();

            FieldText = "";
            ValueText = "";
            _value = 0;
            _isAuto = false;
            m_isPoling = false;
            if(_controlsPanel != null) {
                _controlsPanel.SetActive(true);
            }

            ValueColor = _defaultValueTextColor;
        }

        public void ShowPopup(Transform point, bool adaptScale, bool attachAsChild,
                              string fieldName, float value, 
                              float minValue, float maxValue, float stepSize, 
                              Action<float> setCallback, bool showControls)
        {
            FieldText = fieldName;
            _value = value;
            ValueText = LimitString(value.ToString(), valueTextLength);
            _minValue = minValue;
            _maxValue = maxValue;
            _stepSize = stepSize;
            m_setter = setCallback ?? delegate { };

            m_isPoling = false;

            _controlsPanel.SetActive(showControls);

            Show(point, adaptScale, attachAsChild);
        }

        private string LimitString(string str, int maxLength)
        {
            return str.Length > maxLength ? str.Substring(0, valueTextLength) : str;
        }

        public void ShowPopup(Transform point, bool adaptScale, bool attachAsChild,
                              string fieldName, Func<float> getCallback,
                              float minValue, float maxValue, float stepSize,
                              Action<float> setCallback)
        {
            FieldText = fieldName;
            _value = getCallback();
            ValueText = LimitString(_value.ToString(), valueTextLength);
            _minValue = minValue;
            _maxValue = maxValue;
            _stepSize = stepSize;
            m_setter = setCallback ?? delegate { };

            m_isPoling = true;

            _controlsPanel.SetActive(false);

            Show(point, adaptScale, attachAsChild);
        }

        public void SetAutomaticTargetValue(float targetValue, float stepSize) {
            _targetValue = targetValue;
            _autoStepSize = stepSize / _stepSize;
            _isAuto = true;
            if (_controlsPanel != null) {
                _controlsPanel.SetActive(false);
            }
        }

        // Use this for initialization
        protected override void Start() {
            base.Start();
            if (_instance == null) {
                _instance = this;
            }
            else if (_instance != this) {
                Debug.LogErrorFormat("[{0}]: More than one instance detected, deleting object '{1}'", typeof(ValueChangerMenu).Name, gameObject.name);
                Destroy(this);
                return;
            }

            _defaultValueTextColor = ValueColor;

            //_slider.onValueChanged.AddListener(ValueChange);
            _plusButton.onClick.AddListener(() => ValueChange(1));
            _minusButton.onClick.AddListener(() => ValueChange(-1));
            _stepSize = 1;
        }

        private void FixedUpdate() {
            if (!IsVisible) return;
            if (m_isPoling)
            {
                ValueChange(m_getter());
            }
            else
            {
                _updateCounter += Time.deltaTime;
                if (_updateCounter > updateCycle)
                {
                    if (_isAuto)
                    {
                        ValueChange(Mathf.MoveTowards(_value, _targetValue, _autoStepSize) - _value);
                        _isAuto = _value != _targetValue;
                    }
                    else if (_slider.value != 0)
                    {
                        ValueChange(_slider.value);
                    }
                    _updateCounter = 0;
                }
            }
        }

        private void ValueChange(float newValue) {
            _value += newValue * _stepSize;
            _value = Mathf.Clamp(_value, _minValue, _maxValue);
            ValueText = LimitString(_value.ToString(), 5);
            m_setter(_value);
        }
    }
}