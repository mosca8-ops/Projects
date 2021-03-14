using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class GlobalValuesGetCondition : BaseCondition
    {
        [SerializeField]
        private string m_variableName;
        [SerializeField]
        private ValuesStorage.ValueType m_valueType = ValuesStorage.ValueType.Any;
        [SerializeField]
        [ShowIf(nameof(ShowExists))]
        private bool m_exists;
        [SerializeField]
        [Tooltip("The comparison operator")]
        [ShowIf(nameof(ShowComparison))]
        private GenericOperator m_operator;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Bool)]
        [AssignableFrom(nameof(m_otherVariable))]
        private bool m_boolValue;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Integer)]
        [AssignableFrom(nameof(m_otherVariable))]
        private int m_intValue;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Float)]
        [AssignableFrom(nameof(m_otherVariable))]
        private float m_floatValue;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.String)]
        [AssignableFrom(nameof(m_otherVariable))]
        private string m_stringValue;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Color)]
        [AssignableFrom(nameof(m_otherVariable))]
        private Color m_colorValue;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Vector3)]
        [AssignableFrom(nameof(m_otherVariable))]
        private Vector3 m_vector3Value;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Object)]
        [AssignableFrom(nameof(m_otherVariable))]
        private Object m_objectValue;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.String)]
        private bool m_caseSensitive = true;

        [SerializeField]
        [HideInInspector]
        private string m_otherVariable;

        private bool ShowExists() => m_valueType == ValuesStorage.ValueType.Any;

        private bool ShowComparison() => m_valueType == ValuesStorage.ValueType.Float || m_valueType == ValuesStorage.ValueType.Integer;

        protected override bool EvaluateCondition()
        {
            if (!string.IsNullOrEmpty(m_otherVariable))
            {
                if (!ShowComparison())
                {
                    m_operator.Operator = ComparisonOperator.Equals;
                }
                return m_operator.Evaluate(GlobalValues.Current.GetValue(m_variableName), GlobalValues.Current.GetValue(m_variableName));
            }
            switch (m_valueType)
            {
                case ValuesStorage.ValueType.Any: return m_exists ? GlobalValues.Current.GetVariable(m_variableName) != null : GlobalValues.Current.GetVariable(m_variableName) == null;
                case ValuesStorage.ValueType.Bool: return GlobalValues.Current.GetBool(m_variableName) == m_boolValue;
                case ValuesStorage.ValueType.Color: return GlobalValues.Current.GetColor(m_variableName) == m_colorValue;
                case ValuesStorage.ValueType.Vector3: return GlobalValues.Current.GetVector3(m_variableName) == m_vector3Value;
                case ValuesStorage.ValueType.Float: return m_operator.Evaluate(GlobalValues.Current.GetFloat(m_variableName), m_floatValue);
                case ValuesStorage.ValueType.Integer: return m_operator.Evaluate(GlobalValues.Current.GetInt(m_variableName), m_intValue);
                case ValuesStorage.ValueType.String: return m_caseSensitive ? GlobalValues.Current.GetString(m_variableName) == m_stringValue : string.Equals(GlobalValues.Current.GetString(m_variableName), m_stringValue, System.StringComparison.InvariantCultureIgnoreCase);
                case ValuesStorage.ValueType.Object: return GlobalValues.Current.GetValue<Object>(m_variableName) == m_objectValue;
            }
            return false;
        }

        public override string GetDescription()
        {
            if (!string.IsNullOrEmpty(m_otherVariable))
            {
                return $"[{m_variableName}] {m_operator.Operator.ToMathString()} [{m_otherVariable}]";
            }
            switch (m_valueType)
            {
                case ValuesStorage.ValueType.Any: return $"[{m_variableName}] {(m_exists ? "Exists" : "Does not exist")}";
                case ValuesStorage.ValueType.Bool: return $"[{m_variableName}] = {m_boolValue}";
                case ValuesStorage.ValueType.Float: return $"[{m_variableName}] {m_operator.Operator.ToMathString()} {m_floatValue}";
                case ValuesStorage.ValueType.Integer: return $"[{m_variableName}] {m_operator.Operator.ToMathString()} {m_intValue}";
                case ValuesStorage.ValueType.Color: return $"[{m_variableName}] = {m_colorValue}";
                case ValuesStorage.ValueType.Vector3: return $"[{m_variableName}] = {m_vector3Value}";
                case ValuesStorage.ValueType.String: return $"[{m_variableName}] = {m_stringValue}";
                case ValuesStorage.ValueType.Object: return $"[{m_variableName}] = {(m_objectValue ? m_objectValue.name : "null")}";
            }
            return $"{m_variableName}";
        }
    }
}
