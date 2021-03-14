using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    public class PoseHandler : IDisposable
    {
        public const string k_GameObjectName = "___PoseHandler___";
        private Transform m_root;
        private Renderer[] m_renderers;
        private float m_alpha;

        public Transform Root => m_root;
        public float Alpha => m_alpha;

        public Pose StartPose { get; set; }
        public Pose EndPose { get; set; }

        public PoseHandler(Transform root, float transparency = 0.25f)
        {
            m_root = Object.Instantiate(root);
            m_root.gameObject.hideFlags = HideFlags.HideAndDontSave;
            m_root.gameObject.name = k_GameObjectName;
            m_alpha = transparency;
            m_renderers = m_root.GetComponentsInChildren<Renderer>();

            foreach (var renderer in m_renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => new Material(m)).ToArray();
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    //renderer.materials[i] = new Material(renderer.sharedMaterials[i]);
                    renderer.sharedMaterials[i].MakeTransparent(m_alpha);
                }
                //renderer.MakeTransparent(m_alpha);
            }
        }

        private void ChangeTransparency(float m_alpha)
        {
            foreach (var renderer in m_renderers)
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    renderer.sharedMaterials[i].ChangeAlpha(m_alpha);
                }
                //renderer.ChangeAlpha(m_alpha);
            }
        }

        public void ApplyChangesToTransform()
        {
            m_root.ApplyPose(EndPose);
        }

        public void ResetTransform()
        {
            m_root.ApplyPose(StartPose);
        }

        public void Dispose()
        {
            if (m_root)
            {
                if (Application.isPlaying)
                {
                    foreach (var renderer in m_renderers)
                    {
                        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                        {
                            Object.DestroyImmediate(renderer.sharedMaterials[i], true);
                        }
                    }
                    Object.Destroy(m_root.gameObject);
                }
                else
                {
                    foreach (var renderer in m_renderers)
                    {
                        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                        {
                            Object.DestroyImmediate(renderer.sharedMaterials[i], true);
                        }
                    }
                    Object.DestroyImmediate(m_root.gameObject);
                }
            }
        }
    }

    public class TempGameObject : IDisposable
    {
        public const string k_GameObjectName = "___TempGameObject___";
        private GameObject m_gameObject;
        private HashSet<Material> m_materials;
        private Renderer[] m_renderers;
        private float m_alpha;

        public GameObject GameObject => m_gameObject;
        public float Alpha => m_alpha;

        private bool m_isVisible;
        public bool IsVisible
        {
            get => m_isVisible;
            set
            {
                if(m_isVisible != value)
                {
                    m_isVisible = value;
                    foreach(var renderer in m_renderers)
                    {
                        renderer.enabled = value;
                    }
                }
            }
        }

        public TempGameObject(GameObject gameObject, float transparency = 0.5f, bool noShadows = true, bool resetPose = false)
        {
            m_gameObject = UniqueIDMonitor.Instantiate(gameObject);
            m_gameObject.hideFlags = HideFlags.HideAndDontSave;
            m_gameObject.name = k_GameObjectName;
            if (resetPose)
            {
                m_gameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                m_gameObject.transform.localScale = Vector3.one;
            }
            m_alpha = transparency;

            m_materials = new HashSet<Material>();

            m_isVisible = false;
            m_renderers = m_gameObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in m_renderers)
            {
                var sharedMaterials = renderer.sharedMaterials.Select(m => Clone(m)).ToArray();
                try
                {
                    for (int i = 0; i < sharedMaterials.Length; i++)
                    {
                        if (noShadows)
                        {
                            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                            renderer.receiveShadows = false;
                        }
                        sharedMaterials[i].MakeTransparent(m_alpha);
                        m_materials.Add(sharedMaterials[i]);
                    }
                }
                finally
                {
                    renderer.sharedMaterials = sharedMaterials;
                }
                //renderer.MakeTransparent(m_alpha);
            }
        }

        private Material Clone(Material material)
        {
            var newMat = new Material(material);
            newMat.name += "_temp";
            newMat.CopyPropertiesFromMaterial(material);
            return newMat;
        }

        private void ChangeTransparency(float m_alpha)
        {
            foreach (var material in m_materials)
            {
                material.ChangeAlpha(m_alpha);
            }
        }

        public void Dispose()
        {
            if (m_gameObject)
            {
                if (EditorApplication.isPlaying)
                {
                    try
                    {
                        foreach (var material in m_materials)
                        {
                            Object.DestroyImmediate(material, true);
                        }
                        Object.Destroy(m_gameObject);
                    }
                    catch
                    {
                        Object.DestroyImmediate(m_gameObject);
                    }
                }
                else
                {
                    try
                    {
                        foreach (var material in m_materials)
                        {
                            Object.DestroyImmediate(material, true);
                        }
                        Object.DestroyImmediate(m_gameObject.gameObject);
                    }
                    catch
                    {
                        Object.Destroy(m_gameObject);
                    }
                }
            }
        }
    }
}
