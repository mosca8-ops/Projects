using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.LayoutSystem
{
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("WEAVR/Layout System/Layout Button")]
    public class LayoutButton : BaseLayoutItem
    {
        [SerializeField]
        [Draggable]
        protected Text m_buttonLabel;
        [SerializeField]
        [Draggable]
        protected Button m_button;
        
        [Space]
        [SerializeField]
        protected UnityEventString m_onLabelChanged;
        [SerializeField]
        protected UnityEvent m_onClicked;
        
        private bool m_clicked;
        private bool m_hasClicked;

        public string Label
        {
            get { return m_buttonLabel?.text; }
            set
            {
                if(m_buttonLabel != null && m_buttonLabel.text != value)
                {
                    m_buttonLabel.text = value;
                    m_onLabelChanged.Invoke(value);
                }
            }
        }

        public bool IsVisible
        {
            get { return gameObject.activeSelf; }
            set
            {
                if(gameObject.activeSelf != value)
                {
                    gameObject.SetActive(value);
                }
            }
        }

        public bool Clicked => m_clicked;

        public override void Clear()
        {
            Label = string.Empty;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            GetData();
        }

        private void GetData()
        {
            if (m_buttonLabel == null)
            {
                m_buttonLabel = GetComponentInChildren<Text>();
            }
            if (m_button == null)
            {
                m_button = GetComponent<Button>();
            }
        }

        // Use this for initialization
        protected override void Start()
        {
            base.Start();
            GetData();
            m_button.onClick.AddListener(() => m_clicked = true);
            m_button.onClick.AddListener(m_onClicked.Invoke);
        }

        protected virtual void OnEnable()
        {
            m_clicked = false;
        }

        protected virtual void OnDisable()
        {
            m_clicked = false;
        }

        protected virtual void Update()
        {
            if (m_hasClicked)
            {
                m_clicked = false;
                m_hasClicked = false;
            }
            else if (m_clicked)
            {
                m_hasClicked = true;
            }
        }

        public override void ResetToDefaults()
        {
            IsVisible = true;
            Label = name;
        }
    }
}
