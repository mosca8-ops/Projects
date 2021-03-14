using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TXT.WEAVR.ImpactSystem
{
    [CustomPropertyDrawer(typeof(HitAbsorber.TagArray))]
    public class TagArrayDrawer : PropertyDrawer
    {
        private string m_shortTags;
        private string ShortTags
        {
            get => m_shortTags;
            set
            {
                if(m_shortTags != value)
                {
                    m_shortTags = value.Length > 80 ? value.Substring(0, 77) + "..." : value;
                }
            }
        }

        private string m_newTagArray;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property = property.FindPropertyRelative(nameof(HitAbsorber.TagArray.tags));
            if(ShortTags == null)
            {
                ShortTags = property.stringValue;
            }

            if(m_newTagArray != null)
            {
                ShortTags = property.stringValue = m_newTagArray;
                m_newTagArray = null;
            }

            var buttonRect = EditorGUI.PrefixLabel(position, label);

            if (GUI.Button(buttonRect, ShortTags, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();

                var tags = UnityEditorInternal.InternalEditorUtility.tags;
                var tagArray = property.stringValue;
                for (int i = 0; i < tags.Length; i++)
                {
                    bool contains = false;
                    string newTagArray = null;
                    if (tagArray.Contains(tags[i]))
                    {
                        contains = true;
                        newTagArray = tagArray.Replace(tags[i] + ",", string.Empty).Replace(tags[i], string.Empty);
                    }
                    else
                    {
                        newTagArray = tagArray + tags[i] + ",";
                    }

                    newTagArray = newTagArray.Trim(',');
                    menu.AddItem(new GUIContent(tags[i]), contains, () => m_newTagArray = newTagArray);
                }

                menu.DropDown(buttonRect);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
