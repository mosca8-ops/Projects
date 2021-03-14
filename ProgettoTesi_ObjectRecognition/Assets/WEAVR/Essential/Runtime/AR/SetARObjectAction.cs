using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Procedure;
using TXT.WEAVR.Common;

namespace TXT.WEAVR.AR
{

    public class SetARObjectAction : BaseReversibleAction, ITargetingObject
    {
        [SerializeField]
        private ValueProxyGameObject m_target;
        [SerializeField]
        private bool m_useLineToSurface = true;
        [SerializeField]
        [HiddenBy(nameof(m_useLineToSurface))]
        private OptionalGradient m_lineGradient;
        [SerializeField]
        private bool m_use3DAxis = true;

        public Object Target { 
            get => m_target;
            set => m_target.Value = value is GameObject go ? go : value is Component c ? c.gameObject : value == null ? null : m_target.Value;
        }

        private GameObject m_prevTarget;
        private ARObject.AROptions m_prevOptions;

        public string TargetFieldName => nameof(m_target);

        public override bool Execute(float dt)
        {
            if(Procedure && !Procedure.Capabilities.usesAR)
            {
                throw new System.Exception($"The current procedure is using AR functionality but it is not enabled for AR.\nSelect the procedure and enable it for AR");
            }

            m_prevTarget = ARObject.Global.Target;
            m_prevOptions = ARObject.Global.Options;

            ARObject.Global.SetTarget(m_target, new ARObject.AROptions()
            {
                useLineToSurface = m_useLineToSurface,
                lineGradient = m_lineGradient.enabled ? m_lineGradient.value : null,
                use3DAxes = m_use3DAxis,
            });

            return true;
        }

        public override void FastForward()
        {
            base.FastForward();
            Execute(0);
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            ARObject.Global.SetTarget(m_prevTarget, m_prevOptions);
        }

        public override string GetDescription()
        {
            return $"Set AR Object: {m_target}";
        }
    }
}
