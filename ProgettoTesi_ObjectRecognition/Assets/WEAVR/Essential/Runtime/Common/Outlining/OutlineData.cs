using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.VR;
using TXT.WEAVR.Common;
using UnityEngine.Events;
using System;
using System.Linq;

namespace TXT.WEAVR.Common
{
    public class OutlineData
    {
        static readonly int s_colorId = Shader.PropertyToID("_Color");

        public readonly GameObject gameObject;
        public readonly Renderer[] subRenderers;

        private List<(Color color, Material material)> m_materials;
        private bool m_shouldOutline;

        public int Count
        {
            get
            {
                return m_materials.Count;
            }
        }

        public bool ShouldOutline
        {
            get
            {
                return m_shouldOutline && CurrentMaterial != null;
            }
            set
            {
                if (m_shouldOutline != value)
                {
                    m_shouldOutline = value;
                }
            }
        }

        public Material CurrentMaterial { get; private set; }

        public OutlineData(GameObject gameObject, bool oneRendererOnly)
        {
            ShouldOutline = true;
            this.gameObject = gameObject;
            if (oneRendererOnly)
            {
                subRenderers = new Renderer[] { gameObject.GetComponentInChildren<Renderer>() };
            }
            else
            {
                subRenderers = gameObject.GetComponentsInChildren<Renderer>(true);
            }
            m_materials = new List<(Color, Material)>();
        }

        public OutlineData(OverrideOutline overrideOutline)
        {
            ShouldOutline = true;
            gameObject = overrideOutline.gameObject;
            subRenderers = new List<Renderer>(overrideOutline.toOutline).ToArray();
            m_materials = new List<(Color, Material)>();
        }

        public void Clear(bool destroyMaterials = false)
        {
            if (destroyMaterials)
            {
                foreach(var (_, mat) in m_materials)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(mat);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(mat, true);
                    }
                }
            }
            m_materials.Clear();
            CurrentMaterial = null;
        }

        public void AddMaterial(Color color, Material newMaterial)
        {
            for (int i = 0; i < m_materials.Count; i++)
            {
                if (m_materials[i].color == color)
                {
                    // bring to front -> last element
                    var keyPair = m_materials[i];
                    CurrentMaterial = keyPair.material;
                    m_materials.RemoveAt(i);
                    m_materials.Add(keyPair);
                    return;
                }
            }
            newMaterial.SetColor(s_colorId, color);
            CurrentMaterial = newMaterial;
            m_materials.Add((color, newMaterial));
        }

        public bool TryRemoveMaterial(Color color, out Material material)
        {
            for (int i = 0; i < m_materials.Count; i++)
            {
                if (m_materials[i].color == color)
                {
                    material = m_materials[i].material;
                    if (CurrentMaterial == material)
                    {
                        CurrentMaterial = null;
                    }
                    m_materials.RemoveAt(i);
                    if (!CurrentMaterial && m_materials.Count > 0)
                    {
                        CurrentMaterial = m_materials[m_materials.Count - 1].material;
                    }
                    return true;
                }
            }
            material = null;
            return false;
        }

        public Color? GetFirstColor()
        {
            return m_materials.Count > 0 ? m_materials[0].color : (Color?)null;
        }
    }
}