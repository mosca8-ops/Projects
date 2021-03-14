using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if WEAVR_URP
using UnityEngine.Rendering.Universal;
using BaseClass =
#if WEAVR_URP_AR
        UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature;
#else
        UnityEngine.Rendering.Universal.ScriptableRendererFeature;
#endif
#else
using BaseClass = UnityEngine.ScriptableObject;
#endif

namespace TXT.WEAVR.Rendering.URP
{
    public class ARBackground : BaseClass
    {

#if WEAVR_URP && !WEAVR_URP_AR
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            
        }

        public override void Create()
        {
            
        }
#endif
    }
}