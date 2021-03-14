using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

using System.Reflection;
using System.Linq;
using UnityEditor;

namespace TXT.WEAVR.Procedure
{
    class ActionView : GraphObjectView
    {
        Toggle m_enableToggle;

        VisualElement m_propertiesContainer;

        //public new VFXBlockController controller
        //{
        //    get { return base.controller as VFXBlockController; }
        //    set { base.controller = value; }
        //}

        //public override VFXDataAnchor InstantiateDataAnchor(VFXDataAnchorController controller, BaseNodeView node)
        //{
        //    VFXContextDataAnchorController anchorController = controller as VFXContextDataAnchorController;

        //    VFXEditableDataAnchor anchor = VFXBlockDataAnchor.Create(anchorController, node);
        //    return anchor;
        //}

        protected override bool HasPosition()
        {
            return Controller.HasPosition;
        }

        protected override void OnNewController()
        {
            base.OnNewController();

        }

        public BaseNodeView NodeView
        {
            get { return this.GetFirstAncestorOfType<BaseNodeView>(); }
        }

        public ActionView() : base("uxml/ActionView")
        {
            this.AddStyleSheetPath("ActionView");
            pickingMode = PickingMode.Position;
            m_enableToggle = new Toggle();
            m_enableToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleEnable);
            titleContainer.Insert(1, m_enableToggle);

            capabilities &= ~Capabilities.Ascendable;
            capabilities |= Capabilities.Selectable;

            this.expanded = true;

            //this.AddManipulator(new TrickleClickSelector());
            
            style.position = Position.Relative;
        }

        // On purpose -- until we support Drag&Drop I suppose
        public override void SetPosition(Rect newPos)
        {
            style.position = Position.Relative;
        }

        void OnToggleEnable(ChangeEvent<bool> e)
        {
            //controller.model.enabled = !controller.model.enabled;
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            if(m_propertiesContainer == null)
            {
                m_propertiesContainer = this.Q("propertiesContainer");
                if (m_propertiesContainer == null)
                {
                    return;
                }
            }

            m_propertiesContainer.Clear();
            SerializedObject serObj = new SerializedObject(Controller.Model);

            var property = serObj.FindProperty(nameof(BaseAction.separator));
            while (property.NextVisible(false))
            {
                m_propertiesContainer.Add(GenericPropertyField.CreateField(property));
            }

            //if (controller.model.enabled)
            //{
            //    titleContainer.RemoveFromClassList("disabled");
            //}
            //else
            //{
            //    titleContainer.AddToClassList("disabled");
            //}

            //m_EnableToggle.SetValueWithoutNotify(controller.model.enabled);
            //if (inputContainer != null)
            //    inputContainer.SetEnabled(controller.model.enabled);
            //if (settingsContainer != null)
            //    settingsContainer.SetEnabled(controller.model.enabled);
        }

        public override bool expanded { get => base.expanded; set => base.expanded = value; }

        public override bool SuperCollapsed
        {
            get { return Controller.SuperCollapsed; }
        }
    }
}
