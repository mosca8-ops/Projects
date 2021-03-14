using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{

    class SimpleConditionView : SimpleView, ISettableControlledElement<BaseConditionController>
    {
        protected BaseConditionController m_controller;

        public new BaseConditionController Controller
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

        public SimpleConditionView() : base("uxml/SimpleCondition")
        {
            this.AddStyleSheetPath("SimpleConditionView");
            AddToClassList("simpleConditionView");
        }

        protected override void SelfChange(int controllerChange)
        {
            base.SelfChange(controllerChange);

        }
    }
}
