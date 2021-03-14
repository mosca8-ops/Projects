using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.Player
{
    public class AutoDeactivationBehaviour : MonoBehaviour
    {
        [SerializeField]
        [AbsoluteValue]
        private float m_hideTimeout = 1f;

        [NonSerialized]
        private Coroutine m_autoDeactivateCoroutine;

        public float HideTimeout
        {
            get => m_hideTimeout;
            set
            {
                if(value < 0)
                {
                    throw new ArgumentException("The value for Hide Timeout is negative");
                }
                m_hideTimeout = value;
            }
        }
        
        protected void StartAutoDeactivationCoroutine()
        {
            m_autoDeactivateCoroutine = StartCoroutine(AutoDeactivate(m_hideTimeout));
        }

        protected void StopAutoDeactivationCoroutine()
        {
            if (m_autoDeactivateCoroutine != null)
            {
                StopCoroutine(m_autoDeactivateCoroutine);
            }
        }

        private IEnumerator AutoDeactivate(float timeout)
        {
            yield return new WaitForSeconds(timeout);
            gameObject.SetActive(false);
            m_autoDeactivateCoroutine = null;
        }
    }
}
