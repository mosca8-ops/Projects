using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{
    [Serializable]
    public struct Pose
    {
        private const float k_e = 0.000001f;

        public enum PoseType { World, Local }

        [SerializeField]
        private PoseType m_type;

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        private bool m_isIdentity;
        public Matrix4x4 worldToLocal;
        public Matrix4x4 localToWorld;

        public PoseType type => m_type;
        public bool isLocal => m_type == PoseType.Local;
        public bool isWorld => m_type == PoseType.World;

        public Vector3 euler
        {
            get => rotation.eulerAngles;
            set => rotation.eulerAngles = value;
        }

        public Vector3 worldPosition
        {
            get
            {
                if (m_type == PoseType.World || m_isIdentity)
                {
                    return position;
                }
                else
                {
                    return localToWorld * position;
                }
            }
            set
            {
                if (m_type != PoseType.World && !m_isIdentity)
                {
                    value = localToWorld * value;
                }
                if (position != value)
                {
                    position = value;
                }
            }
        }

        public Quaternion worldRotation
        {
            get
            {
                if (m_type == PoseType.World || m_isIdentity)
                {
                    return rotation;
                }
                else
                {
                    return Quaternion.Euler(localToWorld * rotation.eulerAngles);
                }
            }
            set
            {
                if (m_type != PoseType.World && !m_isIdentity)
                {
                    value.eulerAngles = localToWorld * value.eulerAngles;
                }
                if (rotation != value)
                {
                    rotation = value;
                }
            }
        }

        public Vector3 worldScale
        {
            get
            {
                if (m_type == PoseType.World || m_isIdentity)
                {
                    return scale;
                }
                else
                {
                    return localToWorld * scale;
                }
            }
            set
            {
                if (m_type != PoseType.World && !m_isIdentity)
                {
                    value = localToWorld * value;
                }
                if (scale != value)
                {
                    scale = value;
                }
            }
        }

        public Vector3 localPosition
        {
            get
            {
                if (m_type == PoseType.Local || m_isIdentity)
                {
                    return position;
                }
                else
                {
                    return worldToLocal * position;
                }
            }
            set
            {
                if (m_type != PoseType.Local && !m_isIdentity)
                {
                    value = worldToLocal * value;
                }
                if (position != value)
                {
                    position = value;
                }
            }
        }

        public Quaternion localRotation
        {
            get
            {
                if (m_type == PoseType.Local || m_isIdentity)
                {
                    return rotation;
                }
                else
                {
                    return Quaternion.Euler(worldToLocal * rotation.eulerAngles);
                }
            }
            set
            {
                if (m_type != PoseType.Local && !m_isIdentity)
                {
                    value.eulerAngles = worldToLocal * value.eulerAngles;
                }
                if (rotation != value)
                {
                    rotation = value;
                }
            }
        }

        public Vector3 localScale
        {
            get
            {
                if (m_type == PoseType.Local || m_isIdentity)
                {
                    return scale;
                }
                else
                {
                    return worldToLocal * scale;
                }
            }
            set
            {
                if (m_type != PoseType.Local && !m_isIdentity)
                {
                    value = worldToLocal * value;
                }
                if (scale != value)
                {
                    scale = value;
                }
            }
        }

        public static readonly Pose DefaultLocal = new Pose(PoseType.Local);
        public static readonly Pose DefaultWorld = new Pose(PoseType.World);

        public Pose(PoseType type)
        {
            m_type = type;
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;

            localToWorld = Matrix4x4.identity;
            worldToLocal = Matrix4x4.identity;
            m_isIdentity = true;
        }

        public Pose(Transform t, PoseType type = PoseType.Local)
        {
            m_type = type;
            switch (type)
            {
                case PoseType.Local:
                    position = t.localPosition;
                    rotation = t.localRotation;
                    scale = t.localScale;
                    break;
                case PoseType.World:
                    position = t.position;
                    rotation = t.rotation;
                    scale = t.lossyScale;
                    break;
                default:
                    position = Vector3.zero;
                    rotation = Quaternion.identity;
                    scale = Vector3.one;
                    break;
            }

            worldToLocal = t.worldToLocalMatrix;
            localToWorld = t.localToWorldMatrix;
            m_isIdentity = !t.parent;
        }

        public void UpdateFrom(Transform t)
        {
            switch (m_type)
            {
                case PoseType.Local:
                    position = t.localPosition;
                    rotation = t.localRotation;
                    scale = t.localScale;
                    break;
                case PoseType.World:
                    position = t.position;
                    rotation = t.rotation;
                    scale = t.lossyScale;
                    break;
                default:
                    position = Vector3.zero;
                    rotation = Quaternion.identity;
                    scale = Vector3.one;
                    break;
            }
        }

        public void ApplyTo(Transform t)
        {
            switch (m_type)
            {
                case PoseType.Local:
                    t.localPosition = position;
                    t.localRotation = rotation;
                    t.localScale = scale;
                    break;
                case PoseType.World:
                    t.position = position;
                    t.rotation = rotation;
                    t.localScale = scale;
                    break;
            }
        }

        public void ApplyEnabledTo(Transform t)
        {
            bool wasActive = t.gameObject.activeSelf;
            t.gameObject.SetActive(true);
            ApplyTo(t);
            t.gameObject.SetActive(wasActive);
        }

        public void ApplyAsOffsetTo(Transform t)
        {
            switch (m_type)
            {
                case PoseType.Local:
                    t.localPosition += position;
                    t.localRotation *= rotation;
                    t.localScale = Vector3.Scale(t.localScale, scale);
                    break;
                case PoseType.World:
                    t.position += position;
                    t.rotation *= rotation;
                    t.localScale = Vector3.Scale(t.localScale, scale);
                    break;
            }
        }

        public static Pose operator +(Pose pose, Vector3 position)
        {
            pose.position += position;
            return pose;
        }

        public static Pose operator -(Pose pose, Vector3 position)
        {
            pose.position -= position;
            return pose;
        }

        public static Pose operator *(Pose pose, Vector3 scale)
        {
            pose.scale.Scale(scale);
            return pose;
        }

        public static Pose operator /(Pose pose, Vector3 scale)
        {
            pose.scale.Scale(Invert(scale));
            return pose;
        }

        public static Pose operator +(Pose poseA, Pose poseB)
        {
            if(poseA.type == poseB.type)
            {
                poseA.position += poseB.position;
                poseA.rotation *= poseB.rotation;
                poseA.scale.Scale(poseB.scale);
            }
            else
            {
                switch (poseA.type)
                {
                    case PoseType.Local:
                        poseA.position += poseB.localPosition;
                        poseA.rotation *= poseB.localRotation;
                        poseA.scale.Scale(poseB.localScale);
                        break;
                    case PoseType.World:
                        poseA.position += poseB.worldPosition;
                        poseA.rotation *= poseB.worldRotation;
                        poseA.scale.Scale(poseB.worldScale);
                        break;
                }
            }
            return poseA;
        }

        public static Pose operator -(Pose poseA, Pose poseB)
        {
            if (poseA.type == poseB.type)
            {
                poseA.position -= poseB.position;
                poseA.rotation = poseB.rotation * poseA.rotation;
                poseA.scale.Scale(poseB.scale);
            }
            else
            {
                switch (poseA.type)
                {
                    case PoseType.Local:
                        poseA.position += poseB.localPosition;
                        poseA.rotation = poseB.localRotation * poseA.rotation;
                        poseA.scale.Scale(Invert(poseB.localScale));
                        break;
                    case PoseType.World:
                        poseA.position += poseB.worldPosition;
                        poseA.rotation = poseB.worldRotation * poseA.rotation;
                        poseA.scale.Scale(Invert(poseB.worldScale));
                        break;
                }
            }
            return poseA;
        }

        private static Vector3 Invert(Vector3 v)
        {
            return new Vector3( v.x <= -k_e && k_e <= v.x ? 1f / v.x : 0,
                                v.y <= -k_e && k_e <= v.y ? 1f / v.y : 0,
                                v.z <= -k_e && k_e <= v.z ? 1f / v.z : 0);
        }


        public static Pose Lerp(Pose a, Pose b, float t)
        {
            if (a.type == b.type)
            {
                a.position = Vector3.Lerp(a.position, b.position, t);
                a.rotation = Quaternion.Lerp(a.rotation, b.rotation, t);
                a.scale = Vector3.Lerp(a.scale, b.scale, t);
            }
            else
            {
                switch (a.type)
                {
                    case PoseType.Local:
                        a.position = Vector3.Lerp(a.position, b.localPosition, t);
                        a.rotation = Quaternion.Lerp(a.rotation, b.localRotation, t);
                        a.scale = Vector3.Lerp(a.scale, b.localScale, t);
                        break;
                    case PoseType.World:
                        a.position = Vector3.Lerp(a.position, b.worldPosition, t);
                        a.rotation = Quaternion.Lerp(a.rotation, b.worldRotation, t);
                        a.scale = Vector3.Lerp(a.scale, b.worldScale, t);
                        break;
                }
            }
            return a;
        }
    }
}