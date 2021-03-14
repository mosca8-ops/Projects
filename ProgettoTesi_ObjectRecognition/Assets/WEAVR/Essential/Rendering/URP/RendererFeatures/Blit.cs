using System.Collections.Generic;
using UnityEngine;

#if WEAVR_URP
using UnityEngine.Rendering.Universal;
using RenderEvent = UnityEngine.Rendering.Universal.RenderPassEvent;
using BaseClass = UnityEngine.Rendering.Universal.ScriptableRendererFeature;
#else
using RenderEvent = TXT.WEAVR.Rendering.URP.RenderPassEvent;
using BaseClass = UnityEngine.ScriptableObject;
#endif

namespace TXT.WEAVR.Rendering.URP
{
    public class Blit : BaseClass
    {
        [System.Serializable]
        public class BlitSettings
        {
            public RenderEvent Event = RenderEvent.AfterRenderingOpaques;
            
            public Material blitMaterial = null;
            public int blitMaterialPassIndex = -1;
            public Target destination = Target.Color;
            public string textureId = "_BlitPassTexture";
        }
        
        public enum Target
        {
            Color,
            Texture
        }

        public BlitSettings settings = new BlitSettings();

#if WEAVR_URP
        RenderTargetHandle m_RenderTextureHandle;

        BlitPass blitPass;

        public override void Create()
        {
            var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
            blitPass = new BlitPass(settings.Event, settings.blitMaterial, settings.blitMaterialPassIndex, name);
            m_RenderTextureHandle.Init(settings.textureId);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var src = renderer.cameraColorTarget;
            var dest = (settings.destination == Target.Color) ? RenderTargetHandle.CameraTarget : m_RenderTextureHandle;

            if (settings.blitMaterial == null)
            {
                Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
                return;
            }

            blitPass.Setup(src, dest);
            renderer.EnqueuePass(blitPass);
        }
#endif
    }
}

