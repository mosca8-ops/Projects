using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class SetMaterialAction : BaseReversibleProgressAction, ITargetingObject, ISerializedNetworkProcedureObject, IVariablesUser
    {
        [SerializeField]
        [Tooltip("The target renderer to change the material")]
        [Draggable]
        private ValueProxyRenderer m_target;
        [SerializeField]
        [Tooltip("The material to apply to the renderer")]
        [CanBeAnimatedIf(nameof(CheckIfSameMaterialType))]
        [Reversible]
        private AnimatedMaterial m_material;

        [NonSerialized]
        private Material m_prevMaterial;

        #region [  ISerializedNetworkProcedureObject IMPLEMENTATION  ]

        [SerializeField]
        private bool m_isGlobal = true;
        public string IsGlobalFieldName => nameof(m_isGlobal);
        public bool IsGlobal => m_isGlobal;

        #endregion

        public UnityEngine.Object Target {
            get => m_target;
            set => m_target.Value = value is Renderer r ? r : 
                value is GameObject go ? go.GetComponent<Renderer>() : 
                value is Component c ? c.GetComponent<Renderer>() : 
                value == null ? null : m_target.Value;
        }

        public string TargetFieldName => nameof(m_target);

        public IEnumerable<string> GetActiveVariablesFields() => new string[] {
            m_target.IsVariable ? nameof(m_target) : null,
            //m_text.IsVariable ? nameof(m_text) : null,
        }.Where(v => v != null);

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            m_prevMaterial = m_target.Value.material;
            m_material.Start(m_prevMaterial);
        }

        public override bool Execute(float dt)
        {
            m_target.Value.material = m_material.Next(dt);
            Progress = m_material.Progress;
            return m_material.HasFinished;
        }

        public override void FastForward()
        {
            base.FastForward();
            m_target.Value.material = m_material.TargetValue;
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (m_material.IsAnimated)
            {
                m_material.AutoAnimate(m_prevMaterial, m => m_target.Value.material = m);
            }
            else
            {
                m_target.Value.material = m_prevMaterial;
            }
        }

        public bool CheckIfSameMaterialType()
        {
            return m_target.Value && m_target.Value.sharedMaterial && m_material.TargetValue && m_target.Value.sharedMaterial.shader == m_material.TargetValue.shader;
        }

        public override string GetDescription()
        {
            var materialName = m_material.TargetValue ? m_material.TargetValue.name : "none";
            return m_target.IsVariable ? $"[{m_target.VariableName}].material = {materialName} " : 
                   m_target.Value ? $"{m_target.Value.name}.Material = {materialName} " : $"[ ? ].Material = {materialName} ";
        }
    }
}