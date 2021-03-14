using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    //[CustomPropertyDrawer(typeof(AnimatedLocalizedString), true)]
    public class AnimatedLocalizedStringDrawer : AnimatedValueDrawer
    {
        private class TextStyles : BaseStyles
        {
            public GUIStyle text;

            protected override void InitializeStyles(bool isProSkin)
            {
                text = new GUIStyle(EditorStyles.textField);
                text.wordWrap = true;
            }
        }

        private static readonly TextStyles s_style = new TextStyles();
        private static readonly GUIContent s_content = new GUIContent();

        private LocalizedStringDrawer m_drawer;
        private float? m_width;

        public override void FetchAttributes(IEnumerable<WeavrAttribute> attributes)
        {
            //base.FetchAttributes(attributes);
            //if (attributes.FirstOrDefault(a => a is LongTextAttribute) is LongTextAttribute longText)
            //{
            //    m_drawer = new LocalizedStringDrawer(true);
            //}
        }

        protected override void DrawTarget(Rect position, SerializedProperty property, GUIContent label)
        {
            m_drawer.OnGUI(position, property, label);
        }

        protected override float GetTargetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            s_style.Refresh();
            if(m_drawer == null)
            {
                m_drawer = new LocalizedStringDrawer(fieldInfo.GetAttribute<LongTextAttribute>() != null);
            }
            return m_drawer.GetPropertyHeight(property, label);
        }
    }
}
