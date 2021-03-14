using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(ReorderableListAttribute))]
    public class ReorderableAttributeDrawer : ComposablePropertyDrawer
    {

        private ReorderableList m_reorderableList;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ReorderableListAttribute attr = (ReorderableListAttribute)attribute;

            if (m_reorderableList == null)
            {
                if (property.isArray)
                {
                    m_reorderableList = new ReorderableList(property.serializedObject, property, true, true, false, false);
                }
                else if (fieldInfo.FieldType.GetInterfaces().Contains(typeof(IList))){
                    var list = fieldInfo.GetValue(property.serializedObject.targetObject) as IList;
                    if (list != null)
                    {
                        m_reorderableList = new ReorderableList(list, list.GetType().GetGenericArguments()[0], true, true, false, false);
                    }
                }
                else
                {
                    base.OnGUI(position, property, label);
                    return;
                }
            }

            m_reorderableList.DoList(position);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            //ReorderableListAttribute attr = (ReorderableListAttribute)attribute;
            //return property.isArray || (attr.Collapsing && !property.isExpanded) ? base.GetPropertyHeight(property, label) : EditorGUIUtility.singleLineHeight + Editor;
            if (!property.isArray)
            {
                return base.GetPropertyHeight(property, label);
            }
            if (m_reorderableList == null)
            {
                m_reorderableList = new ReorderableList(property.serializedObject, property, true, true, false, false);
            }
            return m_reorderableList.GetHeight();
        }
    }
}