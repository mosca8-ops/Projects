using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Profiling;
using UnityEditorInternal;

namespace TXT.WEAVR.Procedure
{
    class GraphObjectView : Node, IControlledElement, ISettableControlledElement<GraphObjectController>
    {
        protected static readonly Color s_defaultSelectionColor = new Color(68.0f / 255.0f, 192.0f / 255.0f, 255.0f / 255.0f, 1.0f);
        protected static readonly Color s_defaultHoverColor = new Color(68.0f / 255.0f, 192.0f / 255.0f, 255.0f / 255.0f, 0.5f);

        protected Label m_title;

        Color? m_stateColor;
        public Color StateColor
        {
            get
            {
                return m_stateColor ?? Color.clear;
            }
            set
            {
                m_stateColor = value;
            }
        }

        Color? m_minimapColor;
        public Color MinimapColor
        {
            get
            {
                return m_minimapColor ?? Color.black;
            }
            set
            {
                m_minimapColor = value;
                elementTypeColor = value;
            }
        }

        public void OnMoved()
        {
            Controller.Position = GetPosition().position;
        }

        protected virtual void OnNewController()
        {
            if (Controller != null)
            {
                viewDataKey = ComputePersistenceKey();
            }
        }

        protected string Title
        {
            get => Controller?.Title ?? m_title?.text ?? title;
            set
            {
                if(Controller != null)
                {
                    Controller.Title = value;
                }
                if(m_title != null)
                {
                    m_title.text = value;
                }
            }
        }

        public string ComputePersistenceKey()
        {
            return Controller != null ? $"GraphObject-{Controller.Model?.GetType().Name}-{Controller.Model.GetInstanceID()}" : null;
        }

        public void OnSelectionMouseDown(MouseDownEvent e)
        {
            var gv = GetFirstAncestorOfType<ProcedureView>();
            if (IsSelected(gv))
            {
                if (e.actionKey)
                {
                    Unselect(gv);
                }
            }
            else
            {
                Select(gv, e.actionKey);
            }
        }

        public const string ResourcesRelativePath = "Creator/Resources/";

        static string UXMLResourceToPackage(string resourcePath)
        {
            return WeavrEditor.PATH + ResourcesRelativePath + resourcePath + ".uxml";
        }

        public GraphObjectView(string template) : base(UXMLResourceToPackage(template))
        {
            Initialize();
        }

        protected override void OnCustomStyleResolved(ICustomStyle styles)
        {
            base.OnCustomStyleResolved(styles);

            if(styles.TryGetValue(new CustomStyleProperty<Color>("--state-color"), out Color v)) { m_stateColor = v; } else { m_stateColor = null; }

            if(styles.TryGetValue(new CustomStyleProperty<Color>("--minimap-color"), out Color v3)) { m_minimapColor = v3; } else { m_minimapColor = null; }

            if (m_minimapColor.HasValue)
            {
                elementTypeColor = MinimapColor;
            }
        }

        void OnFocusIn(FocusInEvent e)
        {
            var gv = GetFirstAncestorOfType<GraphView>();
            if (!IsSelected(gv))
                Select(gv, false);
            e.StopPropagation();
        }

        public GraphObjectView() : base(UXMLResourceToPackage("uxml/GraphObject"))
        {
            this.AddStyleSheetPath("StyleSheets/GraphView/Node.uss");
            Initialize();
        }

        public override void OnSelected()
        {
            BringToFront();
            if (Controller != null)
            {
                ProcedureObjectInspector.Selected = Controller;
            }
        }

        public override void OnUnselected()
        {
            if (ProcedureObjectInspector.Selected == Controller)
            {
                schedule.Execute(() =>
                {
                    if (ProcedureObjectInspector.Selected == Controller && panel != null)
                    {
                        ProcedureObjectInspector.Selected = null;
                    }
                }).StartingIn(100);
            }
        }

        void Initialize()
        {
            this.AddStyleSheetPath("GraphObject");
            AddToClassList("graphObjectView");
            var border = this.Q("node-border");
            if(border != null)
            {
                var nodeBorder = new NodeBorder();
                Add(nodeBorder);
                nodeBorder.PlaceInFront(border);
                foreach(var child in border.Children().ToList())
                {
                    nodeBorder.Add(child);
                }
                border.RemoveFromHierarchy();
                nodeBorder.name = "node-border";
                nodeBorder.style.overflow = Overflow.Hidden;
            }

            m_title = this.Q<Label>("user-label");

            if (IsSelectable())
            {
                RegisterCallback<FocusInEvent>(OnFocusIn);
                this.Query("selection-border").First()?.RemoveFromHierarchy();
                //m_SelectionBorder?.RemoveFromHierarchy();
            }
        }

        protected virtual bool HasPosition()
        {
            return Controller.HasPosition;
        }

        public void ForceUpdate()
        {
            SelfChange();
        }

        public void UpdateCollapse()
        {
            if (SuperCollapsed)
            {
                AddToClassList("superCollapsed");
            }
            else
            {
                RemoveFromClassList("superCollapsed");
            }
        }

        protected virtual void SelfChange()
        {
            if (Controller == null)
            {
                return;
            }
            
            Title = Controller.Title;

            if (HasPosition())
            {
                style.position = Position.Absolute;
                style.left = Controller.Position.x;
                style.top = Controller.Position.y;
            }

            base.expanded = Controller.Expanded;

            if (m_CollapseButton != null)
            {
                m_CollapseButton.SetEnabled(false);
                m_CollapseButton.SetEnabled(true);
            }

            RefreshExpandedState();
            RefreshLayout();


            UpdateCollapse();
        }

        public override bool expanded
        {
            get { return base.expanded; }
            set
            {
                if (base.expanded == value)
                {
                    return;
                }

                base.expanded = value;
                Controller.Expanded = value;
            }
        }

        public virtual void GetPreferedWidths(ref float labelWidth, ref float controlWidth)
        {
        }

        public virtual void ApplyWidths(float labelWidth, float controlWidth)
        {
        }

        public virtual void RefreshLayout()
        {
        }

        public virtual void OnControllerChanged(ref ControllerChangedEvent e)
        {
            if(e.controller == Controller)
            {
                SelfChange();
            }
        }

        public virtual bool SuperCollapsed
        {
            get { return Controller.SuperCollapsed; }
            set
            {
                if(Controller.SuperCollapsed != value)
                {
                    Controller.SuperCollapsed = value;

                }
            }
        }

        private GraphObjectController m_controller;

        Controller IControlledElement.Controller => m_controller;

        public GraphObjectController Controller {
            get => m_controller;
            set
            {
                if (m_controller != value)
                {
                    if(m_controller != null)
                    {
                        m_controller.UnregisterHandler(this);
                    }
                    m_controller = value;
                    OnNewController();
                    if (m_controller != null)
                    {
                        m_controller.RegisterHandler(this);
                    }
                }
            }
        }
    }
}
