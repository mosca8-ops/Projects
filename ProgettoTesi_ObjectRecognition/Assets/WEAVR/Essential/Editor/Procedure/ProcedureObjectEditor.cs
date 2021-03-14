using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Profiling;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(ProcedureObject), true)]
    public class ProcedureObjectEditor : UnityEditor.Editor
    {

        #region [  STATIC PART  ]

        private static Dictionary<BaseAction, BaseActionEditor> s_actionEditors = new Dictionary<BaseAction, BaseActionEditor>();

        public static BaseActionEditor Get(BaseAction action)
        {
            if (s_actionEditors.TryGetValue(action, out BaseActionEditor editor) && editor && editor.target)
            {
                return editor;
            }

            if (editor)
            {
                if (Application.isPlaying)
                {
                    Destroy(editor);
                }
                else
                {
                    DestroyImmediate(editor);
                }
            }

            // Else create it
            // Get the interfaces
            var resolver = action.Procedure?.ReferencesResolver;
            if (resolver != null)
            {
                editor = CreateEditorWithContext(new Object[] { action }, resolver as Object) as BaseActionEditor;
            }
            else
            {
                editor = CreateEditor(action) as BaseActionEditor;
            }
            s_actionEditors[action] = editor;
            return editor;
        }

        private static Dictionary<BaseAnimationBlock, BaseAnimationBlockEditor> s_animBlocksEditors = new Dictionary<BaseAnimationBlock, BaseAnimationBlockEditor>();

        public static BaseAnimationBlockEditor Get(BaseAnimationBlock action)
        {
            if (s_animBlocksEditors.TryGetValue(action, out BaseAnimationBlockEditor editor) && editor && editor.target)
            {
                return editor;
            }

            if (editor)
            {
                if (Application.isPlaying)
                {
                    Destroy(editor);
                }
                else
                {
                    DestroyImmediate(editor);
                }
            }

            // Else create it
            // Get the interfaces
            var resolver = action.Procedure?.ReferencesResolver;
            if (resolver != null)
            {
                editor = CreateEditorWithContext(new Object[] { action }, resolver as Object) as BaseAnimationBlockEditor;
            }
            else
            {
                editor = CreateEditor(action) as BaseAnimationBlockEditor;
            }
            s_animBlocksEditors[action] = editor;
            return editor;
        }

        private static Dictionary<BaseCondition, BaseConditionEditor> s_conditionEditors = new Dictionary<BaseCondition, BaseConditionEditor>();

        public static BaseConditionEditor Get(BaseCondition condition)
        {
            if (!condition)
            {
                return null;
            }
            if (s_conditionEditors.TryGetValue(condition, out BaseConditionEditor editor) && editor && editor.target)
            {
                return editor;
            }

            if (editor)
            {
                if (Application.isPlaying)
                {
                    Destroy(editor);
                }
                else
                {
                    DestroyImmediate(editor);
                }
            }

            // Else create it
            // Get the interfaces
            var resolver = condition.Procedure?.ReferencesResolver;
            if (resolver != null)
            {
                editor = CreateEditorWithContext(new Object[] { condition }, resolver as Object) as BaseConditionEditor;
            }
            else
            {
                editor = CreateEditor(condition) as BaseConditionEditor;
            }
            s_conditionEditors[condition] = editor;
            return editor;
        }

        public static ProcedureObjectEditor Get(ProcedureObject elem)
        {
            if (elem is BaseAction action)
            {
                return Get(action);
            }
            if (elem is BaseCondition condition)
            {
                return Get(condition);
            }
            if(elem is BaseAnimationBlock animBlock)
            {
                return Get(animBlock);
            }
            return null;
        }

        public static void DestroyEditor(ProcedureObject elem)
        {
            ProcedureObjectEditor editor = null;
            if (elem is BaseAction action && s_actionEditors.TryGetValue(action, out BaseActionEditor actionEditor))
            {
                editor = actionEditor;
                s_actionEditors.Remove(action);
            }
            if (elem is BaseCondition condition && s_conditionEditors.TryGetValue(condition, out BaseConditionEditor conditionEditor))
            {
                editor = conditionEditor;
                s_conditionEditors.Remove(condition);
            }
            if (elem is BaseAnimationBlock animBlock && s_animBlocksEditors.TryGetValue(animBlock, out BaseAnimationBlockEditor animEditor))
            {
                editor = animEditor;
                s_animBlocksEditors.Remove(animBlock);
            }

            if (editor)
            {
                if (Application.isPlaying)
                {
                    Destroy(editor);
                }
                else
                {
                    DestroyImmediate(editor);
                }
            }
        }

        public static List<T> GetEditors<T>(Func<T, bool> filter = null) where T : class
        {
            List<T> result = new List<T>();
            if(filter != null)
            {
                result.AddRange(s_actionEditors.Where(p => p.Value is T t && filter(t)).Select(p => p.Value).Cast<T>());
                result.AddRange(s_conditionEditors.Where(p => p.Value is T t && filter(t)).Select(p => p.Value).Cast<T>());
            }
            else
            {
                result.AddRange(s_actionEditors.Where(p => p.Value is T).Select(p => p.Value).Cast<T>());
                result.AddRange(s_conditionEditors.Where(p => p.Value is T).Select(p => p.Value).Cast<T>());
            }
            return result;
        }

        public static List<ProcedureObjectEditor> GetEditorsOf<T>(Func<T, bool> filter = null) where T : ProcedureObject
        {
            List<ProcedureObjectEditor> result = new List<ProcedureObjectEditor>();
            if (filter != null)
            {
                result.AddRange(s_actionEditors.Where(p => p.Key is T t && filter(t)).Select(p => p.Value));
                result.AddRange(s_conditionEditors.Where(p => p.Key is T t && filter(t)).Select(p => p.Value));
            }
            else
            {
                result.AddRange(s_actionEditors.Where(p => p.Key is T).Select(p => p.Value));
                result.AddRange(s_conditionEditors.Where(p => p.Key is T).Select(p => p.Value));
            }
            return result;
        }


        public static void ClearAllEditors(bool destroyAsWell = false)
        {
            if (destroyAsWell)
            {
                if (Application.isPlaying)
                {
                    foreach (var pair in s_actionEditors)
                    {
                        Destroy(pair.Value);
                    }

                    foreach (var pair in s_conditionEditors)
                    {
                        Destroy(pair.Value);
                    }
                }
                else
                {
                    foreach (var pair in s_actionEditors)
                    {
                        DestroyImmediate(pair.Value);
                    }

                    foreach (var pair in s_conditionEditors)
                    {
                        DestroyImmediate(pair.Value);
                    }
                }
            }
            s_actionEditors.Clear();
            s_conditionEditors.Clear();
            s_animBlocksEditors.Clear();
        }

        #endregion

        protected Dictionary<string, Type> m_propertyTypes = new Dictionary<string, Type>();
        protected Dictionary<string, float> m_propertyHeights = new Dictionary<string, float>();
        protected Dictionary<string, Action<object, object>> m_propertySetters = new Dictionary<string, Action<object, object>>();
        protected Dictionary<string, GUIContent> m_guiContents = new Dictionary<string, GUIContent>();
        protected GUIContent m_tempContent = new GUIContent();
        protected Action m_preRenderAction;

        protected HashSet<string> m_searchProperties;
        
        protected virtual void OnEnable()
        {
            if (target)
            {
                SearchHub.Current.SearchValueChanged -= SearchHub_SearchValueChanged;
                SearchHub.Current.SearchValueChanged += SearchHub_SearchValueChanged;
                var searchResult = SearchHub.Current.SearchCached(target);
                if (searchResult != null && searchResult.IsValid)
                {
                    m_searchProperties = new HashSet<string>(searchResult.Properties.Select(p => p.Split('.')[0]));
                }
                var property = serializedObject.FindProperty("m_Script");
                while(property.Next(property.propertyType == SerializedPropertyType.Generic))
                {
                    if(property.type == nameof(GenericValue))
                    {
                        var value = property.GetValueGetter()?.Invoke(target) as GenericValue;
                        value.OnObjectChanged = (p, o, pO) => m_preRenderAction = () => SetObjectValue(p, o, pO);
                    }
                    //else if((assignableAttribute = property.GetAttribute<AssignableFromAttribute>()) != null)
                    //{
                    //    assignableAttribute = property.GetAttribute<AssignableFromAttribute>();
                    //    assignableAttribute.OnObjectChanged = (p, o, pO) => m_preRenderAction = () => SetObjectValue(p, o, pO);
                    //}
                    else if(property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var tooltipAttr = property.GetAttribute<TooltipAttribute>();
                        if(tooltipAttr != null)
                        {
                            m_guiContents[property.propertyPath] = new GUIContent(property.displayName, tooltipAttr.tooltip);
                        }
                    }
                }
            }
        }

        protected virtual void OnDisable()
        {
            SearchHub.Current.SearchValueChanged -= SearchHub_SearchValueChanged;
        }

        private void SearchHub_SearchValueChanged(string newValue)
        {
            var searchResult = SearchHub.Current.SearchCached(target);
            if(searchResult != null && searchResult.IsValid)
            {
                m_searchProperties = new HashSet<string>(searchResult.Properties.Select(p => p.Split('.')[0]));
            }
            else
            {
                m_searchProperties = null;
            }
        }

        public virtual void DrawFull(Rect rect)
        {

        }

        protected void DrawDebugLens(Rect rect)
        {
            Event e = Event.current;
            if (e.alt && e.type != EventType.Used && GUIUtility.GUIToScreenRect(rect).Contains(GUIUtility.GUIToScreenPoint(e.mousePosition)))
            {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Zoom);
                if (e.type == EventType.MouseUp)
                {
                    Selection.activeObject = target;
                    e.Use();
                }
            }
        }

        protected virtual void DrawProperty(SerializedProperty property, ref Rect rect, GUIContent label = null)
        {
            rect.height = m_propertyHeights.TryGetValue(property.propertyPath, out float height) ? height : EditorGUI.GetPropertyHeight(property, true);
            if(m_searchProperties != null && Event.current.type == EventType.Repaint && m_searchProperties.Contains(property.propertyPath))
            {
                EditorGUI.DrawRect(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), WeavrStyles.Colors.transparentYellow);
            }
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                if(rect.height <= EditorGUIUtility.standardVerticalSpacing)
                {
                    rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                    return;
                }
                Profiler.BeginSample("ObjectRefDraw");
                var type = GetPropertyType(property);
                if (type != null && (typeof(Component).IsAssignableFrom(type) || typeof(GameObject) == type))
                {
                    if(!m_guiContents.TryGetValue(property.propertyPath, out GUIContent content))
                    {
                        m_tempContent.text = property.displayName;
                        m_tempContent.tooltip = property.tooltip;
                        content = m_tempContent;
                    }

                    if(label != null) { content = label; }
                    
                    //var obj = EditorGUI.ObjectField(rect,
                    //                                content,
                    //                                property.objectReferenceValue,
                    //                                type, true);
                    var obj = WeavrGUI.DraggableObjectField(rect, rect,
                                                    content,
                                                    property.objectReferenceValue,
                                                    type, true);
                    if (property.objectReferenceValue != obj)
                    {
                        var propertyPath = property.propertyPath;
                        var prevObj = property.objectReferenceValue;
                        m_preRenderAction = () => SetObjectValue(propertyPath, obj, prevObj);
                    }
                }
                else if (property.objectReferenceValue is BaseAction)
                {
                    Get(property.objectReferenceValue as BaseAction).DrawFull(rect);
                }
                else if (property.objectReferenceValue is BaseCondition)
                {
                    float moveX = BaseConditionEditor.AllStyles.notToggle.fixedWidth + 2;
                    rect.x += moveX;
                    rect.y += EditorGUIUtility.standardVerticalSpacing;
                    rect.width -= moveX;
                    Get(property.objectReferenceValue as BaseCondition).DrawFull(rect);
                }
                else
                {
                    //property.objectReferenceValue = EditorGUI.ObjectField(rect,
                    //                                property.displayName,
                    //                                property.objectReferenceValue,
                    //                                type, true);
                    if (label != null)
                    {
                        EditorGUI.PropertyField(rect, property, label);
                    }
                    else
                    {
                        EditorGUI.PropertyField(rect, property);
                    }
                }
                Profiler.EndSample();
            }
            else if (property.isArray
                && property.arraySize > 0
                && property.isExpanded
                && property.propertyType == SerializedPropertyType.Generic
                && property.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.ObjectReference
                && RequireSceneObject(property.GetArrayElementAtIndex(0)))
            {

                Profiler.BeginSample("GenericObjectRefDraw");
                Rect innerRect = rect;
                innerRect.height = EditorGUIUtility.singleLineHeight;

                if (label != null)
                {
                    EditorGUI.PropertyField(innerRect, property, label);
                }
                else
                {
                    EditorGUI.PropertyField(innerRect, property);
                }

                EditorGUI.indentLevel++;
                innerRect.y += innerRect.height + EditorGUIUtility.standardVerticalSpacing;
                property.arraySize = EditorGUI.DelayedIntField(innerRect, "Size", property.arraySize);

                innerRect.y += innerRect.height + EditorGUIUtility.standardVerticalSpacing;
                for (int i = 0; i < property.arraySize; i++)
                {
                    DrawProperty(property.GetArrayElementAtIndex(i), ref innerRect, label);
                }
                EditorGUI.indentLevel--;
                Profiler.EndSample();
            }
            else
            {
                Profiler.BeginSample("GenericDraw");
                if (label != null)
                {
                    EditorGUI.PropertyField(rect, property, label, true);
                }
                else
                {
                    EditorGUI.PropertyField(rect, property, true);
                }
                Profiler.EndSample();
            }
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
        }

        protected bool RequireSceneObject(SerializedProperty property)
        {
            var type = GetPropertyType(property);
            return type != null && (typeof(Component).IsAssignableFrom(type) || typeof(GameObject) == type);
        }

        protected Type GetPropertyType(SerializedProperty property)
        {
            //if (property.objectReferenceValue != null)
            //{
            //    return property.objectReferenceValue.GetType();
            //}
            if (!m_propertyTypes.TryGetValue(property.propertyPath, out Type type))
            {
                type = property.GetPropertyType();
                m_propertyTypes[property.propertyPath] = type;
            }
            return type;
        }

        public void RegisterObjectValueChange(string propertyName, Object value, Object prevValue)
        {
            m_preRenderAction = () => SetObjectValue(propertyName, value, prevValue);
        }

        protected void SetObjectValue(string propertyName, Object value, Object prevValue)
        {
            if(!m_propertySetters.TryGetValue(propertyName, out Action<object, object> setter))
            {
                setter = target.GetType().ValuePathSet(propertyName);
                m_propertySetters[propertyName] = setter;
            }
            setter?.Invoke(target, value);
            if(target is ProcedureObject procedureObject)
            {
                if (target is IRequiresValidation item)
                {
                    item.OnValidate();
                }
                procedureObject.Modified();
                var refTable = procedureObject.Procedure?.Graph.ReferencesTable;
                if (refTable)
                {
                    if (prevValue)
                    {
                        refTable.Unregister(procedureObject, prevValue, propertyName);
                    }
                    if (value)
                    {
                        refTable.Register(procedureObject, value, propertyName);
                    }
                }
            }
        }

        protected void ApplyChanges()
        {
            if (serializedObject.ApplyModifiedProperties())
            {
                if (target is IRequiresValidation item)
                {
                    item.OnValidate();
                }
                (target as ProcedureObject).Modified();
            }
        }

        protected virtual float GetPropertyHeight(SerializedProperty property)
        {
            float height = 0;
            if(property.propertyType == SerializedPropertyType.ObjectReference)
            {
                if(property.objectReferenceValue is BaseAction)
                {
                    height = Get(property.objectReferenceValue as BaseAction).GetHeight();
                    m_propertyHeights[property.propertyPath] = height;
                    return height;
                }
                else if(property.objectReferenceValue is BaseCondition)
                {
                    height = Get(property.objectReferenceValue as BaseCondition).GetHeight();
                    m_propertyHeights[property.propertyPath] = height;
                    return height;
                }
            }

            height = EditorGUI.GetPropertyHeight(property, true);
            m_propertyHeights[property.propertyPath] = height;
            return height;
        }


        #region [  SEALED METHODS  ]

        public sealed override void DrawPreview(Rect previewArea)
        {
            base.DrawPreview(previewArea);
        }

        public sealed override bool HasPreviewGUI()
        {
            return base.HasPreviewGUI();
        }

        public sealed override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);
        }

        public sealed override void OnPreviewSettings()
        {
            base.OnPreviewSettings();
        }

        public sealed override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnInteractivePreviewGUI(r, background);
        }

        public sealed override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        #endregion
    }
}
