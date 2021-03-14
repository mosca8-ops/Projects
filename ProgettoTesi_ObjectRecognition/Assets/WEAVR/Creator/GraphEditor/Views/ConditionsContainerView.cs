using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{

    class ConditionsContainerView : SimpleView, ISettableControlledElement<ConditionsController>, IBadgeClient
    {
        protected ConditionsController m_controller;

        private VisualElement m_conditionsContainer;
        private Label m_noConditions;
        private Button m_addConditionButton;
        private Badge m_badge;

        public new ConditionsController Controller
        {
            get => m_controller;
            set
            {
                if (m_controller != value)
                {
                    m_controller = value;
                    base.Controller = value;
                }
            }
        }

        private void SetController(ConditionsController value)
        {
            if (m_controller != value)
            {
                if (m_controller != null)
                {
                    m_controller.UnregisterHandler(this);
                }
                m_controller = value;
                OnNewController();
            }
        }

        public ConditionsContainerView() : base("uxml/ConditionsContainer")
        {
            AddToClassList("conditionsContainerView");

            m_noConditions = this.Q<Label>("no-conditions");
            m_conditionsContainer = this.Q("conditions");

            m_addConditionButton = this.Q<Button>("add-condition");
            if (m_addConditionButton != null)
            {
                m_addConditionButton.clickable.clicked += AddCondition_Clicked;
            }

            m_badge = new Badge(Badge.BadgeType.error);

            RegisterCallback<AttachToPanelEvent>(AttachedToPanel);
        }

        private void AttachedToPanel(AttachToPanelEvent evt)
        {
            schedule.Execute(() =>
            {
                if (Controller != null)
                {
                    UpdateBadge();
                }
            }).StartingIn(100);
        }

        private void UpdateBadge()
        {
            if (Controller.HasErrors)
            {
                if (m_badge.panel == null)
                {
                    //(GetFirstAncestorOfType<BaseNodeView>() ?? parent).Add(m_badge);
                    m_badge.TryAttachTo(this, GetFirstAncestorOfType<BaseNodeView>() ?? parent, SpriteAlignment.LeftCenter);
                }
                m_badge.Type = Badge.BadgeType.error;
                m_badge.badgeText = Controller.ErrorMessage;
            }
            else if (m_badge.panel != null)
            {
                m_badge.Detach();
                m_badge.RemoveFromHierarchy();
            }
        }

        private void AddCondition_Clicked()
        {
            Controller.AddCondition();
        }
        
        protected override void SelfChange(int controllerChange)
        {
            base.SelfChange(controllerChange);
            if(Controller == null) { return; }

            if(panel != null)
            {
                UpdateBadge();
            }

            EnableInClassList("error", Controller.HasErrors);

            RefreshConditions();
        }

        public void RefreshConditions()
        {
            var elementControllers = Controller.FlowConditionsControllers;
            int elementControllerCount = elementControllers.Count();

            // recreate the children list based on the controller list to keep the order.

            var blocksUIs = new Dictionary<FlowConditionController, SimpleFlowConditionView>();

            bool somethingChanged = m_conditionsContainer.childCount < elementControllerCount || m_noConditions.parent != null;

            int cptBlock = 0;
            for (int i = 0; i < m_conditionsContainer.childCount; ++i)
            {
                var child = m_conditionsContainer.ElementAt(i) as SimpleFlowConditionView;
                if (child != null)
                {
                    blocksUIs.Add(child.Controller as FlowConditionController, child);

                    if (!somethingChanged && elementControllerCount > cptBlock && child.Controller != elementControllers[cptBlock])
                    {
                        somethingChanged = true;
                    }
                    cptBlock++;
                }
            }
            if (somethingChanged || cptBlock != elementControllerCount)
            {
                foreach (var kv in blocksUIs)
                {
                    kv.Value.RemoveFromClassList("first");
                    m_conditionsContainer.Remove(kv.Value);
                }
                if (elementControllers.Count() > 0)
                {
                    m_noConditions.RemoveFromHierarchy();
                }
                else if (m_noConditions.parent == null)
                {
                    m_conditionsContainer.Add(m_noConditions);
                }
                if (elementControllers.Count > 0)
                {
                    foreach (var blockController in elementControllers)
                    {
                        SimpleFlowConditionView simpleView;
                        if (blocksUIs.TryGetValue(blockController, out simpleView))
                        {
                            m_conditionsContainer.Add(simpleView);
                        }
                        else
                        {
                            InstantiateBlock(blockController);
                        }
                    }
                    SimpleFlowConditionView firstBlock = m_conditionsContainer.Query<SimpleFlowConditionView>();
                    firstBlock.AddToClassList("first");
                }
            }
        }

        private void InstantiateBlock(FlowConditionController blockController)
        {
            var blockUI = new SimpleFlowConditionView();
            blockUI.Controller = blockController;
            m_conditionsContainer.Add(blockUI);
        }

        public void ClearBadge()
        {
            if(m_badge != null && m_badge.panel != null)
            {
                m_badge.Detach();
                m_badge.RemoveFromHierarchy();
            }
        }
    }
}
