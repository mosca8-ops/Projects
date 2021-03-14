using TXT.WEAVR.Utility;
using UnityEngine;

#if WEAVR_VR
using Valve.VR;
using Valve.VR.InteractionSystem;
using BaseClass = Valve.VR.InteractionSystem.RenderModel;
#else
using BaseClass = UnityEngine.MonoBehaviour;
#endif

namespace TXT.WEAVR.Interaction
{
    [AddComponentMenu("WEAVR/VR/Advanced/Render Model")]
    public class VR_RenderModel : BaseClass
    {
#if WEAVR_VR
        //controller/trackpad
        public Material m_trackPadMaterial = null;
        private const string c_trackPadGOName = "trackpad";
        //controller/trigger
        public Material m_triggerMaterial = null;
        private const string c_triggerGOName = "trigger";
        //controller/lgrip.rgrip
        public Material m_gripMaterial = null;
        private const string c_leftGripGOName = "lgrip";
        private const string c_rightGripGOName = "rgrip";
        //controller/button
        public Material m_menuMaterial = null;
        private const string c_buttonGOName = "button";

        private bool m_initialized = false;

        private void OverrideMaterial(string iName, Material iNewMaterial)
        {
            if (iNewMaterial != null)
            {
                var wTransform = this.transform.FindRecursiveDepthFirst(iName);
                if (wTransform != null)
                {
                    var wMeshRenderer = wTransform.GetComponent<MeshRenderer>();
                    if (wMeshRenderer != null && wMeshRenderer.materials.Length > 0)
                    {
                        wMeshRenderer.material = iNewMaterial;
                    }
                }
            }
        }

        public void Update()
        {
            if (!m_initialized && controllerRenderers != null && controllerRenderers.Length > 0)
            {
                m_initialized = true;
                OverrideMaterial(c_trackPadGOName, m_trackPadMaterial);
                OverrideMaterial(c_rightGripGOName, m_gripMaterial);
                OverrideMaterial(c_leftGripGOName, m_gripMaterial);
                OverrideMaterial(c_buttonGOName, m_menuMaterial);
                OverrideMaterial(c_triggerGOName, m_triggerMaterial);
            }
        }
#endif
    }
}


