using System;
using System.Collections;
using System.Linq;
using TXT.WEAVR;
using TXT.WEAVR.Common;
using TXT.WEAVR.Procedure;
//using UnityEditor.Animations;
using UnityEngine;

public class SetAnimatorValueAction : BaseReversibleProgressAction, ITargetingObject, ISerializedNetworkProcedureObject
{
    [SerializeField]
    [Tooltip("The animator where to set the parameter")]
    [Draggable]
    private Animator m_target;
    [SerializeField]
    [Tooltip("The parameter to set change the value for")]
    [ArrayElement(nameof(m_parametersNames), true)]
    private string m_parameterName;

    [SerializeField]
    [HideInInspector]
    private string[] m_parametersNames;

    [SerializeField]
    [HideInInspector]
    private AnimatorControllerParameterType m_parameterType;
    [SerializeField]
    [Tooltip("The value to set")]
    [ShowIf(nameof(ShowBoolValue))]
    private bool m_boolValue;
    [SerializeField]
    [Tooltip("The value to set")]
    [ShowIf(nameof(ShowIntValue))]
    private int m_intValue;
    [SerializeField]
    [Tooltip("The value to set")]
    [ShowIf(nameof(ShowFloatValue))]
    private float m_floatValue;

    #region [  ISerializedNetworkProcedureObject IMPLEMENTATION  ]

    [SerializeField]
    private bool m_isGlobal = true;
    public string IsGlobalFieldName => nameof(m_isGlobal);
    public bool IsGlobal => m_isGlobal;

    #endregion

    [NonSerialized]
    private string m_stringValue;
    [NonSerialized]
    private string m_prevParameterName;
    [NonSerialized]
    private int[] m_prevStateNameHashes;

    [NonSerialized]
    private int m_prevInt;
    [NonSerialized]
    private float m_prevFloat;
    [NonSerialized]
    private bool m_prevBool;

    public UnityEngine.Object Target
    {
        get => m_target;
        set
        {
            BeginChange();
            m_target = value is GameObject go ? go.GetComponent<Animator>()
                : value is Component c ? c.GetComponent<Animator>()
                : value is Animator anim ? anim
                : value == null ? null : m_target;
            PropertyChanged(nameof(m_target));
        }
    }

    public string TargetFieldName => nameof(m_target);

    public Func<Animator, AnimatorControllerParameter[]> GetParametersCallback;

    public string ParameterName
    {
        get => m_parameterName;
        set
        {
            if (m_parameterName != value)
            {
                BeginChange();
                m_parameterName = value;
                PropertyChanged(nameof(ParameterName));
            }
        }
    }

    public AnimatorControllerParameterType ParameterType
    {
        get => m_parameterType;
        set
        {
            if (m_parameterType != value)
            {
                BeginChange();
                m_parameterType = value;
                PropertyChanged(nameof(ParameterType));
            }
        }
    }

    public bool BoolValue
    {
        get => m_boolValue;
        set
        {
            if (m_boolValue != value)
            {
                BeginChange();
                m_boolValue = value;
                PropertyChanged(nameof(BoolValue));
            }
        }
    }

    public int IntValue
    {
        get => m_intValue;
        set
        {
            if (m_intValue != value)
            {
                BeginChange();
                m_intValue = value;
                PropertyChanged(nameof(IntValue));
            }
        }
    }

    public float FloatValue
    {
        get => m_floatValue;
        set
        {
            if (m_floatValue != value)
            {
                BeginChange();
                m_floatValue = value;
                PropertyChanged(nameof(FloatValue));
            }
        }
    }

    public string[] ParametersNames
    {
        get => m_parametersNames;
        set
        {
            bool change = m_parametersNames == null || m_parametersNames.Length != value.Length;
            if (!change)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (m_parametersNames[i] != value[i])
                    {
                        change = true;
                        break;
                    }
                }
            }
            if (change)
            {
                m_parametersNames = value;
            }
        }
    }

    private bool ShowBoolValue() => !string.IsNullOrEmpty(m_parameterName) && m_parameterType == AnimatorControllerParameterType.Bool;
    private bool ShowFloatValue() => !string.IsNullOrEmpty(m_parameterName) && m_parameterType == AnimatorControllerParameterType.Float;
    private bool ShowIntValue() => !string.IsNullOrEmpty(m_parameterName) && m_parameterType == AnimatorControllerParameterType.Int;

    public override void OnValidate()
    {
        base.OnValidate();
        if (m_prevParameterName == null)
        {
            m_prevParameterName = m_parameterName;
        }

        if (m_target)
        {
            ParametersNames = (GetParametersCallback?.Invoke(m_target) ?? m_target.parameters).Select(p => p.name).ToArray();
        }
        else if (m_target is null)
        {
            GetParametersCallback?.Invoke(null);
            m_parametersNames = new string[0];
            m_parameterName = string.Empty;
            m_prevParameterName = string.Empty;
        }

        if (!string.IsNullOrEmpty(m_parameterName) && m_prevParameterName != m_parameterName)
        {
            m_prevParameterName = m_parameterName;
            var parameter = (GetParametersCallback?.Invoke(m_target) ?? m_target.parameters).FirstOrDefault(p => p.name == m_parameterName);
            if (parameter != null && m_parameterType != parameter.type)
            {
                m_parameterType = parameter.type;
                switch (m_parameterType)
                {
                    case AnimatorControllerParameterType.Bool:
                        m_boolValue = parameter.defaultBool;
                        m_stringValue = m_boolValue.ToString();
                        break;
                    case AnimatorControllerParameterType.Float:
                        m_floatValue = parameter.defaultFloat;
                        m_stringValue = m_floatValue.ToString();
                        break;
                    case AnimatorControllerParameterType.Int:
                        m_intValue = parameter.defaultInt;
                        m_stringValue = m_intValue.ToString();
                        break;
                    default:
                        m_target.SetTrigger(m_parameterName);
                        break;
                }
            }
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        m_prevParameterName = m_parameterName;
    }

    public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
    {
        base.OnStart(flow, executionMode);

        if (RevertOnExit)
        {
            //m_prevLayerId = m_target.la
            m_prevStateNameHashes = new int[m_target.layerCount];
            for (int i = 0; i < m_target.layerCount; i++)
            {
                m_prevStateNameHashes[i] = m_target.GetCurrentAnimatorStateInfo(i).shortNameHash;
            }
            switch (m_parameterType)
            {
                case AnimatorControllerParameterType.Bool:
                    m_prevBool = m_target.GetBool(m_parameterName);
                    break;
                case AnimatorControllerParameterType.Int:
                    m_prevInt = m_target.GetInteger(m_parameterName);
                    break;
                case AnimatorControllerParameterType.Float:
                    m_prevFloat = m_target.GetFloat(m_parameterName);
                    break;
            }
        }
    }

    public override bool Execute(float dt)
    {
        if (!string.IsNullOrEmpty(m_parameterName))
        {
            switch (m_parameterType)
            {
                case AnimatorControllerParameterType.Bool:
                    m_target.SetBool(m_parameterName, m_boolValue);
                    break;
                case AnimatorControllerParameterType.Float:
                    m_target.SetFloat(m_parameterName, m_floatValue);
                    break;
                case AnimatorControllerParameterType.Int:
                    m_target.SetInteger(m_parameterName, m_intValue);
                    break;
                default:
                    m_target.SetTrigger(m_parameterName);
                    break;
            }
        }

        return true;
    }

    public override void OnContextExit(ExecutionFlow flow)
    {
        if (m_prevStateNameHashes != null && m_prevStateNameHashes.Length > 0)
        {
            flow.StartCoroutine(RevertWithDelay());
        }
    }

    private IEnumerator RevertWithDelay()
    {
        yield return null;
        switch (m_parameterType)
        {
            case AnimatorControllerParameterType.Bool:
                m_target.SetBool(m_parameterName, m_prevBool);
                break;
            case AnimatorControllerParameterType.Float:
                m_target.SetFloat(m_parameterName, m_prevFloat);
                break;
            case AnimatorControllerParameterType.Int:
                m_target.SetInteger(m_parameterName, m_prevInt);
                break;
            default:
                m_target.ResetTrigger(m_parameterName);
                break;
        }
        for (int i = 0; i < m_prevStateNameHashes.Length; i++)
        {
            m_target.Play(m_prevStateNameHashes[i], i);
        }
    }

    public override string GetDescription()
    {
        if (m_target == null)
        {
            return "No animator selected";
        }
        else if (m_parameterName == null)
        {
            return $"Animator: {m_target.name}";
        }
        else
        {
            return $"Animator: {m_target.name}.{m_parameterName} = {m_stringValue}";
        }
    }
}
