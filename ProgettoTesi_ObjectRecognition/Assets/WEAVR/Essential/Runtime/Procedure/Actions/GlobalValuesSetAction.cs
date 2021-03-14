using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class GlobalValuesSetAction : BaseReversibleAction
    {
        [SerializeField]
        private string m_variableName;
        [SerializeField]
        private ValuesStorage.ValueType m_valueType = ValuesStorage.ValueType.Any;
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
        [HideInInspector]
        private string m_otherVariable;

        private bool m_existed;
        private bool m_prevBool;
        private float m_prevFloat;
        private int m_prevInt;
        private Color m_prevColor;
        private Vector3 m_prevVector3;
        private string m_prevString;
        private Object m_prevObject;

        public override bool Execute(float dt)
        {
            m_existed = GlobalValues.Current.GetVariable(m_variableName) != null;
            switch (m_valueType)
            {
                case ValuesStorage.ValueType.Any:
                    if (string.IsNullOrEmpty(m_otherVariable))
                    {
                        GlobalValues.Current.SetVariable(m_variableName);
                    }
                    else
                    {
                        var otherVar = GlobalValues.Current.GetVariable(m_otherVariable);
                        var thisVar = GlobalValues.Current.GetOrCreateVariable(m_variableName);
                        thisVar.Type = otherVar?.Type ?? ValuesStorage.ValueType.Any;
                        thisVar.Value = otherVar?.Value;
                    }
                    break;
                case ValuesStorage.ValueType.Bool:
                    m_prevBool = GlobalValues.Current.GetValue(m_variableName, false);
                    GlobalValues.Current.SetValue(m_variableName, GlobalValues.Current.GetValue(m_otherVariable, m_boolValue));
                    break;
                case ValuesStorage.ValueType.Float:
                    m_prevFloat = GlobalValues.Current.GetValue(m_variableName, 0);
                    GlobalValues.Current.SetValue(m_variableName, GlobalValues.Current.GetValue(m_otherVariable, m_floatValue));
                    break;
                case ValuesStorage.ValueType.Integer:
                    m_prevInt = GlobalValues.Current.GetValue(m_variableName, 0);
                    GlobalValues.Current.SetValue(m_variableName, GlobalValues.Current.GetValue(m_otherVariable, m_intValue));
                    break;
                case ValuesStorage.ValueType.String:
                    m_prevString = GlobalValues.Current.GetValue(m_variableName, string.Empty);
                    GlobalValues.Current.SetValue(m_variableName, GlobalValues.Current.GetValue(m_otherVariable, m_stringValue));
                    break;
                case ValuesStorage.ValueType.Color:
                    m_prevColor = GlobalValues.Current.GetValue(m_variableName, Color.clear);
                    GlobalValues.Current.SetValue(m_variableName, GlobalValues.Current.GetValue(m_otherVariable, m_colorValue));
                    break;
                case ValuesStorage.ValueType.Vector3:
                    m_prevVector3 = GlobalValues.Current.GetValue(m_variableName, Vector3.zero);
                    GlobalValues.Current.SetValue(m_variableName, GlobalValues.Current.GetValue(m_otherVariable, m_vector3Value));
                    break;
                case ValuesStorage.ValueType.Object:
                    m_prevObject = GlobalValues.Current.GetValue<Object>(m_variableName, null);
                    GlobalValues.Current.SetValue(m_variableName, GlobalValues.Current.GetValue(m_otherVariable, m_objectValue));
                    break;
            }
            return true;
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (RevertOnExit)
            {
                switch (m_valueType)
                {
                    case ValuesStorage.ValueType.Any:
                        if (!m_existed)
                        {
                            GlobalValues.Current.RemoveVariable(m_variableName);
                        }
                        break;
                    case ValuesStorage.ValueType.Bool:
                        GlobalValues.Current.SetValue(m_variableName, m_prevBool);
                        break;
                    case ValuesStorage.ValueType.Float:
                        GlobalValues.Current.SetValue(m_variableName, m_prevFloat);
                        break;
                    case ValuesStorage.ValueType.Integer:
                        GlobalValues.Current.SetValue(m_variableName, m_prevInt);
                        break;
                    case ValuesStorage.ValueType.String:
                        GlobalValues.Current.SetValue(m_variableName, m_prevString);
                        break;
                    case ValuesStorage.ValueType.Color:
                        GlobalValues.Current.SetValue(m_variableName, m_prevColor);
                        break;
                    case ValuesStorage.ValueType.Vector3:
                        GlobalValues.Current.SetValue(m_variableName, m_prevVector3);
                        break;
                    case ValuesStorage.ValueType.Object:
                        GlobalValues.Current.SetValue(m_variableName, m_prevObject);
                        break;
                }
            }
        }

        public override string GetDescription()
        {
            if (!string.IsNullOrEmpty(m_otherVariable))
            {
                return $"[{m_variableName}] = [{m_otherVariable}]";
            }
            switch (m_valueType)
            {
                case ValuesStorage.ValueType.Any: return $"Create variable {m_variableName}";
                case ValuesStorage.ValueType.Bool: return $"[{m_variableName}] = {m_boolValue}";
                case ValuesStorage.ValueType.Float: return $"[{m_variableName}] = {m_floatValue}";
                case ValuesStorage.ValueType.Integer: return $"[{m_variableName}] = {m_intValue}";
                case ValuesStorage.ValueType.String: return $"[{m_variableName}] = {m_stringValue}";
                case ValuesStorage.ValueType.Color: return $"[{m_variableName}] = {m_colorValue}";
                case ValuesStorage.ValueType.Vector3: return $"[{m_variableName}] = {m_vector3Value}";
                case ValuesStorage.ValueType.Object: return $"[{m_variableName}] = {(m_objectValue ? m_objectValue.name : "null")}";
            }
            return base.GetDescription();
        }
    }
}
