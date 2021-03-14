using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.LayoutSystem
{

    [RequireComponent(typeof(RawImage))]
    [AddComponentMenu("WEAVR/Layout System/Layout Generic Image")]
    public class LayoutGenericImage : BaseLayoutItem
    {
        [Space]
        [SerializeField]
        [Draggable]
        [Button(nameof(SaveDefaults), "Save")]
        protected RawImage m_image;

        [SerializeField]
        [Button(nameof(ResetToDefaults), "Reset")]
        protected bool m_keepSize = false;

        [Space]
        [SerializeField]
        protected UnityEventTexture2D m_onTextureChanged;

        [SerializeField]
        [HideInInspector]
        private AspectRatioFitter m_aspectRatioFitter;

        [SerializeField]
        [HideInInspector]
        private ShadowRectTransformData m_shadowTransformData;

        public Texture2D Texture
        {
            get { return m_image?.texture as Texture2D; }
            set
            {
                if(m_image != null && m_image.texture != value)
                {
                    m_image.texture = value;
                    m_image.enabled = value != null;
                    
                    RestoreShadowTransform();
                    if(value != null && RatioFitter != null && value.height > 0 && value.width > 0)
                    {
                        if (m_keepSize && ShadowTransformData.isValidRect)
                        {
                            SmartResize(value);
                        }
                        else
                        {
                            RatioFitter.aspectRatio = (float)value.width / value.height;
                        }
                    }
                    m_onTextureChanged.Invoke(value);
                }
            }
        }

        private void SmartResize(Texture2D value)
        {
            if(value.width / ShadowTransformData.rect.width > value.height / ShadowTransformData.rect.height)
            {
                RatioFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            }
            else
            {
                RatioFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            }
            RatioFitter.aspectRatio = (float)value.width / value.height;
        }

        public AspectRatioFitter RatioFitter
        {
            get
            {
                if(m_aspectRatioFitter == null)
                {
                    m_aspectRatioFitter = GetComponent<AspectRatioFitter>();
                }
                return m_aspectRatioFitter;
            }
        }

        protected ShadowRectTransformData ShadowTransformData
        {
            get
            {
                if (m_shadowTransformData == null && RatioFitter != null)
                {
                    m_shadowTransformData = new ShadowRectTransformData();
                    m_shadowTransformData.Snapshot(transform as RectTransform);
                }
                return m_shadowTransformData;
            }
        }

        protected virtual void SnapshotShadowTransform()
        {
            ShadowTransformData?.Snapshot(transform as RectTransform);
        }

        protected virtual void RestoreShadowTransform()
        {
            if(RatioFitter != null && ShadowTransformData != null)
            {
                RatioFitter.aspectRatio = ShadowTransformData.aspectRatio;
                ShadowTransformData.Restore(transform as RectTransform);
            }
        }

        protected override void Reset()
        {
            base.Reset();
            if (m_image == null)
            {
                m_image = GetComponent<RawImage>();
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if(m_image == null)
            {
                m_image = GetComponent<RawImage>();
            }
            if(m_aspectRatioFitter == null)
            {
                m_aspectRatioFitter = GetComponent<AspectRatioFitter>();
                SnapshotShadowTransform();
            }
            if(RatioFitter != null && Texture == null)
            {
                SnapshotShadowTransform();
            }
        }

        public override void Clear()
        {
            Texture = null;
        }

        // Use this for initialization
        protected override void Start()
        {
            base.Start();
            OnValidate();
        }

        public void SaveDefaults()
        {
            if (m_image == null)
            {
                m_image = GetComponent<RawImage>();
            }
            if (m_aspectRatioFitter == null)
            {
                m_aspectRatioFitter = GetComponent<AspectRatioFitter>();
                SnapshotShadowTransform();
            }
            Texture = null;
        }

        public override void ResetToDefaults()
        {
            if (m_image != null)
            {
                Texture = null;
            }
        }

        [Serializable]
        protected class ShadowRectTransformData
        {
            public Vector2 anchoredPosition;
            public Vector3 anchoredPosition3D;
            public Vector2 anchorMax;
            public Vector2 anchorMin;
            public Vector2 offsetMax;
            public Vector2 offsetMin;
            public Vector2 pivot;
            public Vector2 sizeDelta;
            public Rect rect;
            public float aspectRatio;
            public bool isValidRect;

            public void Snapshot(RectTransform transform)
            {
                anchoredPosition    = transform.anchoredPosition;
                anchoredPosition3D  = transform.anchoredPosition3D;
                anchorMax           = transform.anchorMax;
                anchorMin           = transform.anchorMin;
                offsetMax           = transform.offsetMax;
                offsetMin           = transform.offsetMin;
                pivot               = transform.pivot;
                sizeDelta           = transform.sizeDelta;
                rect                = transform.rect;

                isValidRect = rect.width > 0 && rect.height > 0;

                if (isValidRect)
                {
                    aspectRatio = rect.width / rect.height;
                }
            }

            public void Restore(RectTransform transform)
            {
                transform.anchoredPosition    = anchoredPosition;
                transform.anchoredPosition3D  = anchoredPosition3D;
                transform.anchorMax           = anchorMax;
                transform.anchorMin           = anchorMin;
                transform.offsetMax           = offsetMax;
                transform.offsetMin           = offsetMin;
                transform.pivot               = pivot;
                transform.sizeDelta           = sizeDelta;
            }
        }
    }
}