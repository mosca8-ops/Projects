
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
    public class BlitPass : BaseClass
    {

#if WEAVR_URP
        public Material blitMaterial = null;
        public int blitShaderPassIndex = 0;
        public FilterMode FilterMode { get; set; }

        private RenderTargetIdentifier Source { get; set; }
        private RenderTargetHandle Destination { get; set; }

        RenderTargetHandle m_temporaryColorTexture;
        string m_profilerTag;

        /// <summary>
        /// Constructor.
        /// </summary>
        public BlitPass(RenderEvent renderPassEvent, Material blitMaterial, int blitShaderPassIndex, string tag)
        {
            this.renderPassEvent = renderPassEvent;
            this.blitMaterial = blitMaterial;
            this.blitShaderPassIndex = blitShaderPassIndex;
            m_profilerTag = tag;
            m_temporaryColorTexture.Init("_TemporaryBlitTexture");
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination)
        {
            this.Source = source;
            this.Destination = destination;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_profilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            // Can't read and write to same color target, create a temp render target to blit. 
            if (Destination == RenderTargetHandle.CameraTarget)
            {
                cmd.GetTemporaryRT(m_temporaryColorTexture.id, opaqueDesc, FilterMode);
                Blit(cmd, Source, m_temporaryColorTexture.Identifier(), blitMaterial, blitShaderPassIndex);
                Blit(cmd, m_temporaryColorTexture.Identifier(), Source);
            }
            else
            {
                Blit(cmd, Source, Destination.Identifier(), blitMaterial, blitShaderPassIndex);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (Destination == RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(m_temporaryColorTexture.id);
            }
        }
#endif
    }
}
