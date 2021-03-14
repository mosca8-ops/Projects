using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Profiling;

namespace TXT.WEAVR.Procedure
{

    class GenericNodeView : BaseNodeView<GenericNodeController>
    {
        VisualElement m_actionsContainer;
        VisualElement m_conditionsContainer;
        VisualElement m_noActions;

        Image m_icon;
        VisualElement m_descriptionContainer;
        Label m_numberLabel;
        Label m_description;

        VisualElement m_DragDisplay;

        Toggle m_mandatoryToggle;

        public override UQueryBuilder<Port> OutputPorts => m_conditionsContainer.Query<Port>();

        public override IEnumerable<T> GetOutputPorts<T>()
        {
            return m_conditionsContainer.Query<Port>().Where(p => p is T).ToList().Select(p => p as T);
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            if(Controller == null) { return; }

            bool isMandatory = Controller.IsMandatory;
            if(isMandatory != m_mandatoryToggle.value)
            {
                m_mandatoryToggle.SetValueWithoutNotify(isMandatory);
            }
            EnableInClassList("mandatory", isMandatory);

            if (Controller.IsPartOfStep)
            {
                m_numberLabel.visible = false;
                m_icon.visible = true;
                if (m_description.panel != null)
                {
                    m_description.RemoveFromHierarchy();
                }
            }
            else
            {
                m_numberLabel.text = Controller.Number;
                m_numberLabel.visible = true;
                m_icon.visible = false;

                m_description.text = Controller.Description;
                if(string.IsNullOrEmpty(Controller.Description))
                {
                    if (m_description.panel != null)
                    {
                        m_description.RemoveFromHierarchy();
                    }
                }
                else if(m_description.panel == null) {
                    m_descriptionContainer.Add(m_description);
                }
            }

            RefreshContext();
        }

        public GenericNodeView() : base("uxml/GenericNode")
        {
            this.AddStyleSheetPath("GenericNodeView");
            this.AddStyleSheetPath("SimpleFlowConditionView");
            AddToClassList("genericNodeView");

            //clippingOptions = ClippingOptions.NoClipping;

            m_actionsContainer = this.Q("actions-container");
            m_noActions = m_actionsContainer.Q("no-actions");
            m_conditionsContainer = m_actionsContainer.Q("conditions-container");

            m_icon = this.Q<Image>("icon");

            m_numberLabel = this.Q<Label>("step-number");
            m_descriptionContainer = this.Q("description-container");
            m_description = m_descriptionContainer.Q<Label>("step-description");
            m_description.RemoveFromHierarchy();

            m_DragDisplay = new VisualElement();
            m_DragDisplay.AddToClassList("dragdisplay");

            m_mandatoryToggle = this.Q<Toggle>("mandatory-toggle");
            if(m_mandatoryToggle != null)
            {
                m_mandatoryToggle.RegisterValueChangedCallback(MandatoryToggle_Changed);
            }

            SearchHub.Current.SearchValueChanged -= SearchValueChanged;
            SearchHub.Current.SearchValueChanged += SearchValueChanged;
            
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            SearchHub.Current.SearchValueChanged -= SearchValueChanged;
            SearchHub.Current.SearchValueChanged += SearchValueChanged;

            if (!string.IsNullOrEmpty(SearchHub.Current.CurrentSearchValue))
            {
                SearchValueChanged(SearchHub.Current.CurrentSearchValue);
            }
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
            newValue = newValue?.ToLower();
            if (!string.IsNullOrEmpty(newValue) && Controller.Description != null
                && (Controller.Description.ToLower().Contains(newValue) 
                    || Controller.Description.Replace(" ", "").ToLower().Contains(newValue)))
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
                this.Q<Label>("user-label")?.AddToClassList("searchPositive");
            }
            else
            {
                this.Q<Label>("user-label")?.RemoveFromClassList("searchPositive");
            }
            if (!string.IsNullOrEmpty(newValue) && (Controller.Number.ToLower().Contains(newValue) || Controller.Number.Replace(" ", "").ToLower().Contains(newValue)))
            {
                m_numberLabel?.AddToClassList("searchPositive");
            }
            else
            {
                m_numberLabel?.RemoveFromClassList("searchPositive");
            }
        }

        protected override void OnNewController()
        {
            base.OnNewController();
            if(Controller != null && m_mandatoryToggle != null)
            {
                m_mandatoryToggle.visible = Controller.CanBeMandatory;
                m_mandatoryToggle.value = Controller.IsMandatory;

                if (!string.IsNullOrEmpty(SearchHub.Current.CurrentSearchValue))
                {
                    SearchValueChanged(SearchHub.Current.CurrentSearchValue);
                }
            }
        }

        private void MandatoryToggle_Changed(ChangeEvent<bool> evt)
        {
            if(Controller == null) { return; }
            Controller.IsMandatory = evt.newValue;
        }

        bool m_CanHaveBlocks = false;

        public bool CanDrop(IEnumerable<ActionView> blocks)
        {
            bool accept = true;
            if (blocks.Count() == 0) return false;
            foreach (var block in blocks)
            {
                //if (!controller.model.AcceptChild(block.controller.model))
                //{
                //    accept = false;
                //    break;
                //}
            }
            return accept;
        }

        public override bool HitTest(Vector2 localPoint)
        {
            return ContainsPoint(localPoint);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            this.Query<SimpleView>().ForEach(s => s.OnSelected());
            //if (controller != null)
            //{
            //    GraphEditor.NodeInspector.Selected = controller.Model;
            //}
        }

        public void RefreshContext()
        {
            var elementControllers = Controller.FlowElementsControllers;
            int elementControllerCount = elementControllers.Count();

            // recreate the children list based on the controller list to keep the order.

            var blocksUIs = new Dictionary<GraphObjectController, SimpleView>();

            bool somethingChanged = m_actionsContainer.childCount < elementControllerCount || (!m_CanHaveBlocks && m_noActions.parent != null);

            int cptBlock = 0;
            for (int i = 0; i < m_actionsContainer.childCount; ++i)
            {
                var child = m_actionsContainer.ElementAt(i) as SimpleView;
                if (child != null)
                {
                    blocksUIs.Add(child.Controller as GraphObjectController, child);

                    if (!somethingChanged && elementControllerCount > cptBlock && child.Controller != elementControllers[cptBlock])
                    {
                        somethingChanged = true;
                    }
                    cptBlock++;
                }
                else
                {
                    m_actionsContainer.RemoveAt(i--);
                }
            }
            if (somethingChanged || cptBlock != elementControllerCount)
            {
                foreach (var kv in blocksUIs)
                {
                    kv.Value.RemoveFromClassList("first");
                    m_actionsContainer.Remove(kv.Value);
                }
                if (elementControllers.Count() > 0 || !m_CanHaveBlocks)
                {
                    m_noActions.RemoveFromHierarchy();
                }
                else if (m_noActions.parent == null)
                {
                    m_actionsContainer.Add(m_noActions);
                }
                if (elementControllers.Count > 0)
                {
                    foreach (var blockController in elementControllers)
                    {
                        SimpleView simpleView;
                        if (blocksUIs.TryGetValue(blockController, out simpleView))
                        {
                            if(simpleView is IBadgeClient badgeClient)
                            {
                                badgeClient.ClearBadge();
                            }
                            m_actionsContainer.Add(simpleView);
                        }
                        else
                        {
                            InstantiateBlock(blockController);
                        }
                    }
                    SimpleView firstBlock = m_actionsContainer.Query<SimpleView>();
                    firstBlock.AddToClassList("first");
                }
            }
        }

        private void InstantiateBlock(GraphObjectController blockController)
        {
            if (blockController is BaseActionController)
            {
                var blockUI = new SimpleActionView();
                blockUI.Controller = blockController as BaseActionController;
                m_actionsContainer.Add(blockUI);
            }
            else if(blockController is ConditionsController)
            {
                var blockUI = new ConditionsContainerView();
                blockUI.Controller = blockController as ConditionsController;
                m_actionsContainer.Add(blockUI);
            }
        }
    }
}