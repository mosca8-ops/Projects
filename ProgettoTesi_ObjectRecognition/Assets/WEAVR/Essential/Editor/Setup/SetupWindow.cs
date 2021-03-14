using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using TXT.WEAVR.License;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Core
{

    public class SetupWindow : EditorWindow
    {
        private static Styles s_styles;

        private class Styles
        {
            public readonly GUISkin skin;
            public readonly GUIStyle header;
            public readonly GUIStyle title;
            public readonly GUIStyle subtitle;
            public readonly GUIStyle setupDescription;
            public readonly GUIStyle logo;
            public readonly GUIStyle successLabel;

            public readonly GUIStyle moduleTitle;
            public readonly GUIStyle moduleDepsLabel;
            public readonly GUIStyle moduleDepsList;
            public readonly GUIStyle moduleDescription;
            public readonly GUIStyle moduleEmptyLabel;
            public readonly GUIStyle moduleLabel;
            public readonly GUIStyle moduleEnableButton;

            public readonly GUIStyle labelBoolean;
            public readonly GUIStyle labelFalse;

            public readonly GUIStyle acceptButton;
            public readonly GUIStyle cancelButton;

            public bool isProSkin;

            public Styles()
            {
                isProSkin = EditorGUIUtility.isProSkin;
                skin = Resources.Load<GUISkin>("GUIStyles/WEAVRSetupSkin_" + (EditorGUIUtility.isProSkin ? "dark" : "light"));
                header = skin.GetStyle("Container_Header");
                title = skin.GetStyle("Label_Title");
                subtitle = skin.GetStyle("Label_SubTitle");
                setupDescription = skin.GetStyle("Label_SetupDescription");
                successLabel = skin.GetStyle("Label_Success");

                moduleTitle = skin.GetStyle("Label_ModuleTitle");
                moduleDepsLabel = skin.GetStyle("Label_ModuleDepsLabel");
                moduleDepsList = skin.GetStyle("Label_ModuleDepsList");
                moduleDescription = skin.GetStyle("Label_ModuleDescription");
                moduleEmptyLabel = skin.GetStyle("Label_ModuleEmpty");
                moduleLabel = skin.GetStyle("Label_ModuleLabel");

                moduleEnableButton = skin.GetStyle("Toggle_ModuleEnable");

                labelBoolean = skin.GetStyle("Label_Boolean");

                acceptButton = skin.GetStyle("Button_Accept");
                cancelButton = skin.GetStyle("Button_Cancel");

                logo = skin.GetStyle("Image_Logo");
            }
        }


        [MenuItem("WEAVR/Setup/Setup Scene", priority = 100)]
        public static void ShowSetupAsWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            var mainWindow = EditorWindow.GetWindow<SetupWindow>(true, "WEAVR Setup");
            mainWindow.minSize = new Vector2(500, 600);
            //mainWindow.ShowPopup();
        }

        private Vector2 m_scrollPosition;
        private List<ModuleWrapper> m_moduleWrappers;
        private List<ModuleWrapper> m_modulesNotFinished;
        private Dictionary<System.Type, WeavrModule> m_modulesData;

        private System.TimeSpan m_successTimeout = new System.TimeSpan(0, 0, 3);
        private System.DateTime m_timeout;

        private WeavrModule.OperationMode m_operationMode;

        private void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }

            if (s_styles == null || s_styles.isProSkin != EditorGUIUtility.isProSkin)
            {
                s_styles = new Styles();
            }
            if (m_moduleWrappers == null || m_modulesNotFinished == null)
            {
                ResetModules();
            }
            m_operationMode = m_moduleWrappers?.FirstOrDefault()?.module?.Mode ?? WeavrModule.OperationMode.VirtualTraining;
        }

        private void ResetModules()
        {
            ClearModules();
            m_moduleWrappers = new List<ModuleWrapper>();
            m_modulesNotFinished = new List<ModuleWrapper>();
            m_modulesData = new Dictionary<System.Type, WeavrModule>();

            ModuleWrapper essentialWrapper = null;
            Scene activeScene = SceneManager.GetActiveScene();
            foreach (var moduleType in EditorTools.GetAllAssemblyTypes().Where(t => t.IsSubclassOf(typeof(WeavrModule))))
            {
                var module = Resources.Load<WeavrModule>(moduleType.Name + "_InitData");
                if (module != null)
                {
                    module = Instantiate(module);
                }
                else
                {
                    module = CreateInstance(moduleType) as WeavrModule;
                }
                module.InitializeData(activeScene);
                var wrapper = new ModuleWrapper(module);
                m_moduleWrappers.Add(wrapper);
                m_modulesData[moduleType] = module;

                // TODO if(moduleType == typeof(WeavrEssential)) {
                if (moduleType.Name == "WeavrEssential")
                {
                    essentialWrapper = wrapper;
                }
            }

            m_moduleWrappers = m_moduleWrappers.OrderBy(w => w.dependencies.Length).ToList();
            // Bring essential to front
            if (essentialWrapper != null && m_moduleWrappers.Remove(essentialWrapper))
            {
                m_moduleWrappers.Insert(0, essentialWrapper);
            }

            m_modulesNotFinished.AddRange(m_moduleWrappers);
        }

        private void ClearModules()
        {
            if (m_moduleWrappers != null)
            {
                foreach (var wrapper in m_moduleWrappers)
                {
                    DestroyImmediate(wrapper.module);
                }
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            float logoLength = s_styles.logo.fixedHeight > 0 ? s_styles.logo.fixedHeight : 100;
            EditorGUILayout.BeginHorizontal(GUILayout.Height(logoLength + 2));
            Rect iconRect = EditorGUILayout.GetControlRect(false, logoLength, s_styles.header, GUILayout.Width(logoLength));
            iconRect.width = iconRect.height;
            Rect backgroundRect = iconRect;
            backgroundRect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(backgroundRect, s_styles.header.normal.textColor);
            GUI.DrawTexture(iconRect, s_styles.logo.normal.background);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("WEAVR Scene Setup", s_styles.title);
            EditorGUILayout.LabelField("This setup will prepare the current scene for creation and execution of procedural lessons", s_styles.subtitle);
            GUILayout.FlexibleSpace();
            //EditorGUILayout.LabelField("This setup will prepare the current scene for creation and execution of procedural lessons", s_styles.setupDescription);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (m_modulesNotFinished.Count == 0)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Setup successful", s_styles.successLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                if (System.DateTime.Now > m_timeout)
                {
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    Close();
                }
                Repaint();
                return;
            }

            m_timeout = System.DateTime.Now + m_successTimeout;

            //m_operationMode = (WeavrModule.OperationMode)EditorGUILayout.EnumPopup("Operation Mode: ", m_operationMode);

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            foreach (var wrapper in m_moduleWrappers)
            {
                if (wrapper.module)
                {
                    wrapper.module.Mode = m_operationMode;
                }
                DrawWrapper(wrapper);
            }
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel", s_styles.cancelButton))
            {
                ClearModules();
                Close();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", s_styles.acceptButton))
            {
                Scene activeScene = SceneManager.GetActiveScene();
                StartModuleSetup(-1, activeScene);
                //foreach(var wrapper in m_moduleWrappers) {
                //    if (wrapper.enable) {
                //        wrapper.coroutine = EditorCoroutine.StartCoroutine(wrapper.module.ApplyData(activeScene, m_modulesData), () => m_modulesNotFinished.Remove(wrapper));
                //    }
                //    else {
                //        m_modulesNotFinished.Remove(wrapper);
                //    }
                //}
            }
            EditorGUILayout.EndHorizontal();
        }

        private void StartModuleSetup(int index, Scene scene)
        {
            if (0 <= index && index < m_moduleWrappers.Count)
            {
                m_modulesNotFinished.Remove(m_moduleWrappers[index]);
            }
            while (++index < m_moduleWrappers.Count)
            {
                var wrapper = m_moduleWrappers[index];
                if (wrapper.enable)
                {
                    wrapper.currentWork = "Applying Data...";
                    wrapper.coroutine = EditorCoroutine.StartCoroutine(wrapper.module.ApplyData(scene, m_modulesData), () => FixModuleReferences(index, scene));
                    break;
                }
                else
                {
                    m_modulesNotFinished.Remove(wrapper);
                }
            }
        }

        private void FixModuleReferences(int index, Scene scene)
        {
            if (index < m_moduleWrappers.Count)
            {
                var wrapper = m_moduleWrappers[index];
                wrapper.currentWork = "Applying References...";
                wrapper.coroutine = EditorCoroutine.StartCoroutine(wrapper.module.FixModuleReferences(scene, m_modulesData), () => StartModuleSetup(index, scene));
            }
        }

        private static void DrawWrapper(ModuleWrapper wrapper)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.BeginHorizontal();
            if (wrapper.enable)
            {
                wrapper.expanded = GUILayout.Toggle(wrapper.expanded, wrapper.moduleName, s_styles.moduleTitle, GUILayout.MinWidth(150));
            }
            else
            {
                GUILayout.Label(wrapper.moduleName, s_styles.moduleTitle, GUILayout.MinWidth(150));
            }
            GUILayout.FlexibleSpace();
            if (wrapper.dependencies.Length > 0)
            {
                EditorGUILayout.LabelField("Depends on:", s_styles.moduleDepsLabel);
                EditorGUILayout.LabelField(wrapper.dependenciesFormatted, s_styles.moduleDepsList);
            }
            wrapper.enable = GUILayout.Toggle(wrapper.enable, wrapper.enable ? "ON" : "OFF", s_styles.moduleEnableButton);
            EditorGUILayout.EndHorizontal();
            if (wrapper.enable && wrapper.expanded)
            {
                if (wrapper.attribute != null)
                {
                    EditorGUILayout.LabelField(wrapper.attribute.Description, s_styles.moduleDescription);
                }
                DrawWrapperInternals(wrapper);
                wrapper.module.OnUpdate();
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawWrapperInternals(ModuleWrapper wrapper)
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = !wrapper.module.IsRunning;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Use custom values:", s_styles.moduleLabel);
            wrapper.UseCustomValues = GUILayout.Toggle(wrapper.UseCustomValues, wrapper.UseCustomValues ? "True" : "False", s_styles.labelBoolean);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (wrapper.UseCustomValues)
            {
                EditorGUILayout.BeginVertical("GroupBox");

                wrapper.serializedObject.Update();
                var iterator = wrapper.serializedObject.FindProperty("m_operationMode");
                bool isEmpty = true;
                while (iterator.NextVisible(false))
                {
                    isEmpty = false;
                    EditorGUILayout.PropertyField(iterator);
                    if (iterator.isExpanded && iterator.hasVisibleChildren)
                    {
                        EditorGUI.indentLevel++;
                        var innerIterator = iterator.Copy();
                        var nextProperty = iterator.Copy();
                        nextProperty.NextVisible(false);
                        while (innerIterator.NextVisible(innerIterator.propertyType == SerializedPropertyType.Generic) && innerIterator.propertyPath != nextProperty.propertyPath)
                        {
                            EditorGUILayout.PropertyField(innerIterator);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                if (isEmpty)
                {
                    EditorGUILayout.LabelField("This module do not have data to be set", s_styles.moduleEmptyLabel);
                }
                wrapper.serializedObject.ApplyModifiedProperties();
                GUI.enabled = wasEnabled;

                if (wrapper.module.IsRunning)
                {
                    var progressBarRect = EditorGUILayout.GetControlRect();
                    EditorGUI.ProgressBar(progressBarRect, wrapper.module.Progress, wrapper.currentWork);
                }
                EditorGUILayout.EndVertical();
            }
            else if (wrapper.module.IsRunning)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                var progressBarRect = EditorGUILayout.GetControlRect();
                EditorGUI.ProgressBar(progressBarRect, wrapper.module.Progress, wrapper.currentWork);
                EditorGUILayout.EndVertical();
            }

        }

        private void OnDestroy()
        {
            if (m_moduleWrappers != null)
            {
                foreach (var wrapper in m_moduleWrappers)
                {
                    if (wrapper != null && wrapper.coroutine != null)
                    {
                        EditorCoroutine.StopCoroutine(wrapper.coroutine);
                    }
                }
            }
        }

        private class ModuleWrapper
        {
            public readonly WeavrModule module;
            public readonly string moduleName;
            public readonly SerializedObject serializedObject;
            public readonly string[] dependencies;
            public readonly string dependenciesFormatted;
            public readonly GlobalModuleAttribute attribute;
            public bool enable;
            public bool expanded;
            private WeavrModule m_resetModule;
            private bool m_useCustomValues;

            public string currentWork;
            public EditorCoroutine coroutine;

            public bool UseCustomValues {
                get {
                    return m_useCustomValues;
                }
                set {
                    if (m_useCustomValues != value)
                    {
                        m_useCustomValues = value;
                        if (value)
                        {
                            Reset();
                        }
                    }
                }
            }

            public ModuleWrapper(WeavrModule module)
            {
                this.module = module;
                m_useCustomValues = false;
                m_resetModule = Instantiate(module);
                m_resetModule.hideFlags = HideFlags.HideAndDontSave;
                attribute = module.GetType().GetAttribute<GlobalModuleAttribute>();
                moduleName = attribute != null ? attribute.ModuleName : module.GetType().Name;
                if (attribute != null && attribute.Dependencies != null && attribute.Dependencies.Length > 0)
                {
                    dependencies = attribute.Dependencies;
                    dependenciesFormatted = string.Format("[ {0} ]", string.Join(" | ", dependencies));
                }
                else
                {
                    dependencies = new string[0];
                    dependenciesFormatted = "";
                }
                enable = module.StartEnabled;
                expanded = module.StartEnabled;
                serializedObject = new SerializedObject(module);
            }

            public void Reset()
            {
                var copySerObject = new SerializedObject(m_resetModule);
                var iterator = copySerObject.GetIterator();
                while (iterator.NextVisible(true))
                {
                    serializedObject.CopyFromSerializedProperty(iterator);
                }
            }
        }
    }
}
