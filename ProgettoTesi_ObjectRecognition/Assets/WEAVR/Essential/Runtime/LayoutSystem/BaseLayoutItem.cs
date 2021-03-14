using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.LayoutSystem
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class BaseLayoutItem : MonoBehaviour
    {
        [Draggable]
        public LayoutContainer container;
        [Draggable]
        public Texture2D layoutPreviewImage;

        private RectTransform m_rectTransform;

        public RectTransform RectTransform
        {
            get
            {
                if(m_rectTransform == null)
                {
                    m_rectTransform = transform as RectTransform;
                }
                return m_rectTransform;
            }
        }

        public abstract void Clear();

        public abstract void ResetToDefaults();

        protected virtual void Reset()
        {
            GetComponentInParent<LayoutContainer>()?.UpdateList();
        }

        protected virtual void Start() { }

        protected virtual void OnValidate() { }
    }

}
