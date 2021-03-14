using System.Collections;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TXT.WEAVR.UI
{
    [DefaultExecutionOrder(10000)]
    public class KeyboardBlocker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        [HideInInspector]
        private KeyboardPlayer m_keyboard;

        public void OnPointerDown(PointerEventData eventData)
        {
            m_keyboard.SetCurrentFocus(gameObject);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            
        }

        IEnumerator BlockKeyboardHiding()
        {
            yield return null;
            m_keyboard.CancelKeyboardHiding();
        }

        private void OnValidate()
        {
            m_keyboard = GetComponentInParent<KeyboardPlayer>();
        }

        private void Reset()
        {
            m_keyboard = GetComponentInParent<KeyboardPlayer>();
        }

        private void Start()
        {
            if (!m_keyboard)
            {
                m_keyboard = GetComponentInParent<KeyboardPlayer>();
            }
        }
    }
}


