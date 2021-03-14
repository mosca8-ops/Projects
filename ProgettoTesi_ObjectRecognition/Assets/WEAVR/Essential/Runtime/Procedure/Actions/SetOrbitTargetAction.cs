using System;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public class SetOrbitTargetAction : BaseReversibleProgressAction, ITargetingObject, ISerializedNetworkProcedureObject
    {
        private enum FocusType { Pivot, Bounds }

        [SerializeField]
        [Tooltip("The target object for the orbiting camera")]
        [Draggable]
        private ValueProxyGameObject m_target;
        [SerializeField]
        [Tooltip("Whether to center the camera on the object or not")]
        [DisabledBy("m_target.m_value")]
        private bool m_centerOnTarget;
        [SerializeField]
        [Tooltip("The distance of the camera from the target")]
        [DisabledBy("m_target.m_value")]
        private OptionalFloat m_cameraDistance;
        [SerializeField]
        [Tooltip("A hint for the camera to focus on")]
        [DisabledBy("m_target.m_value")]
        [ShowIf(nameof(ShowFocusType))]
        private FocusType m_focusType = FocusType.Bounds;
        [SerializeField]
        [Tooltip("Whether to set this target as default one or not")]
        [DisabledBy("m_target.m_value")]
        private bool m_isDefaultTarget;

        [NonSerialized]
        private GameObject m_previousTarget;
        [NonSerialized]
        private float? m_previousDistance;
        [NonSerialized]
        private bool m_prevPreferBounds;

        #region [  ISerializedNetworkProcedureObject IMPLEMENTATION  ]

        [SerializeField]
        private bool m_isGlobal = true;
        public string IsGlobalFieldName => nameof(m_isGlobal);
        public bool IsGlobal => m_isGlobal;

        #endregion

        public UnityEngine.Object Target
        {
            get => m_target;
            set => m_target.Value = value is GameObject go ? go : value is Component c ? c.gameObject : value == null ? null : m_target.Value;
        }

        public string TargetFieldName => nameof(m_target);

        private bool ShowFocusType() => m_target.IsVariable || !m_target.Value || m_target.Value.GetComponentInChildren<Renderer>();

        private bool ShowIfSampleDistance() => !m_cameraDistance.enabled;

        public override bool Execute(float dt)
        {
            var target = m_target.Value;
            m_prevPreferBounds = CameraOrbit.Instance.PreferBounds;
            m_previousTarget = CameraOrbit.Instance.Target;
            if (target)
            {
                if (m_isDefaultTarget)
                {
                    CameraOrbit.DefaultTarget = m_target;
                }

                CameraOrbit.Instance.PreferBounds = m_focusType == FocusType.Bounds;
                CameraOrbit.Instance.Target = m_target;

                if (!m_centerOnTarget)
                {
                    CameraOrbit.Instance.UseFocalPointAsTarget();
                }

                m_previousDistance = null;
                if (m_cameraDistance.enabled)
                {
                    m_previousDistance = CameraOrbit.Instance.DistanceToCamera;
                    CameraOrbit.Instance.DistanceToCamera = m_cameraDistance.value;
                }
            }
            else
            {
                CameraOrbit.Instance.Target = null;
            }

            return true;
        }

        public override void FastForward()
        {
            base.FastForward();
            Execute(0);
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (RevertOnExit)
            {
                if (m_previousDistance.HasValue)
                {
                    CameraOrbit.Instance.DistanceToCamera = m_previousDistance.Value;
                }
                CameraOrbit.Instance.PreferBounds = m_prevPreferBounds;
                CameraOrbit.Instance.Target = m_previousTarget;
            }
        }

        public override string GetDescription()
        {
            return m_target.Value || m_target.IsVariable ? $"Set orbit target to {m_target}" : "Remove orbit target";
        }
    }
}