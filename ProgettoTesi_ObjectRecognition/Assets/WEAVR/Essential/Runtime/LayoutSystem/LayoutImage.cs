using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.LayoutSystem
{
    [Serializable]
    public class UnityEventTexture2D : UnityEvent<Texture2D> { }

    [RequireComponent(typeof(Image))]
    [AddComponentMenu("WEAVR/Layout System/Layout Image")]
    public class LayoutImage : BaseLayoutItem
    {
        [SerializeField]
        [Draggable]
        [Button(nameof(SaveDefaults), "Reset")]
        protected Image m_image;

        [Space]
        [SerializeField]
        protected UnityEventTexture2D m_onSpriteChanged;
        
        [SerializeField]
        [HideInInspector]
        private Color m_initialColor;

        public Sprite Sprite
        {
            get { return m_image?.sprite; }
            set
            {
                if(m_image != null && m_image.sprite != value)
                {
                    m_image.sprite = value;
                    var color = m_image.color;
                    color.a = value != null ? m_initialColor.a : 0;
                    m_image.color = color;

                    m_onSpriteChanged.Invoke(value?.texture);
                }
            }
        }

        public Texture2D Texture
        {
            get { return m_image?.sprite?.texture; }
        }

        protected override void Reset()
        {
            base.Reset();
            if (m_image == null)
            {
                m_image = GetComponent<Image>();
            }
            if (m_image != null)
            {
                m_initialColor = m_image.color;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if(m_image == null)
            {
                m_image = GetComponent<Image>();
            }
        }

        public override void Clear()
        {
            Sprite = null;
        }

        // Use this for initialization
        protected override void Start()
        {
            base.Start();
            if (m_image == null)
            {
                m_image = GetComponent<Image>();
            }
        }

        public void SaveDefaults()
        {
            if (m_image == null)
            {
                m_image = GetComponent<Image>();
            }
            m_initialColor = m_image.color;
        }

        public override void ResetToDefaults()
        {
            if (m_image != null)
            {
                m_image.sprite = null;
                m_image.color = m_initialColor;
            }
        }
    }
}