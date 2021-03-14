using System;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;

#if WEAVR_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RenderEvent = UnityEngine.Rendering.Universal.RenderPassEvent;
using BaseClass = UnityEngine.Rendering.Universal.ScriptableRendererFeature;
#else
using RenderEvent = TXT.WEAVR.Rendering.URP.RenderPassEvent;
using BaseClass = UnityEngine.ScriptableObject;
#endif

namespace TXT.WEAVR.Rendering.URP
{
    public class SimpleOutline : BaseClass
#if WEAVR_URP
        , IObjectOutliner
#endif
    {
        [Serializable]
        public class OutlineSettings
        {
            public RenderEvent Event = RenderEvent.AfterRenderingTransparents;
            public OutlineMode outlineMode = OutlineMode.FirstChildren;
            public Target destination = Target.Color;
            [Range(1.0f, 4.0f)]
            public float lineThickness = 1f;
            [Range(0, 5)]
            public float lineIntensity = .5f;
            [Range(0, 1)]
            public float fillAmount = 0.2f;

            [Header("Precision")]
            public bool outlineCorners = false;
            [HiddenBy(nameof(outlineCorners))]
            public bool preciseCornerOutlines = false;

            [Header("Advanced Options")]
            public int blitMaterialPassIndex = 5;
            public bool scaleWithScreenSize = true;
            [Range(0.1f, .9f)]
            public float alphaCutoff = .5f;
            public bool flipY = false;
        }

        public enum Target
        {
            Color,
            Texture
        }

        public OutlineSettings settings = new OutlineSettings();
        public Material debugMaterial;

#if WEAVR_URP
        private RenderTargetHandle m_renderTextureHandle;

        private OutlinePass m_outlinePass;
        private BlitPass m_blitPass;
        private Dictionary<GameObject, OutlineData> m_outlines;
        private Stack<Material> m_materialsBuffer;

        [NonSerialized]
        protected Shader m_outlineShader;
        [NonSerialized]
        protected Shader m_outlineBufferShader;
        [NonSerialized]
        protected Material m_outlineSampleMaterial;
        [NonSerialized]
        protected Material m_outlineShaderMaterial;

        private string m_textureId = "_BlitPassTexture";
        private RenderTexture m_renderTexture;
        private RenderTargetIdentifier m_renderTextureId;

        private Camera m_sourceCamera;
        public Camera SourceCamera
        {
            get => m_sourceCamera;
            set
            {
                if (m_sourceCamera != value)
                {
                    m_sourceCamera = value;
                    ClearRenderTexture();
                    if (m_sourceCamera)
                    {
                        UpdateLineThickness();
                        CreateRenderTexture(m_sourceCamera);
                    }
                }
            }
        }
        
        public Material SampleMaterial
        {
            get
            {
                if (!m_outlineSampleMaterial)
                {
                    CreateMaterialsIfNeeded();
                    UpdateMaterialsPublicProperties();
                }
                return m_outlineSampleMaterial;
            }
        }

        public bool Active => isActive;

        public override void Create()
        {
            Outliner.Register(this);

            m_outlines = new Dictionary<GameObject, OutlineData>();
            m_materialsBuffer = new Stack<Material>();

            if (m_renderTexture)
            {
                DestroyImmediate(m_renderTexture, true);
            }
            m_outlinePass = new OutlinePass(settings.Event, name + "_buffer");

            if (m_outlineShaderMaterial == null)
            {
                CreateMaterialsIfNeeded();
            }
            var passIndex = m_outlineShaderMaterial != null ? m_outlineShaderMaterial.passCount - 1 : 1;
            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
            m_blitPass = new BlitPass(RenderEvent.AfterRenderingTransparents, m_outlineShaderMaterial, settings.blitMaterialPassIndex, name + "_postprocess");
            m_renderTextureHandle.Init(m_textureId);
        }

        private void OnDisable()
        {
            DestroyMaterials();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(m_outlines == null || m_outlines.Count == 0) { return; }

            SourceCamera = renderingData.cameraData.camera;
            UpdateRenderTexture(SourceCamera);
            UpdateMaterialsPublicProperties();
            var src = renderer.cameraColorTarget;
            var dest = (settings.destination == Target.Color) ? RenderTargetHandle.CameraTarget : m_renderTextureHandle;

            if (!m_outlineShaderMaterial)
            {
                Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
                return;
            }

            m_outlineShaderMaterial.SetTexture("_OutlineSource", m_renderTexture);
            m_outlinePass.Setup(m_renderTextureId, m_outlines.Values);
            m_blitPass.Setup(src, dest);
            renderer.EnqueuePass(m_outlinePass);
            renderer.EnqueuePass(m_blitPass);
        }
        
        public void Outline(GameObject go, Color color)
        {
            if (!m_outlines.TryGetValue(go, out OutlineData data))
            {
                OverrideOutline overrideOutline = go.GetComponent<OverrideOutline>();
                if (overrideOutline != null)
                {
                    data = new OutlineData(overrideOutline);
                }
                else
                {
                    Renderer renderer = go.GetComponentInChildren<Renderer>();
                    if (!renderer) return;
                    data = new OutlineData(settings.outlineMode == OutlineMode.AllChildren ? go : renderer.gameObject,
                                          settings.outlineMode == OutlineMode.OneRenderer);
                }

                m_outlines[go] = data;
            }

            if (m_materialsBuffer.Count > 0)
            {
                data.AddMaterial(color, m_materialsBuffer.Pop());
            }
            else
            {
                data.AddMaterial(color, new Material(SampleMaterial));
            }
        }

        public void RemoveOutline(GameObject go, Color color)
        {
            if (m_outlines.TryGetValue(go, out OutlineData data) && data.TryRemoveMaterial(color, out Material material))
            {
                m_materialsBuffer.Push(material);
                if (data.Count == 0)
                {
                    m_outlines.Remove(go);
                }
            }
        }

        private void CreateRenderTexture(Camera camera)
        {
            m_renderTexture = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 16, RenderTextureFormat.Default);
            m_renderTextureId = new RenderTargetIdentifier(m_renderTexture);
            if (debugMaterial)
            {
                debugMaterial.mainTexture = m_renderTexture;
            }
        }

        private void UpdateRenderTexture(Camera camera)
        {
            if (!m_renderTexture || m_renderTexture.width != camera.pixelWidth || m_renderTexture.height != camera.pixelHeight)
            {
                ClearRenderTexture();
                CreateRenderTexture(camera);
            }
        }

        private void ClearRenderTexture()
        {
            if (m_renderTexture)
            {
                m_renderTexture.Release();
                if (Application.isPlaying)
                {
                    Destroy(m_renderTexture);
                }
                else
                {
                    DestroyImmediate(m_renderTexture, true);
                }
            }
            m_renderTexture = null;
        }

        private void CreateMaterialsIfNeeded()
        {
            if (m_outlineShader == null)
            {
                m_outlineShader = Shader.Find("Hidden/WEAVROutlineEffect");
            }
            if (m_outlineBufferShader == null)
            {
                m_outlineBufferShader = Shader.Find("Hidden/WEAVROutlineBufferEffect");
            }
            if (m_outlineShaderMaterial == null)
            {
                m_outlineShaderMaterial = new Material(m_outlineShader);
                m_outlineShaderMaterial.hideFlags = HideFlags.HideAndDontSave;
                m_outlineShaderMaterial.hideFlags = HideFlags.HideAndDontSave;
                UpdateMaterialsPublicProperties();
            }
            if (m_outlineSampleMaterial == null)
            {
                m_outlineSampleMaterial = CreateMaterial();
                m_outlineSampleMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private void DestroyMaterials()
        {
            RemoveAllOutlines();
            foreach (var material in m_materialsBuffer)
            {
                DestroyImmediate(material);
            }
            m_materialsBuffer.Clear();
            DestroyImmediate(m_outlineShaderMaterial);
            DestroyImmediate(m_outlineSampleMaterial);
            m_outlineShader = null;
            m_outlineBufferShader = null;
            m_outlineShaderMaterial = null;
            m_outlineSampleMaterial = null;
        }

        private void RemoveAllOutlines()
        {
            foreach (var pair in m_outlines)
            {
                pair.Value?.Clear(destroyMaterials: true);
            }
            m_outlines.Clear();
            foreach (var mat in m_materialsBuffer)
            {
                if (Application.isPlaying)
                {
                    Destroy(mat);
                }
                else
                {
                    DestroyImmediate(mat, true);
                }
            }
            m_materialsBuffer.Clear();
        }

        public void UpdateMaterialsPublicProperties()
        {
            if (m_outlineShaderMaterial == null)
            {
                CreateMaterialsIfNeeded();
            }
            if (m_outlineShaderMaterial)
            {
                UpdateLineThickness();

                m_outlineShaderMaterial.SetFloat("_LineIntensity", settings.lineIntensity);
                m_outlineShaderMaterial.SetFloat("_FillAmount", settings.fillAmount);
                m_outlineShaderMaterial.SetInt("_FlipY", settings.flipY ? 1 : 0);
                m_outlineShaderMaterial.SetInt("_CornerOutlines", settings.outlineCorners ? 1 : 0);
                m_outlineShaderMaterial.SetInt("_CornerPrecision", settings.preciseCornerOutlines ? 1 : 0);

                Shader.SetGlobalFloat("_OutlineAlphaCutoff", settings.alphaCutoff);
            }
        }

        private void UpdateLineThickness()
        {
            float scalingFactor = 1;
            if (settings.scaleWithScreenSize)
            {
                // If Screen.height gets bigger, outlines gets thicker
                scalingFactor = Screen.height / 360.0f;
            }

            if (SourceCamera)
            {
                // If scaling is too small (height less than 360 pixels), make sure you still render the outlines, but render them with 1 thickness
                if (settings.scaleWithScreenSize && scalingFactor < 1)
                {
                    if (UnityEngine.XR.XRSettings.isDeviceActive && SourceCamera.stereoTargetEye != StereoTargetEyeMask.None)
                    {
                        m_outlineShaderMaterial.SetFloat("_LineThicknessX", (1 / 1000.0f) * (1.0f / UnityEngine.XR.XRSettings.eyeTextureWidth) * 1000.0f);
                        m_outlineShaderMaterial.SetFloat("_LineThicknessY", (1 / 1000.0f) * (1.0f / UnityEngine.XR.XRSettings.eyeTextureHeight) * 1000.0f);
                    }
                    else
                    {
                        m_outlineShaderMaterial.SetFloat("_LineThicknessX", (1 / 1000.0f) * (1.0f / Screen.width) * 1000.0f);
                        m_outlineShaderMaterial.SetFloat("_LineThicknessY", (1 / 1000.0f) * (1.0f / Screen.height) * 1000.0f);
                    }
                }
                else
                {
                    if (UnityEngine.XR.XRSettings.isDeviceActive && SourceCamera != null && SourceCamera.stereoTargetEye != StereoTargetEyeMask.None)
                    {
                        m_outlineShaderMaterial.SetFloat("_LineThicknessX", scalingFactor * (settings.lineThickness / 1000.0f) * (1.0f / UnityEngine.XR.XRSettings.eyeTextureWidth) * 1000.0f);
                        m_outlineShaderMaterial.SetFloat("_LineThicknessY", scalingFactor * (settings.lineThickness / 1000.0f) * (1.0f / UnityEngine.XR.XRSettings.eyeTextureHeight) * 1000.0f);
                    }
                    else
                    {
                        m_outlineShaderMaterial.SetFloat("_LineThicknessX", scalingFactor * (settings.lineThickness / 1000.0f) * (1.0f / Screen.width) * 1000.0f);
                        m_outlineShaderMaterial.SetFloat("_LineThicknessY", scalingFactor * (settings.lineThickness / 1000.0f) * (1.0f / Screen.height) * 1000.0f);
                    }
                }
            }
        }

        Material CreateMaterial()
        {
            Material m = new Material(m_outlineBufferShader);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            return m;
        }

#endif

    }
}

