using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TXT.WEAVR.Core.DataTypes
{
    public static class SpecialStateContainers
    {
        public readonly static Dictionary<Type, Func<Component, AbstractComponentState>> SpecialComponentStates = new Dictionary<Type, Func<Component, AbstractComponentState>>()
        {
            { typeof(Transform), c => new TransformState() },
            { typeof(Rigidbody), c => new RigidBodyState() },
            { typeof(Camera), c => new CameraState() },
            { typeof(Renderer), c => new RendererState() },
            { typeof(MeshRenderer), c => new RendererState() },
            { typeof(SkinnedMeshRenderer), c => new RendererState() },
        };

        public static void Register(Type type, Func<Component, AbstractComponentState> constructor)
        {
            SpecialComponentStates[type] = constructor;
        }

        public static void Register<T>(Func<Component, SpecialComponentState<T>> constructor) where T : Component
        {
            SpecialComponentStates[typeof(T)] = constructor;
        }
    }

    public class TransformState : SpecialComponentState<Transform>
    {
        private bool m_active;
        private Vector3 m_localPosition;
        private Quaternion m_localRotation;
        private Vector3 m_localScale;
        private Transform m_parent;

        protected override bool Restore(Transform t)
        {
            t.gameObject.SetActive(m_active);
            t.localPosition = m_localPosition;
            t.localRotation = m_localRotation;
            t.localScale = m_localScale;
            t.parent = m_parent;

            return true;
        }

        public override bool Snapshot()
        {
            m_active = m_component.gameObject.activeSelf;
            m_localPosition = m_component.localPosition;
            m_localRotation = m_component.localRotation;
            m_localScale = m_component.localScale;
            m_parent = m_component.parent;
            return true;
        }
    }

    public class RigidBodyState : SpecialComponentState<Rigidbody>
    {
        private float m_angularDrag;
        private Vector3 m_angularVelocity;
        private Vector3 m_centerOfMass;
        private CollisionDetectionMode m_collisionDetectionMode;
        private RigidbodyConstraints m_constraints;
        private bool m_detectCollisions;
        private float m_drag;
        private bool m_freezeRotation;
        //private Vector3 m_inertiaTensor;
        //private Quaternion m_inertiaTensorRotation;
        private RigidbodyInterpolation m_interpolation;
        private bool m_isKinematic;
        private float m_mass;
        private float m_maxAngularVelocity;
        private float m_maxDepenetrationVelocity;
        private Vector3 m_position;
        private Quaternion m_rotation;
        private float m_sleepThreshold;
        private int m_solverIterations;
        private int m_solverVelocityIterations;
        private bool m_useGravity;
        private Vector3 m_velocity;

        protected override bool Restore(Rigidbody r)
        {
            r.angularDrag = m_angularDrag;
            r.angularVelocity = m_angularVelocity;
            r.centerOfMass = m_centerOfMass;
            r.collisionDetectionMode = m_collisionDetectionMode;
            r.constraints = m_constraints;
            r.detectCollisions = m_detectCollisions;
            r.drag = m_drag;
            r.freezeRotation = m_freezeRotation;
            //r.inertiaTensor             = m_inertiaTensor           ;
            //r.inertiaTensorRotation     = m_inertiaTensorRotation   ;
            r.interpolation = m_interpolation;
            r.isKinematic = m_isKinematic;
            r.mass = m_mass;
            r.maxAngularVelocity = m_maxAngularVelocity;
            r.maxDepenetrationVelocity = m_maxDepenetrationVelocity;
            r.position = m_position;
            r.rotation = m_rotation;
            r.sleepThreshold = m_sleepThreshold;
            r.solverIterations = m_solverIterations;
            r.solverVelocityIterations = m_solverVelocityIterations;
            r.useGravity = m_useGravity;
            r.velocity = m_velocity;

            return true;
        }

        public override bool Snapshot()
        {
            m_angularDrag = m_component.angularDrag;
            m_angularVelocity = m_component.angularVelocity;
            m_centerOfMass = m_component.centerOfMass;
            m_collisionDetectionMode = m_component.collisionDetectionMode;
            m_constraints = m_component.constraints;
            m_detectCollisions = m_component.detectCollisions;
            m_drag = m_component.drag;
            m_freezeRotation = m_component.freezeRotation;
            //m_inertiaTensor             = m_component.inertiaTensor;
            //m_inertiaTensorRotation     = m_component.inertiaTensorRotation;
            m_interpolation = m_component.interpolation;
            m_isKinematic = m_component.isKinematic;
            m_mass = m_component.mass;
            m_maxAngularVelocity = m_component.maxAngularVelocity;
            m_maxDepenetrationVelocity = m_component.maxDepenetrationVelocity;
            m_position = m_component.position;
            m_rotation = m_component.rotation;
            m_sleepThreshold = m_component.sleepThreshold;
            m_solverIterations = m_component.solverIterations;
            m_solverVelocityIterations = m_component.solverVelocityIterations;
            m_useGravity = m_component.useGravity;
            m_velocity = m_component.velocity;
            return true;
        }
    }

    public class CameraState : SpecialComponentState<Camera>
    {
        private float m_aspect;
        private Color m_backgroundColor;
        private int m_cullingMask;
        private float m_depth;
        private bool m_enabled;
        private float m_farClipPlane;
        private float m_fieldOfView;
        private float m_nearClipPlane;
        private Rect m_rect;
        private int m_targetDisplay;

        protected override bool Restore(Camera c)
        {
            c.aspect = m_aspect;
            c.backgroundColor = m_backgroundColor;
            c.cullingMask = m_cullingMask;
            c.depth = m_depth;
            c.enabled = m_enabled;
            c.farClipPlane = m_farClipPlane;
            c.fieldOfView = m_fieldOfView;
            c.nearClipPlane = m_nearClipPlane;
            c.rect = m_rect;
            c.targetDisplay = m_targetDisplay;
            return true;
        }

        public override bool Snapshot()
        {
            m_aspect = m_component.aspect;
            m_backgroundColor = m_component.backgroundColor;
            m_cullingMask = m_component.cullingMask;
            m_depth = m_component.depth;
            m_enabled = m_component.enabled;
            m_farClipPlane = m_component.farClipPlane;
            m_fieldOfView = m_component.fieldOfView;
            m_nearClipPlane = m_component.nearClipPlane;
            m_rect = m_component.rect;
            m_targetDisplay = m_component.targetDisplay;
            return true;
        }

    }

    public class RendererState : SpecialComponentState<Renderer>
    {
        private uint m_renderingLayerMask;
        private int m_rendererPriority;
        private string m_sortingLayerName;
        private int m_sortingLayerID;
        private int m_sortingOrder;
        private Material[] m_materials;
        private Material m_material;
        private MaterialData[] m_materialsData;
        private MaterialData m_materialData;
        private bool m_receiveShadows;
        private bool m_enabled;

        protected override bool Restore(Renderer r)
        {
            r.renderingLayerMask = m_renderingLayerMask;
            r.rendererPriority = m_rendererPriority;
            r.sortingLayerName = m_sortingLayerName;
            r.sortingLayerID = m_sortingLayerID;
            r.sortingOrder = m_sortingOrder;
            r.receiveShadows = m_receiveShadows;
            r.enabled = m_enabled;

            //r.materials = m_materials;
            //r.material = m_material;
            if (Application.isPlaying)
            {
                Material[] materials = new Material[m_materialsData.Length];
                if (r.materials.Length != m_materialsData.Length)
                {
                    r.materials = new Material[m_materialsData.Length];
                }
                for (int i = 0; i < m_materialsData.Length; i++)
                {
                    materials[i] = m_materialsData[i].Restore();
                }
                r.materials = materials;
            }
            else
            {
                var materials = r.sharedMaterials;
                if (r.sharedMaterials.Length != m_materialsData.Length)
                {
                    materials = new Material[m_materialsData.Length];
                }
                for (int i = 0; i < m_materialsData.Length; i++)
                {
                    materials[i] = m_materialsData[i].Restore();
                }
                r.sharedMaterials = materials;
            }
            //m_materialData.Restore(r.material);
            return true;
        }

        public override bool Snapshot()
        {
            m_renderingLayerMask = m_component.renderingLayerMask;
            m_rendererPriority = m_component.rendererPriority;
            m_sortingLayerName = m_component.sortingLayerName;
            m_sortingLayerID = m_component.sortingLayerID;
            m_sortingOrder = m_component.sortingOrder;

            m_materials = Application.isPlaying ? m_component.materials : m_component.sharedMaterials.Select(m => new Material(m)).ToArray();
            //m_material = m_component.material;

            m_receiveShadows = m_component.receiveShadows;
            m_enabled = m_component.enabled;

            m_materialsData = new MaterialData[m_materials.Length];
            for (int i = 0; i < m_materialsData.Length; i++)
            {
                m_materialsData[i].Snapshot(m_materials[i]);
            }
            //m_materialData.Snapshot(m_component.material);

            return true;
        }
    }

    public struct MaterialData
    {
        int m_renderQueue;
        Material m_mat;
        Vector2 m_mainTextureScale;
        Vector2 m_mainTextureOffset;
        Texture m_mainTexture;
        Color m_color;
        int m_mode;

        //string[]    m_shaderKeywords;

        public Material material => m_mat;

        public void Snapshot(Material mat)
        {
            m_mat = mat;
            m_renderQueue = mat.renderQueue;
            if (mat.HasProperty("_MainTex"))
            {
                m_mainTextureScale = mat.mainTextureScale;
                m_mainTextureOffset = mat.mainTextureOffset;
                m_mainTexture = mat.mainTexture;
            }
            else if (mat.HasProperty("_BaseTex"))
            {
                m_mainTexture = mat.GetTexture("_BaseTex");
            }
            if (mat.HasProperty("_Color"))
            {
                m_color = mat.color;
            }
            if (mat.HasProperty("_Mode"))
            {
                m_mode = mat.GetInt("_Mode");
            }
            //m_shaderKeywords = mat.shaderKeywords;
        }

        public void Restore(Material mat)
        {
            mat.renderQueue = m_renderQueue;
            if (mat.HasProperty("_MainTex"))
            {
                mat.mainTextureScale = m_mainTextureScale;
                mat.mainTextureOffset = m_mainTextureOffset;
                mat.mainTexture = m_mainTexture;
            }
            else if (mat.HasProperty("_BaseTex"))
            {
                mat.SetTexture("_BaseTex", m_mainTexture);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.color = m_color;
            }
            if (mat.HasProperty("_Mode"))
            {
                mat.SetInt("_Mode", m_mode);
            }
            //mat.shaderKeywords = m_shaderKeywords;
        }

        public Material Restore()
        {
            m_mat.renderQueue = m_renderQueue;
            if (m_mat.HasProperty("_MainTex"))
            {
                m_mat.mainTextureScale = m_mainTextureScale;
                m_mat.mainTextureOffset = m_mainTextureOffset;
                m_mat.mainTexture = m_mainTexture;
            }
            else if (m_mat.HasProperty("_BaseTex"))
            {
                m_mat.SetTexture("_BaseTex", m_mainTexture);
            }
            if (m_mat.HasProperty("_Color"))
            {
                m_mat.color = m_color;
            }
            if (m_mat.HasProperty("_Mode"))
            {
                m_mat.SetInt("_Mode", m_mode);
            }
            //mat.shaderKeywords = m_shaderKeywords;

            return m_mat;
        }

        public static implicit operator Material(MaterialData data)
        {
            return data.Restore();
        }
    }
}
