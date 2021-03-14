using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using static TXT.WEAVR.Pose;

namespace TXT.WEAVR.Procedure
{

    public class AnimateMaterialBlock : ComponentAnimation<Renderer>
    {
        [SerializeField]
        [Tooltip("The destination material to reach")]
        [Draggable]
        private Material m_material;
        [SerializeField]
        [Tooltip("The index of material to animate if the renderer has more materials")]
        [ShowIf(nameof(HasManyMaterials))]
        [ArrayElement(nameof(m_indices))]
        private int m_index;
        [SerializeField]
        [HideInInspector]
        private int[] m_indices = new int[] { 0 };

        private Material m_originalMaterial;
        private Renderer m_lastTarget;

        [NonSerialized]
        private Material[] m_materials;
        [NonSerialized]
        private int m_i;
        [NonSerialized]
        private List<int> m_texturesIDs;
        [NonSerialized]
        private float m_lastNormValue;
        [NonSerialized]
        private bool m_lerpTextureKeywords;
        [NonSerialized]
        private bool m_swapShaders;

        public override bool CanProvide<T>()
        {
            return false;
        }

        private bool HasManyMaterials() => m_indices != null && m_indices.Length > 1;

        public override void OnValidate()
        {
            base.OnValidate();
            if (m_lastTarget != m_target)
            {
                if (m_target)
                {
                    m_indices = new int[m_target.sharedMaterials.Length];
                    for (int i = 0; i < m_indices.Length; i++)
                    {
                        m_indices[i] = i;
                    }
                    m_index = Mathf.Clamp(m_index, 0, m_indices.Length - 1);
                }
                else if(m_lastTarget)
                {
                    m_indices = new int[] { 0 };
                    m_index = 0;
                }
                m_lastTarget = m_target;
            }
            else if (m_target && m_target.sharedMaterials.Length != m_indices.Length)
            {
                m_indices = new int[m_target.sharedMaterials.Length];
                for (int i = 0; i < m_indices.Length; i++)
                {
                    m_indices[i] = i;
                }
                m_index = Mathf.Clamp(m_index, 0, m_indices.Length - 1);
            }
        }

        public override void OnStart()
        {
            base.OnStart();
            m_lastNormValue = 0;
            if (m_index < 0)
            {
                m_index = 0;
            }
            m_materials = Application.isPlaying ? m_target.materials : m_target.sharedMaterials;
            m_i = Mathf.Min(m_materials.Length - 1, m_index);
            m_originalMaterial = new Material(m_materials[m_i]);
            m_swapShaders = m_originalMaterial.shader != m_material.shader;
            var sourceKeywords = m_originalMaterial.GetTexturePropertyNameIDs();
            var targetKeywords = m_material.GetTexturePropertyNameIDs();

            m_texturesIDs = new List<int>();
            for (int i = 0; i < sourceKeywords.Length; i++)
            {
                for (int j = 0; j < targetKeywords.Length; j++)
                {
                    if (targetKeywords[j] == sourceKeywords[i]
                        && m_material.GetTexture(sourceKeywords[i]) != m_originalMaterial.GetTexture(sourceKeywords[i]))
                    {
                        m_lerpTextureKeywords = true;
                        m_texturesIDs.Add(sourceKeywords[i]);
                    }
                }
                //if(sourceKeywords[i] != 0 
                //    && m_material.HasProperty(sourceKeywords[i]) 
                //    && m_material.GetTexture(sourceKeywords[i]) != m_originalMaterial.GetTexture(sourceKeywords[i]))
                //{
                //    m_lerpTextureKeywords = true;
                //    m_texturesIDs.Add(i);
                //}
            }
        }

        public override void OnEnd(float normalizedDelta)
        {
            base.OnEnd(normalizedDelta);
        }

        protected override void Animate(float delta, float normalizedValue)
        {
            if (!m_target) { return; }
            if (m_material)
            {
                if(m_lastNormValue < 0.5f && normalizedValue >= 0.5f)
                {
                    if (m_swapShaders)
                    {
                        m_materials[m_i].shader = m_material.shader;
                    }
                    m_materials[m_i].shaderKeywords = m_material.shaderKeywords;
                    SetTextures(ref m_materials[m_i], m_material);
                    m_lastNormValue = normalizedValue;
                }
                else if(m_lastNormValue >= 0.5f && normalizedValue < 0.5f)
                {
                    if (m_swapShaders)
                    {
                        m_materials[m_i].shader = m_originalMaterial.shader;
                    }
                    m_materials[m_i].shaderKeywords = m_originalMaterial.shaderKeywords;
                    SetTextures(ref m_materials[m_i], m_originalMaterial);
                    m_lastNormValue = normalizedValue;
                }

                m_materials[m_i].Lerp(m_originalMaterial, m_material, normalizedValue);
                if (Application.isPlaying)
                {
                    m_target.materials = m_materials;
                }
                else
                {
                    m_target.sharedMaterials = m_materials;
                }
            }
        }

        private void SetTextures(ref Material destination, Material source)
        {
            if (m_lerpTextureKeywords)
            {
                for (int i = 0; i < m_texturesIDs.Count; i++)
                {
                    destination.SetTexture(m_texturesIDs[i], source.GetTexture(m_texturesIDs[i]));
                }
            }
            destination.mainTexture = source.mainTexture;
        }

        public override bool CanPreview()
        {
            return true;
        }
    }
}
