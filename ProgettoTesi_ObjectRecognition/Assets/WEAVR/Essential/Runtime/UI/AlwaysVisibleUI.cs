using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{

    [AddComponentMenu("WEAVR/UI/Always Visible UI")]
    public class AlwaysVisibleUI : MonoBehaviour
    {
        protected const int k_OverlayComparison = (int)UnityEngine.Rendering.CompareFunction.Always;
        protected const int k_NotOverlayComparison = (int)UnityEngine.Rendering.CompareFunction.Less;

        public bool ignoreChildren = false;

        protected const string k_AlphaTestProperty = "unity_GUIZTestMode";
        private List<(Graphic graphic, int overlay)> m_overlays = new List<(Graphic graphic, int overlay)>();

        private static Material s_defaultUIMaterial;

        private void OnEnable()
        {
            if (!s_defaultUIMaterial)
            {
                s_defaultUIMaterial = new Material(Weavr.Shaders.DefaultUI);
            }
            m_overlays = GetGraphics().Where(g => g.material && g.material.HasProperty(k_AlphaTestProperty))
                                                               .Select(g => (g, g.material.GetInt(k_AlphaTestProperty))).ToList();
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
                foreach (var graphic in GetGraphics())
                {
                    Material updatedMaterial = graphic.material == graphic.defaultMaterial ? new Material(graphic.material.shader == graphic.defaultMaterial.shader ? s_defaultUIMaterial : graphic.defaultMaterial) : graphic.material;
                    updatedMaterial.SetInt(k_AlphaTestProperty, k_OverlayComparison);
                    graphic.material = updatedMaterial;
                }
            }
        }

        private void RestoreIgnoreRenderDepth()
        {
            if (Application.isPlaying)
            {
                foreach (var (graphic, overlay) in m_overlays)
                {
                    Material updatedMaterial = graphic.material;// new Material(graphic.material);
                    updatedMaterial.SetInt(k_AlphaTestProperty, overlay);
                    graphic.material = updatedMaterial;
                }
                foreach (var graphic in GetGraphics().Where(g => !m_overlays.Any(p => p.graphic == g)))
                {
                    Material updatedMaterial = graphic.material;// new Material(graphic.material);
                    updatedMaterial.SetInt(k_AlphaTestProperty, k_NotOverlayComparison);
                    graphic.material = updatedMaterial;
                }
            }
        }
    }
}
