using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Rendering.URP
{

    public class WEAVR_URP_Initialization : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void SetupHooks()
        {
            WorldSpaceAxes.AddCameraOverlay = AddCameraOverlay;
            WorldSpaceAxes.RemoveCameraOverlay = RemoveCameraOverlay;
        }

        private static void AddCameraOverlay(Camera target, Camera overlay)
        {
#if WEAVR_URP
            var cameraData = target.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if(cameraData && !cameraData.cameraStack.Contains(overlay))
            {
                cameraData.cameraStack.Add(overlay);
            }
#endif
        }

        private static void RemoveCameraOverlay(Camera target, Camera overlay)
        {
#if WEAVR_URP
            var cameraData = target.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (cameraData)
            {
                cameraData.cameraStack.Remove(overlay);
            }
#endif
        }
    }
}
