using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class SetColorAndTextureAction : BaseReversibleProgressAction, ITargetingObject, ISerializedNetworkProcedureObject, IVariablesUser
    {
        private enum SearchType
        {
            FirstRenderer, 
            FirstLevelChildren, 
            AllChildren
        }

        private enum WhichMaterial
        {
            Main,
            AtIndexOrMain,
            AtIndexOrLastOne,
            AtIndexOrNone,
            AllMaterials,
        }

        [SerializeField]
        [Tooltip("The target renderer to change the texture and/or color for")]
        [Draggable]
        private ValueProxyGameObject m_target;
        [SerializeField]
        [Tooltip("The texture to apply to the target renderer")]
        private OptionalProxyTexture m_texture;
        [SerializeField]
        [Tooltip("The offset to apply to the main texture of the target renderer")]
        [Reversible]
        private OptionalAnimatedVector2 m_offset;
        [SerializeField]
        [Tooltip("The scale to apply to the main texture of the target renderer")]
        [Reversible]
        private OptionalAnimatedVector2 m_scale;
        [SerializeField]
        [Tooltip("The color to apply to the main material of the target renderer")]
        [Reversible]
        private OptionalAnimatedProxyColor m_color;
        [SerializeField]
        [Tooltip("Where to apply these changes")]
        private SearchType m_applyTo;
        [SerializeField]
        [Tooltip("Where to apply these changes")]
        private WhichMaterial m_forMaterial;
        [SerializeField]
        [Tooltip("Material index to apply")]
        [ShowIf(nameof(ShowIndexField))]
        private ValueProxyInt m_materialIndex;

        [NonSerialized]
        private Texture texture;
        [NonSerialized]
        private Vector2 offset;
        [NonSerialized]
        private Vector2 scale;
        [NonSerialized]
        private Color color;

        private List<MaterialProxy> m_materials;

        #region [  ISerializedNetworkProcedureObject IMPLEMENTATION  ]

        [SerializeField]
        private bool m_isGlobal = true;
        public string IsGlobalFieldName => nameof(m_isGlobal);
        public bool IsGlobal => m_isGlobal;

        #endregion

        public UnityEngine.Object Target {
            get => m_target;
            set => m_target.Value = value is GameObject go ? go : 
                value is Component c ? c.gameObject : value == null ? null : m_target.Value; }

        public string TargetFieldName => nameof(m_target);

        public IEnumerable<string> GetActiveVariablesFields() => new string[] {
            m_target.IsVariable ? nameof(m_target) : null,
            //m_text.IsVariable ? nameof(m_text) : null,
        }.Where(v => v != null);


        private class MaterialProxy
        {
            public Renderer renderer;
            public Material material;
            public int index;
            public Texture texture;
            public Vector2 offset;
            public Vector2 scale;
            public Color? color;

            public AnimatedProxyColor animColor;
            public AnimatedVector2 animOffset;
            public AnimatedVector2 animScale;

            public MaterialProxy(Renderer renderer, int index, Material material)
            {
                this.renderer = renderer;
                this.index = index;
                this.material = material;
                texture = material.HasProperty("_MainTex") || material.HasProperty("_BaseTex") ? material.mainTexture : null;
                offset =  material.mainTextureOffset;
                scale =   material.mainTextureScale;
                color =   material.HasProperty("_Color") ? material.color : (Color?)null;
            }
        }

        private bool ShowIndexField() => m_forMaterial == WhichMaterial.AtIndexOrLastOne 
                                      || m_forMaterial == WhichMaterial.AtIndexOrMain 
                                      || m_forMaterial == WhichMaterial.AtIndexOrNone;

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_Color"))
            {
                material.color = color;
            }
        }

        private static void SetMaterialTexture(Material material, Texture texture)
        {
            if(material.HasProperty("_MainTex") || material.HasProperty("_BaseTex"))
            {
                material.mainTexture = texture;
            }
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            var target = m_target.Value;

            if(m_materials == null) { m_materials = new List<MaterialProxy>(); }
            else { m_materials.Clear(); }

            var renderers = GetRenderers();

            switch (m_forMaterial)
            {
                case WhichMaterial.Main:
                    RetrieveMainMaterials(renderers);
                    break;
                case WhichMaterial.AllMaterials:
                    RetrieveAllMaterials(renderers);
                    break;
                case WhichMaterial.AtIndexOrNone:
                    RetrieveAtIndexOrNone(renderers);
                    break;
                case WhichMaterial.AtIndexOrMain:
                    RetrieveAtIndexOrMain(renderers);
                    break;
                case WhichMaterial.AtIndexOrLastOne:
                    RetrieveAtIndexOrLastOne(renderers);
                    break;
            }

            foreach (var proxy in m_materials)
            {
                if (m_offset.enabled)
                {
                    proxy.animOffset = m_offset.value.Clone<AnimatedVector2>();
                    proxy.animOffset.Start(proxy.offset);
                }
                if (m_scale.enabled)
                {
                    proxy.animScale = m_scale.value.Clone<AnimatedVector2>();
                    proxy.animScale.Start(proxy.scale);
                }
                if (m_color.enabled && proxy.color.HasValue)
                {
                    proxy.animColor = m_color.value.Clone<AnimatedProxyColor>();
                    proxy.animColor.Start(proxy.color.Value);
                }
            }
        }

        private void RetrieveMainMaterials(IEnumerable<Renderer> renderers)
        {
            foreach(var renderer in renderers)
            {
                m_materials.Add(new MaterialProxy(renderer, 0, renderer.material));
            }
        }

        private void RetrieveAllMaterials(IEnumerable<Renderer> renderers)
        {
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    m_materials.Add(new MaterialProxy(renderer, i, materials[i]));
                }
            }
        }

        private void RetrieveAtIndexOrNone(IEnumerable<Renderer> renderers)
        {
            var index = m_materialIndex.Value;
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                if(index < materials.Length)
                {
                    m_materials.Add(new MaterialProxy(renderer, index, materials[index]));
                }
            }
        }

        private void RetrieveAtIndexOrMain(IEnumerable<Renderer> renderers)
        {
            var index = m_materialIndex.Value;
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                if (index < materials.Length)
                {
                    m_materials.Add(new MaterialProxy(renderer, index, materials[index]));
                }
                else
                {
                    m_materials.Add(new MaterialProxy(renderer, 0, renderer.material));
                }
            }
        }

        private void RetrieveAtIndexOrLastOne(IEnumerable<Renderer> renderers)
        {
            var index = m_materialIndex.Value;
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                if (index < materials.Length)
                {
                    m_materials.Add(new MaterialProxy(renderer, index, materials[index]));
                }
                else
                {
                    m_materials.Add(new MaterialProxy(renderer, materials.Length - 1, materials[materials.Length - 1]));
                }
            }
        }

        private IEnumerable<Renderer> GetRenderers()
        {
            switch (m_applyTo)
            {
                case SearchType.FirstRenderer:
                    var firstRenderer = m_target.Value.GetComponent<Renderer>();
                    return firstRenderer ? 
                        new Renderer[] { firstRenderer } :
                        new Renderer[0];
                case SearchType.AllChildren: return m_target.Value.GetComponentsInChildren<Renderer>();
                case SearchType.FirstLevelChildren:
                    List<Renderer> renderers = new List<Renderer>();
                    var renderer = m_target.Value.GetComponent<Renderer>();
                    if (renderer)
                    {
                        renderers.Add(renderer);
                    }
                    var transform = m_target.Value.transform;
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        renderer = transform.GetChild(i).GetComponent<Renderer>();
                        if (renderer)
                        {
                            renderers.Add(renderer);
                        }
                    }
                    return renderers;
            }
            return new Renderer[0];
        }

        public override bool Execute(float dt)
        {
            float minProgress = 1;
            foreach(var proxy in m_materials)
            {
                if (m_texture.enabled)
                {
                    SetMaterialTexture(proxy.material, m_texture.value.Value);
                }
                if(proxy.animOffset != null)
                {
                    proxy.material.mainTextureOffset = proxy.animOffset.Next(dt);
                    minProgress = Mathf.Min(minProgress, proxy.animOffset.Progress);
                }
                if(proxy.animScale != null)
                {
                    proxy.material.mainTextureScale = proxy.animScale.Next(dt);
                    minProgress = Mathf.Min(minProgress, proxy.animScale.Progress);
                }
                if(proxy.animColor != null)
                {
                    proxy.material.color = proxy.animColor.Next(dt);
                    minProgress = Mathf.Min(minProgress, proxy.animColor.Progress);
                }
            }
            
            Progress = minProgress;
            return minProgress >= 1;
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (RevertOnExit)
            {
                foreach(var proxy in m_materials)
                {
                    var material = proxy.material;
                    if (m_texture.enabled)
                    {
                        SetMaterialTexture(material, proxy.texture);
                    }
                    proxy.animOffset?.AutoAnimate(proxy.offset, v => material.mainTextureOffset = v);
                    proxy.animScale?.AutoAnimate(proxy.scale, v => material.mainTextureScale = v);
                    proxy.animColor?.AutoAnimate(proxy.color.Value, v => material.color = v);
                }
            }
        }

        public override void FastForward()
        {
            base.FastForward();
            foreach(var proxy in m_materials)
            {
                var material = proxy.material;
                if (m_texture.enabled)
                {
                    SetMaterialTexture(material, m_texture);
                }
                if (m_offset.enabled) { material.mainTextureOffset = m_offset.value.TargetValue; }
                if (m_scale.enabled) { material.mainTextureScale = m_scale.value.TargetValue; }
                if (m_color.enabled) { SetMaterialColor(material, m_color.value.TargetValue); }
            }
            Progress = 1;
        }

        public override string GetDescription()
        {
            string textureName = m_texture.ToString();
            string targetName = m_target.IsVariable ? $"[{m_target.VariableName}]" : m_target.Value ? m_target.Value.name : "[ ? ]";
            return m_texture.enabled && m_offset.enabled && m_color.enabled ? 
                $"{targetName}: Texture = {textureName} with Offset = {m_offset.value} and Color = {m_color.value} " :
                m_texture.enabled && m_offset.enabled ? 
                $"{targetName}: Texture = {textureName} with Offset = {m_offset.value} " :
                m_texture.enabled && m_color.enabled ? $"{targetName}: Texture = {textureName} and Color = {m_color.value} " :
                m_texture.enabled ? $"{targetName}: Texture = {textureName} " :
                m_color.enabled ? $"{targetName}: Color = {m_color.value}  " : $"{targetName}: No Texture or Color set ";
        }
    }
}