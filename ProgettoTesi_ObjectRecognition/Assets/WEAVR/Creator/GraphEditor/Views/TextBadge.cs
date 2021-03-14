using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{

    class TextBadge : IconBadge
    {
        public enum BadgeType
        {
            info = 0,
            warning = 1,
            error = 2,
        }

        private Label m_mainLabel;
        private Label m_text;
        
        public string mainText
        {
            get => m_mainLabel.text;
            set => m_mainLabel.text = value;
        }

        private BadgeType m_type = BadgeType.error;
        public BadgeType Type
        {
            get => m_type;
            set
            {
                if(m_type != value)
                {
                    RemoveFromClassList($"{m_type}Badge");
                    m_type = value;
                    AddToClassList($"{m_type}Badge");
                }
            }
        }

        public TextBadge(BadgeType type = BadgeType.info)
        {
            name = nameof(TextBadge);
            this.AddStyleSheetPath("Badges");
            RemoveFromClassList("iconBadge");
            AddToClassList("textBadge");
            m_type = type;
            AddToClassList($"{m_type}Badge");
            Remove(this.Q<Image>("icon"));
            m_mainLabel = new Label();
            m_mainLabel.name = "mainText";
            Add(m_mainLabel);
            m_text = this.Q<Label>("text");
            var tip = this.Q<Image>("tip");
            if(tip != null)
            {
                tip.image = null;
            }
            m_text?.RegisterCallback<AttachToPanelEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(AttachToPanelEvent evt)
        {
            if (m_text != null)
            {
                var length = m_text.style.left;
                length.value = length.value.value + m_mainLabel.layout.width;
                m_text.style.left = length;
            }
        }
    }
}
