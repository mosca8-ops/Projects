using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using static TXT.WEAVR.Pose;

namespace TXT.WEAVR.Procedure
{

    public class DeltaMoveBlock : ComponentAnimation<Transform>
    {
        private enum Space { World, Local }
        [SerializeField]
        [HideInInspector]
        private Space m_space = Space.World;
        [SerializeField]
        [Tooltip("The amount of movement to apply")]
        [UseHandle(HandleType.Position)]
        private OptionalVector3 m_moveBy;
        [SerializeField]
        [Tooltip("The amount of rotation to apply")]
        private OptionalVector3 m_rotateBy;
        //[ShowAsEuler]
        [SerializeField]
        [UseHandle(HandleType.Rotation)]
        [ShowIf(nameof(ShowRotation))]
        //[HideInInspector]
        private OptionalQuaternion m_deltaRotation = Quaternion.identity;
        [SerializeField]
        [Tooltip("The amount of scale to apply")]
        [UseHandle(HandleType.Scale)]
        private OptionalVector3 m_scaleBy;

        [SerializeField]
        [HideInInspector]
        private Vector3 m_axis;
        [SerializeField]
        [HideInInspector]
        private float m_angle;

        private Vector3 m_moveVector;
        private Quaternion m_lastRotation;
        private Vector3 m_lastEuler;

        public Vector3? MoveBy
        {
            get => m_moveBy.enabled ? m_moveBy.value : (Vector3?)null;
            set
            {
                if(m_moveBy.enabled != value.HasValue || (value.HasValue && m_moveBy.value != value.Value))
                {
                    BeginChange();
                    m_moveBy.enabled = value.HasValue;
                    if (value.HasValue)
                    {
                        m_moveBy.value = value.Value;
                    }
                    PropertyChanged(nameof(MoveBy));
                }
            }
        }

        public Vector3? RotateBy
        {
            get => m_rotateBy.enabled ? m_rotateBy.value : (Vector3?)null;
            set
            {
                if (m_rotateBy.enabled != value.HasValue || (value.HasValue && m_rotateBy.value != value.Value))
                {
                    BeginChange();
                    m_rotateBy.enabled = value.HasValue;
                    if (value.HasValue)
                    {
                        m_rotateBy.value = value.Value;
                    }
                    PropertyChanged(nameof(RotateBy));
                }
            }
        }

        public Vector3? ScaleBy
        {
            get => m_scaleBy.enabled ? m_scaleBy.value : (Vector3?)null;
            set
            {
                if (m_scaleBy.enabled != value.HasValue || (value.HasValue && m_scaleBy.value != value.Value))
                {
                    BeginChange();
                    m_scaleBy.enabled = value.HasValue;
                    if (value.HasValue)
                    {
                        m_scaleBy.value = value.Value;
                    }
                    PropertyChanged(nameof(ScaleBy));
                }
            }
        }

        public override bool CanProvide<T>()
        {
            return false;
        }

        private bool ShowRotation() => false;


        public override void OnValidate()
        {
            base.OnValidate();
            m_deltaRotation.enabled = m_rotateBy.enabled;
            if (m_rotateBy.enabled)
            {
                ComputeRotationValues();
                //NewMethod();
            }
        }


        Quaternion m_smallestQuaternion;
        float m_smallestQuant;
        private void NewMethod()
        {
            if (m_lastEuler != m_rotateBy.value)
            {
                m_deltaRotation.value = GetFromEuler(m_rotateBy.value);
            }
            else if(m_lastRotation != m_deltaRotation.value)
            {
                var lastEuler = m_lastRotation.eulerAngles;
                var nowEuler = m_deltaRotation.value.eulerAngles;
                if(Mathf.Abs(lastEuler.z - nowEuler.z) < 10f)
                {
                    m_rotateBy.value.z += nowEuler.z - lastEuler.z;
                }
                else if(Mathf.Abs(lastEuler.x - nowEuler.x) < 10f)
                {
                    m_rotateBy.value.x += nowEuler.x - lastEuler.x;
                }
                else if (Mathf.Abs(lastEuler.y - nowEuler.y) < 10f)
                {
                    m_rotateBy.value.y += nowEuler.y - lastEuler.y;
                }
                //var deltaEuler = Quaternion.RotateTowards(m_lastRotation, m_deltaRotation.value, 180).eulerAngles;
                //m_lastRotation = m_deltaRotation.value;
                //m_rotateBy.value += deltaEuler;
                
            }
            m_lastEuler = m_rotateBy.value;
            m_lastRotation = m_deltaRotation.value;
        }

        private void ComputeRotationValues()
        {
            if (m_lastEuler != m_rotateBy.value)
            {
                m_deltaRotation.value = GetFromEuler(m_rotateBy.value);
                m_deltaRotation.value.ToAngleAxis(out m_angle, out m_axis);
                m_angle = Vector3.Dot(m_rotateBy.value, m_axis);
            }
            else if (m_lastRotation != m_deltaRotation.value)
            {
                var deltaEuler = (m_deltaRotation.value * Quaternion.Inverse(m_lastRotation)).eulerAngles;
                if (deltaEuler.x >= 180 && deltaEuler.x <= 360) { deltaEuler.x -= 360; }
                else if (deltaEuler.x <= -180 && deltaEuler.x >= -360) { deltaEuler.x += 360; }
                if (Mathf.Abs(deltaEuler.x) > 90) { deltaEuler.x = 0; }
                if (deltaEuler.y >= 180 && deltaEuler.y <= 360) { deltaEuler.y -= 360; }
                else if (deltaEuler.y <= -180 && deltaEuler.y >= -360) { deltaEuler.y += 360; }
                if (Mathf.Abs(deltaEuler.y) > 90) { deltaEuler.y = 0; }
                if (deltaEuler.z >= 180 && deltaEuler.z <= 360) { deltaEuler.z -= 360; }
                else if (deltaEuler.z <= -180 && deltaEuler.z >= -360) { deltaEuler.z += 360; }
                if (Mathf.Abs(deltaEuler.z) > 90) { deltaEuler.z = 0; }

                m_rotateBy.value += deltaEuler;

                m_deltaRotation.value.ToAngleAxis(out m_angle, out m_axis);
                m_angle = Vector3.Dot(m_rotateBy.value, m_axis);
            }
            m_lastEuler = m_rotateBy.value;
            m_lastRotation = m_deltaRotation.value;
        }

        private static Quaternion GetFromEuler(Vector3 euler)
        {
            //euler = Clamp360(euler);
            return Quaternion.Euler(euler);
        }

        private static Vector3 Clamp360(Vector3 euler)
        {
            return new Vector3(euler.x % 360f, euler.y % 360f, euler.z % 360f);
        }

        private static int Rounds(Vector3 euler)
        {
            return Mathf.CeilToInt(Mathf.Max(Mathf.Abs(euler.x), Mathf.Abs(euler.y), Mathf.Abs(euler.z)) / 360f);
        }

        public override void OnStart()
        {
            base.OnStart();
            if(m_space == Space.Local)
            {
                if (m_moveBy.enabled)
                {
                    //m_moveVector = m_target.right * m_moveBy.value.x + m_target.up * m_moveBy.value.y + m_target.forward * m_moveBy.value.z;
                    m_moveVector = m_target.TransformPoint(m_moveBy.value);
                }
            }
            else
            {
                if (m_moveBy.enabled)
                {
                    m_moveVector = m_moveBy.value;
                }
            }
        }

        protected override void Animate(float delta, float normalizedValue)
        {
            if (!m_target) { return; }
            if (m_moveBy.enabled)
            {
                m_target.position += m_moveVector * delta;
            }
            if (m_rotateBy.enabled)
            {
                m_target.Rotate(m_axis, m_angle * delta);
                //m_target.rotation *= Quaternion.Euler(m_rotateBy.value * delta);
            }
            if (m_scaleBy.enabled)
            {
                m_target.localScale += m_scaleBy.value * delta;
            }
        }

        public override bool CanPreview()
        {
            return true;
        }
    }
}
