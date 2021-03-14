
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

#if WEAVR_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RenderEvent = UnityEngine.Rendering.Universal.RenderPassEvent;
using BaseClass = UnityEngine.Rendering.Universal.ScriptableRenderPass;
#else
using RenderEvent = TXT.WEAVR.Rendering.URP.RenderPassEvent;
using BaseClass = UnityEngine.ScriptableObject;
#endif

namespace TXT.WEAVR.Rendering.URP
{
    internal class OutlinePass : BaseClass
    {

#if WEAVR_URP
        private string m_profilerTag;
        private IEnumerable<OutlineData> m_outlines;
        private RenderTargetIdentifier m_renderTextureId;

        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public OutlinePass(RenderEvent renderPassEvent, string tag)
        {
            this.renderPassEvent = renderPassEvent;
            m_profilerTag = tag;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RenderTargetIdentifier renderTextureId, IEnumerable<OutlineData> outlines)
        {
            m_renderTextureId = renderTextureId;
            m_outlines = outlines;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_profilerTag);
            cmd.Clear();
            cmd.SetRenderTarget(m_renderTextureId);
            cmd.ClearRenderTarget(true, true, Color.clear);
            RenderToBuffer(cmd, renderingData.cameraData.camera.cullingMask);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void RenderToBuffer(CommandBuffer commandBuffer, LayerMask cullingMask)
        {
            foreach (var renderData in m_outlines)
            {
                if (!renderData.ShouldOutline) continue;
                //Renderer renderer = keyPair.Value.renderer;

                foreach (var renderer in renderData.subRenderers)
                {

                    if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy && cullingMask == (cullingMask | (1 << renderer.gameObject.layer)))
                    {
                        for (int v = 0; v < renderer.sharedMaterials.Length; v++)
                        {
                            Material m = renderData.CurrentMaterial;

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
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
        }

#endif
    }
}
