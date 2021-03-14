using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Animation/Animator Parameters Setter")]
    public class AnimatorParameterSetter : MonoBehaviour
    {
        public static Func<Animator, AnimatorControllerParameter[]> s_getParameters;

        [SerializeField]
        [Draggable]
        private Animator m_animator;
        [SerializeField]
        [Button(nameof(RefreshList), "Refresh")]
        [DisplayName(MethodToGetName = nameof(GetParameterTypename))]
        [ArrayElement(nameof(m_parameters), nameof(Parameter.name))]
        private Parameter m_parameter;
        [SerializeField]
        [HideInInspector]
        private Parameter[] m_parameters;

        public int IntValue
        {
            get => m_parameter.type == AnimatorControllerParameterType.Int ? m_animator.GetInteger(m_parameter.hashId) : 0;
            set
            {
                if(m_parameter.type == AnimatorControllerParameterType.Int)
                {
                    m_animator.SetInteger(m_parameter.hashId, value);
                }
            }
        }

        public bool BoolValue
        {
            get => m_parameter.type == AnimatorControllerParameterType.Bool ? m_animator.GetBool(m_parameter.hashId) : false;
            set
            {
                if (m_parameter.type == AnimatorControllerParameterType.Bool)
                {
                    m_animator.SetBool(m_parameter.hashId, value);
                }
            }
        }

        public float FloatValue
        {
            get => m_parameter.type == AnimatorControllerParameterType.Float ? m_animator.GetFloat(m_parameter.hashId) : 0;
            set
            {
                if (m_parameter.type == AnimatorControllerParameterType.Float)
                {
                    m_animator.SetFloat(m_parameter.hashId, value);
                }
            }
        }

        public bool Trigger
        {
            get => false;
            set
            {
                if (m_parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    if (value)
                    {
                        m_animator.SetTrigger(m_parameter.hashId);
                    }
                    else
                    {
                        m_animator.ResetTrigger(m_parameter.hashId);
                    }
                }
            }
        }

        private string GetParameterTypename()
        {
            return string.IsNullOrEmpty(m_parameter.name) ? "Parameter" : $"[{m_parameter.type}]";
        }

        private void OnValidate()
        {
            if (!m_animator)
            {
                m_animator = GetComponentInChildren<Animator>();
                if (!m_animator)
                {
                    m_animator = GetComponentInParent<Animator>();
                }
            }
            if (s_getParameters != null)
            {
                var parameters = s_getParameters(m_animator);
                if(m_parameters == null || parameters.Length != m_parameters.Length)
                {
                    m_parameters = parameters.Select(p => new Parameter(p)).ToArray();
                }
                else
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if(parameters[i].name != m_parameters[i].name)
                        {
                            m_parameters = parameters.Select(p => new Parameter(p)).ToArray();
                            break;
                        }
                    }
                }
            }
        }

        private void RefreshList()
        {
            OnValidate();
            m_parameter.hashId = Animator.StringToHash(m_parameter.name);
        }

        private void OnEnable()
        {
            m_parameter.hashId = Animator.StringToHash(m_parameter.name);
        }

        public void SetBool(bool value)
        {
            BoolValue = value;
        }

        public void SetInt(int value)
        {
            IntValue = value;
        }

        public void SetFloat(float value)
        {
            FloatValue = value;
        }

        public void SetTrigger()
        {
            Trigger = true;
        }

        public void ResetTrigger()
        {
            Trigger = false;
        }

        [Serializable]
        private struct Parameter
        {
            public string name;
            public int hashId;
            public AnimatorControllerParameterType type;

            public Parameter(AnimatorControllerParameter parameter)
            {
                name = parameter.name;
                type = parameter.type;
                hashId = 0;
            }
        }
    }
}
