using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UEditor = UnityEditor.Editor;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(BillboardAction), true)]
    class BillboardActionEditor : ActionEditor
    {

        private BillboardAction m_action;
        private bool m_showBillboard;
        private bool m_showHandles;
        private float[] m_modifierHeights;
        private List<BillboardModifierEditor> m_editors;
        private float m_totalListHeight;

        private Tool m_prevTool;
        private Billboard m_lastSample;

        private static Dictionary<Type, Type> s_modifierTypes;
        public static IReadOnlyDictionary<Type, Type> ModifierTypes
        {
            get
            {
                if (s_modifierTypes == null)
                {
                    s_modifierTypes = new Dictionary<Type, Type>();
                    foreach (var modifier in typeof(BillboardModifier).GetAllSubclasses())
                    {
                        if (modifier.IsAbstract || modifier.IsGenericType || modifier == typeof(BillboardModifier)) { continue; }

                        var modifierType = modifier;
                        while (modifierType.BaseType != typeof(BillboardModifier))
                        {
                            modifierType = modifierType.BaseType;
                        }
                        var genericTypes = modifierType.GenericTypeArguments;
                        foreach (var genType in genericTypes)
                        {
                            if (typeof(Component).IsAssignableFrom(genType))
                            {
                                s_modifierTypes[genType] = modifier;
                            }
                        }
                    }
                    foreach (var kvp in EditorTools.GetAttributesWithTypes<BillboardModifierAttribute>())
                    {
                        s_modifierTypes[kvp.Key.ElementType] = kvp.Value;
                    }
                }
                return s_modifierTypes;
            }
        }

        private class Styles : BaseStyles
        {
            public GUIStyle modifierBox;
            public GUIStyle previewToggle;

            protected override void InitializeStyles(bool isProSkin)
            {
                modifierBox = new GUIStyle("Box");
                previewToggle = new GUIStyle("Button");
            }
        }

        private static Styles s_styles = new Styles();

        private Billboard m_previewBillboard;

        private bool ShowHandles
        {
            get => m_showHandles;
            set
            {
                if (m_showHandles != value)
                {
                    m_showHandles = value;
                    SceneView.duringSceneGui -= OnSceneGUI;
                    if (m_showHandles)
                    {
                        m_prevTool = UnityEditor.Tools.current;
                        UnityEditor.Tools.current = Tool.None;
                        SceneView.duringSceneGui += OnSceneGUI;
                    }
                    else
                    {
                        UnityEditor.Tools.current = m_prevTool;
                    }
                }
            }
        }

        private SerializedObject m_billboardSerObj;
        private Billboard BillboardSample
        {
            get
            {
                if (serializedObject == null)
                {
                    return null;
                }
                if (m_billboardSerObj == null)
                {
                    m_billboardSerObj = new SerializedObject(serializedObject.targetObject);
                }
                m_billboardSerObj.Update();
                return m_billboardSerObj.FindProperty("m_sample.m_value").objectReferenceValue as Billboard;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_action = target as BillboardAction;
            m_modifierHeights = new float[0];
            m_editors = new List<BillboardModifierEditor>();
            if (!m_action) { return; }
            m_lastSample = m_action.BillboardSample;
            m_action.OnModified -= Action_OnModified;
            m_action.OnModified += Action_OnModified;
            if (m_lastSample)
            {
                m_lastSample.RefreshElements();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EditorApplication.update -= RefreshPreview;
            if (m_action)
            {
                DestroyEditors();
                m_action.OnModified -= Action_OnModified;
                if (m_previewBillboard)
                {
                    m_showBillboard = false;
                    DestroyPreview();
                }
                ShowHandles = false;
            }
        }

        private void DestroyEditors()
        {
            foreach (var editor in m_editors)
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
            m_editors.Clear();
        }

        private void Action_OnModified(ProcedureObject obj)
        {
            bool recreateEditors = false;
            if (m_editors.Count == m_action.Modifiers.Count)
            {
                for (int i = 0; i < m_action.Modifiers.Count; i++)
                {
                    if (m_editors[i].target != m_action.Modifiers[i] || !m_editors[i].target)
                    {
                        recreateEditors = true;
                        break;
                    }
                }
            }
            else
            {
                recreateEditors = true;
            }

            if (recreateEditors)
            {
                RecreateEditors();
            }

            if (m_action.ShowBillboard && m_showBillboard)
            {
                m_showBillboard = ReapplyPreview();
            }
            else if (m_previewBillboard)
            {
                DestroyPreview();
            }
        }

        private void DestroyPreview()
        {
            if (Application.isPlaying)
            {
                Destroy(m_previewBillboard.gameObject);
            }
            else
            {
                DestroyImmediate(m_previewBillboard.gameObject);
            }
        }

        private bool ReapplyPreview()
        {
            if (m_previewBillboard)
            {
                DestroyPreview();
            }

            bool hasSample = /*m_action.*/BillboardSample;
            var sample = hasSample ? /*m_action.*/BillboardSample : BillboardManager.Instance.BillboardDefaultSample;

            if (!sample)
            {
                Debug.Log("[BillboardActionPreview]: Unable to preview a null Billboard Sample");
                return false;
            }

            m_previewBillboard = Instantiate(sample);
            m_previewBillboard.gameObject.SetActive(true);
            m_previewBillboard.gameObject.hideFlags = HideFlags.HideAndDontSave;

            m_action.ApplyPreview(m_previewBillboard, !hasSample);
            m_previewBillboard.PreviewOn(m_action.Target.GetGameObject(), SceneView.lastActiveSceneView.camera);

            return true;
        }

        private void RecreateEditors()
        {
            RecreateEditors(m_lastSample ? m_lastSample : BillboardSample);
        }

        private void RecreateEditors(Billboard billboard)
        {
            DestroyEditors();
            if (billboard)
            {
                foreach (var modifier in m_action.Modifiers)
                {
                    if (!modifier) { return; }
                    var editor = CreateEditor(modifier) as BillboardModifierEditor;
                    if (!editor.Target)
                    {
                        editor.GetTargetFrom(billboard);
                    }
                    m_editors.Add(editor);
                }
                if (m_modifierHeights == null || m_modifierHeights.Length != m_action.Modifiers.Count)
                {
                    m_modifierHeights = new float[m_action.Modifiers.Count];
                }
            }
        }

        private void CreateModifiers()
        {
            foreach (var elemPair in m_action.BillboardSample.Elements)
            {
                if (!elemPair.Value) { continue; }

                foreach (var component in elemPair.Value.GetComponents<Component>())
                {
                    if (ModifierTypes.TryGetValue(component.GetType(), out Type modifierType))
                    {
                        var modifier = ProcedureObject.Create(modifierType, m_action.Procedure) as BillboardModifier;
                        if (modifier is ITargetingObject tObject)
                        {
                            tObject.Target = component;
                        }
                        modifier.OnModified += Modifier_OnModified;
                        m_action.Modifiers.Add(modifier);
                    }
                }
            }

            m_action.Modified();
            //m_advanced.startPoint.value = m_sample.StartPoint ?? Vector3.zero;
            //m_advanced.endPoint.value = m_sample.EndPoint ?? Vector3.zero;
        }

        private void Modifier_OnModified(ProcedureObject obj)
        {
            m_action.Modified();
        }

        private void ClearModifiers()
        {
            if (m_action.Modifiers.Count > 0)
            {
                foreach (var mod in m_action.Modifiers)
                {
                    mod.OnModified -= Modifier_OnModified;
                    if (Application.isPlaying)
                    {
                        Destroy(mod);
                    }
                    else
                    {
                        DestroyImmediate(mod, true);
                    }
                }
                m_action.Modifiers.Clear();
            }
        }

        private void ClearMissingModifiers(List<int> existingIds)
        {
            if (m_action.Modifiers.Count > 0)
            {
                for (int i = 0; i < m_action.Modifiers.Count; i++)
                {
                    var mod = m_action.Modifiers[i];
                    if (mod && existingIds.Contains(mod.ElementId))
                    {
                        existingIds.Remove(mod.ElementId);
                        continue;
                    }

                    if (mod)
                    {
                        mod.OnModified -= Modifier_OnModified;
                        if (Application.isPlaying)
                        {
                            Destroy(mod);
                        }
                        else
                        {
                            DestroyImmediate(mod, true);
                        }
                    }
                    m_action.Modifiers.RemoveAt(i--);
                }
            }
        }

        public override void Draw(Rect rect)
        {
            if (m_action.ShowBillboard && m_lastSample != m_action.BillboardSample)
            {
                m_lastSample = m_action.BillboardSample;
                if (m_lastSample && !m_action.Modifiers.TrueForAll(m => m))
                {
                    m_lastSample.RefreshElements();
                    ClearModifiers();
                    CreateModifiers();
                    RecreateEditors();
                }
                else if (!m_lastSample)
                {
                    ClearModifiers();
                    DestroyEditors();
                }
            }
            else if (m_action.BillboardSample)
            {
                m_action.BillboardSample.ClearInvalidElements();
                if (m_action.BillboardSample.Elements.Count != m_action.Modifiers.Count)
                {
                    m_action.BillboardSample.RefreshElements();
                    ClearMissingModifiers(new List<int>(m_action.BillboardSample.Elements.Keys));
                    if (m_action.BillboardSample.Elements.Count != m_action.Modifiers.Count)
                    {
                        CreateModifiers();
                    }
                    RecreateEditors();
                }
            }
            else if (!m_lastSample && m_lastSample is object)
            {
                serializedObject.Update();
                m_lastSample = serializedObject.FindProperty("m_sample").objectReferenceValue as Billboard;
                if (m_lastSample)
                {
                    RecreateEditors();
                }
            }
            base.Draw(rect);
        }

        protected override float GetHeightInternal()
        {
            s_styles.Refresh();
            return base.GetHeightInternal();
        }


        protected override float GetPropertyHeight(SerializedProperty property)
        {
            //if (property.name == "m_sample" && !m_action.ShowBillboard)
            //{
            //    return 0;
            //}
            //else 
            if (property.name == "m_modifiers")
            {
                m_totalListHeight = 0;
                if (m_action.ShowBillboard)
                {
                    if (m_editors.Count != m_action.Modifiers.Count || m_modifierHeights.Length != m_action.Modifiers.Count)
                    {
                        m_preRenderAction += RecreateEditors;
                        return m_totalListHeight;
                    }
                    for (int i = 0; i < m_modifierHeights.Length; i++)
                    {
                        m_modifierHeights[i] = m_editors[i].GetHeight() + 4;
                        m_totalListHeight += m_modifierHeights[i] + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
                return m_totalListHeight;
            }
            return base.GetPropertyHeight(property);
        }

        protected override void DrawProperty(SerializedProperty property, ref Rect rect, GUIContent label = null)
        {
            if (property.name == "m_modifiers")
            {
                if (m_action.ShowBillboard)
                {
                    bool drawBox = Event.current.type == EventType.Repaint;

                    for (int i = 0; i < m_editors.Count; i++)
                    {
                        if (!m_editors[i].IsValid)
                        {
                            m_preRenderAction += RecreateEditors;
                            return;
                        }
                        rect.height = m_modifierHeights[i];
                        if (drawBox)
                        {
                            s_styles.modifierBox.Draw(rect, false, false, false, false);
                        }
                        rect.x += 2;
                        rect.width -= 4;
                        rect.y += 2;
                        rect.height -= 4;
                        m_editors[i].DrawFull(rect);
                        rect.x -= 2;
                        rect.width += 4;
                        rect.y += rect.height + 2 + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }
            else
            {
                base.DrawProperty(property, ref rect);
            }
        }

        protected override bool HasMiniPreview => m_action.ShowBillboard && m_action.Target;

        protected override float MiniPreviewHeight => EditorGUIUtility.singleLineHeight;

        protected override void DrawMiniPreview(Rect r)
        {
            r.x += r.width - 60;
            r.width = 60;
            var showBillboard = m_action.ShowBillboard
                && GUI.Toggle(r, m_showBillboard, "Preview", m_showBillboard ? EditorStyles.miniButtonRight : EditorStyles.miniButton);

            if (showBillboard != m_showBillboard)
            {
                ShowHandles &= m_showBillboard = showBillboard;
                if (m_showBillboard)
                {
                    EditorApplication.update -= RefreshPreview;
                    EditorApplication.update += RefreshPreview;
                    m_showBillboard = ReapplyPreview();
                }
                else if (m_previewBillboard)
                {
                    EditorApplication.update -= RefreshPreview;
                    DestroyPreview();
                }
            }

            if (!m_showBillboard) { return; }

            bool wasEnable = GUI.enabled;
            ShowHandles &= GUI.enabled = m_action.Target && (m_action.StartPoint.enabled || m_action.EndPoint.enabled);

            r.x -= r.width;
            ShowHandles = GUI.Toggle(r, m_showHandles, "Handles", EditorStyles.miniButtonLeft);
            GUI.enabled = wasEnable;
        }

        private void RefreshPreview()
        {
            if (m_previewBillboard && m_action.ShowBillboard && m_previewBillboard.LookAtCamera && SceneView.lastActiveSceneView)
            {
                m_previewBillboard.UpdatePreview(SceneView.lastActiveSceneView.camera);
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!m_action.Target) { return; }
            var prevMatrix = Handles.matrix;
            var targetTransform = m_action.Target.GetGameObject().transform;
            var inverseRotation = Quaternion.Inverse(targetTransform.rotation);
            EditorGUI.BeginChangeCheck();
            if (m_action.EndPoint.enabled)
            {
                Handles.matrix = Matrix4x4.TRS(targetTransform.position, Quaternion.identity, Vector3.one);
                m_action.StartPoint.value = Handles.PositionHandle(m_action.StartPoint.value, Quaternion.identity);
            }
            if (m_action.EndPoint.enabled)
            {
                Handles.matrix = Matrix4x4.TRS(targetTransform.position, Quaternion.identity, Vector3.one);
                m_action.EndPoint.value = Handles.PositionHandle(m_action.EndPoint.value, Quaternion.identity);
            }
            if (EditorGUI.EndChangeCheck())
            {
                ShowHandles = m_showBillboard = ReapplyPreview();
            }
            Handles.matrix = prevMatrix;
        }

    }
}
