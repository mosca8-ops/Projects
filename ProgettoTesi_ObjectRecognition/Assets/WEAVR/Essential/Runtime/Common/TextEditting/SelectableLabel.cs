using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Text Editing/Selectable Label")]
    public class SelectableLabel : SelectableText, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, IEventSystemHandler
    {
        [Header("Label")]
        [SerializeField]
        [TextArea]
        private string m_text;
        [SerializeField]
        private bool m_selectable;

        public bool CanBeSelected
        {
            get => m_selectable;
            set
            {
                if(m_selectable != value)
                {
                    m_selectable = value;
                    ApplySelectableChanges();
                }
            }
        }

        public string Text
        {
            get => m_foreground ? m_foreground.text : m_text;
            set
            {
                if (m_foreground)
                {
                    m_foreground.text = value;
                }
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            ApplySelectableChanges();
            Text = m_text;
        }

        protected override void Start()
        {
            base.Start();
            ApplySelectableChanges();
            Text = m_text;
        }

        private void ApplySelectableChanges()
        {
            foreach(var graphic in GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = m_selectable;
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            IsSelected = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsSelected = !IsSelected;
        }

        public void OnSelect(BaseEventData eventData)
        {
            IsSelected = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            
        }
    }
}
