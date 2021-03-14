using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace TXT.WEAVR.Common
{
    [ExecuteAlways]
    [AddComponentMenu("WEAVR/Components/Keep Aspect Ratio Scale")]
    public class KeepAspectRatioScale : MonoBehaviour
    {
        public bool continuousUpdate = true;
        public Axis scaleAxis;

        private Vector3 m_targetScale;
        private Vector3 m_prevLocalScale;

        private void OnEnable()
        {
            m_prevLocalScale = transform.localScale;
            m_targetScale = transform.localScale;
            UpdateScale();
        }

        public void UpdateScale()
        {
            if (!enabled) { return; }
            var lossyScale = transform.lossyScale;
            var localScale = transform.localScale;

            if (scaleAxis != Axis.None)
            {
                switch (scaleAxis)
                {
                    case Axis.X:
                        m_targetScale *= m_prevLocalScale[0] / localScale[0];
                        break;
                    case Axis.Y:
                        m_targetScale *= m_prevLocalScale[1] / localScale[1];
                        break;
                    case Axis.Z:
                        m_targetScale *= m_prevLocalScale[2] / localScale[2];
                        break;
                    case Axis.X | Axis.Y:
                        m_targetScale *= Mathf.Min(m_prevLocalScale[0] / localScale[0], m_prevLocalScale[1] / localScale[1]);
                        break;
                    case Axis.Z | Axis.Y:
                        m_targetScale *= Mathf.Min(m_prevLocalScale[2] / localScale[2], m_prevLocalScale[1] / localScale[1]);
                        break;
                    case Axis.X | Axis.Z:
                        m_targetScale *= Mathf.Min(m_prevLocalScale[0] / localScale[0], m_prevLocalScale[2] / localScale[2]);
                        break;
                    case Axis.X | Axis.Y | Axis.Z:
                        m_targetScale *= Mathf.Min(m_prevLocalScale[0] / localScale[0], m_prevLocalScale[1] / localScale[1], m_prevLocalScale[2] / localScale[2]);
                        break;
                }
            }

            localScale.x = ValidOnly(localScale.x * m_targetScale.x / lossyScale.x, localScale.x);
            localScale.y = ValidOnly(localScale.y * m_targetScale.y / lossyScale.y, localScale.y);
            localScale.z = ValidOnly(localScale.z * m_targetScale.z / lossyScale.z, localScale.z);

            transform.localScale = localScale;
        }

        private float ValidOnly(float v, float fallback)
        {
            return float.IsInfinity(v) || float.IsNaN(v) ? fallback : v;
        }

        private float Rescale(int axis, Vector3 localScale, Vector3 lossyScale, Vector3 factor)
        {
            return lossyScale[axis] != 0 ? localScale[axis] / lossyScale[axis] * factor[axis] : localScale[axis];
        }

        private void LateUpdate()
        {
            if (continuousUpdate)
            {
                UpdateScale();
            }
        }

        private void OnDisable()
        {
            transform.localScale = m_prevLocalScale;
        }
    }
}
