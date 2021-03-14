using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TXT.WEAVR.UI
{
    public class VirtualTabletTarget : MonoBehaviour, IPointerEnterHandler
    {
        private bool m_hasEntered = false;

        public bool HasEntered {
            get { return m_hasEntered; }
            set { m_hasEntered = value; }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_hasEntered = true;
        }

    }
}
