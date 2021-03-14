using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Procedure
{
    public class SetTextAction : BaseReversibleProgressAction, ITargetingObject, ISerializedNetworkProcedureObject
    {
        [SerializeField]
        [Tooltip("The target Text Component to change the text in")]
        [Draggable]
        private Text m_target;
        [SerializeField]
        [Tooltip("The text to change")]
        [LongText]
        private OptionalAnimatedLocalizedString m_text;
        [SerializeField]
        [Tooltip("The font size to apply to the text component")]
        [Reversible]
        private OptionalAnimatedInt m_fontSize;
        [SerializeField]
        [Tooltip("The style of the font to apply")]
        private OptionalFontStyle m_fontStyle;
        [SerializeField]
        [Tooltip("The alignment of the text to apply")]
        private OptionalTextAnchor m_alignment;
        [SerializeField]
        [Tooltip("The color to apply to the text")]
        [Reversible]
        private OptionalAnimatedColor m_color;

        #region [  ISerializedNetworkProcedureObject IMPLEMENTATION  ]

        [SerializeField]
        private bool m_isGlobal = true;
        public string IsGlobalFieldName => nameof(m_isGlobal);
        public bool IsGlobal => m_isGlobal;

        #endregion

        private string m_prevText;
        private int m_prevSize;
        private FontStyle m_prevStyle;
        private TextAnchor m_prevAlignment;
        private Color m_prevColor;

        public Text TextComponent
        {
            get => m_target;
            set
            {
                if(m_target != value)
                {
                    BeginChange();
                    m_target = value;
                    PropertyChanged(nameof(TextComponent));
                }
            }
        }

        public UnityEngine.Object Target {
            get => TextComponent;
            set => TextComponent = value is Text text ? text :
                value is GameObject go ? go.GetComponent<Text>() :
                value is Component c ? c.GetComponent<Text>() : value == null ? null : m_target; }

        public string TargetFieldName => nameof(m_target);

        public LocalizedString LocalizedText
        {
            get => m_text.value.TargetValue;
            set
            {
                if(m_text.value.TargetValue != value)
                {
                    m_text.value.TargetValue = value;
                }
            }
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            m_prevText = m_target.text;
            m_prevSize = m_target.fontSize;
            m_prevStyle = m_target.fontStyle;
            m_prevColor = m_target.color;
            m_prevAlignment = m_target.alignment;

            if (m_text.enabled)
            {
                m_text.value.Start(m_prevText);
            }
            if (m_fontSize.enabled)
            {
                m_fontSize.value.Start(m_prevSize);
            }
            if (m_color.enabled)
            {
                m_color.value.Start(m_prevColor);
            }
        }

        public override bool Execute(float dt)
        {
            if (m_text.enabled)
            {
                m_target.text = m_text.value.Next(dt);
            }
            if (m_fontSize.enabled)
            {
                m_target.fontSize = m_fontSize.value.Next(dt);
            }
            if (m_fontStyle.enabled)
            {
                m_target.fontStyle = m_fontStyle.value;
            }
            if (m_alignment.enabled)
            {
                m_target.alignment = m_alignment.value;
            }
            if (m_color.enabled)
            {
                m_target.color = m_color.value.Next(dt);
            }
            Progress = ProgressElements.Min(m_text, m_fontSize, m_color);
            return m_text.HasFinished && m_fontSize.HasFinished && m_color.HasFinished;
        }

        public override void FastForward()
        {
            base.FastForward();
            if (m_text.enabled)
            {
                m_target.text = m_text.value.TargetValue;
            }
            if (m_fontSize.enabled)
            {
                m_target.fontSize = m_fontSize.value.TargetValue;
            }
            if (m_fontStyle.enabled)
            {
                m_target.fontStyle = m_fontStyle.value;
            }
            if (m_alignment.enabled)
            {
                m_target.alignment = m_alignment.value;
            }
            if (m_color.enabled)
            {
                m_target.color = m_color.value.TargetValue;
            }
            Progress = 1;
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (RevertOnExit)
            {
                if (m_text.enabled)
                {
                    m_text.value.AutoAnimate(m_prevText, v => m_target.text = v);
                }
                if (m_fontSize.enabled)
                {
                    m_fontSize.value.AutoAnimate(m_prevSize, v => m_target.fontSize = v);
                }
                m_target.fontStyle = m_prevStyle;
                m_target.alignment = m_prevAlignment;
                if (m_color.enabled)
                {
                    m_color.value.AutoAnimate(m_prevColor, v => m_target.color = v);
                }
            }
        }

        public override string GetDescription()
        {
            return !m_target ? $"[ ? ].text = {m_text.value}" : 
                  m_text.enabled ? $"{m_target.name}.text = {m_text.value}" : 
                  m_color.enabled ? $"{m_target.name}.TextColor = {m_color.value}" : $"{m_target.name} no text or color set";
        }
    }
}