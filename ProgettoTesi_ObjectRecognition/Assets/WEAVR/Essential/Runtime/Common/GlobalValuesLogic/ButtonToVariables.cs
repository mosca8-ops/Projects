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

    [AddComponentMenu("WEAVR/Variables/Button To Variables")]
    public class ButtonToVariables : MonoBehaviour, IExecuteDisabled
    {
        [SerializeField]
        private GameObject m_activeObject;
        [SerializeField]
        private Button m_button;
        [SerializeField]
        private string m_varForInteraction;
        [SerializeField]
        private string m_varForClick;
        [SerializeField]
        private string m_varForActive;

        private ValuesStorage.Variable m_clickVar;
        private ValuesStorage.Variable m_interactionVar;
        private ValuesStorage.Variable m_activeVar;

        private bool m_initialized;

        private void Reset()
        {
            m_button = GetComponentInChildren<Button>();
            m_activeObject = gameObject;
        }

        private void Start()
        {
            if (m_initialized)
            {
                return;
            }

            if (!m_button)
            {
                m_button = GetComponentInChildren<Button>();
            }
            if (!m_activeObject)
            {
                m_activeObject = gameObject;
            }

            //GlobalValues.VariableRemoved -= GlobalValues_VariableRemoved;
            //GlobalValues.VariableRemoved += GlobalValues_VariableRemoved;

            m_button.onClick.AddListener(ButtonClicked);
            m_clickVar = GlobalValues.Current.GetOrCreateVariable(m_varForClick, ValuesStorage.ValueType.Bool, false);
            m_interactionVar = GlobalValues.Current.GetOrCreateVariable(m_varForInteraction, ValuesStorage.ValueType.Bool, m_button.interactable);
            m_interactionVar.ValueChanged -= InteractionVar_ValueChanged;
            m_interactionVar.ValueChanged += InteractionVar_ValueChanged;

            m_activeVar = GlobalValues.Current.GetOrCreateVariable(m_varForActive, ValuesStorage.ValueType.Bool, m_activeObject.activeSelf);
            m_activeVar.ValueChanged -= EnableVar_ValueChanged;
            m_activeVar.ValueChanged += EnableVar_ValueChanged;

            m_initialized = true;
        }

        private void OnEnable()
        {
            //m_activeVar = GlobalValues.Current.GetOrCreateVariable(m_varForActive, ValuesStorage.ValueType.Bool, m_activeObject.activeSelf);
            //m_activeVar.ValueChanged -= EnableVar_ValueChanged;
            //m_activeVar.ValueChanged += EnableVar_ValueChanged;
        }

        private void GlobalValues_VariableRemoved(ValuesStorage.Variable variable)
        {
            if(variable == m_activeVar)
            {
                m_activeObject.SetActive(false);
                m_activeVar = null;
            }
        }

        private void InteractionVar_ValueChanged(object obj)
        {
            if (obj is bool enable)
            {
                m_button.interactable = enable;
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
