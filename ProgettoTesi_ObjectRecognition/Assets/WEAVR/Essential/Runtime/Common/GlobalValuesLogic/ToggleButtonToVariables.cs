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

    [AddComponentMenu("WEAVR/Variables/Toggles Button To Variables")]
    public class ToggleButtonToVariables : MonoBehaviour, IExecuteDisabled
    {
        [SerializeField]
        private GameObject m_activeObject;
        [SerializeField]
        private Button m_buttonOn;
        [SerializeField]
        private Button m_buttonOff;
        [SerializeField]
        private string m_varForInteraction;
        [SerializeField]
        private string m_varForClick;
        [SerializeField]
        private string m_varForActive;
        [SerializeField]
        private string m_varForValue;

        private ValuesStorage.Variable m_clickVar;
        private ValuesStorage.Variable m_interactionVar;
        private ValuesStorage.Variable m_activeVar;
        private ValuesStorage.Variable m_valueVar;

        private bool m_initialized;

        public bool Value
        {
            get => m_buttonOff && m_buttonOff.gameObject.activeSelf;
            set
            {
                m_buttonOff.gameObject.SetActive(value);
                m_buttonOn.gameObject.SetActive(!value);
                GlobalValues.Current.SetValue(m_varForValue, value);
            }
        }

        private void Reset()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            m_buttonOn = buttons.Length > 0 ? buttons[0] : null;
            m_buttonOff = buttons.Length > 1 ? buttons[1] : null;
            m_activeObject = gameObject;
        }

        private void Start()
        {
            if (m_initialized)
            {
                return;
            }

            if (!m_buttonOn || !m_buttonOff)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                m_buttonOn = !m_buttonOn && buttons.Length > 0 ? buttons[0] : m_buttonOn;
                m_buttonOff = !m_buttonOff && buttons.Length > 1 ? buttons[1] : m_buttonOff;
            }
            if (!m_activeObject)
            {
                m_activeObject = gameObject;
            }

            //GlobalValues.VariableRemoved -= GlobalValues_VariableRemoved;
            //GlobalValues.VariableRemoved += GlobalValues_VariableRemoved;

            m_buttonOn.onClick.AddListener(On_Clicked);
            m_buttonOff.onClick.AddListener(Off_Clicked);

            m_buttonOn.onClick.AddListener(ButtonClicked);
            m_buttonOff.onClick.AddListener(ButtonClicked);
            m_clickVar = GlobalValues.Current.GetOrCreateVariable(m_varForClick, ValuesStorage.ValueType.Bool, false);
            m_interactionVar = GlobalValues.Current.GetOrCreateVariable(m_varForInteraction, ValuesStorage.ValueType.Bool, m_buttonOn.interactable);
            m_interactionVar.ValueChanged -= InteractionVar_ValueChanged;
            m_interactionVar.ValueChanged += InteractionVar_ValueChanged;

            m_activeVar = GlobalValues.Current.GetOrCreateVariable(m_varForActive, ValuesStorage.ValueType.Bool, m_activeObject.activeSelf);
            m_activeVar.ValueChanged -= EnableVar_ValueChanged;
            m_activeVar.ValueChanged += EnableVar_ValueChanged;

            m_valueVar = GlobalValues.Current.GetOrCreateVariable(m_varForValue, ValuesStorage.ValueType.Bool, Value);
            m_valueVar.ValueChanged -= ValueVar_ValueChanged;
            m_valueVar.ValueChanged += ValueVar_ValueChanged;

            m_initialized = true;
        }

        private void ValueVar_ValueChanged(object obj)
        {
            if(obj is bool b)
            {
                Value = b;
            }
        }

        private void Off_Clicked()
        {
            Value = false;
        }

        private void On_Clicked()
        {
            Value = true;
        }

        private void OnEnable()
        {
            //m_activeVar = GlobalValues.Current.GetOrCreateVariable(m_varForActive, ValuesStorage.ValueType.Bool, m_activeObject.activeSelf);
            //m_activeVar.ValueChanged -= EnableVar_ValueChanged;
            //m_activeVar.ValueChanged += EnableVar_ValueChanged;

            //m_valueVar = GlobalValues.Current.GetOrCreateVariable(m_varForValue, ValuesStorage.ValueType.Bool, Value);
            //m_valueVar.ValueChanged -= ValueVar_ValueChanged;
            //m_valueVar.ValueChanged += ValueVar_ValueChanged;
        }

        private void GlobalValues_VariableRemoved(ValuesStorage.Variable variable)
        {
            if(variable == m_activeVar)
            {
                m_activeObject.SetActive(false);
                m_activeVar.ValueChanged -= EnableVar_ValueChanged;
                m_activeVar = null;
            }
            if(variable == m_valueVar)
            {
                m_valueVar.ValueChanged -= ValueVar_ValueChanged;
                m_valueVar = null;
            }
        }

        private void InteractionVar_ValueChanged(object obj)
        {
            if (obj is bool enable)
            {
                m_buttonOn.interactable = enable;
                m_buttonOff.interactable = enable;
            }
        }

        private void EnableVar_ValueChanged(object obj)
        {
            if (obj is bool enable)
            {
                m_activeObject.SetActive(enable);
            }
        }

        private async void ButtonClicked()
        {
            m_clickVar.Value = true;
            await Task.Delay(100);
            m_clickVar.Value = false;
        }

        public void InitDisabled()
        {
            Start();
        }
    }
}
