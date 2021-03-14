using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Variables/Toggle To Variables")]
    public class ToggleToVariables : MonoBehaviour
    {
        [SerializeField]
        private Toggle m_toggle;
        [SerializeField]
        private string m_varForToggleOn;


        private ValuesStorage.Variable m_toggleOnVar;


        private void Reset()
        {
            m_toggle = GetComponentInChildren<Toggle>();
        }

        private void Start()
        {
            if (!m_toggle)
            {
                m_toggle = GetComponentInChildren<Toggle>();
            }

            m_toggle.onValueChanged.AddListener(delegate { ToggleValueChanged(m_toggle.isOn); });
            m_toggleOnVar = GlobalValues.Current.GetOrCreateVariable(m_varForToggleOn, ValuesStorage.ValueType.Bool, m_toggle.isOn);           
        }

        private void ToggleValueChanged(object obj)
        {
            if (obj is bool enable)
            {
                m_toggleOnVar.Value = enable;
            }
        }
    }
}
