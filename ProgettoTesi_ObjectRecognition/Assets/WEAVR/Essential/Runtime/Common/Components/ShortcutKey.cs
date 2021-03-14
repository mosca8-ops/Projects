using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{

    [ExecuteInEditMode]
    [AddComponentMenu("WEAVR/Utilities/Shortcut Key")]
    public class ShortcutKey : MonoBehaviour
    {

        [System.Serializable]
        public class UnityEventString : UnityEvent<string> { }

        [SerializeField]
        protected string m_description = "Description";

        [SerializeField]
        [KeyBinding(nameof(m_key))]
        protected bool m_value = true;

        [SerializeField]
        [HideInInspector]
        protected KeyBinding m_key = new KeyBinding(KeyCode.N);

        [Space]
        [SerializeField]
        private ValueEvents m_toggleEvents;

        public UnityEvent OnTrue => m_toggleEvents.OnTrue;
        public UnityEvent OnFalse => m_toggleEvents.OnFalse;
        public UnityEventBoolean OnToggle => m_toggleEvents.OnToggle;

        [Space]
        [SerializeField]
        private TextEvents m_textEvents;

        public UnityEventString OnDescriptionChanged => m_textEvents.OnDescriptionChanged;
        public UnityEventString OnKeyChanged => m_textEvents.OnKeyChanged;

        public bool Value {
            get { return m_value; }
            set {
                if (m_value != value)
                {
                    m_value = value;
                    OnToggle.Invoke(value);
                    if (value)
                    {
                        OnTrue.Invoke();
                    }
                    else
                    {
                        OnFalse.Invoke();
                    }
                }
            }
        }

        public string Description {
            get { return m_description; }
            set {
                if (m_description != value)
                {
                    m_description = value;
                    OnDescriptionChanged.Invoke(value);
                }
            }
        }

        public KeyCode Key {
            get { return m_key.KeyCode; }
            set {
                if (m_key.KeyCode != value)
                {
                    m_key.KeyCode = value;
                    OnKeyChanged.Invoke(value.ToString());
                }
            }
        }

        private void OnValidate()
        {
            if (OnDescriptionChanged != null)
            {
                OnDescriptionChanged.Invoke(m_description);
            }
            if (OnKeyChanged != null)
            {
                OnKeyChanged.Invoke(m_key.KeyCode.ToString());
            }
        }

        // Use this for initialization
        void Start()
        {
            if (Application.isPlaying)
            {
                OnDescriptionChanged.Invoke(m_description);
                OnKeyChanged.Invoke(m_key.KeyCode.ToString());
                if (m_value)
                {
                    OnTrue.Invoke();
                }
                else
                {
                    OnFalse.Invoke();
                }
                KeyBinding.BindKeyUp(m_key, ToggleValue);
            }
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                m_key.Update();
            }
        }

        public void ToggleValue()
        {
            Value = !Value;
        }

        [System.Serializable]
        private struct ValueEvents
        {
            public UnityEvent OnTrue;
            public UnityEvent OnFalse;
            public UnityEventBoolean OnToggle;
        }

        [System.Serializable]
        private struct TextEvents
        {
            public UnityEventString OnDescriptionChanged;
            public UnityEventString OnKeyChanged;
        }
    }
}