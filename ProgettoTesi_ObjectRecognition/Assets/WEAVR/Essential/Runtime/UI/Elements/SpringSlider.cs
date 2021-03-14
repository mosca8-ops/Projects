using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    [AddComponentMenu("WEAVR/UI/Spring Slider")]
    public class SpringSlider : Slider
    {
        bool m_isHoldingDown;

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            m_isHoldingDown = false;
            value = 0;
        }

        //public override void OnDrag(PointerEventData eventData) {
        //    base.OnDrag(eventData);
        //}

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            m_isHoldingDown = true;
        }

        protected override void Update()
        {
            base.Update();
            if (m_isHoldingDown)
            {
                onValueChanged.Invoke(value);
            }
        }
    }
}