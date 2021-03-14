using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/VR/Advanced/Camera Handler")]
    public class VR_CameraHandler : MonoBehaviour
    {

        private FallbackCameraHandler m_fallbackCamera = null;

        private void Awake()
        {
            var wFallbackCameras = FindObjectsOfType<FallbackCameraHandler>();
            if (wFallbackCameras != null && wFallbackCameras.Length > 0)
            {
                m_fallbackCamera = wFallbackCameras[0];
            }
        }

        private void OnEnable()
        {
            if (m_fallbackCamera != null)
            {
                m_fallbackCamera.ExitFallBackMode();
            }
        }
        private void OnDisable()
        {
            if (m_fallbackCamera != null)
            {
                m_fallbackCamera.EnterFallBackMode();
            }
        }

        private void OnDestroy()
        {
            if (m_fallbackCamera != null)
            {
                m_fallbackCamera.EnterFallBackMode();
            }
        }
    }
}
