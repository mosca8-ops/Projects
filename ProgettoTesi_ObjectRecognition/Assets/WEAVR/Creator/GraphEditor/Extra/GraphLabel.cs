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

    //[InitializeOnLoad]
    public class GraphLabel : GraphElement
    {
        private Label m_label;
        private Label m_shadowLabel;

        private Vector2 m_shadowOffset = Vector2.one;
        
        public float shadowOffsetX { get; set; }
        
        public float shadowOffsetY { get; set; }

        public Vector2 shadowOffset
        {
            get => m_shadowOffset;
            set
            {
                if(m_shadowOffset != value)
                {
                    m_shadowOffset = value;
                    RepositionShadow();
                }
            }
        }

        public bool useShadow
        {
            get => m_shadowLabel.visible;
            set
            {
                if(m_shadowLabel.visible != value)
                {
                    m_shadowLabel.visible = value;
                    RepositionShadow();
                }
            }
        }

        private void RepositionShadow()
        {
            m_shadowLabel.style.position = Position.Absolute;
            m_shadowLabel.style.top = m_label.layout.position.y + shadowOffsetY;
            m_shadowLabel.style.left = m_label.layout.position.x + shadowOffsetX;
        }

        public string text { get => m_label.text; set => m_label.text = m_shadowLabel.text = value; }

        public override bool ContainsPoint(Vector2 localPoint) => false;

        public GraphLabel()
        {
            var nodeBorder = new NodeBorder();
            nodeBorder.name = "gradient-border";
            Add(nodeBorder);

            m_shadowLabel = new Label();
            m_shadowLabel.name = "shadow-label";
            m_shadowLabel.visible = false;
            nodeBorder.Add(m_shadowLabel);

            m_label = new Label();
            m_label.name = "inner-label";
            nodeBorder.Add(m_label);

            capabilities = 0;
            style.overflow = Overflow.Hidden;
            cacheAsBitmap = true;
            layer = -1000;

            shadowOffsetX = shadowOffset.x;
            shadowOffsetY = shadowOffset.y;

            RegisterCallback<GeometryChangedEvent>(e => RepositionShadow());
        }

        protected override void OnCustomStyleResolved(ICustomStyle styles)
        {
            base.OnCustomStyleResolved(styles);
            
            if(styles.TryGetValue(new CustomStyleProperty<float>("--shadow-offset-x"), out float v)) { shadowOffsetX = v; } else { shadowOffsetX = 1; }
            if(styles.TryGetValue(new CustomStyleProperty<float>("--shadow-offset-y"), out float v1)) { shadowOffsetY = v1; } else { shadowOffsetY = 1; }

            RepositionShadow();
            //m_descriptionDefaultColor = m_description.style.color;
        }

        public override void SetPosition(Rect newPos)
        {
            style.position = Position.Absolute;
            style.left = newPos.x;
            style.top = newPos.y;

            RepositionShadow();
        }
    }
}
