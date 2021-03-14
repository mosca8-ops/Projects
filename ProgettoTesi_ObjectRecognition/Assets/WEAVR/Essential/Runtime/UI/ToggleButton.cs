using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    [AddComponentMenu("")]
    public class ToggleButton : Button
    {
        public string EnabledTrigger = "Enabled";
        [SerializeField]
        private bool m_value;

        [Space]
        public UnityEventBoolean onValueChanged;

        public bool Value
        {
            get => m_value;
            set
            {
                if (m_value != value)
                {
                    m_value = value;
                    onValueChanged?.Invoke(m_value);
                }
            }
        }

        public void EnableButton(bool bvalue)
        {
            Value = bvalue;
            if (animator != null)
            {
                animator.SetBool(EnabledTrigger, Value);
            }

            CameraOrbit.Instance.EnableComponent(Value);
        }

        public void ToggleValue()
        {
            EnableButton(!Value);
        }

        //public void SetInteractable(bool value)
        //{
        //    interactable = value;
        //    //EnableButton(value);
        //}

        public void SetActive(bool value)
        {
            interactable = value;
            EnableButton(false);
            gameObject.SetActive(value);
            //EnableButton(value);
        }
    }
}
