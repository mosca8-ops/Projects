using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using static TXT.WEAVR.Pose;

namespace TXT.WEAVR.Procedure
{

    public class AnimateTransparencyBlock : GameObjectAnimation
    {
        [SerializeField]
        [Tooltip("The final destination transparency to reach")]
        private OptionalFloat m_transparency;
        
        [SerializeField]
        [Tooltip("The delta transparency to apply to the current one")]
        private OptionalFloat m_deltaTransparency;
        
        private Dictionary<Renderer, Material[]> m_prevMaterials;
        private Dictionary<Renderer, Material[]> m_currentMaterials;

        public override bool CanProvide<T>()
        {
            return false;
        }

        public override void OnValidate()
        {
            base.OnValidate();
            if (m_transparency.enabled) {
                m_transparency.value = Mathf.Clamp01(m_transparency.value);
                m_deltaTransparency.enabled = false;
            }
            else if (m_deltaTransparency.enabled)
            {
                m_deltaTransparency.value = Mathf.Clamp(m_deltaTransparency.value, -1, 1);
            }
        }

        public override void OnStart()
        {
            base.OnStart();
            m_prevMaterials = new Dictionary<Renderer, Material[]>();
            m_currentMaterials = new Dictionary<Renderer, Material[]>();
            var renderers = new List<Renderer>(m_target.GetComponentsInChildren<Renderer>());
            if(!m_transparency.enabled && !m_deltaTransparency.enabled) { return; }
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled) { continue; }
                var materials = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;
                Material[] rendMaterials = new Material[materials.Length];
                m_prevMaterials[renderer] = materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    materials[i] = material;
                    var newMaterial = new Material(material);
                    newMaterial.CopyPropertiesFromMaterial(material);
                    rendMaterials[i] = MakeTransparent(newMaterial);
                }
                
                m_currentMaterials[renderer] = rendMaterials;
                if (Application.isPlaying)
                {
                    renderer.materials = rendMaterials;
                }
                else
                {
                    renderer.sharedMaterials = rendMaterials;
                }
            }
        }

        public override void OnEnd(float normalizedDelta)
        {
            base.OnEnd(normalizedDelta);
            if (Application.isPlaying)
            {
                foreach (var pair in m_prevMaterials)
                {
                    pair.Key.materials = pair.Value;
                }
            }
            else
            {
                foreach (var pair in m_prevMaterials)
                {
                    pair.Key.sharedMaterials = pair.Value;
                }
            }
        }

        protected override void Animate(float delta, float normalizedValue)
        {
            if (!m_target) { return; }
            if (m_transparency.enabled)
            {
                foreach (var pair in m_currentMaterials)
                {
                    if (pair.Key && pair.Key.enabled)
                    {
                        var originals = m_prevMaterials[pair.Key];
                        for (int i = 0; i < pair.Value.Length; i++)
                        {
                            var color = pair.Value[i].color;
                            color.a = Mathf.MoveTowards(originals[i].color.a, m_transparency.value, normalizedValue);
                            pair.Value[i].color = color;
                        }
                        if (Application.isPlaying)
                        {
                            pair.Key.materials = pair.Value;
                        }
                        else
                        {
                            pair.Key.sharedMaterials = pair.Value;
                        }
                    }
                }
            }
            else if (m_deltaTransparency.enabled)
            {
                foreach(var pair in m_currentMaterials)
                {
                    if (pair.Key && pair.Key.enabled)
                    {
                        foreach (var material in pair.Value)
                        {
                            ChangeDeltaAlpha(material, m_deltaTransparency.value * delta);
                        }
                        if (Application.isPlaying)
                        {
                            pair.Key.materials = pair.Value;
                        }
                        else
                        {
                            pair.Key.sharedMaterials = pair.Value;
                        }
                    }
                }
            }
        }

        public static bool IsTransparent(Material material)
        {
            return material.GetInt("_Mode") == 2;
        }

        public static Material MakeTransparent(Material material)
        {
            if (material.GetInt("_Mode") == 0 || material.GetInt("_Mode") == 1)
            {
                material.SetInt("_Mode", 2);

                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
            return material;
        }

        public static Material ChangeAlpha(Material material, float newAlpha)
        {
            var color = material.color;
            color.a = Mathf.Clamp01(newAlpha);
            material.color = color;

            return material;
        }

        public static Material ChangeDeltaAlpha(Material material, float deltaAlpha)
        {
            var color = material.color;
            color.a = Mathf.Clamp01(color.a + deltaAlpha);
            material.color = color;

            return material;
        }

        public override bool CanPreview()
        {
            return true;
        }
    }
}
