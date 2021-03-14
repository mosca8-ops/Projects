using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class VisualInspectCondition : BaseCondition, ITargetingObject, IProgressElement, IEvaluationEndedCallback
    {
        [SerializeField]
        [Draggable]
        private GameObject m_target;
        [SerializeField]
        [Draggable]
        private VisualInspector m_inspector;
        [SerializeField]
        [Draggable]
        private AbstractInspectionLogic m_inspectionLogic;
        [SerializeField]
        private bool m_useCustomBounds;

        [SerializeField]
        [ShowIf(nameof(ShowFocusLogic))]
        private OptionalBool m_accumulative;
        [SerializeField]
        [ShowIf(nameof(ShowFocusLogic))]
        private OptionalFloat m_time;
        [SerializeField]
        [ShowIf(nameof(ShowFocusLogic))]
        private OptionalFloat m_distance;

        [SerializeField]
        private OptionalExecutionModesContainer m_showMarker;
        [SerializeField]
        [ShowIf(nameof(ShowMarkerValues))]
        [Draggable]
        private AbstractInspectionMarker m_marker;
        [SerializeField]
        [ShowIf(nameof(ShowMarkerValues))]
        private OptionalBool m_lookAtInspector;
        [SerializeField]
        [ShowIf(nameof(ShowMarkerValues))]
        private OptionalColor m_markerColor;
        [SerializeField]
        [ShowIf(nameof(ShowMarkerValues))]
        private OptionalLocalizedString m_markerText;

        [SerializeField]
        [HideInInspector]
        private Bounds m_bounds;
        [SerializeField]
        [HideInInspector]
        private Vector3 m_markerPosition;
        [SerializeField]
        [HideInInspector]
        private Quaternion m_markerRotation;
        [SerializeField]
        [HideInInspector]
        private Vector3 m_markerScale;
        [SerializeField]
        [ShowIf(nameof(MarkerCanBeSeenInHierarchy))]
        private bool m_markerInHierarchy = false;

        private bool m_inspected;
        private IVisualInspectionLogic m_currentInspection;
        private IVisualInspectionMarker m_visualInspectionMarker;

        public Object Target {
            get => m_target;
            set => m_target = value is GameObject go ? go : value is Component c ? c.gameObject : value == null ? null : m_target;
        }

        public string TargetFieldName => nameof(m_target);

        public float Progress { get; private set; }

        public bool UseCustomBounds => m_useCustomBounds;
        public Bounds CustomBounds => m_bounds;

        public OptionalBool MarkerLookAtInspector => m_lookAtInspector;
        public OptionalColor MarkerColor => m_markerColor;
        public OptionalLocalizedString MarkerText => m_markerText;
        public IVisualMarker Marker => m_marker;
        public Pose MarkerPose => new Pose(Pose.PoseType.Local)
        {
            localPosition = m_markerPosition,
            localRotation = m_markerRotation,
            localScale = m_markerScale
        };

        public bool MarkerIsVisibleInHierarchy => m_markerInHierarchy;
        private bool MarkerCanBeSeenInHierarchy() => m_markerInHierarchy;

        public void ResetProgress()
        {
            Progress = 0;
        }

        protected override void OnAssignedToProcedure(Procedure value)
        {
            base.OnAssignedToProcedure(value);
            if (m_showMarker == null)
            {
                m_showMarker = new OptionalExecutionModesContainer();
                m_showMarker.value = new ExecutionModesContainer();
                m_showMarker.enabled = false;
            }

            m_showMarker.value.Procedure = null;
            m_showMarker.value.Procedure = Procedure;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_showMarker == null)
            {
                m_showMarker = new OptionalExecutionModesContainer();
                m_showMarker.value = new ExecutionModesContainer();
                m_showMarker.enabled = false;
            }
            if (Application.isPlaying)
            {
                ResetProgress();
            }
            m_showMarker.value.Procedure = Procedure;
        }

        private bool ShowMarkerValues()
        {
            return m_showMarker.enabled;
        }

        private bool ShowFocusLogic() => m_inspectionLogic == null || m_inspectionLogic is IFocusInspectionLogic;

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            ResetProgress();

            m_inspected = false;
            m_visualInspectionMarker = null;
            IVisualMarker marker = null;

            m_currentInspection = VisualInspectionPool.Current?.GetDefaultInspection(m_inspectionLogic) ?? m_inspectionLogic;
            RemoveListeners();
            AddListeners();

            if (m_showMarker.enabled && m_showMarker.value.HasMode(mode))
            {
                marker = VisualInspectionPool.Current?.GetMarker(m_marker);
                if(marker != null)
                {
                    if(m_markerColor.enabled) { marker.Color = m_markerColor; }
                    if(m_lookAtInspector.enabled) { marker.LookAtInspector = m_lookAtInspector.value; }
                    if(m_markerText.enabled) { marker.Text = m_markerText.value; }
                }
            }

            m_currentInspection.ResetValues();

            if (m_currentInspection is IFocusInspectionLogic focusLogic)
            {
                if (m_accumulative.enabled) { focusLogic.IsAccumulativeInspection = m_accumulative.value; }
                if (m_time.enabled) { focusLogic.InspectionTime = m_time.value; }
                if (m_distance.enabled) { focusLogic.InspectionDistance = m_distance.value; }
            }

            var inspector = m_inspector ? m_inspector : VisualInspector.Main;

            var markerPose = new Pose(Pose.PoseType.Local)
            {
                localPosition = m_markerPosition,
                localRotation = m_markerRotation,
                localScale = m_markerScale
            };

            marker?.SetTarget(m_target, markerPose);
            m_visualInspectionMarker = marker as IVisualInspectionMarker;
            m_visualInspectionMarker?.StartInspection(m_currentInspection, inspector);

            m_currentInspection.InspectTarget(inspector, m_target, markerPose, m_useCustomBounds ? m_bounds : (Bounds?)null);
        }

        private void RemoveListeners()
        {
            if (m_currentInspection is IVisualInspectionEvents tEvents)
            {
                tEvents.OnInspectionDone -= InspectionDone;
                if (Application.isEditor)
                {
                    tEvents.OnOngoingInspectionNormalized -= UpdateInspectionProgress;
                }
            }
        }

        private void AddListeners()
        {
            if (m_currentInspection is IVisualInspectionEvents tEvents)
            {
                tEvents.OnInspectionDone += InspectionDone;
                if (Application.isEditor)
                {
                    tEvents.OnOngoingInspectionNormalized += UpdateInspectionProgress;
                }
            }
        }

        protected override bool EvaluateCondition()
        {
            return m_inspected;
        }

        public void NodesEvaluationEnded()
        {
            RemoveListeners();
            m_visualInspectionMarker?.EndInspection();
            if (m_currentInspection != null)
            {
                m_currentInspection.ResetValues();
                VisualInspectionPool.Current?.Reclaim(m_currentInspection);
            }
            m_currentInspection = null;
        }

        private void InspectionDone() => m_inspected = true;

        private void UpdateInspectionProgress(float progress) => Progress = progress;

        public override string GetDescription()
        {
            return (m_target ? m_target.name : "") + " is visually inspected";
        }
    }
}
