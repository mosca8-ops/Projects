using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(CallMethodAction), true)]
    class CallMethodActionEditor : ActionEditor
    {
        private static string[] s_hiddenMethodNames = {
            nameof(MonoBehaviour.IsInvoking),
            nameof(MonoBehaviour.CancelInvoke),

            nameof(MonoBehaviour.StartCoroutine),
            nameof(MonoBehaviour.StopCoroutine),
            nameof(MonoBehaviour.StopAllCoroutines),
            nameof(MonoBehaviour.SendMessage),
            nameof(MonoBehaviour.SendMessageUpwards),
            nameof(MonoBehaviour.CancelInvoke),
            nameof(MonoBehaviour.Invoke),
            nameof(MonoBehaviour.InvokeRepeating),
            nameof(MonoBehaviour.BroadcastMessage),
            nameof(MonoBehaviour.CompareTag),
        };


        private Object m_lastTarget;
        private float[] m_heights;

        private GUIContent m_methodLabel = new GUIContent("Method");

        protected SerializedProperty Parameters => serializedObject.FindProperty("m_parameters");

        protected string MethodId
        {
            get => serializedObject.FindProperty("m_methodId").stringValue;
            set => serializedObject.FindProperty("m_methodId").stringValue = value;
        }

        private bool m_initialized = false;
        private string m_lastMethodId;

        protected Method m_method;
        protected string m_methodName;
        protected List<GUIContent> m_parameterNames = new List<GUIContent>();

        protected CallMethodAction Action { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            Action = target as CallMethodAction;
        }

        protected override void DrawProperties(Rect rect, SerializedProperty targetProperty)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 120;

            var methodIdProperty = serializedObject.FindProperty("m_methodId");
            var parameters = serializedObject.FindProperty("m_parameters");
            var componentType = serializedObject.FindProperty("m_componentType");

            if (!m_initialized)
            {
                m_initialized = true;
                m_lastTarget = Action.Target;
                m_lastMethodId = methodIdProperty.stringValue;
                Action?.ValidateCurrentValues();
                if (m_lastTarget)
                {
                    for (int i = 0; i < parameters.arraySize; i++)
                    {
                        var value = parameters.GetArrayElementAtIndex(i).GetValueGetter()?.Invoke(target) as ParameterValue;
                        value.OnObjectChanged = (p, o, pO) => m_preRenderAction = () => SetObjectValue(p, o, pO);
                    }
                }
            }
            if (Action.Target != m_lastTarget)
            {
                var newTarget = Action.Target;
                if (!(newTarget && m_lastTarget && CallMethodAction.GetComponent(newTarget as GameObject, componentType.stringValue)))
                {
                    m_lastMethodId = null;
                    methodIdProperty.stringValue = string.Empty;
                    componentType.stringValue = string.Empty;
                    m_method = null;
                    ClearParameters(parameters);
                }
                m_lastTarget = newTarget;
            }

            DrawProperty(targetProperty, ref rect);
            
            rect.height = EditorGUIUtility.singleLineHeight;
            if (m_lastTarget)
            {
                if(m_lastMethodId != methodIdProperty.stringValue)
                {
                    m_lastMethodId = methodIdProperty.stringValue;
                    m_method = null;
                    m_methodLabel.image = null;
                    m_methodLabel.text = "Method";
                    m_methodName = "Select Method";
                }
                if (m_method == null && !string.IsNullOrEmpty(m_lastMethodId))
                {
                    m_method = MethodFactory.GetMethod(m_lastMethodId);
                    if(m_method != null)
                    {
                        var component = CallMethodAction.GetComponent(targetProperty.objectReferenceValue as GameObject, componentType.stringValue);
                        var objContent = component ? EditorGUIUtility.ObjectContent(component, component.GetType()) 
                                                   : EditorGUIUtility.ObjectContent(targetProperty.objectReferenceValue, typeof(GameObject));
                        m_methodLabel.image = objContent?.image;
                        m_methodLabel.text = string.IsNullOrEmpty(componentType.stringValue) ? "GameObject" : componentType.stringValue;
                        m_methodName = m_method.FullName;
                    }
                    else
                    {
                        m_methodLabel.image = null;
                        m_methodLabel.text = "Method";
                        m_methodName = "Select Method";
                    }
                }
                var methodRect = EditorGUI.PrefixLabel(rect, m_methodLabel);
                if (GUI.Button(methodRect, m_methodName, EditorStyles.popup))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach(var m in MethodFactory.GetValidMethods(typeof(GameObject))
                                                  .Where(m => m.ReturnType == typeof(void) || m.ReturnType == typeof(bool))
                                                  .Where(m => !s_hiddenMethodNames.Any(s => s == m.Name))
                                                  .OrderBy(m => m.Name)
                                                  .ThenBy(m => m.Parameters.Length))
                    {
                        menu.AddItem(new GUIContent($"GameObject/{m.FullName}"), m_method == m, () => m_preRenderAction = () =>
                        {
                            serializedObject.Update();
                            MethodId = m.Id;
                            componentType.stringValue = string.Empty;
                            ClearParameters(parameters);
                            m_lastMethodId = null;
                            ApplyChanges();
                        });
                    }

                    Dictionary<Type, int> typeIndices = new Dictionary<Type, int>();
                    foreach(var component in (m_lastTarget as GameObject).GetComponents<Component>())
                    {
                        if(!component || component.GetType().GetCustomAttribute<DoNotExposeAttribute>() != null) { continue; }

                        if(!typeIndices.TryGetValue(component.GetType(), out int index))
                        {
                            index = 0;
                        }
                        typeIndices[component.GetType()] = index + 1;

                        foreach(var m in MethodFactory.GetValidMethods(component.GetType())
                                                      .Where(m => m.ReturnType == typeof(void) || m.ReturnType == typeof(bool))
                                                      .Where(m => !s_hiddenMethodNames.Any(s => s == m.Name))
                                                      .OrderBy(m => m.Name)
                                                      .ThenBy(m => m.Parameters.Length))
                        {
                            var componentTypename = component.GetType().Name + (index > 0 ? $"[{index + 1}]" : string.Empty);
                            menu.AddItem(new GUIContent(componentTypename + $"/{m.FullName}"),
                                         componentTypename == componentType.stringValue && m_method == m,
                                         () => m_preRenderAction = () =>
                                         {
                                             serializedObject.Update();
                                             MethodId = m.Id;
                                             m_lastMethodId = null;
                                             componentType.stringValue = componentTypename;
                                             ClearParameters(parameters);
                                             ApplyChanges();
                                         });
                        }
                    }

                    menu.DropDown(rect);
                }

                // Parameters
                if(m_method != null && m_method.Parameters.Length > 0)
                {
                    if (parameters.arraySize == 0)
                    {
                        m_parameterNames.Clear();
                        for (int i = 0; i < m_method.Parameters.Length; i++)
                        {
                            parameters.InsertArrayElementAtIndex(i);
                            parameters.GetArrayElementAtIndex(i).FindPropertyRelative("m_paramId").intValue = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                            parameters.GetArrayElementAtIndex(i).FindPropertyRelative("m_typename").stringValue = m_method.Parameters[i].type.AssemblyQualifiedName;
                            m_parameterNames.Add(new GUIContent(EditorTools.NicifyName(m_method.Parameters[i].name)));
                        }
                        serializedObject.ApplyModifiedProperties();
                        serializedObject.Update();
                        for (int i = 0; i < m_method.Parameters.Length; i++)
                        {
                            var value = parameters.GetArrayElementAtIndex(i).GetValueGetter()?.Invoke(target) as ParameterValue;
                            value.OnObjectChanged = (p, o, pO) => m_preRenderAction = () => SetObjectValue(p, o, pO);
                        }
                    }
                    else if(m_parameterNames.Count == 0)
                    {
                        m_parameterNames.AddRange(m_method.Parameters.Select(p => new GUIContent(EditorTools.NicifyName(p.name))));
                    }
                    int length = Mathf.Min(m_heights.Length, parameters.arraySize, m_parameterNames.Count);
                    for (int i = 0; i < length; i++)
                    {
                        rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                        rect.height = m_heights[i];
                        EditorGUI.PropertyField(rect, parameters.GetArrayElementAtIndex(i), m_parameterNames[i], true);
                    }
                }
            }
            else
            {
                var style = EditorStyles.centeredGreyMiniLabel;
                bool wasRichText = style.richText;
                style.richText = true;
                GUI.Label(rect, $"No object at <color=#ffa500ff>Target</color>", style);
                style.richText = wasRichText;
                methodIdProperty.stringValue = string.Empty;
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }


        protected void ClearParameters(SerializedProperty parameters)
        {
            for (int i = 0; i < parameters.arraySize; i++)
            {
                var valProperty = parameters.GetArrayElementAtIndex(i).FindPropertyRelative("m_objectValue");
                SetObjectValue(valProperty.propertyPath, null, valProperty.objectReferenceValue);
            }
            parameters.ClearArray();
        }

        protected override float GetHeightInternal()
        {
            var parameters = serializedObject.FindProperty("m_parameters");
            if (m_heights == null || m_heights.Length != parameters.arraySize)
            {
                m_heights = new float[parameters.arraySize];
            }
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            for (int i = 0; i < m_heights.Length; i++)
            {
                m_heights[i] = EditorGUI.GetPropertyHeight(parameters.GetArrayElementAtIndex(i), true);
                height += m_heights[i] + EditorGUIUtility.standardVerticalSpacing;
            }
            return height;
        }
    }
}
