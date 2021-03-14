using System.Collections.Generic;
using TXT.WEAVR.Editor;
using TXT.WEAVR.License;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Procedure
{
    public class ProcedureObjectInspector : EditorWindow
    {
        private UnityEditor.Editor m_focusedEditor;
        private object m_selectedObject;
        private Object m_selectedModel;
        [SerializeField]
        private Scene m_activeScene;

        private static List<System.Action> s_importCallbacks = new List<System.Action>();
        private static ProcedureObjectInspector s_instance;

        private bool m_canAcceptNewObject;

        private static ProcedureObjectInspector Instance {
            get {
                if (s_instance == null)
                {
                    ShowWindow();
                }
                return s_instance;
            }
        }

        public static void CloseInstance()
        {
            if (s_instance)
            {
                if (Application.isPlaying)
                {
                    if (s_instance.m_focusedEditor)
                    {
                        Destroy(s_instance.m_focusedEditor);
                    }
                    Destroy(s_instance);
                }
                else
                {
                    if (s_instance.m_focusedEditor)
                    {
                        DestroyImmediate(s_instance.m_focusedEditor);
                    }
                    DestroyImmediate(s_instance);
                }
                s_instance = null;
            }
        }

        public static object Selected {
            get {
                return s_instance != null ? s_instance.m_selectedObject : null;
            }
            set {
                if (Instance != null && s_instance.m_selectedObject != value)
                {
                    if (s_instance.m_focusedEditor != null)
                    {
                        TryImportAssets();
                        DestroyImmediate(s_instance.m_focusedEditor);
                        s_instance.m_focusedEditor = null;
                        s_instance.m_selectedObject = null;
                        s_instance.m_selectedModel = null;
                    }
                    if (value is Controller)
                    {
                        s_instance.m_selectedObject = value;
                        s_instance.m_selectedModel = (value as Controller).GetModel();
                        s_instance.m_focusedEditor = UnityEditor.Editor.CreateEditor(s_instance.m_selectedModel);
                    }
                    else if (value is Object)
                    {
                        s_instance.m_selectedObject = value;
                        s_instance.m_selectedModel = value as Object;
                        s_instance.m_focusedEditor = UnityEditor.Editor.CreateEditor(s_instance.m_selectedModel);
                    }
                    if (s_instance.m_focusedEditor is IEditorWindowClient client)
                    {
                        client.Window = s_instance;
                    }
                }
            }
        }

        public static UnityEditor.Editor CurrentEditor => s_instance ? s_instance.m_focusedEditor : null;

        private static void TryImportAssets()
        {
            s_importCallbacks.Clear();
            if (s_instance.m_focusedEditor is IAssetImporter bgWorker && bgWorker.TryImport(s_importCallbacks))
            {
                AssetDatabase.Refresh();
                foreach (var callback in s_importCallbacks)
                {
                    callback?.Invoke();
                }
            }
        }

        internal static void ResetSelection()
        {
            if (s_instance && s_instance.m_focusedEditor != null)
            {
                TryImportAssets();
                DestroyImmediate(s_instance.m_focusedEditor);
                s_instance.m_focusedEditor = null;
                s_instance.m_selectedObject = null;
                s_instance.m_selectedModel = null;
            }
        }

        internal static void ResetSelectionFor(object obj)
        {
            if (s_instance && s_instance.m_focusedEditor)
            {
                if(obj is Controller c && c == s_instance.m_selectedObject)
                {
                    ResetSelection();
                }
                else if(obj is Object o && o == s_instance.m_selectedModel)
                {
                    ResetSelection();
                }
            }
        }

        // Add menu item named "My Window" to the Window menu
        [MenuItem("WEAVR/Procedures/Inspector", priority = 0)]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            s_instance = GetWindow<ProcedureObjectInspector>("Procedure Inspector");
            s_instance.Init();
        }

        public void Init()
        {
            minSize = new Vector2(360, 420);
            //position = new Rect(position.position, minSize);
            m_activeScene = SceneManager.GetActiveScene();
        }

        //private void Update() {
        //    Repaint();
        //}

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        public static void Repaint(Object target)
        {
            if (s_instance && s_instance.m_selectedModel == target)
            {
                s_instance.Repaint();
            }
        }

        public static void RepaintFull()
        {
            if (s_instance)
            {
                s_instance.Repaint();
            }
        }

        private void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }

            titleContent = new GUIContent("Procedure Inspector", WeavrStyles.Icons["logo_icon"]);
            EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;
            m_canAcceptNewObject = true;
        }

        private void EditorApplication_PlayModeStateChanged(PlayModeStateChange state)
        {
            m_canAcceptNewObject = state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode;
            if (state == PlayModeStateChange.ExitingEditMode && m_focusedEditor is IAssetImporter bgWorker)
            {
                s_importCallbacks.Clear();
                if (bgWorker.TryImport(s_importCallbacks))
                {
                    AssetDatabase.Refresh();
                    foreach (var callback in s_importCallbacks)
                    {
                        callback?.Invoke();
                    }
                }
            }
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ProcedureObjectEditor.ClearAllEditors(true);
            }

        }

        private void OnDisable()
        {
            s_importCallbacks.Clear();
            if (m_focusedEditor is IAssetImporter bgWorker && bgWorker.TryImport(s_importCallbacks))
            {
                AssetDatabase.Refresh();
                foreach (var callback in s_importCallbacks)
                {
                    callback?.Invoke();
                }
            }
            ProcedureObjectEditor.ClearAllEditors();
        }

        void OnGUI()
        {
            //if (Application.isPlaying) {
            //    EditorGUILayout.LabelField("Application is playing.", EditorStyles.boldLabel);
            //    return;
            //}
            if (m_activeScene != SceneManager.GetActiveScene())
            {
                Selected = null;
                m_activeScene = SceneManager.GetActiveScene();
                return;
            }
            //if (!ProcedureWindow.IsEditorReady)
            //{
            //    Selected = null;
            //    return;
            //}
            //if (Application.isPlaying && Event.current.isMouse)
            //{
            //    Event.current.Use();
            //}
            //GUI.enabled = !Application.isPlaying;
            if (m_selectedModel)
            {
                //DrawBackground();
                if (!m_focusedEditor)
                {
                    m_focusedEditor = UnityEditor.Editor.CreateEditor(m_selectedModel);
                }
                m_focusedEditor.DrawHeader();
                m_focusedEditor.OnInspectorGUI();
            }
            else if (m_focusedEditor != null)
            {
                DestroyImmediate(m_focusedEditor);
            }
        }

        private void DrawBackground()
        {
            var background = position;
            background.x = background.y = 0;
            EditorGUI.DrawRect(background, Color.white);
        }
    }
}