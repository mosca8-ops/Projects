using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Procedure
{

    public class TextModifier : BillboardModifier<Text>
    {
        [SerializeField]
        [Tooltip("The text to show")]
        [LongText]
        private ValueProxyLocalizedString m_text;
        [SerializeField]
        private AdvancedOptions m_advanced;

        private string m_prevText;
        private int m_prevSize;
        private bool m_prevBestFit;
        private FontStyle m_prevStyle;
        private TextAnchor m_prevAlignment;
        private Color m_prevColor;

        public override void Prepare(Billboard billboard)
        {
            base.Prepare(billboard);
            m_prevText = m_target.text;
            m_prevSize = m_target.fontSize;
            m_prevStyle = m_target.fontStyle;
            m_prevColor = m_target.color;
            m_prevBestFit = m_target.resizeTextForBestFit;
            m_prevAlignment = m_target.alignment;

            if (m_advanced.fontSize.enabled)
            {
                m_advanced.fontSize.value.Start(m_prevSize);
            }
            if (m_advanced.color.enabled)
            {
                m_advanced.color.value.Start(m_prevColor);
            }
        }

        public override void Apply(float dt)
        {
            m_target.text = m_text;
            if (m_advanced.fontSize.enabled)
            {
                m_target.fontSize = m_advanced.fontSize.value.Next(dt);
            }
            if (m_advanced.fontStyle.enabled)
            {
                m_target.fontStyle = m_advanced.fontStyle.value;
            }
            if (m_advanced.alignment.enabled)
            {
                m_target.alignment = m_advanced.alignment.value;
            }
            if (m_advanced.color.enabled)
            {
                m_target.color = m_advanced.color.value.Next(dt);
            }
            Progress = ProgressElements.Min(m_advanced.fontSize, m_advanced.color);
        }

        public override void FastForward()
        {
            base.FastForward();
            m_target.text = m_text;
            if (m_advanced.bestFit.enabled)
            {
                m_target.resizeTextForBestFit = m_advanced.bestFit.value;
            }
            if (m_advanced.fontSize.enabled)
            {
                m_target.fontSize = m_advanced.fontSize.value.TargetValue;
            }
            if (m_advanced.fontStyle.enabled)
            {
                m_target.fontStyle = m_advanced.fontStyle.value;
            }
            if (m_advanced.alignment.enabled)
            {
                m_target.alignment = m_advanced.alignment.value;
            }
            if (m_advanced.color.enabled)
            {
                m_target.color = m_advanced.color.value.TargetValue;
            }
            Progress = 1;
        }

        public override void OnRevert()
        {
            m_target.text = m_prevText;
            if (m_advanced.fontSize.enabled)
            {
                m_advanced.fontSize.value.AutoAnimate(m_prevSize, v => m_target.fontSize = v);
            }
            if (m_advanced.bestFit.enabled)
            {
                m_target.resizeTextForBestFit = m_prevBestFit;
            }
            m_target.fontStyle = m_prevStyle;
            m_target.alignment = m_prevAlignment;
            if (m_advanced.color.enabled)
            {
                m_advanced.color.value.AutoAnimate(m_prevColor, v => m_target.color = v);
            }
        }

        protected override void ApplyPreview(Text preview)
        {
            preview.text = m_text;
            if (m_advanced.fontSize.enabled)
            {
                preview.fontSize = m_advanced.fontSize.value.TargetValue;
            }
            if (m_advanced.fontStyle.enabled)
            {
                preview.fontStyle = m_advanced.fontStyle.value;
            }
            if (m_advanced.alignment.enabled)
            {
                preview.alignment = m_advanced.alignment.value;
            }
            if (m_advanced.color.enabled)
            {
                preview.color = m_advanced.color.value.TargetValue;
            }
        }

        public override string Description
        {
            get
            {
                string target = m_target ? m_target.name + "." : "";
                return m_text.IsVariable ? $"{target}text = [{m_text.VariableName}]" : $"{target}text = {m_text.Value}";
            }
        }

        [Serializable]
        private struct AdvancedOptions
        {
            [Tooltip("The font size to apply")]
            [Reversible]
            public OptionalAnimatedInt fontSize;
            [Tooltip("The font style to apply")]
            public OptionalFontStyle fontStyle;
            [Tooltip("The alignment to apply")]
            public OptionalTextAnchor alignment;
            [Tooltip("Whether to have the best fit for the text or not")]
            public OptionalBool bestFit;
            [Tooltip("The color to apply to the text")]
            [Reversible]
            public OptionalAnimatedColor color;
        }
    }
}
