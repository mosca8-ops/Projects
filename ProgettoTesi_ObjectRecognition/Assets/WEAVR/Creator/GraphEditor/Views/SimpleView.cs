using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{

    abstract class SimpleView : VisualElement, IControlledElement, ISettableControlledElement<Controller>
    {
        public const string ResourcesRelativePath = "Creator/Resources/";

        private Controller m_controller;

        Controller IControlledElement.Controller => m_controller;

        public Controller Controller
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

        protected virtual void OnNewController()
        {
            
        }

        static string UXMLResourceToPackage(string resourcePath)
        {
            return WeavrEditor.PATH + ResourcesRelativePath + resourcePath + ".uxml";
        }

        public SimpleView(string template)
        {
            var tpl = EditorGUIUtility.Load(UXMLResourceToPackage(template)) as VisualTreeAsset;

            tpl?.CloneTree(this);
            Initialize();
        }

        public SimpleView()
        {
            Initialize();
        }

        void Initialize()
        {
            AddToClassList("simpleView");
            RegisterCallback<CustomStyleResolvedEvent>(evt => OnCustomStyleResolved(evt.customStyle));
        }

        protected virtual void OnCustomStyleResolved(ICustomStyle styles)
        {

        }

        public void ForceUpdate()
        {
            SelfChange(Controller.AnyThing);
        }

        protected virtual void SelfChange(int controllerChange)
        {
            //Profiler.BeginSample("BaseNodeView.SelfChange");
            //if (controller == null)
            //    return;

            
        }

        public void OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == Controller)
            {
                SelfChange(e.change);
            }
        }

        public virtual void OnSelected()
        {

        }
    }
}
