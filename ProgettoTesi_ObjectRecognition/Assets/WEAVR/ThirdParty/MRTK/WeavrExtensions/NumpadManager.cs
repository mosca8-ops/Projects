#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.Common
{
    using System.Text;
    using UnityEngine;

    public delegate void OnStringValueChange(string newValue);

    public class NumpadManager : MonoBehaviour
    {
        public TextInput textInput;
        public bool showTextInput = true;

        public event OnStringValueChange InputChanged;
        public event OnStringValueChange InputAccepted;

        private StringBuilder _numberBuilder;
        private bool _dotSet = false;

        void Awake()
        {
            _numberBuilder = new StringBuilder();
            _numberBuilder.Append('0');

            textInput.gameObject.SetActive(showTextInput);
        }

        private void OnValidate()
        {
            textInput.gameObject.SetActive(showTextInput);
        }

        public void AddDigit(string digit)
        {
            if (_numberBuilder.Length == 1 && _numberBuilder[0] == '0')
            {
                _numberBuilder.Length = 0;
            }
            _numberBuilder.Append(digit);
            UpdateEventListeners();
        }

        public void AddDot()
        {
            if (!_dotSet)
            {
                _numberBuilder.Append('.');
                UpdateEventListeners();
                _dotSet = true;
            }
        }

        public void Clear()
        {
            _numberBuilder.Length = 0;
            _numberBuilder.Append('0');
            _dotSet = false;
            UpdateEventListeners();
        }

        public void Backspace()
        {
            if (_numberBuilder[_numberBuilder.Length - 1] == '.')
            {
                _dotSet = false;
            }
            _numberBuilder.Length--;
            if (_numberBuilder.Length == 0)
            {
                _numberBuilder.Append('0');
            }
            UpdateEventListeners();
        }

        public void ToggleSign()
        {
            if (_numberBuilder.Length > 0 && !(_numberBuilder[_numberBuilder.Length - 1] == '.' || _numberBuilder[0] == '0'))
            {
                if (_numberBuilder[0] == '-')
                {
                    _numberBuilder.Remove(0, 1);
                }
                else
                {
                    _numberBuilder.Insert(0, '-');
                }
                UpdateEventListeners();
            }
        }

        public void OkClicked()
        {
            if (InputAccepted != null)
            {
                InputAccepted(_numberBuilder.ToString());
            }
        }

        public void CancelClicked()
        {

        }

        private void UpdateEventListeners()
        {
            textInput.TextValue = _numberBuilder.ToString();
            if (InputChanged != null)
            {
                InputChanged(_numberBuilder.ToString());
            }
        }

    }
}
#endif
