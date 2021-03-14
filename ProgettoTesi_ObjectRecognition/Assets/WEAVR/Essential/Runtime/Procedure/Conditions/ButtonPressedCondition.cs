using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{

    public class ButtonPressedCondition : BaseCondition, ITargetingObject
    {
        [System.Serializable]
        public class ValueProxyButton : ValueProxyComponent<Button> { }

        [SerializeField]
        [Tooltip("The button to be pressed")]
        [Draggable]
        public ValueProxyButton m_isPressed;
        [SerializeField]
        [Tooltip("If true, the check will be instantaneous, otherwise if the button will be pressed, the validation will hold till the end of condition evaluation")]
        private bool m_instantCheck = false;

        public Object Target {
            get => m_isPressed;
            set => m_isPressed.Value = value is Component c ? c.GetComponent<Button>() : value is GameObject go ? go.GetComponent<Button>() : m_isPressed.Value;
        }

        public string TargetFieldName => nameof(m_isPressed);

        private bool m_buttonWasPressed;
        private int m_lastPressedFrame;

        private void OnButtonPressed()
        {
            m_buttonWasPressed = true;
            m_lastPressedFrame = Time.frameCount;
        }

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            m_buttonWasPressed = false;
            m_isPressed.Value.onClick.RemoveListener(OnButtonPressed);
            m_isPressed.Value.onClick.AddListener(OnButtonPressed);
        }

        protected override bool EvaluateCondition()
        {
            if (m_buttonWasPressed)
            {
                if (m_instantCheck && m_lastPressedFrame != Time.frameCount)
                {
                    m_buttonWasPressed = false;
                }
                return true;
            }
            return false;
        }

        public override void ForceEvaluation()
        {
            base.ForceEvaluation();
            m_isPressed.Value.onClick.Invoke();
            m_buttonWasPressed = false;
        }

        public override void OnEvaluationEnded()
        {
            base.OnEvaluationEnded();
            m_isPressed.Value.onClick.RemoveListener(OnButtonPressed);
        }

        public override string GetDescription()
        {
            return $"{m_isPressed} is pressed";
        }

        public override string ToFullString()
        {
            return $"[{GetDescription()}]";
        }
    }
}
