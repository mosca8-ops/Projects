using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.UIElements;

using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Profiling;
using System.Reflection;
using UnityEditor;

namespace TXT.WEAVR.Procedure
{
    abstract class BaseNodeView<T> : BaseNodeView, ISettableControlledElement<T> where T : BaseNodeController
    {
        public new T Controller
        {
            get => base.Controller as T;
            set
            {
                base.Controller = value;
            }
        }
        public BaseNodeView(string template = null) : base(template) { }

        protected override void SelfChange()
        {
            base.SelfChange();
            if (Controller != null)
            {
                EnableInClassList("unreacheable", Controller.Reacheability == Reachability.NotReacheable);
            }
            UpdateStateChanges();
        }

        protected ContextState m_lastState;

        protected void UpdateStateChanges()
        {
            if (m_lastState == Controller.CurrentState) { return; }

            //RemoveFromClassList(m_lastState.ToString());

            m_lastState = Controller.CurrentState;

            if (Controller.CurrentState != ContextState.Standby)
            {
                foreach (ContextState state in s_states)
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
                foreach (ContextState state in s_states)
                {
                    RemoveFromClassList(state.ToString());
                }
            }

            OnControllerStateChanged();
        }

        protected virtual void OnControllerStateChanged()
        {

        }
    }

    class BaseNodeView : GraphObjectView
    {
        protected static readonly ContextState[] s_states =
        {
            ContextState.Standby,
            ContextState.Ready,
            ContextState.Running,
            ContextState.Finished,
            ContextState.Faulted,
            ContextState.ForceStopped,
        };


        Image m_headerIcon;
        Image m_headerSpace;

        VisualElement m_footer;
        Image m_footerIcon;
        Label m_footerTitle;

        VisualElement m_flowInputConnectorContainer;
        VisualElement m_flowOutputConnectorContainer;

        Label m_label;

        protected NodeInputPort m_inputPort;

        protected Image HeaderIcon => m_headerIcon;
        protected VisualElement Footer => m_footer;
        protected Image FooterIcon => m_footerIcon;
        protected Label FooterTitle => m_footerTitle;
        protected VisualElement FlowInputContainer => m_flowInputConnectorContainer;
        protected VisualElement FlowOutputContainer => m_flowOutputConnectorContainer;

        public NodeInputPort InputPort => m_inputPort;

        public virtual UQueryBuilder<Port> OutputPorts => this.Query<Port>();

        public virtual IEnumerable<T> GetOutputPorts<T>() where T : Port { return this.Query<Port>().ToList().Where(p => p is T).Select(p => p as T); }
        
        protected override void SelfChange()
        {
            base.SelfChange();

            if (inputContainer.childCount == 0)
            {
                mainContainer.AddToClassList("empty");
            }
            else
            {
                mainContainer.RemoveFromClassList("empty");
            }
        }

        public BaseNodeView(string template = null) : base(template ?? "uxml/BaseNode")
        {
            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable | Capabilities.Collapsible;

            this.AddStyleSheetPath("BaseNodeView");
            this.AddStyleSheetPath("Selectable");

            AddToClassList("baseNodeView");
            AddToClassList("selectable");

            m_flowInputConnectorContainer = this.Q("flow-inputs");
            m_flowInputConnectorContainer.style.overflow = Overflow.Hidden;
            m_flowOutputConnectorContainer = this.Q("flow-outputs");
            m_flowOutputConnectorContainer.style.overflow = Overflow.Hidden;

            m_headerIcon = titleContainer.Q<Image>("icon");

            m_footer = this.Q("footer");

            m_footerTitle = m_footer.Q<Label>("title-label");
            m_footerIcon = m_footer.Q<Image>("icon");

            m_label = this.Q<Label>("user-label");

            this.RegisterCallback<MouseDownEvent>(OnDoubleClick);

            m_inputPort = NodeInputPort.Create(Orientation.Horizontal, Direction.Input);
            m_flowInputConnectorContainer.Add(m_inputPort);

            
        }

        private void OnDoubleClick(MouseDownEvent evt)
        {
            if(evt.clickCount == 2)
            {
                var graphView = GetFirstAncestorOfType<GraphView>();
                if (graphView != null)
                {
                    graphView.selection.Clear();
                    graphView.selection.Add(this);
                    graphView.FrameSelection();
                }
            }
        }

        public override bool HitTest(Vector2 localPoint)
        {
            return ContainsPoint(localPoint);
        }

        
        public override void SetPosition(Rect newPos)
        {
            style.position = Position.Absolute;
            style.left = newPos.x;
            style.top = newPos.y;

            base.SetPosition(newPos);
            OnMoved();
        }

        public IEnumerable<Port> GetAllAnchors(bool input, bool output)
        {
            return null; // (IEnumerable<Port>)GetFlowAnchors(input, output);
        }
    }
}
