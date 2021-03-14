using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{

    [AddComponentMenu("WEAVR/UI/Cull UI")]
    public class CullUI : MonoBehaviour
    {
        protected const int k_NoCull = (int)UnityEngine.Rendering.CullMode.Off;

        public bool ignoreChildren = false;
        public UnityEngine.Rendering.CullMode hidePart = UnityEngine.Rendering.CullMode.Back;

        protected const string k_CullProperty = "_Cull";
        private List<(Graphic graphic, int cull)> m_culls = new List<(Graphic graphic, int cull)>();

        private static Material s_defaultUIMaterial;

        private void OnEnable()
        {
            if (!s_defaultUIMaterial)
            {
                s_defaultUIMaterial = new Material(Weavr.Shaders.DefaultUI);
            }
            m_culls = GetGraphics().Where(g => g.material && g.material.HasProperty(k_CullProperty))
                                                               .Select(g => (g, g.material.GetInt(k_CullProperty))).ToList();
            ApplyIgnoreRenderDepth();
        }

        private Graphic[] GetGraphics()
        {
            return ignoreChildren ? GetComponents<Graphic>() : GetComponentsInChildren<Graphic>(true);
        }

        private void OnDisable()
        {
            RestoreIgnoreRenderDepth();
        }

        private void ApplyIgnoreRenderDepth()
        {
            if (Application.isPlaying)
            {
                int hide = (int)hidePart;
                foreach (var graphic in GetGraphics())
                {
                    Material updatedMaterial = graphic.material == graphic.defaultMaterial ? new Material(s_defaultUIMaterial ? s_defaultUIMaterial : graphic.material) : graphic.material;
                    updatedMaterial.SetInt(k_CullProperty, hide);
                    graphic.material = updatedMaterial;
                }
            }
        }

        private void RestoreIgnoreRenderDepth()
        {
            if (Application.isPlaying)
            {
                foreach (var (graphic, cull) in m_culls)
                {
                    Material updatedMaterial = graphic.material;// new Material(graphic.materialForRendering);
                    updatedMaterial.SetInt(k_CullProperty, cull);
                    graphic.material = updatedMaterial;
                }
                foreach (var graphic in GetGraphics().Where(g => !m_culls.Any(p => p.graphic == g)))
                {
                    Material updatedMaterial = graphic.material;// new Material(graphic.material);
                    updatedMaterial.SetInt(k_CullProperty, k_NoCull);
                    graphic.material = updatedMaterial;
                }
            }
        }
    }
}
