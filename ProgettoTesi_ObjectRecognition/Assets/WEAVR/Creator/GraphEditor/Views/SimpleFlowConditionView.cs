using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace TXT.WEAVR.Procedure
{

    class SimpleFlowConditionView : SimpleView, ISettableControlledElement<FlowConditionController>
    {
        private VisualElement m_flowOutput;
        private VisualElement m_contents;
        private ConditionPort m_conditionPort;
        private Image m_icon;
        private VisualElement m_coloredBox;
        private Image m_stateIcon;
        private Label m_description;
        private VisualElement m_negated;
        private VisualElement m_progressBar;

        protected FlowConditionController m_controller;

        private Color? m_descriptionDefaultColor;
        private TimeStateHandler m_timeValue;
        private IVisualElementScheduledItem m_lastAnimation;
        private IVisualElementScheduledItem m_updateState;

        public new FlowConditionController Controller
        {
            get => m_controller;
            set
            {
                if (m_controller != value)
                {
                    if(m_controller != null)
                    {
                        RemovePort();
                    }
                    m_controller = value;
                    base.Controller = value;
                }
            }
        }

        Color? m_refreshColor;
        public Color RefreshColor
        {
            get
            {
                return m_refreshColor ?? Color.white;
            }
            set
            {
                m_refreshColor = value;
            }
        }

        Color? m_descriptionColor;
        public Color DescriptionLabelColor
        {
            get
            {
                return m_descriptionColor ?? m_description?.style?.color.value ?? Color.white;
            }
            set
            {
                m_descriptionColor = value;
            }
        }

        int? m_progressBarUpdateRate;
        public int ProgressBarUpdateRate
        {
            get
            {
                return m_progressBarUpdateRate ?? 33;
            }
            set
            {
                m_progressBarUpdateRate = value;
            }
        }

        private void RemovePort()
        {
            var existingPort = m_conditionPort ?? m_flowOutput.Q<ConditionPort>();
            if (existingPort != null)
            {
                GetFirstAncestorOfType<ProcedureView>()?.UnregisterFlowPort(m_conditionPort.Controller);
                existingPort.Controller?.Dispose();
                m_flowOutput.Remove(existingPort);
            }
        }

        public SimpleFlowConditionView() : base("uxml/SimpleFlowCondition")
        {
            AddToClassList("simpleFlowConditionView");

            m_contents = this.Q("contents");
            m_icon = m_contents.Q<Image>("icon");
            m_coloredBox = m_contents.Q("colored-box");
            m_stateIcon = m_contents.Q<Image>("state-icon");
            m_description = m_contents.Q<Label>("description");

            m_flowOutput = this.Q("flow-outputs");
            m_negated = this.Q("negated");

            m_progressBar = this.Q("progress-bar");

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);

            SearchHub.Current.SearchValueChanged -= SearchValueChanged;
            SearchHub.Current.SearchValueChanged += SearchValueChanged;
        }

        private void SearchValueChanged(string newValue)
        {
            if (panel == null)
            {
                SearchHub.Current.SearchValueChanged -= SearchValueChanged;
                return;
            }
            if(Controller == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(newValue))
            {
                RemoveFromClassList("searchPositive");
                RemoveFromClassList("searchNegative");
            }
            else if (SearchRecursive(Controller.GetModel() as BaseCondition) || Controller.Model.Guid.StartsWith(newValue))
            {
                AddToClassList("searchPositive");
                RemoveFromClassList("searchNegative");
            }
            else
            {
                AddToClassList("searchNegative");
                RemoveFromClassList("searchPositive");
            }
        }

        private bool SearchRecursive(BaseCondition condition)
        {
            if (!condition) { return false; }
            if (SearchHub.Current.FastSearch(condition))
            {
                return true;
            }
            if(condition is IConditionsContainer container)
            {
                foreach (var child in container.Children)
                {
                    if (SearchRecursive(child))
                    {
                        return true;
                    }
                }
            }
            if(condition is IConditionParent parent && parent.Child)
            {
                return SearchRecursive(parent.Child);
            }
            return false;
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            SearchHub.Current.SearchValueChanged -= SearchValueChanged;
            SearchHub.Current.SearchValueChanged += SearchValueChanged;
            if (m_controller == null) { return; }
            GetFirstAncestorOfType<ProcedureView>()?.RegisterFlowPort(m_conditionPort.Controller, m_conditionPort);

            if (!string.IsNullOrEmpty(SearchHub.Current.CurrentSearchValue))
            {
                SearchValueChanged(SearchHub.Current.CurrentSearchValue);
            }

            if (m_progressBar != null && m_progressBar.panel != null)
            {
                if (Controller.IsProgressElement && Application.isPlaying)
                {
                    if (m_updateState != null)
                    {
                        m_updateState.Pause();
                    }
                    m_updateState = schedule.Execute(() => UpdateProgress(Controller.Progress)).Every(ProgressBarUpdateRate);
                }
                else if (m_updateState != null)
                {
                    m_updateState.Pause();
                    m_updateState = null;
                    //UpdateProgress(1);
                }
            }
        }

        protected override void OnCustomStyleResolved(ICustomStyle styles)
        {
            base.OnCustomStyleResolved(styles);

            if(styles.TryGetValue(new CustomStyleProperty<Color>("--refresh-color"), out Color v)) { m_refreshColor = v; } else { m_refreshColor = null; }
            if(styles.TryGetValue(new CustomStyleProperty<Color>("--description-color"), out Color v1)) { m_descriptionColor = v1; } else { m_descriptionColor = null; }
            if(styles.TryGetValue(new CustomStyleProperty<int>("--progress-update-ms"), out int v2)) { m_progressBarUpdateRate = v2; } else { m_progressBarUpdateRate = null; }

            if (m_descriptionColor.HasValue)
            {
                m_description.style.color = m_descriptionColor.Value;
            }
            //m_description.style.color = MainColor;
            //m_descriptionDefaultColor = m_description.style.color;
        }


        protected override void OnNewController()
        {
            base.OnNewController();
            if(m_controller != null)
            {
                m_conditionPort = ConditionPort.Create(m_controller.PortController);
                m_flowOutput.Add(m_conditionPort);
                if (panel != null) {
                    GetFirstAncestorOfType<ProcedureView>()?.RegisterFlowPort(m_conditionPort.Controller, m_conditionPort);
                }

                if (!string.IsNullOrEmpty(SearchHub.Current.CurrentSearchValue))
                {
                    SearchValueChanged(SearchHub.Current.CurrentSearchValue);
                }
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
            if(Controller != null)
            {
                m_description.text = Controller.Description + " ";
            }
        }

        protected override void SelfChange(int controllerChange)
        {
            base.SelfChange(controllerChange);
            EnableInClassList("mandatory", Controller.IsMainFlow);

            if (controllerChange == FlowConditionController.Change.IsMainFlow)
            {
                m_lastAnimation?.Pause();
                m_timeValue.Invalidate();
                m_descriptionDefaultColor = null;
                return;
            }

            if(m_negated != null)
            {
                m_negated.visible = Controller.Negated;
            }

            var syncType = Controller.GetSyncType();
            EnableInClassList("is-global", syncType.HasFlag(FlowConditionController.SyncType.Global));
            EnableInClassList("is-local", syncType.HasFlag(FlowConditionController.SyncType.Local));

            m_description.text = Controller.Description + " ";
            EnableInClassList("triggered", Controller.WasTriggered);

            if (m_progressBar != null && m_progressBar.panel != null)
            {
                if (Controller.IsProgressElement && Application.isPlaying)
                {
                    if(m_updateState != null)
                    {
                        m_updateState.Pause();
                    }
                    m_updateState = schedule.Execute(() => UpdateProgress(Controller.Progress)).Every(ProgressBarUpdateRate);
                }
                else if(m_updateState != null)
                {
                    m_updateState.Pause();
                    m_updateState = null;
                    //UpdateProgress(1);
                }
            }

            //if (!m_descriptionDefaultColor.HasValue && m_stylesReady)
            //{
            //    m_descriptionDefaultColor = m_descriptionColor;
            //}
            //if (m_descriptionDefaultColor.HasValue)
            //{
                m_lastAnimation?.Pause();
                m_timeValue.Invalidate();
                m_lastAnimation = schedule.Execute(AnimateChange).Every(20).ForDuration(1200);
            //}
        }

        private void UpdateProgress(float progress)
        {
            if (m_updateState != null)
            {
                m_progressBar.style.width = progress * (m_progressBar.parent.resolvedStyle.width - m_progressBar.resolvedStyle.marginRight);

                if (!Application.isPlaying)
                {
                    m_updateState.Pause();
                    m_updateState = null;
                }
            }
        }

        private void AnimateChange(TimerState time)
        {
            Color from = RefreshColor;
            Color to = DescriptionLabelColor;
            float t = m_timeValue.GetElapsed(time);
            m_description.style.color = Color.Lerp(from, to, t);
        }
    }
}
