using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Procedure
{

    public class ImageModifier : BillboardModifier<Image>
    {
        [SerializeField]
        [Tooltip("The image to apply")]
        private OptionalSprite m_sprite;
        [SerializeField]
        private AdvancedOptions m_advanced;
        //[Reversible]
        //private OptionalAnimatedColor m_color;
        //[SerializeField]
        //private OptionalImageType m_imageType;
        //[SerializeField]
        //[Reversible]
        //private OptionalAnimatedFloat m_fillAmount;

        private Sprite m_prevSprite;
        private float m_prevFillAmount;
        private Image.Type m_prevType;
        private Color m_prevColor;

        public override void Prepare(Billboard billboard)
        {
            base.Prepare(billboard);
            m_prevSprite = m_target.sprite;
            m_prevFillAmount = m_target.fillAmount;
            m_prevType = m_target.type;
            m_prevColor = m_target.color;

            if (m_advanced.fillAmount.enabled)
            {
                m_advanced.fillAmount.value.Start(m_prevFillAmount);
            }
            if (m_advanced.color.enabled)
            {
                m_advanced.color.value.Start(m_prevColor);
            }
        }

        public override void Apply(float dt)
        {
            if (m_sprite.enabled)
            {
                m_target.sprite = m_sprite.value;
            }
            if (m_advanced.imageType.enabled)
            {
                m_target.type = m_advanced.imageType.value;
            }
            if (m_advanced.fillAmount.enabled)
            {
                m_target.fillAmount = m_advanced.fillAmount.value.Next(dt);
            }
            if (m_advanced.color.enabled)
            {
                m_target.color = m_advanced.color.value.Next(dt);
            }
            Progress = ProgressElements.Min(m_advanced.fillAmount, m_advanced.color);
        }

        public override void FastForward()
        {
            base.FastForward();
            if (m_sprite.enabled)
            {
                m_target.sprite = m_sprite.value;
            }
            if (m_advanced.imageType.enabled)
            {
                m_target.type = m_advanced.imageType.value;
            }
            if (m_advanced.fillAmount.enabled)
            {
                m_target.fillAmount = m_advanced.fillAmount.value.TargetValue;
            }
            if (m_advanced.color.enabled)
            {
                m_target.color = m_advanced.color.value.TargetValue;
            }
            Progress = 1;
        }

        public override void OnRevert()
        {
            m_target.sprite = m_prevSprite;
            m_target.type = m_prevType;
            if (m_advanced.fillAmount.enabled)
            {
                m_advanced.fillAmount.value.AutoAnimate(m_prevFillAmount, v => m_target.fillAmount = v);
            }
            if (m_advanced.color.enabled)
            {
                m_advanced.color.value.AutoAnimate(m_prevColor, v => m_target.color = v);
            }
        }

        protected override void ApplyPreview(Image preview)
        {
            if (m_sprite.enabled)
            {
                preview.sprite = m_sprite.value;
            }
            if (m_advanced.imageType.enabled)
            {
                preview.type = m_advanced.imageType.value;
            }
            if (m_advanced.fillAmount.enabled)
            {
                preview.fillAmount = m_advanced.fillAmount.value.TargetValue;
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
                return target + (m_sprite.enabled && m_sprite.value ? $"image = {m_sprite.value?.name}" : m_advanced.color.enabled ? $"color = {m_advanced.color.value}" : "nothing");
            }
        }

        [Serializable]
        private struct AdvancedOptions
        {
            [Tooltip("The color to apply to the image")]
            [Reversible]
            public OptionalAnimatedColor color;
            [Tooltip("The type of image to apply")]
            public OptionalImageType imageType;
            [Tooltip("The fill amount to apply if the image can be filled")]
            [Reversible]
            public OptionalAnimatedFloat fillAmount;
        }
    }
}
