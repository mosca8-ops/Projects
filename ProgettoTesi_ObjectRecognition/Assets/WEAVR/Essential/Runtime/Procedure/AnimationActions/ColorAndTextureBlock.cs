using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using static TXT.WEAVR.Pose;

namespace TXT.WEAVR.Procedure
{

    public class ColorAndTextureBlock : ComponentAnimation<Renderer>
    {
        [SerializeField]
        [Tooltip("The texture to apply")]
        private OptionalTexture m_texture;
        [SerializeField]
        [Tooltip("The offset to apply to the texture")]
        private OptionalVector2 m_offset;
        [SerializeField]
        [Tooltip("The scale to apply to the texture")]
        private OptionalVector2 m_scale;
        [SerializeField]
        [Tooltip("The color to apply to the material")]
        private OptionalColor m_color;

        [SerializeField]
        [Tooltip("The amount of offset to add to the texture")]
        private OptionalVector2 m_deltaOffset;
        [SerializeField]
        [Tooltip("The amount of scale to add to the texture")]
        private OptionalVector2 m_deltaScale;
        //[SerializeField]
        //[Tooltip("The amount of color to add to the texture")]
        //private OptionalColor m_deltaColor;

        [NonSerialized]
        private Texture m_prevTexture;
        [NonSerialized]
        private Vector2 m_prevOffset;
        [NonSerialized]
        private Vector2 m_prevScale;
        [NonSerialized]
        private Color m_prevColor;

        private Renderer m_prevTarget;
        
        public override bool CanProvide<T>()
        {
            return false;
        }

        public override void OnValidate()
        {
            base.OnValidate();
            if (m_offset.enabled) { m_deltaOffset.enabled = false; }
            if (m_scale.enabled) { m_deltaScale.enabled = false; }
            //if (m_color.enabled) { m_deltaColor.enabled = false; }
        }

        public override void OnStart()
        {
            base.OnStart();
            SnapshotValues();
            m_prevTarget = m_target;
        }

        private void SnapshotValues()
        {
            var material = Application.isPlaying ? m_target.material : m_target.sharedMaterial;
            if (m_texture.enabled)
            {
                m_prevTexture = material.mainTexture;
            }
            if (m_offset.enabled || m_deltaOffset.enabled)
            {
                m_prevOffset = material.mainTextureOffset;
            }
            if (m_scale.enabled || m_deltaScale.enabled)
            {
                m_prevScale = material.mainTextureScale;
            }
            if (m_color.enabled)
            {
                m_prevColor = material.color;
            }
        }

        protected override void Animate(float delta, float normalizedValue)
        {
            if (!m_target) { return; }
            if(m_target != m_prevTarget)
            {
                SnapshotValues();
                m_prevTarget = m_target;
            }
            var material = Application.isPlaying ? m_target.material : m_target.sharedMaterial;
            if (m_texture.enabled)
            {
                material.mainTexture = normalizedValue > 0 ? m_texture.value : m_prevTexture;
            }
            if (m_offset.enabled)
            {
                material.mainTextureOffset = Vector2.Lerp(m_prevOffset, m_offset.value, normalizedValue);
            }
            else if (m_deltaOffset.enabled)
            {
                material.mainTextureOffset += m_deltaOffset.value * delta;
            }
            if (m_scale.enabled)
            {
                material.mainTextureScale = Vector2.Lerp(m_prevScale, m_scale.value, normalizedValue);
            }
            else if (m_deltaScale.enabled)
            {
                material.mainTextureScale += (m_deltaScale.value - Vector2.one) * delta;
            }
            if (m_color.enabled)
            {
                material.color = Color.Lerp(m_prevColor, m_color.value, normalizedValue);
            }
            //else if (m_deltaColor.enabled)
            //{
            //    material.color += m_deltaColor.value * delta;
            //}
        }

        public override bool CanPreview()
        {
            return true;
        }
    }
}
