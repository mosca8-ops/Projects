using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.VR;
using TXT.WEAVR.Common;
using UnityEngine.Events;
using System;

namespace TXT.WEAVR.Common
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public class BorderOutliner : MonoBehaviour, IObjectOutliner
    {
        [Serializable]
        public class UnityEventGameObject : UnityEvent<GameObject> { }
        [Serializable]
        public class UnityEventGameObjectColor : UnityEvent<GameObject, Color> { }

        protected readonly List<(GameObject iGO, OutlineData iRenderData)> m_outlines = new List<(GameObject iGO, OutlineData iRenderData)>();
        protected readonly Stack<Material> m_materialsBuffer = new Stack<Material>();

        [Range(1.0f, 4.0f)]
        public float lineThickness = 1f;
        [Range(0, 5)]
        public float lineIntensity = .5f;
        [Range(0, 1)]
        public float fillAmount = 0.2f;

        public OutlineMode renderMode = OutlineMode.FirstChildren;
        public Color defaultColor = Color.green;

        public bool backfaceCulling = true;
        public bool renderAllChildren = true;

        [Header("Precision Options")]
        public bool cornerOutlines = false;
        [DisabledBy("cornerOutlines")]
        public bool preciseCornerOutlines = false;

        [Header("Advanced Options")]
        public bool scaleWithScreenSize = true;
        [Range(0.1f, .9f)]
        public float alphaCutoff = .5f;
        public bool flipY = false;

        [Header("Shaders")]
        [SerializeField]
        protected Shader m_outlineShader;
        [SerializeField]
        protected Shader m_outlineBufferShader;

        [SerializeField]
        [HideInInspector]
        protected Camera m_outlineCamera;
        protected Material m_outlineSampleMaterial;
        [SerializeField]
        [HideInInspector]
        protected Material m_outlineShaderMaterial;
        [SerializeField]
        [HideInInspector]
        protected RenderTexture m_renderTexture;

        [SerializeField]
        protected UnityEventGameObject m_onOutlined;
        [SerializeField]
        protected UnityEventGameObjectColor m_onColorOutlined;
        [SerializeField]
        protected UnityEventGameObject m_onOutlineRemoved;

        public event Action<GameObject, Color> Outlined;
        public event Action<GameObject> OutlineRemoved;

        protected CommandBuffer commandBuffer;

        private Camera m_sourceCamera;
        public Camera SourceCamera
        {
            get
            {
                if (m_sourceCamera == null)
                {
                    m_sourceCamera = GetComponent<Camera>();

                    if (m_sourceCamera == null)
                    {
                        m_sourceCamera = Camera.main;
                    }

                }
                return m_sourceCamera;
            }
        }

        protected Material SampleMaterial
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

        public bool RemoveAllOutlines { get => false; set => RemoveOutlineAll(); }

        public bool Active => isActiveAndEnabled && m_sourceCamera && m_sourceCamera.isActiveAndEnabled;

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

        private void Awake()
        {
            //s_instance = this;
            m_sourceCamera = GetComponent<Camera>();
            Outliner.Register(this);
        }

        void Start()
        {
            CreateMaterialsIfNeeded();
            UpdateMaterialsPublicProperties();

            CreateOutlineCameraIfNeeded();

            m_renderTexture = new RenderTexture(SourceCamera.pixelWidth, SourceCamera.pixelHeight, 16, RenderTextureFormat.Default);
            UpdateOutlineCameraFromSource();

            commandBuffer = new CommandBuffer();
            m_outlineCamera.AddCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
        }

        private void OnEnable()
        {

        }

        private void OnDisable()
        {
            if (m_renderTexture != null)
            {
                m_renderTexture.Release();
            }
        }

        private void CreateOutlineCameraIfNeeded()
        {
            if (m_outlineCamera == null || m_outlineCamera.transform.parent != SourceCamera.transform)
            {
                GameObject cameraGameObject = null;
                if (SourceCamera.transform.Find("Outline Camera") != null)
                {
                    cameraGameObject = SourceCamera.transform.Find("Outline Camera").gameObject;
                }
                if (cameraGameObject == null)
                {
                    cameraGameObject = new GameObject("Outline Camera");
                }
                cameraGameObject.transform.SetParent(SourceCamera.transform, false);
                m_outlineCamera = cameraGameObject.GetComponent<Camera>();
                if (m_outlineCamera == null)
                {
                    m_outlineCamera = cameraGameObject.AddComponent<Camera>();
                }
                m_outlineCamera.hideFlags = HideFlags.DontSave;
                m_outlineCamera.enabled = false;

            }
        }

        public void OnPreRender()
        {
            if (commandBuffer == null || m_sourceCamera == null)
                return;

            CreateMaterialsIfNeeded();

            if (m_renderTexture == null || m_renderTexture.width != SourceCamera.pixelWidth || m_renderTexture.height != SourceCamera.pixelHeight)
            {
                m_renderTexture = new RenderTexture(SourceCamera.pixelWidth, SourceCamera.pixelHeight, 16, RenderTextureFormat.Default);
                m_outlineCamera.targetTexture = m_renderTexture;
            }
            UpdateMaterialsPublicProperties();
            UpdateOutlineCameraFromSource();
            m_outlineCamera.targetTexture = m_renderTexture;
            commandBuffer.SetRenderTarget(m_renderTexture);

            commandBuffer.Clear();
            foreach (var (wGO, wRenderData) in m_outlines)
            {
                if (!wRenderData.ShouldOutline) continue;
                LayerMask l = SourceCamera.cullingMask;
                //Renderer renderer = keyPair.Value.renderer;

                foreach (var renderer in wRenderData.subRenderers)
                {

                    if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy && l == (l | (1 << renderer.gameObject.layer)))
                    {
                        for (int v = 0; v < renderer.sharedMaterials.Length; v++)
                        {
                            Material m = wRenderData.CurrentMaterial;

                            if (renderer.sharedMaterials[v] != null)
                            {
                                m.mainTexture = renderer.sharedMaterials[v].mainTexture;
                            }

                            commandBuffer.DrawRenderer(renderer, m, 0, 0);
                            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                            if (meshFilter)
                            {
                                if (meshFilter.sharedMesh)
                                {
                                    for (int i = 1; i < meshFilter.sharedMesh.subMeshCount; i++)
                                        commandBuffer.DrawRenderer(renderer, m, i, 0);
                                }
                            }
                            SkinnedMeshRenderer skinnedMeshFilter = renderer.GetComponent<SkinnedMeshRenderer>();
                            if (skinnedMeshFilter)
                            {
                                if (skinnedMeshFilter.sharedMesh)
                                {
                                    for (int i = 1; i < skinnedMeshFilter.sharedMesh.subMeshCount; i++)
                                        commandBuffer.DrawRenderer(renderer, m, i, 0);
                                }
                            }
                        }
                    }
                }
            }

            m_outlineCamera.Render();
        }

        void OnDestroy()
        {
            OnDisable();
            DestroyMaterials();
            Outliner.Unregister(this);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (m_outlineShaderMaterial == null)
            {
                CreateMaterialsIfNeeded();
            }
            m_outlineShaderMaterial.SetTexture("_OutlineSource", m_renderTexture);

            Graphics.Blit(source, destination, m_outlineShaderMaterial, 1);
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
                UpdateMaterialsPublicProperties();
            }
            if (m_outlineSampleMaterial == null)
            {
                m_outlineSampleMaterial = CreateMaterial();
            }
        }

        private void DestroyMaterials()
        {
            RemoveOutlineAll();
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

        public void UpdateMaterialsPublicProperties()
        {
            if (m_outlineShaderMaterial == null)
            {
                CreateMaterialsIfNeeded();
            }
            if (m_outlineShaderMaterial)
            {
                float scalingFactor = 1;
                if (scaleWithScreenSize)
                {
                    // If Screen.height gets bigger, outlines gets thicker
                    scalingFactor = Screen.height / 360.0f;
                }

                // If scaling is too small (height less than 360 pixels), make sure you still render the outlines, but render them with 1 thickness
                if (scaleWithScreenSize && scalingFactor < 1)
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
                        m_outlineShaderMaterial.SetFloat("_LineThicknessX", scalingFactor * (lineThickness / 1000.0f) * (1.0f / UnityEngine.XR.XRSettings.eyeTextureWidth) * 1000.0f);
                        m_outlineShaderMaterial.SetFloat("_LineThicknessY", scalingFactor * (lineThickness / 1000.0f) * (1.0f / UnityEngine.XR.XRSettings.eyeTextureHeight) * 1000.0f);
                    }
                    else
                    {
                        m_outlineShaderMaterial.SetFloat("_LineThicknessX", scalingFactor * (lineThickness / 1000.0f) * (1.0f / Screen.width) * 1000.0f);
                        m_outlineShaderMaterial.SetFloat("_LineThicknessY", scalingFactor * (lineThickness / 1000.0f) * (1.0f / Screen.height) * 1000.0f);
                    }
                }
                m_outlineShaderMaterial.SetFloat("_LineIntensity", lineIntensity);
                m_outlineShaderMaterial.SetFloat("_FillAmount", fillAmount);
                m_outlineShaderMaterial.SetInt("_FlipY", flipY ? 1 : 0);
                m_outlineShaderMaterial.SetInt("_CornerOutlines", cornerOutlines ? 1 : 0);
                m_outlineShaderMaterial.SetInt("_CornerPrecision", preciseCornerOutlines ? 1 : 0);

                Shader.SetGlobalFloat("_OutlineAlphaCutoff", alphaCutoff);
            }
        }

        void UpdateOutlineCameraFromSource()
        {
            CreateOutlineCameraIfNeeded();
            m_outlineCamera.CopyFrom(SourceCamera);
            m_outlineCamera.renderingPath = RenderingPath.Forward;
            m_outlineCamera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            m_outlineCamera.clearFlags = CameraClearFlags.SolidColor;
            m_outlineCamera.rect = new Rect(0, 0, 1, 1);
            m_outlineCamera.cullingMask = 0;
            m_outlineCamera.targetTexture = m_renderTexture;
            m_outlineCamera.enabled = false;
            m_outlineCamera.allowHDR = false;
        }

        public virtual void Outline(GameObject go)
        {
            Outline(go, defaultColor, true);
        }

        public virtual void Outline(GameObject go, Color color)
        {
            Outline(go, color, true);
        }

        private OutlineData GetRenderDataFromGO(GameObject iGo)
        {
            foreach (var (wGO, wRenderData) in m_outlines)
            {
                if (wGO == iGo)
                {
                    return wRenderData;
                }
            }
            return null;
        }

        protected virtual void Outline(GameObject go, Color color, bool overwriteAllOutlines)
        {
            OutlineData data = GetRenderDataFromGO(go);

            if (data == null)
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
                    data = new OutlineData(renderMode == OutlineMode.AllChildren ? go : renderer.gameObject,
                                          renderMode == OutlineMode.OneRenderer);
                }

                m_outlines.Add((go, data));
            }
            if (overwriteAllOutlines)
            {
                data.Clear();
            }
            if (m_materialsBuffer.Count > 0)
            {
                data.AddMaterial(color, m_materialsBuffer.Pop());
            }
            else
            {
                data.AddMaterial(color, new Material(SampleMaterial));
            }

            m_onOutlined.Invoke(go);
            m_onColorOutlined.Invoke(go, color);
            Outlined?.Invoke(go, color);
        }

        private void RemoveOutliner(GameObject iGo)
        {
            m_outlines.RemoveAll(x => System.Object.ReferenceEquals(x.iGO, iGo));
        }

        public virtual void RemoveOutline(GameObject go, Color color)
        {
            OutlineData data = GetRenderDataFromGO(go);
            Material material = null;
            if (data != null && data.TryRemoveMaterial(color, out material))
            {
                m_materialsBuffer.Push(material);
                if (data.Count == 0)
                {
                    RemoveFromOutlines(go);
                    //RemoveOutliner(go);
                    m_onOutlineRemoved.Invoke(go);
                    OutlineRemoved?.Invoke(go);
                }
            }
        }

        public virtual void RemoveOutline(GameObject go)
        {
            OutlineData data = GetRenderDataFromGO(go);
            if (data != null)
            {
                data.Clear();
                RemoveFromOutlines(go);
                //RemoveOutline(go);
                m_onOutlineRemoved.Invoke(go);
                OutlineRemoved?.Invoke(go);
            }
        }

        protected void RemoveFromOutlines(GameObject go)
        {
            for (int i = 0; i < m_outlines.Count; i++)
            {
                if (m_outlines[i].iGO == go)
                {
                    m_outlines.RemoveAt(i--);
                    return;
                }
            }
        }

        protected virtual void RemoveOutlineAll()
        {
            foreach (var (wGo, wRenderData) in m_outlines)
            {
                wRenderData.Clear();
            }
            m_outlines.Clear();
        }
    }
}