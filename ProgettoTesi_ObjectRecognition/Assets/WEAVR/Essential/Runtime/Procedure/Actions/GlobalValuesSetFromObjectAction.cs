using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{

    public class GlobalValuesSetFromObjectAction : BaseReversibleAction, ITargetingObject
    {
        [SerializeField]
        private string m_setVariable;
        [SerializeField]
        private ValuesStorage.ValueType m_ofType = ValuesStorage.ValueType.Any;
        [SerializeField]
        [Tooltip("The target object to change a value in")]
        [Draggable]
        private GameObject m_fromTarget;
        [SerializeField]
        [Tooltip("The property to change")]
        [PropertyDataFrom(nameof(m_fromTarget), isSetter: false, TypeFilterGetMethod = nameof(GetTypeFilter))]
        private Property m_property;

        private bool m_existed;
        private bool m_prevBool;
        private int m_prevFloat;
        private float m_prevInt;
        private Color m_prevColor;
        private Vector3 m_prevVector3;
        private string m_prevString;
        private Object m_prevObject;

        public Object Target { get => m_fromTarget; set => m_fromTarget = value is GameObject go ? go : value is Component c ? c.gameObject : value == null ? null : m_fromTarget; }

        public string TargetFieldName => nameof(m_fromTarget);

        private Type GetTypeFilter()
        {
            switch (m_ofType)
            {
                case ValuesStorage.ValueType.String: return typeof(string);
                case ValuesStorage.ValueType.Any: return typeof(object);
                case ValuesStorage.ValueType.Bool: return typeof(bool);
                case ValuesStorage.ValueType.Float: return typeof(float);
                case ValuesStorage.ValueType.Integer: return typeof(int);
                case ValuesStorage.ValueType.Color: return typeof(Color);
                case ValuesStorage.ValueType.Vector3: return typeof(Vector3);
                case ValuesStorage.ValueType.Object: return typeof(Object);
            }
            return null;
        }

        public override bool Execute(float dt)
        {
            m_existed = GlobalValues.Current.GetVariable(m_setVariable) != null;
            m_property.Target = m_fromTarget;
            switch (m_ofType)
            {
                case ValuesStorage.ValueType.Any:
                    switch(m_property.Value)
                    {
                        case int value:
                            GlobalValues.Current.SetValue(m_setVariable, value);
                            break;
                        case float value:
                            GlobalValues.Current.SetValue(m_setVariable, value);
                            break;
                        case bool value:
                            GlobalValues.Current.SetValue(m_setVariable, value);
                            break;
                        case string value:
                            GlobalValues.Current.SetValue(m_setVariable, value);
                            break;
                        case Color value:
                            GlobalValues.Current.SetValue(m_setVariable, value);
                            break;
                        case Vector3 value:
                            GlobalValues.Current.SetValue(m_setVariable, value);
                            break;
                        case Object value:
                            GlobalValues.Current.SetValue(m_setVariable, value);
                            break;
                        default:
                            if (m_property.Value != null)
                            {
                                GlobalValues.Current.SetVariable(m_setVariable);
                            }
                            break;
                    }
                    break;
                case ValuesStorage.ValueType.Bool:
                    m_prevBool = GlobalValues.Current.GetValue(m_setVariable, false);
                    GlobalValues.Current.SetValue(m_setVariable, (bool)m_property.Value);
                    break;
                case ValuesStorage.ValueType.Float:
                    m_prevFloat = GlobalValues.Current.GetValue(m_setVariable, 0);
                    GlobalValues.Current.SetValue(m_setVariable, (float)m_property.Value);
                    break;
                case ValuesStorage.ValueType.Integer:
                    m_prevInt = GlobalValues.Current.GetValue(m_setVariable, 0);
                    GlobalValues.Current.SetValue(m_setVariable, (int)m_property.Value);
                    break;
                case ValuesStorage.ValueType.Color:
                    m_prevColor = GlobalValues.Current.GetValue(m_setVariable, Color.clear);
                    GlobalValues.Current.SetValue(m_setVariable, (Color)m_property.Value);
                    break;
                case ValuesStorage.ValueType.Vector3:
                    m_prevVector3 = GlobalValues.Current.GetValue(m_setVariable, Vector3.zero);
                    GlobalValues.Current.SetValue(m_setVariable, (Vector3)m_property.Value);
                    break;
                case ValuesStorage.ValueType.String:
                    m_prevString = GlobalValues.Current.GetValue(m_setVariable, string.Empty);
                    GlobalValues.Current.SetValue(m_setVariable, m_property.Value?.ToString());
                    break;
                case ValuesStorage.ValueType.Object:
                    m_prevObject = GlobalValues.Current.GetValue<Object>(m_setVariable, null);
                    GlobalValues.Current.SetValue(m_setVariable, m_property.Value as Object);
                    break;
            }
            return true;
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (RevertOnExit)
            {
                switch (m_ofType)
                {
                    case ValuesStorage.ValueType.Any:
                        if (!m_existed)
                        {
                            GlobalValues.Current.RemoveVariable(m_setVariable);
                        }
                        break;
                    case ValuesStorage.ValueType.Bool:
                        GlobalValues.Current.SetValue(m_setVariable, m_prevBool);
                        break;
                    case ValuesStorage.ValueType.Float:
                        GlobalValues.Current.SetValue(m_setVariable, m_prevFloat);
                        break;
                    case ValuesStorage.ValueType.Integer:
                        GlobalValues.Current.SetValue(m_setVariable, m_prevInt);
                        break;
                    case ValuesStorage.ValueType.Color:
                        GlobalValues.Current.SetValue(m_setVariable, m_prevColor);
                        break;
                    case ValuesStorage.ValueType.Vector3:
                        GlobalValues.Current.SetValue(m_setVariable, m_prevVector3);
                        break;
                    case ValuesStorage.ValueType.String:
                        GlobalValues.Current.SetValue(m_setVariable, m_prevString);
                        break;
                    case ValuesStorage.ValueType.Object:
                        GlobalValues.Current.SetValue(m_setVariable, m_prevObject);
                        break;
                }
            }
        }

        public override string GetDescription()
        {
            return $"[{m_setVariable}] = {(!m_fromTarget ? "[Null]" : m_property == null || string.IsNullOrEmpty(m_property.Path) ? $"{m_fromTarget.name}.[ ? ]" : $"{m_fromTarget.name}.{m_property.PropertyName}")}";
        }
    }
}
