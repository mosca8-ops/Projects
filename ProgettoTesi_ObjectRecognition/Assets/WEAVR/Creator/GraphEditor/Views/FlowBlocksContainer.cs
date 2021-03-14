using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


namespace TXT.WEAVR.Procedure
{

    class FlowBlocksGraphContainer : GraphElement
    {
        private FlowBlocksContainer m_blocksContainer;

        public FlowBlocksContainer BlocksContainer => m_blocksContainer;

        public override bool IsSelectable()
        {
            return false;
        }

        public FlowBlocksGraphContainer()
        {
            m_blocksContainer = new FlowBlocksContainer();
            Initialize();
        }

        public FlowBlocksGraphContainer(string containerTemplate)
        {
            m_blocksContainer = new FlowBlocksContainer(containerTemplate);
            Initialize();
        }

        private void Initialize()
        {
            contentContainer.Add(m_blocksContainer);
            //contentContainer.cacheAsBitmap = true;
            style.alignItems = Align.Center;
            style.paddingLeft = style.marginLeft = 0;
            layer = 10;
        }

        public override void SetPosition(Rect newPos)
        {
            //base.SetPosition(newPos);
            //clippingOptions = ClippingOptions.NoClipping;
            style.position = Position.Absolute;
            style.left = newPos.x;
            style.top = newPos.y;
        }
    }

    class FlowBlocksContainer : VisualElement
    {
        public const string ResourcesRelativePath = "Creator/Resources/";
        static string UXMLResourceToPackage(string resourcePath)
        {
            return WeavrEditor.PATH + ResourcesRelativePath + resourcePath + ".uxml";
        }

        VisualElement m_container;
        VisualElement m_noActions;

        private Func<GraphObjectController, VisualElement> m_instantiateBlock;

        public Func<GraphObjectController, VisualElement> InstantiateBlock
        {
            get => m_instantiateBlock ?? DefaultInstantiateBlock;
            set
            {
                if(m_instantiateBlock != value)
                {
                    m_instantiateBlock = value;
                }
            }
        }


        public FlowBlocksContainer(string template)
        {
            var tpl = EditorGUIUtility.Load(UXMLResourceToPackage(template)) as VisualTreeAsset;

            tpl?.CloneTree(this);
            Initialize();
        }

        public FlowBlocksContainer()
        {
            Initialize();
        }

        void Initialize()
        {
            m_container = this.Q("blocksContainer") ?? this;
            m_noActions = this.Q("noActions") ?? new Label("No Actions");
            m_noActions.AddToClassList("no-actions");
        }

        public void RefreshContext(IReadOnlyList<GraphObjectController> controllers)
        {
            var elementControllers = controllers;
            int elementControllerCount = elementControllers.Count;

            // recreate the children list based on the controller list to keep the order.

            var blocksUIs = new Dictionary<GraphObjectController, SimpleView>();

            bool somethingChanged = m_container.childCount < elementControllerCount || m_noActions.parent != null;

            int cptBlock = 0;
            for (int i = 0; i < m_container.childCount; ++i)
            {
                var child = m_container.ElementAt(i) as SimpleView;
                if (child != null)
                {
                    blocksUIs.Add(child.Controller as GraphObjectController, child);

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
                    m_container.Remove(kv.Value);
                }
                if (elementControllers.Count > 0)
                {
                    m_noActions.RemoveFromHierarchy();
                }
                else if (m_noActions.parent == null)
                {
                    m_container.Add(m_noActions);
                }
                if (elementControllers.Count > 0)
                {
                    foreach (var blockController in elementControllers)
                    {
                        SimpleView simpleView;
                        if (blocksUIs.TryGetValue(blockController, out simpleView))
                        {
                            m_container.Add(simpleView);
                        }
                        else
                        {
                            var newBlock = InstantiateBlock?.Invoke(blockController);
                            if (newBlock != null)
                            {
                                m_container.Add(newBlock);
                            }
                        }
                    }
                    SimpleView firstBlock = m_container.Query<SimpleView>();
                    firstBlock.AddToClassList("first");
                }
            }
        }

        private VisualElement DefaultInstantiateBlock(GraphObjectController blockController)
        {
            if (blockController is BaseActionController)
            {
                var blockUI = new SimpleActionView();
                blockUI.Controller = blockController as BaseActionController;
                return blockUI;
            }
            else if (blockController is ConditionsController)
            {
                var blockUI = new ConditionsContainerView();
                blockUI.Controller = blockController as ConditionsController;
                return blockUI;
            }
            return null;
        }
    }
}
