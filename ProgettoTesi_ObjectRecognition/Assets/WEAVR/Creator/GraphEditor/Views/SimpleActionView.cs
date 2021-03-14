using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace TXT.WEAVR.Procedure
{

    class SimpleActionView : SimpleView, ISettableControlledElement<BaseActionController>, IBadgeClient
    {
        protected static readonly ExecutionState[] s_states =
        {
            //ExecutionState.NotStarted,
            ExecutionState.Ready,
            ExecutionState.Started,
            ExecutionState.Running,
            ExecutionState.Finished,
            ExecutionState.Faulted,
            ExecutionState.Paused,
            ExecutionState.ForceStopped,
            ExecutionState.Skipped,
            ExecutionState.Breakpoint,
        };


        protected BaseActionController m_controller;

        public new BaseActionController Controller {
            get => m_controller;
            set
            {
                if(m_controller != value)
                {
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
        public Color DescriptionColor
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

        private VisualElement m_contents;
        private VisualElement m_progressBar;
        private Image m_icon;
        private VisualElement m_coloredBox;
        private Image m_stateIcon;
        private Label m_description;
        private Badge m_badge;

        private Color? m_descriptionDefaultColor;
        private TimeStateHandler m_timeValue;
        private bool m_initialized;

        private ExecutionState m_lastState;
        private IVisualElementScheduledItem m_lastAnimation;
        private IVisualElementScheduledItem m_updateState;

        public SimpleActionView() : base("uxml/SimpleAction")
        {
            AddToClassList("simpleActionView");

            m_contents = this.Q("contents");
            m_icon = m_contents.Q<Image>("icon");
            m_coloredBox = m_contents.Q("colored-box");
            m_stateIcon = m_contents.Q<Image>("state-icon");
            m_description = m_contents.Q<Label>("description");
            m_progressBar = this.Q("progress-bar");

            m_badge = new Badge(Badge.BadgeType.error);

            RegisterCallback<AttachToPanelEvent>(AttachedToPanel);

            SearchHub.Current.SearchValueChanged -= SearchValueChanged;
            SearchHub.Current.SearchValueChanged += SearchValueChanged;
        }

        private void SearchValueChanged(string newValue)
        {
            if(panel == null)
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
            else if (SearchHub.Current.FastSearch(Controller.GetModel()) || Controller.Model.Guid.StartsWith(newValue) || (Controller.Description != null && Controller.Description.ToLower().Contains(newValue.ToLower())))
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

        private void AttachedToPanel(AttachToPanelEvent evt)
        {
            SearchHub.Current.SearchValueChanged -= SearchValueChanged;
            SearchHub.Current.SearchValueChanged += SearchValueChanged;

            if (!string.IsNullOrEmpty(SearchHub.Current.CurrentSearchValue))
            {
                SearchValueChanged(SearchHub.Current.CurrentSearchValue);
            }

            schedule.Execute(() =>
            {
                if (Controller != null)
                {
                    UpdateBadge();
                    UpdateStateChanges();
                    if (Controller.IsProgressElement)
                    {
                        if (Controller.CurrentState == ExecutionState.Running)
                        {
                            m_updateState = schedule.Execute(() => UpdateProgress(Controller.Progress)).Every(ProgressBarUpdateRate);
                        }
                        else
                        {
                            UpdateProgress(Controller.Progress);
                        }
                    }
                    else if(Controller.CurrentState == ExecutionState.Finished)
                    {
                        UpdateProgress(1);
                    }
                }
            }).StartingIn(100);
        }

        protected override void OnCustomStyleResolved(ICustomStyle styles)
        {
            base.OnCustomStyleResolved(styles);

            if(styles.TryGetValue(new CustomStyleProperty<Color>("--refresh-color"), out Color v)) { m_refreshColor = v; } else { m_refreshColor = null; }
            if(styles.TryGetValue(new CustomStyleProperty<Color>("--description-color"), out Color v1)) { m_descriptionColor = v1; } else { m_descriptionColor = null; }
            if(styles.TryGetValue(new CustomStyleProperty<int>("--progress-update-ms"), out int v2)) { m_progressBarUpdateRate = v2; } else { m_progressBarUpdateRate = null; }
            //m_descriptionDefaultColor = m_description.style.color;

            if (m_descriptionColor.HasValue)
            {
                m_description.style.color = DescriptionColor;
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
            if (Controller != null)
            {
                Controller.RefreshInfo();
                m_coloredBox.style.backgroundColor = Controller.Color;
                m_icon.image = Controller.Icon;
                m_description.text = m_controller.Description + " ";
                if (panel != null)
                {
                    UpdateBadge();
                }
            }
        }

        protected override void SelfChange(int controllerChange)
        {
            base.SelfChange(controllerChange);
            if (m_controller == null) { return; }

            if (!m_initialized)
            {
                m_initialized = true;
                m_coloredBox.style.backgroundColor = Controller.Color;
                m_icon.image = Controller.Icon;
                m_description.text = m_controller.Description + " ";
                schedule.Execute(() => SelfChange(controllerChange)).StartingIn(50);
                return;
            }

            if (controllerChange == BaseActionController.Change.StateChanged)
            {
                UpdateStateChanges();
                return;
            }

            if (panel != null)
            {
                UpdateBadge();
            }

            UpdateStateChanges();
            UpdateProgress(Controller.IsProgressElement ? Controller.Progress : 0);

            m_coloredBox.style.backgroundColor = Controller.Color;
            m_icon.image = Controller.Icon;
            m_description.text = m_controller.Description + " ";

            //if (!m_descriptionDefaultColor.HasValue && m_description.resolvedStyle.color != Color.clear)
            //{
            //    m_descriptionDefaultColor = m_description.resolvedStyle.color;
            //}
            //if (m_descriptionDefaultColor.HasValue)
            //{
                m_lastAnimation?.Pause();
                m_timeValue.Invalidate();
                m_lastAnimation = schedule.Execute(AnimateChange).Every(20).ForDuration(1200);
            //}
        }

        public void ClearBadge()
        {
            if(m_badge != null)
            {
                m_badge.Detach();
                m_badge.RemoveFromHierarchy();
            }
        }

        private void UpdateBadge()
        {
            if (Controller.HasErrors)
            {
                if (m_badge.panel == null)
                {
                    //var parentToAttach = GetFirstAncestorOfType<Node>() ?? GetFirstAncestorOfType<GraphElement>() ?? parent;
                    Add(m_badge);
                    //m_badge.AttachTo(this, SpriteAlignment.RightCenter);
                }
                m_badge.Type = Badge.BadgeType.error;
                m_badge.badgeText = Controller.ErrorMessage;

                RemoveFromClassList("with-warning");
                if (!Application.isPlaying)
                {
                    AddToClassList("with-error");
                }
            }
            else if (Controller.HasWarning)
            {
                if (m_badge.panel == null)
                {
                    //var parentToAttach = GetFirstAncestorOfType<Node>() ?? GetFirstAncestorOfType<GraphElement>() ?? parent;
                    //m_badge.TryAttachTo(this, parentToAttach, SpriteAlignment.RightCenter);
                    //m_badge.AttachTo(this, SpriteAlignment.RightCenter);
                    Add(m_badge);
                }
                m_badge.Type = Badge.BadgeType.warning;
                m_badge.badgeText = Controller.WarningMessage;

                RemoveFromClassList("with-error");
                AddToClassList("with-warning");
            }
            else if (m_badge.panel != null)
            {
                m_badge.Detach();
                m_badge.RemoveFromHierarchy();

                RemoveFromClassList("with-warning");
                RemoveFromClassList("with-error");
            }
        }

        private void UpdateStateChanges()
        {
            if(m_lastState == Controller.CurrentState) { return; }

            //RemoveFromClassList(m_lastState.ToString());

            m_lastState = Controller.CurrentState;

            if (Controller.CurrentState != ExecutionState.NotStarted)
            {
                foreach(ExecutionState state in s_states)
                {
                    if (Controller.CurrentState.HasFlag(state))
                    {
                        AddToClassList(state.ToString());
                    }
                    else
                    {
                        RemoveFromClassList(state.ToString());
                    }
                }
                //AddToClassList(Controller.CurrentState.ToString());
            }
            else
            {
                foreach (ExecutionState state in s_states)
                {
                    RemoveFromClassList(state.ToString());
                }
            }

            if (m_progressBar != null && m_progressBar.panel != null)
            {
                if (Controller.CurrentState == ExecutionState.Running && Controller.IsProgressElement)
                {
                    m_updateState = schedule.Execute(() => UpdateProgress(Controller.Progress)).Every(ProgressBarUpdateRate);
                }
                else if (Controller.CurrentState == ExecutionState.Finished)
                {
                    m_updateState?.Pause();
                    m_updateState = null;
                    UpdateProgress(1);
                }
                else if(Controller.CurrentState == ExecutionState.NotStarted)
                {
                    m_updateState?.Pause();
                    m_updateState = null;
                    UpdateProgress(0);
                }
            }
            else if (m_updateState != null)
            {
                m_updateState.Pause();
                m_updateState = null;
            }
        }

        protected override void OnNewController()
        {
            base.OnNewController();
            if(Controller != null)
            {
                foreach (var state in Enum.GetNames(typeof(ExecutionState)))
                {
                    RemoveFromClassList(state);
                }
                m_lastState = Controller.CurrentState;
                if(m_progressBar != null)
                {
                    UpdateProgress(Controller.IsProgressElement ? Controller.Progress : 0);
                }

                if (!string.IsNullOrEmpty(SearchHub.Current.CurrentSearchValue))
                {
                    SearchValueChanged(SearchHub.Current.CurrentSearchValue);
                }
            }
        }

        private void AnimateChange(TimerState time)
        {
            Color from = RefreshColor;
            //Color to = m_descriptionDefaultColor.Value;
            Color to = DescriptionColor;
            float t = m_timeValue.GetElapsed(time);
            m_description.style.color = Color.Lerp(from, to, t);
        }

        private void UpdateProgress(float progress)
        {
            m_progressBar.style.width = progress * (m_progressBar.parent.resolvedStyle.width - m_progressBar.resolvedStyle.marginRight);

            if (!Application.isPlaying)
            {
                m_updateState?.Pause();
                m_updateState = null;
            }
        }
    }
}
