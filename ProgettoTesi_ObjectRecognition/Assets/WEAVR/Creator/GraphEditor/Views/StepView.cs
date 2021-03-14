using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEngine.UIElements.StyleSheets;

namespace TXT.WEAVR.Procedure
{

    class StepView : Group, IControlledElement, ISettableControlledElement<StepController>
    {

        private Label m_titleItem;
        private TextField m_titleEditor;
        private Label m_description;
        private Label m_number;
        private Toggle m_mandatoryToggle;
        private const int k_TitleEditorFocusDelay = 100;

        private StepController m_controller;

        Controller IControlledElement.Controller => m_controller;

        public StepController Controller
        {
            get => m_controller;
            set
            {
                if (m_controller != value)
                {
                    if (m_controller != null)
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

        Color? m_stateColor;
        public Color StateColor
        {
            get
            {
                return m_stateColor ?? Color.black;
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
                if (Controller.HasPosition)
                {
                    style.left = Controller.Position.x;
                    style.top = Controller.Position.y;

                    //SetPosition(new Rect(Controller.Position, layout.size));
                }

                if (!string.IsNullOrEmpty(SearchHub.Current.CurrentSearchValue))
                {
                    SearchValueChanged(SearchHub.Current.CurrentSearchValue);
                }
            }
        }

        protected override void OnElementsAdded(IEnumerable<GraphElement> elements)
        {
            base.OnElementsAdded(elements);
            if(Controller != null)
            {
                Controller.AddNodes(elements.Select(e => (e as IControlledElement)?.Controller as GraphObjectController)
                                            .Where(e => e != null));
            }
        }

        protected override void OnElementsRemoved(IEnumerable<GraphElement> elements)
        {
            base.OnElementsRemoved(elements);
            if (Controller != null)
            {
                Controller.RemoveNodes(elements.Select(e => (e as IControlledElement)?.Controller as GraphObjectController)
                                            .Where(e => e != null));
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

        public StepView()
        {
            Initialize();

            SearchHub.Current.SearchValueChanged -= SearchValueChanged;
            SearchHub.Current.SearchValueChanged += SearchValueChanged;

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
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
            if (Controller == null)
            {
                return;
            }
            newValue = newValue.ToLower();
            if (!string.IsNullOrEmpty(newValue) && (Controller.Description.ToLower().Contains(newValue) || Controller.Description.Replace(" ", "").ToLower().Contains(newValue)))
            {
                m_description?.AddToClassList("searchPositive");
            }
            else
            {
                m_description?.RemoveFromClassList("searchPositive");
            }
            if (!string.IsNullOrEmpty(newValue) 
                && (Controller.Title.ToLower().Contains(newValue) 
                   || Controller.Title.Replace(" ", "").ToLower().Contains(newValue)
                   || Controller.Model.Guid.ToLower().StartsWith(newValue)))
            {
                this.Q<Label>("titleLabel")?.AddToClassList("searchPositive");
            }
            else
            {
                this.Q<Label>("titleLabel")?.RemoveFromClassList("searchPositive");
            }
            if (!string.IsNullOrEmpty(newValue) && (Controller.Number.ToLower().Contains(newValue) || Controller.Number.Replace(" ", "").ToLower().Contains(newValue)))
            {
                this.Q<Label>("number")?.AddToClassList("searchPositive");
            }
            else
            {
                this.Q<Label>("number")?.RemoveFromClassList("searchPositive");
            }
        }

        protected override void OnCustomStyleResolved(ICustomStyle styles)
        {
            base.OnCustomStyleResolved(styles);

            if(styles.TryGetValue(new CustomStyleProperty<Color>("--state-color"), out Color v)) { m_stateColor = v; } else { m_stateColor = null; }
            if(styles.TryGetValue(new CustomStyleProperty<Color>("--minimap-color"), out Color v1)) { m_minimapColor = v1; } else { m_minimapColor = null; }

            if (m_minimapColor.HasValue)
            {
                elementTypeColor = m_minimapColor.Value;
            }
        }


        void OnFocusIn(FocusInEvent e)
        {
            var gv = GetFirstAncestorOfType<GraphView>();
            if (!IsSelected(gv))
                Select(gv, false);
            e.StopPropagation();
        }
        
        VisualElement m_dashedBorder;

        //bool m_Hovered;

        //void OnMouseEnter(MouseEnterEvent e)
        //{
        //    m_Hovered = true;
        //    UpdateBorder();
        //    e.PreventDefault();
        //}

        //void OnMouseLeave(MouseLeaveEvent e)
        //{
        //    m_Hovered = false;
        //    UpdateBorder();
        //    e.PreventDefault();
        //}

        //private void OnMouseDownEvent(MouseDownEvent e)
        //{
        //    if (e.clickCount == 2)
        //    {
        //        m_TitleEditor.Blur();
        //        Debug.Log("Gotcha!!");
        //        if (HitTest(e.localMousePosition))
        //        {
        //            m_TitleEditor.value = title;
        //            m_TitleEditor.visible = true;
        //            m_TitleItem.visible = false;
        //            schedule.Execute(GiveFocusToTitleEditor).StartingIn(k_TitleEditorFocusDelay);
        //        }
        //    }
        //}

        //private void GiveFocusToTitleEditor()
        //{
        //    m_TitleEditor.SelectAll();
        //    m_TitleEditor.Focus();
        //}

        bool m_Selected;

        public override void OnSelected()
        {
            m_Selected = true;
            UpdateBorder();
            if (Controller != null)
            {
                ProcedureObjectInspector.Selected = Controller;
            }
            BringToFront();
        }

        public override void OnUnselected()
        {
            m_Selected = false;
            UpdateBorder();
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

        void UpdateBorder()
        {
            m_dashedBorder?.EnableInClassList("selectedBorder", m_Selected);
        }

        void Initialize()
        {
            this.AddStyleSheetPath("GraphObject");
            AddToClassList("graphObjectView");
            this.AddStyleSheetPath("StepView");
            AddToClassList("stepView");

            var firstChild = Children().First();
            m_dashedBorder = new GroupBorder();
            m_dashedBorder.name = "group-border";
            Add(m_dashedBorder);

            var titleContainer = this.Q("titleContainer");

            m_titleItem = titleContainer.Q<Label>(name: "titleLabel");

            m_titleEditor = titleContainer.Q(name: "titleField") as TextField;
            m_titleEditor.visible = false;

            m_description = new Label();
            m_description.name = "description";
            m_description.AddToClassList("small-label");
            titleContainer.parent.Add(m_description);

            m_number = new Label();
            m_number.name = "number";
            titleContainer.parent.Add(m_number);

            //m_mandatoryToggle = new Toggle();
            //m_mandatoryToggle.name = "mandatory-toggle";
            //if (m_mandatoryToggle != null)
            //{
            //    m_mandatoryToggle.RegisterValueChangedCallback(MandatoryToggle_Changed);
            //}
            //titleContainer.parent.Add(m_mandatoryToggle);

            //UnregisterCallback()
            //RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

            //this.RegisterCallback<GeometryChangedEvent>()

            if (IsSelectable())
            {
                RegisterCallback<FocusInEvent>(OnFocusIn);
            }
        }

        private void MandatoryToggle_Changed(ChangeEvent<bool> evt)
        {
            if (Controller == null) { return; }
            //Controller.IsMandatory = evt.newValue;
        }

        public override bool HitTest(Vector2 localPoint)
        {
            return ContainsPoint(localPoint);
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

        public override void SetPosition(Rect newPos)
        {
            if (Controller != null && Controller.HasPosition)
            {
                Controller.Position = newPos.position;
            }
            base.SetPosition(newPos);
        }

        public virtual void SelfChange()
        {
            if (Controller == null)
            {
                return;
            }
            
            title = Controller.Title;
            m_description.text = Controller.Description;
            m_number.text = Controller.Number;

            //if (HasPosition())
            //{
            //    style.position = Position.Absolute;
            //    style.left = Controller.Position.x;
            //    style.top = Controller.Position.y;
            //}

            expanded = Controller.Expanded;

            RefreshNodeViews();

            //if (m_CollapseButton != null)
            //{
            //    m_CollapseButton.SetEnabled(false);
            //    m_CollapseButton.SetEnabled(true);
            //}

            //RefreshExpandedState();
            RefreshLayout();


            UpdateCollapse();
        }

        private void RefreshNodeViews()
        {
            var procedureView = GetFirstAncestorOfType<ProcedureView>();
            if(procedureView == null) { return; }

            RemoveElementsWithoutNotification(containedElements.ToList());
            foreach(var controller in Controller.NodesControllers)
            {
                var nodeView = procedureView.GetNode(controller);
                if(nodeView != null)
                {
                    AddElement(nodeView);
                }
            }
        }

        private bool m_expanded;
        internal static bool inRemoveElement;

        public bool expanded
        {
            get { return m_expanded; }
            set
            {
                if (m_expanded == value)
                {
                    return;
                }
                
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

        public void OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == Controller)
            {
                SelfChange();
            }
        }

        public virtual bool SuperCollapsed
        {
            get { return Controller.SuperCollapsed; }
            set
            {
                if (Controller.SuperCollapsed != value)
                {
                    Controller.SuperCollapsed = value;

                }
            }
        }
    }
}
