using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Tweening
{
    [AddComponentMenu("WEAVR/UI/Animations/Rotation")]
    public class Tween_Rotation : MonoBehaviour
    {
        [Tooltip("The axis to rotate around")]
        public Vector3 axis = new Vector3(0, 0, -1);
        [Tooltip("Degrees per second")]
        public float speed = 180;
        public bool limited;
        [HiddenBy(nameof(limited))]
        public float minDegrees;
        [HiddenBy(nameof(limited))]
        public float maxDegrees;
        [HiddenBy(nameof(limited))]
        public bool alternating;

        private Transform m_targetTransform;

        private float m_target;
        private float m_currentRotation;

        public float CurrentRotation
        {
            get => m_currentRotation;
            set
            {
                if(m_currentRotation != value)
                {
                    float deltaRotation = value - m_currentRotation;
                    m_currentRotation = value;
                    m_targetTransform.Rotate(axis, deltaRotation);
                }
            }
        }

        private void OnEnable()
        {
            if (limited) {
                CurrentRotation = Mathf.Clamp(CurrentRotation, minDegrees, maxDegrees);
                m_target = minDegrees;
            }
            if (!m_targetTransform)
            {
                m_targetTransform = transform;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (limited)
            {
                if (CurrentRotation >= maxDegrees)
                {
                    m_target = alternating ? maxDegrees : minDegrees;
                }
                else if (CurrentRotation <= minDegrees)
                {
                    m_target = alternating ? minDegrees : maxDegrees;
                }
                CurrentRotation = Mathf.MoveTowards(CurrentRotation, m_target, Time.deltaTime * speed);
            }
            else
            {
                CurrentRotation += speed * Time.deltaTime;
            }
        }
    }
}
