using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Variables/Query Variable")]
    public class QueryGlobalValue : MonoBehaviour
    {
        [SerializeField]
        private string m_variableName;
        [SerializeField]
        private ValuesStorage.ValueType m_valueType = ValuesStorage.ValueType.Any;
        [SerializeField]
        [ShowIf(nameof(ShowExists))]
        private bool m_exists;
        [SerializeField]
        [ShowIf(nameof(ShowComparisonOperator))]
        private GenericOperator m_operator;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Bool)]
        private bool m_boolValue;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Integer)]
        private int m_intValue;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Float)]
        private float m_floatValue;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.String)]
        private string m_stringValue;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.String)]
        private bool m_caseSensitive = true;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Color)]
        private Color m_colorValue;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Vector3)]
        private Vector3 m_vector3Value;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Object)]
        private Object m_objectValue;

        [Space]
        [SerializeField]
        private bool m_checkOnStart = false;

        private bool ShowExists() => m_valueType == ValuesStorage.ValueType.Any;
        private bool ShowComparisonOperator() => m_valueType == ValuesStorage.ValueType.Float || m_valueType == ValuesStorage.ValueType.Integer;

        public UnityEvent onEqualValue;

        private ValuesStorage.Variable m_variable;
        public object Value
        {
            get => m_variable?.Value;
            set { if (m_variable != null) m_variable.Value = value; }
        }

        private void OnEnable()
        {
            m_variable = GlobalValues.Current.GetVariable(m_variableName);
            if(m_variable != null)
            {
                m_variable.ValueChanged -= Variable_ValueChanged;
                m_variable.ValueChanged += Variable_ValueChanged;
            }
        }

        private void Start()
        {
            if (m_checkOnStart)
            {
                RecheckValue();
            }
        }

        private void OnDisable()
        {
            if (m_variable != null)
            {
                m_variable.ValueChanged -= Variable_ValueChanged;
            }
        }

        private void Variable_ValueChanged(object value)
        {
            RecheckValue();
        }

        private void RecheckValue()
        {
            if ((m_valueType == ValuesStorage.ValueType.Any && m_exists && GlobalValues.Current.GetVariable(m_variableName) != null))
            {
                onEqualValue.Invoke();
            }
            else if (m_variable != null && m_variable.Type == m_valueType)
            {
                switch (m_variable.Type)
                {
                    case ValuesStorage.ValueType.Bool:
                        if ((bool)Value == m_boolValue)
                        {
                            onEqualValue.Invoke();
                        }
                        break;
                    case ValuesStorage.ValueType.Float:
                        if (m_operator.Evaluate((float)Value, m_floatValue))
                        {
                            onEqualValue.Invoke();
                        }
                        break;
                    case ValuesStorage.ValueType.Integer:
                        if (m_operator.Evaluate((int)Value, m_intValue))
                        {
                            onEqualValue.Invoke();
                        }
                        break;
                    case ValuesStorage.ValueType.Color:
                        if(Value is Color c && c == m_colorValue)
                        {
                            onEqualValue.Invoke();
                        }
                        break;
                    case ValuesStorage.ValueType.Vector3:
                        if (Value is Vector3 v && v == m_vector3Value)
                        {
                            onEqualValue.Invoke();
                        }
                        break;
                    case ValuesStorage.ValueType.Object:
                        if (Value is Object o && o == m_objectValue)
                        {
                            onEqualValue.Invoke();
                        }
                        break;
                    case ValuesStorage.ValueType.String:
                        if (m_caseSensitive ? Value as string == m_stringValue : string.Equals(Value as string, m_stringValue, System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            onEqualValue.Invoke();
                        }
                        break;
                }
            }
        }
    }
}
