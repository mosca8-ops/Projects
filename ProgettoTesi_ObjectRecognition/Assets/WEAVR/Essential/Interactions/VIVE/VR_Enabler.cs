using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Advanced/VR Enabler")]
    public class VR_Enabler : MonoBehaviour
    {
        public enum EnableOption { IsImmersive, IsHolographic, IsNotImmersive }

        public EnableOption enableIf = EnableOption.IsNotImmersive;

        private void OnEnable()
        {
            switch (enableIf)
            {
                case EnableOption.IsImmersive:
#if WEAVR_VR
                    
                    gameObject.SetActive(XRSettings.enabled);
#else
                    gameObject.SetActive(false);
#endif
                    break;
                case EnableOption.IsHolographic:

                    break;
                case EnableOption.IsNotImmersive:
#if WEAVR_VR
                    gameObject.SetActive(!XRSettings.enabled);
#else
                    gameObject.SetActive(true);
#endif
                    break;
            }
        }
    }
}
