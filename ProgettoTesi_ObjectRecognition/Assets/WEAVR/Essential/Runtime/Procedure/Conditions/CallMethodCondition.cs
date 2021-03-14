using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TXT.WEAVR.Core;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    public class CallMethodCondition : BaseCondition, ITargetingObject
    {
        [SerializeField]
        [Tooltip("The object to call the method on")]
        [Draggable]
        private GameObject m_target;
        [SerializeField]
        [HideInInspector]
        private string m_componentType;
        [SerializeField]
        [HideInInspector]
        private string m_methodId;
        [SerializeField]
        [HideInInspector]
        private string m_fieldPath;
        [SerializeField]
        //[HideInInspector]
        private ParameterValue[] m_parameters;
        [SerializeField]
        [Tooltip("The operator for the comparison")]
        private GenericOperator m_operator;
        [SerializeField]
        [Tooltip("The value to compare with")]
        private ParameterValue m_value;

        [NonSerialized]
        private Method m_method;
        [NonSerialized]
        private Func<object, object> m_secondaryGetter;
        [NonSerialized]
        private Component m_component;

        private StringBuilder m_sb;

        public Object Target
        {
            get => m_target;
            set => m_target = value is GameObject go ? go : value is Component c ? c.gameObject : value == null ? null : m_target;
        }

        public string TargetFieldName => nameof(m_target);

        public ParameterValue[] Parameters
        {
            get => m_parameters;
            set
            {
                if (!Application.isPlaying && m_parameters != value)
                {
                    m_parameters = value;
                }
            }
        }

        public override void OnValidate()
        {
            base.OnValidate();
            if (m_sb != null)
            {
                m_sb.Clear();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_value == null)
            {
                m_value = new ParameterValue();
            }
            m_operator.OnChanged -= Modified;
            m_operator.OnChanged += Modified;
        }

        protected override void OnDestroy()
        {
            m_operator.OnChanged -= Modified;
            base.OnDestroy();
        }

        public void ValidateCurrentValues()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                if (!string.IsNullOrEmpty(m_methodId))
                {
                    m_method = MethodFactory.GetMethod(m_methodId);
                }
                RetrieveComponent();
                if (m_method == null || m_method.Id != m_methodId || m_method.Parameters.Length != m_parameters?.Length || !m_component)
                {
                    m_methodId = string.Empty;
                    m_fieldPath = string.Empty;
                    m_method = null;
                    m_componentType = string.Empty;
                    m_component = null;
                    m_parameters = new ParameterValue[0];
                }
            }
        }

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            if (m_method == null)
            {
                m_method = MethodFactory.GetMethod(m_methodId);
                if(m_method != null && m_method.ReturnType != typeof(void) && !string.IsNullOrEmpty(m_fieldPath))
                {
                    m_secondaryGetter = m_method.ReturnType.GetField(m_fieldPath, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.FastGetter()
                                     ?? m_method.ReturnType.GetProperty(m_fieldPath, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.FastGetter();
                }
            }
            RetrieveComponent();
        }

        private void RetrieveComponent()
        {
            if (!string.IsNullOrEmpty(m_componentType) && (!m_component || !m_componentType.Contains(m_component.GetType().Name)))
            {
                m_component = GetComponent(m_target, m_componentType);
            }
        }

        public static Component GetComponent(GameObject target, string componentType)
        {
            if (string.IsNullOrEmpty(componentType)) { return null; }

            int index = 0;
            string cType = componentType;
            if (componentType[componentType.Length - 1] == ']')
            {
                int bracketIndex = componentType.IndexOf('[');
                cType = componentType.Substring(0, bracketIndex).Trim();
                int.TryParse(componentType.Substring(bracketIndex + 1, componentType.Length - 1), out index);
            }
            foreach (var c in target.GetComponents<Component>())
            {
                if (c && c.GetType().Name == cType && --index <= 0)
                {
                    return c;
                }
            }
            return null;
        }

        protected override bool EvaluateCondition()
        {
            object value = null;
            if (m_component)
            {
                value = m_method.Invoke(m_component, m_parameters.Select(p => p.Value).ToArray());
            }
            else
            {
                value = m_method.Invoke(m_target, m_parameters.Select(p => p.Value).ToArray());
            }
            if(m_secondaryGetter != null)
            {
                value = m_secondaryGetter(value);
            }
            return m_operator.Evaluate(value, m_value.Value);
        }

        public override void ForceEvaluation()
        {
            base.ForceEvaluation();
            try
            {
                if (m_component)
                {
                    m_method.Invoke(m_component, m_parameters.Select(p => p.Value).ToArray());
                }
                else
                {
                    m_method.Invoke(m_target, m_parameters.Select(p => p.Value).ToArray());
                }
            }
            catch (Exception e)
            {
                WeavrDebug.LogException(this, e);
            }
        }

        public override string GetDescription()
        {
            if (m_sb == null || m_sb.Length == 0)
            {
                m_sb = new StringBuilder();
                m_sb.Append(m_target ? m_target.name : "[ ? ]").Append('.');
                if (string.IsNullOrEmpty(m_methodId))
                {
                    m_sb.Append("Nothing");
                }
                else
                {
                    m_sb.Append(MethodFactory.GetMethod(m_methodId).Name).Append(' ').Append('(');
                    if (m_parameters != null && m_parameters.Length > 0)
                    {
                        for (int i = 0; i < m_parameters.Length; i++)
                        {
                            var value = m_parameters[i].Value;
                            m_sb.Append((value is Object o && o ? o.name : value) ?? "null").Append(',');
                        }
                        if (m_parameters.Length > 0)
                        {
                            m_sb.Length--;
                        }
                    }
                    m_sb.Append(')').Append(' ').Append(m_operator).Append(' ');
                    var val = m_value.Value;
                    m_sb.Append(val is Object ov && ov ? ov.name : val == null ? "null" : val.ToString());

                }
            }
            return m_sb.ToString();
        }
    }
}
