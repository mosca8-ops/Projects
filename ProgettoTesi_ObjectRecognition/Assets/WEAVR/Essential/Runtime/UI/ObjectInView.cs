using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.UI
{
    [AddComponentMenu("WEAVR/UI/Object In View")]
    public class ObjectInView : MonoBehaviour
    {
        public Camera UIObjectCamera;
        public float xOffset;
        public float yOffset;

        private float m_oldAspect;

        private void OnValidate()
        {
            if (UIObjectCamera)
            {
                ResetPosition();
                m_oldAspect = UIObjectCamera.aspect;
            }
        }

        void Start()
        {
            if (UIObjectCamera)
            {
                ResetPosition();
                m_oldAspect = UIObjectCamera.aspect;
            }
        }

        void Update()
        {
            if (UIObjectCamera && UIObjectCamera.aspect != m_oldAspect)
            {
                ResetPosition();
                m_oldAspect = UIObjectCamera.aspect;
            }
        }

        private void ResetPosition()
        {
            if (UIObjectCamera)
            {
                Vector3 pos = UIObjectCamera.WorldToViewportPoint(transform.position);
                pos.x = Mathf.Clamp01(xOffset);
                pos.y = Mathf.Clamp01(yOffset);
                transform.position = UIObjectCamera.ViewportToWorldPoint(pos);
            }
        }
    }
}

