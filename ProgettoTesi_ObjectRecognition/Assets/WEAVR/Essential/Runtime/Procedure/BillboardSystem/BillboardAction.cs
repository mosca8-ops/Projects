using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class ValueProxyBillboard : ValueProxyComponent<Billboard> { }

    public class BillboardAction : BaseReversibleProgressAction, 
                                   ITargetingObject, 
                                   IReplayModeElement, 
                                   IVariablesUser,
                                   ISerializedNetworkProcedureObject
    {
        [Serializable]
        private class OptionalBillboardOrientation : Optional<PivotAxis>
        {
            public static implicit operator OptionalBillboardOrientation(PivotAxis value)
            {
                return new OptionalBillboardOrientation()
                {
                    enabled = true,
                    value = value
                };
            }

            public static implicit operator PivotAxis(OptionalBillboardOrientation optional)
            {
                return optional.value;
            }
        }

        [SerializeField]
        [Tooltip("The target object to show the billboard and/or outline")]
        [Draggable]
        private ValueProxyGameObject m_target;
        [SerializeField]
        [Tooltip("Whether to show the billboard or not")]
        private bool m_showBillboard;
        [SerializeField]
        [Tooltip("The billboard sample to use. If left empty, the default billboard sample will be used")]
        [HiddenBy(nameof(m_showBillboard))]
        [Draggable]
        private ValueProxyBillboard m_sample;
        [SerializeField]
        [Tooltip("The text for the billboard")]
        [ShowIf(nameof(ShowDefaultText))]
        //[LongText]
        private ValueProxyLocalizedString m_text;
        [SerializeField]
        private List<BillboardModifier> m_modifiers;
        [SerializeField]
        [HiddenBy(nameof(m_showBillboard))]
        private AdvancedOptions m_advanced;
        
        [SerializeField]
        [Tooltip("The amount of time to show the billboard/outline after which it will disappear")]
        [AbsoluteValue]
        private OptionalProxyFloat m_timeout;
        [SerializeField]
        [Tooltip("Whether to outline or not the target with specified color")]
        private OptionalProxyColor m_outline;
        [SerializeField]
        [Tooltip("Whether to show the navigation to the target location or not")]
        private ValueProxyBool m_showNavigation;

        [SerializeField]
        [HideInInspector]
        private Quaternion m_targetOrientation = Quaternion.identity;

        #region [  ISerializedNetworkProcedureObject IMPLEMENTATION  ]

        [SerializeField]
        private bool m_isGlobal = false;
        public string IsGlobalFieldName => nameof(m_isGlobal);
        public bool IsGlobal => m_isGlobal;

        #endregion

        private bool m_outlined;
        private bool m_navigationEnabled;

        private NavigationArrow m_navigationArrow;

        public bool ShowBillboard
        {
            get => m_showBillboard;
            set
            {
                if(m_showBillboard != value)
                {
                    BeginChange();
                    m_showBillboard = value;
                    PropertyChanged(nameof(ShowBillboard));
                }
            }
        }

        public Billboard BillboardSample
        {
            get => m_sample.Value;// ? m_sample.Value : BillboardManager.Instance.BillboardDefaultSample;
            set
            {
                if(m_sample != value)
                {
                    BeginChange();
                    m_sample.Value = value;
                    PropertyChanged(nameof(BillboardSample));
                }
            }
        }

        public OptionalVector3 StartPoint => m_advanced.startPoint;
        public OptionalVector3 EndPoint => m_advanced.endPoint;

        public void SetBillboardSample(Billboard sample)
        {
            if (!m_sample.Value)
            {
                m_sample.Value = m_lastSample = sample;
                foreach (var modifier in m_modifiers)
                {
                    modifier.Action = this;
                    modifier.OnModified -= Modifier_OnModified;
                    modifier.OnModified += Modifier_OnModified;
                }
            }
        }

        private Billboard m_lastSample;

        public List<BillboardModifier> Modifiers => m_modifiers;

        [NonSerialized]
        private Billboard m_billboard;
        [NonSerialized]
        private float m_remainingTime;
        [NonSerialized]
        private bool m_shouldShowBillboard;
        [NonSerialized]
        private bool m_alreadyHidden;

        public Object Target {
            get => m_target;
            set
            {
                m_target.Value = value is Component c && c ? c.gameObject :
                value is GameObject go && go ? go : value == null ? null : m_target.Value;
            }
        }

        public string TargetFieldName => "m_target.m_value";

        public LocalizedString Text
        {
            get => m_text.Value;
            set
            {
                if(m_text != value)
                {
                    BeginChange();
                    m_text.Value = value;
                    PropertyChanged(nameof(Text));
                }
            }
        }

        public Color? OutlineColor
        {
            get => m_outline.enabled ? m_outline.value.Value : (Color?)null;
            set
            {
                if(m_outline.enabled != value.HasValue || (value.HasValue && m_outline.value != value.Value))
                {
                    BeginChange();
                    m_outline.enabled = value.HasValue;
                    if (value.HasValue)
                    {
                        m_outline.value = value.Value;
                    }
                    PropertyChanged(nameof(OutlineColor));
                }
            }
        }

        public IEnumerable<string> GetActiveVariablesFields() => new string[] { 
            m_target.IsVariable ? nameof(m_target) : null, 
            m_text.IsVariable ? nameof(m_text) : null,
        }.Where(v => v != null);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_modifiers == null)
            {
                m_modifiers = new List<BillboardModifier>();
            }
            else if (!Application.isPlaying)
            {
                for (int i = 0; i < m_modifiers.Count; i++)
                {
                    if (!m_modifiers[i])
                    {
                        Debug.Log($"[BillboardAction]: Removed modifier because it is null");
                        DestroyImmediate(m_modifiers[i], false);
                        m_modifiers.RemoveAt(i--);
                    }
                }
            }
            if(m_modifiers.Count > 0)
            {
                if (Application.isEditor)
                {
                    foreach (var modifier in m_modifiers)
                    {
                        modifier.Action = this;
                        modifier.OnModified -= Modifier_OnModified; ;
                        modifier.OnModified += Modifier_OnModified; ;
                    }
                }
                else
                {
                    foreach (var modifier in m_modifiers)
                    {
                        modifier.Action = this;
                    }
                }
            }
            m_lastSample = m_sample;
        }

        private bool ShowDefaultText()
        {
            return m_showBillboard && ReferenceEquals(m_sample.Value, null);
        }

        private void Modifier_OnModified(ProcedureObject obj)
        {
            Modified();
        }

        public override void OnValidate()
        {
            base.OnValidate();
            //if (m_target) { m_targetOrientation = m_target.transform.rotation; }
        }

        public override void CollectProcedureObjects(List<ProcedureObject> list)
        {
            base.CollectProcedureObjects(list);
            foreach(var modifier in m_modifiers)
            {
                modifier.CollectProcedureObjects(list);
            }
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            m_outlined = false;
            m_navigationEnabled = false;
            m_alreadyHidden = false;
            if (m_timeout.enabled)
            {
                m_remainingTime = m_timeout;
            }
            if (m_showBillboard)
            {
                Progress = 1;
                m_shouldShowBillboard = true;
                m_billboard = m_target.Value.GetSingleton<BillboardManager>().GetBillboard(m_sample, false);
                m_billboard.ChangedVisibility -= Billboard_ChangedVisibility;
                m_billboard.ChangedVisibility += Billboard_ChangedVisibility;
                foreach (var modifier in m_modifiers)
                {
                    if (modifier.Enabled)
                    {
                        modifier.Prepare(m_billboard);
                        modifier.Progress = 0;
                    }
                }
            }
        }

        public override bool Execute(float dt)
        {
            if (m_showBillboard)
            {
                if (!m_sample.Value)
                {
                    Progress = 1;
                    m_billboard.Text = m_text;
                }
                else
                {
                    Progress = 1;
                    foreach (var modifier in m_modifiers)
                    {
                        if (modifier.Enabled && modifier.Progress < 1)
                        {
                            modifier.Apply(dt);
                            Progress = Mathf.Min(modifier.Progress, Progress);
                        }
                    }
                }
                if (m_shouldShowBillboard)
                {
                    // Show billboard on point with offset
                    m_shouldShowBillboard = false;
                    ShowBillboardOnTarget();
                }
                //if (m_timeout.enabled && m_timeout.value != 0)
                //{
                //    m_remainingTime -= dt;
                //    Progress = 1 - (m_remainingTime / m_timeout.value);
                //}
            }

            // Outline if needed
            if (m_outline.enabled && !m_outlined)
            {
                m_outlined = true;
                Outliner.Outline(m_target, m_outline.value);
            }

            // Show navigation if needed
            if(m_showNavigation && !m_navigationEnabled)
            {
                m_navigationEnabled = true;
                m_navigationArrow = NavigationArrow.Current;
                if (m_navigationArrow)
                {
                    m_navigationArrow.Target = m_target;
                }
            }

            if(m_timeout.enabled && m_timeout.value != 0)
            {
                m_remainingTime -= dt;
                Progress = 1 - (m_remainingTime / m_timeout.value);
                if (m_remainingTime <= 0)
                {
                    HideAll();
                    return true;
                }
            }
            return (!m_showBillboard && !m_timeout.enabled) || Progress > 0.99999f;
        }

        public override void FastForward()
        {
            if (m_showBillboard)
            {
                if (!m_billboard)
                {
                    // Show billboard on point with offset
                    m_billboard = m_target.Value.GetSingleton<BillboardManager>().GetBillboard(m_sample);
                    m_billboard.ChangedVisibility -= Billboard_ChangedVisibility;
                    m_billboard.ChangedVisibility += Billboard_ChangedVisibility;
                }
                ShowBillboardOnTarget();
                if (!m_sample.Value)
                {
                    Progress = 1;
                    m_billboard.Text = m_text;
                }
                else
                {
                    foreach (var modifier in m_modifiers)
                    {
                        if (modifier.Enabled)
                        {
                            modifier.FastForward();
                        }
                    }
                }
            }
            if (m_outline.enabled && !m_outlined)
            {
                m_outlined = true;
                Outliner.Outline(m_target, m_outline.value);
            }
            if (m_showNavigation && !m_navigationEnabled)
            {
                m_navigationEnabled = true;
                m_navigationArrow = NavigationArrow.Current;
                if (m_navigationArrow)
                {
                    m_navigationArrow.Target = m_target;
                }
            }
        }

        private void ShowBillboardOnTarget()
        {
            ApplyValuesTo(m_billboard);
            m_billboard.ShowOn(m_target, m_targetOrientation);
        }

        private void ApplyValuesTo(Billboard billboard)
        {
            var useStartPoint = true;
            var useEndPoint = true;
            if (m_advanced.usePopupPoints.enabled)
            {
                var popupPoint = m_target.Value.GetComponent<TXT.WEAVR.UI.PopupPoint>();
                if (popupPoint)
                {
                    if (popupPoint.origin) { billboard.StartPoint = Vector3.Scale(popupPoint.origin.localPosition, popupPoint.transform.lossyScale); useStartPoint = false; }
                    if (popupPoint.point) { billboard.EndPoint = Vector3.Scale(popupPoint.point.localPosition, popupPoint.transform.lossyScale); useEndPoint = false; }
                }
            }
            if (m_advanced.startPoint.enabled && useStartPoint) { billboard.StartPoint = m_advanced.startPoint.value; }
            if (m_advanced.endPoint.enabled && useEndPoint) { billboard.EndPoint = m_advanced.endPoint.value; }
            if (m_advanced.avoidOcclusion.enabled) { billboard.IgnoreRenderDepth = m_advanced.avoidOcclusion.value; }
            if (m_advanced.showLine.enabled) { billboard.ShowConnectingLine = m_advanced.showLine.value; }
            if (m_advanced.worldSize.enabled) { billboard.WorldSize = m_advanced.worldSize.value; }
            if (m_advanced.visibleDistance.enabled) { billboard.VisibleDistance = m_advanced.visibleDistance.value; }
            if (m_advanced.dynamicSize.enabled) { billboard.DynamicSize = m_advanced.dynamicSize.value; }
            if (m_advanced.lookAtCamera.enabled) { billboard.LookAtCamera = m_advanced.lookAtCamera.value; }
            if (m_advanced.keepOrientation.enabled) { billboard.KeepSameOrientation = m_advanced.keepOrientation.value; }
            if (m_advanced.rotationAxis.enabled) { billboard.RotationAxis = m_advanced.rotationAxis.value; }
        }

        private void Billboard_ChangedVisibility(Billboard billboard, bool visible)
        {
            if(billboard == m_billboard && !visible)
            {
                m_billboard = null;
                billboard.ChangedVisibility -= Billboard_ChangedVisibility;
                foreach (var modifier in m_modifiers)
                {
                    if (modifier.Enabled)
                    {
                        modifier.OnRevert();
                    }
                }
            }
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            HideAll();
        }

        private void HideAll()
        {
            if (m_alreadyHidden) { return; }
            if (m_showBillboard)
            {
                if (m_billboard)
                {
                    m_billboard.Hide();
                    try
                    {
                        BillboardManager.Instance.HideBillboardOn(m_target.Value, BillboardSample);
                    }
                    catch { }
                }
            }
            if (m_outlined)
            {
                m_outlined = false;
                Outliner.RemoveOutline(m_target, m_outline.value);
            }
            if (m_navigationEnabled)
            {
                m_navigationEnabled = false;
                if (m_navigationArrow && m_navigationArrow.Target == m_target)
                {
                    m_navigationArrow.Target = null;
                }
                m_navigationArrow = null;
            }
            m_alreadyHidden = true;
        }

        public override void OnStop()
        {
            base.OnStop();
            HideAll();
        }

        public void ApplyPreview(Billboard previewBillboard, bool isDefaultSample)
        {
            if (!m_showBillboard || !previewBillboard) { return; }

            if (m_target.Value)
            {
                var deltaRotation = m_target.Value.transform.rotation * Quaternion.Inverse(m_targetOrientation);
                if (m_advanced.startPoint.enabled) { m_advanced.startPoint.value = deltaRotation * m_advanced.startPoint.value; }
                if (m_advanced.endPoint.enabled) { m_advanced.endPoint.value = deltaRotation * m_advanced.endPoint.value; }
                m_targetOrientation = m_target.Value.transform.rotation;
            }

            ApplyValuesTo(previewBillboard);
            if (isDefaultSample)
            {
                previewBillboard.Text = m_text;
            }
            else
            {
                foreach(var modifier in m_modifiers)
                {
                    if(modifier.Enabled && modifier.CanPreview())
                    {
                        modifier.ApplyPreview(previewBillboard.gameObject);
                    }
                }
            }
        }

        public bool CanPreview()
        {
            return m_sample.Value && m_modifiers.Count > 0;
        }

        public override string GetDescription()
        {
            var validModifiers = m_modifiers.Where(m => m && m.Enabled).ToArray();
            string target = m_target.IsVariable ? $"[{m_target.VariableName}]" : m_target.Value ? m_target.Value.name : "[ ? ]";
            string andOutline = m_outline.enabled ? " and outline " : "";
            return !m_showBillboard && m_outline.enabled ? $"{target}: outline with {m_outline.value}" 
                : validModifiers.Length == 0 ? $"{target}: show billboard" + andOutline
                : validModifiers.Length == 1? $"{target}:{validModifiers[0]?.Description}" + andOutline
                : validModifiers.Length == 2? $"{target}:{validModifiers[0]?.Description} and {validModifiers[1]?.Description}" + andOutline
                : $"{target}: show complex billboard [{validModifiers.Length}]" + andOutline;
        }

        [Serializable]
        private struct AdvancedOptions
        {
            [Tooltip("Whether to use the popup points which stores the start and end points or not")]
            public OptionalBool usePopupPoints;
            [Tooltip("The point as the billboard target")]
            [HiddenBy(nameof(usePopupPoints), hiddenWhenTrue: true)]
            public OptionalVector3 startPoint;
            [Tooltip("Where the billboard will be shown")]
            [HiddenBy(nameof(usePopupPoints), hiddenWhenTrue: true)]
            public OptionalVector3 endPoint;
            [Tooltip("Whether to show the billboard over other objects (even if occluded) or not")]
            public OptionalBool avoidOcclusion;
            [Tooltip("Whether to show or not the connecting line of the billboard")]
            public OptionalBool showLine;
            [Tooltip("The min max size of the billboard in the world")]
            public OptionalSpan worldSize;
            [Tooltip("The min max distance at which the billboard is visible. Out of these limits the billboard will fade away")]
            public OptionalSpan visibleDistance;
            [Tooltip("Whether to change the billboard size dinamically (based on distance) or not")]
            public OptionalBool dynamicSize;
            [Tooltip("Whether to always look at camera or not")]
            public OptionalBool lookAtCamera;
            [Tooltip("Whether to keep the billboard in the same position relative to its target")]
            [FormerlySerializedAs("keepAlwaysUp")]
            public OptionalBool keepOrientation;
            [Tooltip("How to rotate the billboard with respect to the camera")]
            public OptionalBillboardOrientation rotationAxis;
        }
    }
}
