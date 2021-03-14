using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Procedure
{
    public class SetImageAction : BaseReversibleProgressAction, ITargetingObject, ISerializedNetworkProcedureObject
    {
        [SerializeField]
        [Tooltip("The target Image component to change the image")]
        [Draggable]
        private Image m_target;
        [SerializeField]
        [Tooltip("The image to apply")]
        private OptionalSprite m_sprite;
        [SerializeField]
        [Tooltip("The color to apply to the image")]
        [Reversible]
        private OptionalAnimatedColor m_color;
        [SerializeField]
        [Tooltip("How to render the image")]
        private OptionalImageType m_imageType;
        [SerializeField]
        [Tooltip("The fill amount for the image, if it can be filled")]
        [Reversible]
        private OptionalAnimatedFloat m_fillAmount;

        #region [  ISerializedNetworkProcedureObject IMPLEMENTATION  ]

        [SerializeField]
        private bool m_isGlobal = true;
        public string IsGlobalFieldName => nameof(m_isGlobal);
        public bool IsGlobal => m_isGlobal;

        #endregion

        private Sprite m_prevSprite;
        private float m_prevFillAmount;
        private Image.Type m_prevType;
        private Color m_prevColor;

        public Image Target
        {
            get => m_target;
            set
            {
                if (m_target != value)
                {
                    BeginChange();
                    m_target = value;
                    PropertyChanged(nameof(Target));
                }
            }
        }

        UnityEngine.Object ITargetingObject.Target {
            get => Target;
            set => Target = value is Image image ? image : 
                value is GameObject go ? go.GetComponent<Image>() : 
                value is Component c ? c.GetComponent<Image>() : 
                value == null ? null : Target;
        }

        public string TargetFieldName => nameof(m_target);

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            m_prevSprite = m_target.sprite;
            m_prevFillAmount = m_target.fillAmount;
            m_prevType = m_target.type;
            m_prevColor = m_target.color;
            
            if (m_fillAmount.enabled)
            {
                m_fillAmount.value.Start(m_prevFillAmount);
            }
            if (m_color.enabled)
            {
                m_color.value.Start(m_prevColor);
            }
        }

        public override bool Execute(float dt)
        {
            if (m_sprite.enabled)
            {
                m_target.sprite = m_sprite.value;
            }
            if (m_imageType.enabled)
            {
                m_target.type = m_imageType.value;
            }
            if (m_fillAmount.enabled)
            {
                m_target.fillAmount = m_fillAmount.value.Next(dt);
            }
            if (m_color.enabled)
            {
                m_target.color = m_color.value.Next(dt);
            }
            Progress = ProgressElements.Min(m_fillAmount, m_color);
            return m_fillAmount.HasFinished && m_color.HasFinished;
        }

        public override void FastForward()
        {
            base.FastForward();
            if (m_sprite.enabled)
            {
                m_target.sprite = m_sprite.value;
            }
            if (m_imageType.enabled)
            {
                m_target.type = m_imageType.value;
            }
            if (m_fillAmount.enabled)
            {
                m_target.fillAmount = m_fillAmount.value.TargetValue;
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
                m_target.sprite = m_prevSprite;
                m_target.type = m_prevType;
                if (m_fillAmount.enabled)
                {
                    m_fillAmount.value.AutoAnimate(m_prevFillAmount, v => m_target.fillAmount = v);
                }
                if (m_color.enabled)
                {
                    m_color.value.AutoAnimate(m_prevColor, v => m_target.color = v);
                }
            }
        }

        public override string GetDescription()
        {
            return !m_target ? $"[ ? ].text = {m_sprite.value}" :
                  m_sprite.enabled && m_color.enabled ? $"{m_target.name}.Sprite = {m_sprite.value} with color {m_color.value}" :
                  m_sprite.enabled ? $"{m_target.name}.Sprite = {m_sprite.value}" :
                  m_color.enabled ? $"{m_target.name}.Color = {m_color.value}" : $"{m_target.name} no sprite or color set";
        }
    }
}