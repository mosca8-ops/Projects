using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{

    class Badge : IconBadge
    {
        public enum BadgeType
        {
            info = 0,
            warning = 1,
            error = 2,
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

        public Badge(BadgeType type = BadgeType.info)
        {
            this.AddStyleSheetPath("Badges");
            m_type = type;
            AddToClassList($"{m_type}Badge");
        }

        public bool TryAttachTo(VisualElement element, VisualElement parent, SpriteAlignment alignment)
        {
            if(parent == null || element.hierarchy.parent == null || element == null || element.panel != parent.panel)
            {
                return false;
            }
            parent.Add(this);
            AttachTo(element, alignment);
            return true;
            //return true;
        }
    }
}
