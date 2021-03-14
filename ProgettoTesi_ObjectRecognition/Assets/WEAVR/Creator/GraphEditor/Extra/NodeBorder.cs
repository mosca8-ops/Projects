using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using System;

using UnityObject = UnityEngine.Object;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace TXT.WEAVR.Procedure
{
    public class NodeBorderFactory : UxmlFactory<NodeBorder>
    { }

    //[InitializeOnLoad]
    public class NodeBorder : ImmediateModeElement, IDisposable
    {
        Material m_Mat;

        static Mesh s_Mesh;

        public NodeBorder()
        {
            RecreateResources();

            RegisterCallback<CustomStyleResolvedEvent>(evt => OnCustomStyleResolved(evt.customStyle));

            startColor = resolvedStyle.borderTopColor;
            endColor = resolvedStyle.borderBottomColor;
        }

        void RecreateResources()
        {
            if (s_Mesh == null)
            {
                s_Mesh = new Mesh();
                int verticeCount = 16;

                var vertices = new Vector3[verticeCount];
                var uvsBorder = new Vector2[verticeCount];

                for (int ix = 0; ix < 4; ++ix)
                {
                    for (int iy = 0; iy < 4; ++iy)
                    {
                        vertices[ix + iy * 4] = new Vector3(ix < 2 ? -1 : 1, iy < 2 ? -1 : 1, 0);
                        uvsBorder[ix + iy * 4] = new Vector2(ix == 0 || ix == 3 ? 1 : 0, iy == 0 || iy == 3 ? 1 : 0);
                    }
                }

                var indices = new int[4 * 8];

                for (int ix = 0; ix < 3; ++ix)
                {
                    for (int iy = 0; iy < 3; ++iy)
                    {
                        int quadIndex = (ix + iy * 3);
                        if (quadIndex == 4)
                            continue;
                        else if (quadIndex > 4)
                            --quadIndex;

                        int vertIndex = quadIndex * 4;
                        indices[vertIndex] = ix + iy * 4;
                        indices[vertIndex + 1] = ix + (iy + 1) * 4;
                        indices[vertIndex + 2] = ix + 1 + (iy + 1) * 4;
                        indices[vertIndex + 3] = ix + 1 + iy * 4;
                    }
                }

                s_Mesh.vertices = vertices;
                s_Mesh.uv = uvsBorder;
                s_Mesh.SetIndices(indices, MeshTopology.Quads, 0);
            }

            if (!m_Mat)
            {
                m_Mat = new Material(Shader.Find("Hidden/WEAVR/GradientBorder"));
            }
        }

        void IDisposable.Dispose()
        {
            UnityObject.DestroyImmediate(m_Mat);
        }

        public Color startColor { get; set; }
        
        public Color endColor { get; set; }

        public float borderWidth { get; set; }

        protected virtual void OnCustomStyleResolved(ICustomStyle styles)
        {
            if(styles.TryGetValue(new CustomStyleProperty<Color>("--start-color"), out Color v)) { startColor = v; }
            if(styles.TryGetValue(new CustomStyleProperty<Color>("--end-color"), out Color v2)) { endColor = v2; }
            if(styles.TryGetValue(new CustomStyleProperty<float>("--border-width"), out float v3)) { borderWidth = v3; }
        }

        protected override void ImmediateRepaint()
        {
            RecreateResources();
            var view = GetFirstAncestorOfType<GraphView>();
            if (view != null && m_Mat != null)
            {
                float radius = resolvedStyle.borderTopLeftRadius;
                float realBorder = borderWidth * view.scale;

                float finalBorder = realBorder < 1.75f ? 1.75f / view.scale : borderWidth;

                Vector4 size = new Vector4(layout.width * .5f, layout.height * 0.5f, 0, 0);
                m_Mat.SetVector("_Size", size);
                m_Mat.SetFloat("_Border", finalBorder);
                m_Mat.SetFloat("_Radius", radius);

                m_Mat.SetColor("_ColorStart", (QualitySettings.activeColorSpace == ColorSpace.Linear) ? startColor.gamma : startColor);
                m_Mat.SetColor("_ColorEnd", (QualitySettings.activeColorSpace == ColorSpace.Linear) ? endColor.gamma : endColor);

                m_Mat.SetPass(0);

                Graphics.DrawMeshNow(s_Mesh, Matrix4x4.Translate(new Vector3(size.x, size.y, 0)));
            }
        }
    }
}
