using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core.DataTypes;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.States
{

    public static class UIComponents
    {
        public static void RegisterStates()
        {
            SpecialStateContainers.Register(c => new RectTransformState());
            SpecialStateContainers.Register(c => new TextState());
            SpecialStateContainers.Register(c => new ImageState());
        }

        public class RectTransformState : SpecialComponentState<RectTransform>
        {
            bool m_active;
            Vector3 m_localPosition;
            Quaternion m_localRotation;
            Vector3 m_localScale;
            Transform m_parent;

            Vector2 offsetMax;
            Vector2 offsetMin;
            Vector3 anchoredPosition3D;
            Vector2 pivot;
            Vector2 sizeDelta;
            Vector2 anchorMax;
            Vector2 anchoredPosition;
            Vector2 anchorMin;

            public override bool Snapshot()
            {
                m_active = m_component.gameObject.activeSelf;
                m_localPosition = m_component.localPosition;
                m_localRotation = m_component.localRotation;
                m_localScale = m_component.localScale;
                m_parent = m_component.parent;

                offsetMax = m_component.offsetMax;
                offsetMin = m_component.offsetMin;
                anchoredPosition3D = m_component.anchoredPosition3D;
                pivot = m_component.pivot;
                sizeDelta = m_component.sizeDelta;
                anchorMax = m_component.anchorMax;
                anchoredPosition = m_component.anchoredPosition;
                anchorMin = m_component.anchorMin;

                return true;
            }

            protected override bool Restore(RectTransform t)
            {
                t.gameObject.SetActive(m_active);
                t.localPosition = m_localPosition;
                t.localRotation = m_localRotation;
                t.localScale = m_localScale;
                t.parent = m_parent;

                t.offsetMax = offsetMax;
                t.offsetMin = offsetMin;
                t.anchoredPosition3D = anchoredPosition3D;
                t.pivot = pivot;
                t.sizeDelta = sizeDelta;
                t.anchorMax = anchorMax;
                t.anchoredPosition = anchoredPosition;
                t.anchorMin = anchorMin;

                return true;
            }
        }

        public class TextState : SpecialComponentState<Text>
        {
            //private int                 m_resizeTextMinSize ;
            //private int                 m_resizeTextMaxSize ;
            //private bool                m_alignByGeometry ;
            //private bool                m_supportRichText ;
            private TextAnchor m_alignment;
            private int m_fontSize;
            private HorizontalWrapMode m_horizontalOverflow;
            private VerticalWrapMode m_verticalOverflow;
            private FontStyle m_fontStyle;
            private bool m_resizeTextForBestFit;
            private float m_lineSpacing;
            private Font m_font;
            private string m_text;
            private bool m_raycastTarget;
            private Color m_color;
            private MaterialData m_material;

            protected override bool Restore(Text t)
            {
                //t.resizeTextMinSize = m_resizeTextMinSize;
                //t.resizeTextMaxSize = m_resizeTextMaxSize;
                //t.alignByGeometry = m_alignByGeometry;
                //t.supportRichText = m_supportRichText;
                t.alignment = m_alignment;
                t.fontSize = m_fontSize;
                t.horizontalOverflow = m_horizontalOverflow;
                t.verticalOverflow = m_verticalOverflow;
                t.fontStyle = m_fontStyle;
                t.resizeTextForBestFit = m_resizeTextForBestFit;
                t.lineSpacing = m_lineSpacing;
                t.font = m_font;
                t.text = m_text;
                t.raycastTarget = m_raycastTarget;
                t.color = m_color;
                t.material = m_material.Restore();

                return true;
            }

            public override bool Snapshot()
            {
                //m_resizeTextMinSize = m_component.resizeTextMinSize;
                //m_resizeTextMaxSize = m_component.resizeTextMaxSize;
                //m_alignByGeometry = m_component.alignByGeometry;
                //m_supportRichText = m_component.supportRichText;
                m_alignment = m_component.alignment;
                m_fontSize = m_component.fontSize;
                m_horizontalOverflow = m_component.horizontalOverflow;
                m_verticalOverflow = m_component.verticalOverflow;
                m_fontStyle = m_component.fontStyle;
                m_resizeTextForBestFit = m_component.resizeTextForBestFit;
                m_lineSpacing = m_component.lineSpacing;
                m_font = m_component.font;
                m_text = m_component.text;
                m_raycastTarget = m_component.raycastTarget;
                m_color = m_component.color;
                m_material.Snapshot(m_component.material);

                return true;
            }
        }

        public class ImageState : SpecialComponentState<Image>
        {
            int m_fillOrigin;
            bool m_fillClockwise;
            Image.FillMethod m_fillMethod;
            bool m_fillCenter;
            bool m_preserveAspect;
            Sprite m_sprite;
            float m_fillAmount;
            private bool m_raycastTarget;
            private Color m_color;
            private MaterialData m_material;

            protected override bool Restore(Image i)
            {
                i.fillOrigin = m_fillOrigin;
                i.fillClockwise = m_fillClockwise;
                i.fillMethod = m_fillMethod;
                i.fillCenter = m_fillCenter;
                i.preserveAspect = m_preserveAspect;
                i.sprite = m_sprite;
                i.fillAmount = m_fillAmount;
                i.raycastTarget = m_raycastTarget;
                i.color = m_color;
                i.material = m_material.Restore();

                return true;
            }

            public override bool Snapshot()
            {
                m_fillOrigin = m_component.fillOrigin;
                m_fillClockwise = m_component.fillClockwise;
                m_fillMethod = m_component.fillMethod;
                m_fillCenter = m_component.fillCenter;
                m_preserveAspect = m_component.preserveAspect;
                m_sprite = m_component.sprite;
                m_fillAmount = m_component.fillAmount;
                m_raycastTarget = m_component.raycastTarget;
                m_color = m_component.color;
                m_material.Snapshot(m_component.material);

                return true;
            }
        }
    }
}
