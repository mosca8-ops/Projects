using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR;
using TXT.WEAVR.Common;
using TXT.WEAVR.Procedure;
using UnityEngine;

public class SetAnimatorStateAction : BaseReversibleProgressAction, ITargetingObject, ISerializedNetworkProcedureObject
{
    [SerializeField]
    [Tooltip("The target animator")]
    [Draggable]
    private Animator m_target;
    [SerializeField]
    [Tooltip("The layer where the states are")]
    [ArrayElement(nameof(m_layersNames), true)]
    private string m_layer;
    [SerializeField]
    [Tooltip("The state of the animator to be set")]
    [ArrayElement(nameof(m_statesNames), true)]
    private string m_stateName;

    [SerializeField]
    [HideInInspector]
    private string[] m_layersNames;
    [SerializeField]
    [HideInInspector]
    private string[] m_statesNames;

    [SerializeField]
    [HideInInspector]
    private string[][] m_allStatesNames;

    [SerializeField]
    [HideInInspector]
    private int m_layerIndex;

    #region [  ISerializedNetworkProcedureObject IMPLEMENTATION  ]

    [SerializeField]
    private bool m_isGlobal = true;
    public string IsGlobalFieldName => nameof(m_isGlobal);
    public bool IsGlobal => m_isGlobal;

    #endregion

    [NonSerialized]
    private string m_prevLayerName;
    [NonSerialized]
    private int[] m_prevStateNameHashes;
    [NonSerialized]
    private Animator m_prevTarget;

    private Func<Animator, string[][]> m_getAllStatesCallback;
    public Func<Animator, string[][]> GetAllStatesCallback
    {
        get => m_getAllStatesCallback;
        set
        {
            if(m_getAllStatesCallback != value)
            {
                m_getAllStatesCallback = value;
                OnValidate();
                PropertyChanged(nameof(GetAllStatesCallback));
            }
        }
    }

    public string[][] AllStateNames
    {
        get => m_allStatesNames;
        set
        {
            bool change = m_allStatesNames == null || m_allStatesNames.Length != value.Length;
            if (!change)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if(m_allStatesNames[i] == null || m_allStatesNames[i].Length != value[i].Length)
                    {
                        change = true;
                        break;
                    }
                    for (int j = 0; j < value[i].Length; j++)
                    {
                        if(m_allStatesNames[i][j] != value[i][j])
                        {
                            change = true;
                            break;
                        }
                    }
                }
            }
            if(change)
            {
                //BeginChange();
                m_allStatesNames = value;
                //PropertyChanged(nameof(AllStateNames));
            }
        }
    }

    public UnityEngine.Object Target {
        get => m_target;
        set {
            BeginChange();
            m_target = value is Animator anim ? anim : 
                value is GameObject go ? go.GetComponent<Animator>() : 
                value is Component c ? c.GetComponent<Animator>() : 
                value == null ? null : m_target;
            PropertyChanged(nameof(m_target));
        }
    }

    public string TargetFieldName => nameof(m_target);

    public override void OnValidate()
    {
        base.OnValidate();
        if(m_prevLayerName == null)
        {
            m_prevLayerName = m_layer;
        }
        
        if (m_target != m_prevTarget && m_target)
        {

            List<string> layersNames = new List<string>();
            for (int i = 0; i < m_target.layerCount; i++)
            {
                layersNames.Add(m_target.GetLayerName(i));
            }

            m_layersNames = layersNames.ToArray();

            int id = layersNames.IndexOf(m_layer);
            if (id < 0)
            {
                m_layer = m_layersNames.Length > 0 ? m_layersNames[0] : string.Empty;
            }
            
            if(GetAllStatesCallback != null)
            {
                m_allStatesNames = GetAllStatesCallback(m_target);
            }

            if (string.IsNullOrEmpty(m_layer))
            {
                m_statesNames = new string[0];
                m_stateName = string.Empty;
            }
            else if(id >= 0 && m_allStatesNames != null){
                m_statesNames = m_allStatesNames[id];

                if (!m_statesNames.Contains(m_stateName))
                {
                    m_stateName = m_statesNames[0];
                }
            }
        }
        else if(!m_target)
        {
            m_layersNames = new string[0];
            m_statesNames = new string[0];

            m_allStatesNames = null;

            m_layerIndex = -1;
            m_layer = string.Empty;
            m_stateName = string.Empty;
        }

        if (m_target)
        {
            if (GetAllStatesCallback != null && (m_allStatesNames == null || m_allStatesNames.Length == 0))
            {
                m_allStatesNames = GetAllStatesCallback(m_target);
            }

            if (m_prevLayerName != m_layer || (m_stateName == null && m_allStatesNames != null && m_allStatesNames.Length > 0))
            {
                int id = m_layersNames.ToList().IndexOf(m_layer);
                if (id >= 0 && m_allStatesNames != null) 
                {
                    m_statesNames = m_allStatesNames[id];
                    m_stateName = m_statesNames[0];
                }
            }
        }

        m_prevTarget = m_target;
        m_prevLayerName = m_layer;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        m_prevTarget = m_target;
    }

    public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
    {
        base.OnStart(flow, executionMode);
        m_layerIndex = m_target.GetLayerIndex(m_layer);
        if (RevertOnExit)
        {
            m_prevStateNameHashes = new int[m_target.layerCount];
            for (int i = 0; i < m_target.layerCount; i++)
            {
                m_prevStateNameHashes[i] = m_target.GetCurrentAnimatorStateInfo(i).shortNameHash;
            }
        }
    }

    public override bool Execute(float dt)
    {
        m_target.Play(m_stateName, m_layerIndex);

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
        for (int i = 0; i < m_prevStateNameHashes.Length; i++)
        {
            m_target.Play(m_prevStateNameHashes[i], i);
        }
    }

    public override void OnStop()
    {
        base.OnStop();
    }

    public override string GetDescription()
    {
        if (m_target == null)
        {
            return "No animator selected";
        }
        else if (string.IsNullOrEmpty(m_stateName))
        {
            return $"Animator: {m_target.name}";
        }
        else
        {
            return $"Animator: {m_target.name}.state = {m_stateName}";
        }
    }
}
