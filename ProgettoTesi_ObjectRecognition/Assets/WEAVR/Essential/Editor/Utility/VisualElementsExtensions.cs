using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TXT.WEAVR.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR
{
    public static class VisualElementExtensions
    {
        static MethodInfo m_ValidateLayoutMethod;
        public static void InternalValidateLayout(this IPanel panel)
        {
            if (m_ValidateLayoutMethod == null)
                m_ValidateLayoutMethod = panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public);

            m_ValidateLayoutMethod.Invoke(panel, new object[] { });
        }

        static PropertyInfo m_OwnerPropertyInfo;

        public static object InternalGetGUIView(this IPanel panel)
        {
            if (m_OwnerPropertyInfo == null)
                m_OwnerPropertyInfo = panel.GetType().GetProperty("ownerObject", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public);


            return m_OwnerPropertyInfo.GetValue(panel, new object[] { });
        }

        static PropertyInfo s_ownerPropertyInfo;

        private static Vector2 GUIViewScreenPosition(this IPanel panel)
        {
            if (s_ownerPropertyInfo == null)
                {
                    s_ownerPropertyInfo = panel.GetType().GetProperty("ownerObject", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public);
                }

                if (s_ownerPropertyInfo != null)
                {
                    var guiView = s_ownerPropertyInfo.GetValue(panel);
                    if (guiView != null)
                    {
                        PropertyInfo screenPosition = guiView.GetType().GetProperty("screenPosition", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public);

                        if (screenPosition != null)
                        {
                            return ((Rect)screenPosition.GetValue(guiView)).position;
                        }
                    }
                }
                return Vector2.zero;
        }


        public static Vector2 ScreenToViewPosition(this IPanel element, Vector2 position)
        {
            return position - element.GUIViewScreenPosition();
        }

        public static Vector2 ViewToScreenPosition(this IPanel element, Vector2 position)
        {
            return position + element.GUIViewScreenPosition();
        }

        public static bool HasFocus(this VisualElement visualElement)
        {
            if (visualElement.panel == null) return false;
            return visualElement.panel.focusController.focusedElement == visualElement;
        }

        public static void AddStyleSheetPathWithSkinVariant(this VisualElement visualElement, string path)
        {
            visualElement.AddStyleSheetPath(path);
            //if (true)
            {
                visualElement.AddStyleSheetPath(path + "Dark");
            }
            /*else
            {
                visualElement.AddStyleSheetPath(path + "Light");
            }*/
        }

        public static void AddStyleSheetPath(this VisualElement elem, string styleSheet)
        {
            var ss = WeavrStyles.StyleSheets.GetStyleSheet(styleSheet, elem.GetType());
            if (ss && !elem.styleSheets.Contains(ss))
            {
                elem.styleSheets.Add(ss);
            }
        }

        public static Vector2 GlobalToBound(this VisualElement visualElement, Vector2 position)
        {
            return visualElement.worldTransform.inverse.MultiplyPoint3x4(position);
        }

        public static Vector2 BoundToGlobal(this VisualElement visualElement, Vector2 position)
        {
            /*do
            {*/
            position = visualElement.worldTransform.MultiplyPoint3x4(position);
            /*
            visualElement = visualElement.parent;
        }
        while (visualElement != null;)*/

            return position;
        }

        public static void ToggleInClassList(this VisualElement element, string className, bool enable)
        {
            element?.EnableInClassList(className, enable);
        }

    }
}
