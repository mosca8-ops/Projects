using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    static class GraphExtensions
    {
        public static bool TryFind(this VisualElement root, Func<VisualElement, bool> findFunctor, out VisualElement elem)
        {
            elem = findFunctor(root) ? root : FindFirstInHierarchy(root, findFunctor);
            return elem != null;
        }

        private static VisualElement FindFirstInHierarchy(VisualElement element, Func<VisualElement, bool> findFunctor)
        {
            for (int i = 0; i < element.childCount; i++)
            {
                var child = element.ElementAt(i);
                if (findFunctor(child)) { return child; }
                var found = FindFirstInHierarchy(child, findFunctor);
                if (found != null) { return found; }
            }
            return null;
        }

        public static IEnumerable<VisualElement> FindElements(this VisualElement root, Func<VisualElement, bool> findFunctor)
        {
            List<VisualElement> foundElements = new List<VisualElement>();
            if (findFunctor(root))
            {
                foundElements.Add(root);
            }
            FindInHierarchy(root, findFunctor, foundElements);
            return foundElements;
        }

        private static void FindInHierarchy(VisualElement element, Func<VisualElement, bool> findFunctor, List<VisualElement> foundElements)
        {
            for (int i = 0; i < element.childCount; i++)
            {
                var child = element.ElementAt(i);
                if (findFunctor(child)) {
                    foundElements.Add(child);
                }
                FindInHierarchy(child, findFunctor, foundElements);
            }
        }
    }

    static class UIElementsAnimator
    {
        public static int duration_ms = 20;

        private class AnimData
        {
            public VisualElement element;
            public IVisualElementScheduledItem currentAnimation;
            public Rect originalLayout;
            public Rect originalWorldLayout;
            public StyleColor originalBackgroundColor;
            public StyleColor originalForegroundColor;
            public StyleColor originalBorderColor;

            public Vector3 originalPosition;
            public Quaternion originalRotation;
            public Vector3 originalScale;

            private Matrix4x4 originalMatrix;

            private bool m_layoutChanged;
            private bool m_transformChanged;
            private bool m_worldLayoutChanged;
            private bool m_backgroundColorChanged;
            private bool m_foregroundColorChanged;
            private bool m_borderColorChanged;

            public AnimData(VisualElement element)
            {
                this.element = element;
                Save();
            }

            public void Save()
            {
                originalPosition = element.transform.position;
                originalScale = element.transform.scale;
                originalRotation = element.transform.rotation;
                originalMatrix = element.transform.matrix;

                originalLayout = element.layout;
                originalWorldLayout = element.worldBound;
                originalBackgroundColor = element.style.backgroundColor;
                originalForegroundColor = element.style.color;
                originalBorderColor = element.style.borderColor;
            }

            public void Reset()
            {
                if (currentAnimation != null)
                {
                    currentAnimation.Until(() => true);
                    currentAnimation = null;
                }
                if (m_transformChanged)
                {
                    element.transform.rotation = originalRotation;
                    element.transform.position = originalPosition;
                    element.transform.scale = originalScale;
                }
                if(m_layoutChanged) element.SetLayout(originalLayout);
                if(m_backgroundColorChanged) element.style.backgroundColor = originalBackgroundColor;
                if(m_foregroundColorChanged) element.style.color = originalForegroundColor;
                if(m_borderColorChanged) element.style.borderColor = originalBorderColor;
            }

            public bool IsOriginalData()
            {
                return (!m_layoutChanged || originalLayout == element.layout)
                    && (!m_transformChanged || originalMatrix == element.transform.matrix)
                    && (!m_backgroundColorChanged || originalBackgroundColor.value == element.resolvedStyle.backgroundColor)
                    && (!m_foregroundColorChanged || originalForegroundColor.value == element.resolvedStyle.color)
                    && (!m_borderColorChanged || originalBorderColor.value == element.resolvedStyle.borderColor)
                    ;
            }

            public void Animate(Action<VisualElement, AnimData, float> animation)
            {
                currentAnimation = element.schedule.Execute(s => animation(element, this, s.deltaTime / 1000f)).Every(duration_ms)
                .Until(() =>
                {
                    if (IsOriginalData())
                    {
                        Reset();
                        s_runningAnimations.Remove(element);
                        return true;
                    }
                    return false;
                });
            }

            public void Animate(Action<VisualElement, AnimData, float> animation, int timeout_ms)
            {
                currentAnimation = element.schedule.Execute(s => animation(element, this, s.deltaTime / 1000f)).Every(duration_ms)
                .Until(() =>
                {
                    timeout_ms -= duration_ms;
                    if (timeout_ms <= 0 || IsOriginalData())
                    {
                        Reset();
                        s_runningAnimations.Remove(element);
                        return true;
                    }
                    return false;
                });
            }

            public Rect layout
            {
                get => element.layout;
                set
                {
                    if(element.layout != value)
                    {
                        element.SetLayout(value);
                        m_layoutChanged = true;
                    }
                }
            }

            public Vector3 position
            {
                get => element.transform.position;
                set
                {
                    if(element.transform.position != value)
                    {
                        element.transform.position = value;
                        m_transformChanged = true;
                    }
                }
            }

            public Vector3 scale
            {
                get => element.transform.scale;
                set
                {
                    if (element.transform.scale != value)
                    {
                        element.transform.scale = value;
                        m_transformChanged = true;
                    }
                }
            }

            public Quaternion rotation
            {
                get => element.transform.rotation;
                set
                {
                    if (element.transform.rotation != value)
                    {
                        element.transform.rotation = value;
                        m_transformChanged = true;
                    }
                }
            }

            public Color backgroundColor
            {
                get => element.resolvedStyle.backgroundColor;
                set => element.style.backgroundColor = ApplyColor(element.resolvedStyle.backgroundColor, value, ref m_backgroundColorChanged);
            }

            public Color foregroundColor
            {
                get => element.resolvedStyle.color;
                set => element.style.color = ApplyColor(element.resolvedStyle.color, value, ref m_foregroundColorChanged);
            }

            public Color borderColor
            {
                get => element.resolvedStyle.borderColor;
                set => element.style.borderColor = ApplyColor(element.resolvedStyle.borderColor, value, ref m_borderColorChanged);
            }

            private StyleColor ApplyColor(StyleColor original, Color value, ref bool valueChanged)
            {
                if(Equals(original.value, value)) { return original; }
                if (!valueChanged)
                {
                    valueChanged = true;
                    original = new StyleColor(value);
                }
                else
                {
                    original.value = value;
                }
                return original;
            }
        }

        private static Dictionary<VisualElement, AnimData> s_runningAnimations = new Dictionary<VisualElement, AnimData>();

        public static void Ping(this VisualElement element)
        {
            if(s_runningAnimations.TryGetValue(element, out AnimData data))
            {
                data.Reset();
                data.Save();
            }
            else
            {
                data = new AnimData(element);
                s_runningAnimations[element] = data;
            }

            var pingColor = Color.cyan;
            var resize = 1.25f;

            data.position -= new Vector3(data.layout.size.x, data.layout.size.y) * (1 - 1 / resize);
            var positionSpeed = (data.position - data.originalPosition).magnitude * 2;
            data.scale = data.originalScale * resize;
            var scaleSpeed = (data.scale - data.originalScale).magnitude * 2;
            data.backgroundColor = pingColor;
            data.Animate((e, d, dt) =>
            {
                data.scale = Vector3.MoveTowards(data.scale, data.originalScale, dt * scaleSpeed);
                data.position = Vector3.MoveTowards(data.position, data.originalPosition, dt * positionSpeed);
                data.backgroundColor = Color.Lerp(data.backgroundColor, data.originalBackgroundColor.value, dt * 0.5f);
            }, 1000);
        }

        public static void SetLayout(this VisualElement element, Rect layout)
        {
            element.style.top = layout.y;
            element.style.left = layout.x;
            element.style.width = layout.width;
            element.style.height = layout.height;
        }

        public static float GetSpecifiedValueOrDefault(this StyleFloat styleValue, float fallbackValue)
        {
            return styleValue.keyword == StyleKeyword.Null ? fallbackValue : styleValue.value;
        }

        public static int GetSpecifiedValueOrDefault(this StyleInt styleValue, int fallbackValue)
        {
            return styleValue.keyword == StyleKeyword.Null ? fallbackValue : styleValue.value;
        }

        public static Background GetSpecifiedValueOrDefault(this StyleBackground styleValue, Background fallbackValue)
        {
            return styleValue.keyword == StyleKeyword.Null ? fallbackValue : styleValue.value;
        }

        public static Color GetSpecifiedValueOrDefault(this StyleColor styleValue, Color fallbackValue)
        {
            return styleValue.keyword == StyleKeyword.Null ? fallbackValue : styleValue.value;
        }

        public static UnityEngine.UIElements.Cursor GetSpecifiedValueOrDefault(this StyleCursor styleValue, UnityEngine.UIElements.Cursor fallbackValue)
        {
            return styleValue.keyword == StyleKeyword.Null ? fallbackValue : styleValue.value;
        }

        public static T GetSpecifiedValueOrDefault<T>(this StyleEnum<T> styleValue, T fallbackValue) where T : struct, IConvertible
        {
            return styleValue.keyword == StyleKeyword.Null ? fallbackValue : styleValue.value;
        }

        public static Font GetSpecifiedValueOrDefault(this StyleFont styleValue, Font fallbackValue)
        {
            return styleValue.keyword == StyleKeyword.Null ? fallbackValue : styleValue.value;
        }

        public static Length GetSpecifiedValueOrDefault(this StyleLength styleValue, Length fallbackValue)
        {
            return styleValue.keyword == StyleKeyword.Null ? fallbackValue : styleValue.value;
        }
    }
}
