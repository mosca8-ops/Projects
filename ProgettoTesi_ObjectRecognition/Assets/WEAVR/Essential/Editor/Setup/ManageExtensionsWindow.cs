using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TXT.WEAVR.Editor;
using TXT.WEAVR.License;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    public class ManageExtensionsWindow : EditorWindow, IActiveBuildTargetChanged
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
            public readonly GUIStyle moduleCurrentLabel;

            public readonly GUIStyle labelBoolean;
            public readonly GUIStyle labelFalse;

            public readonly GUIStyle acceptButton;
            public readonly GUIStyle repairButton;
            public readonly GUIStyle cancelButton;

            public bool isProSkin;

            public Styles()
            {
                isProSkin = EditorGUIUtility.isProSkin;
                skin = Resources.Load<GUISkin>("GUIStyles/WEAVRSetupSkin_" + (EditorGUIUtility.isProSkin ? "dark" : "light"));
                header = skin.GetStyle("Container_Header");
                title = new GUIStyle(skin.GetStyle("Label_Title"));
                subtitle = skin.GetStyle("Label_SubTitle");
                setupDescription = skin.GetStyle("Label_SetupDescription");
                successLabel = skin.GetStyle("Label_Success");

                moduleTitle = skin.GetStyle("Label_ModuleTitle");
                moduleTitle.normal.textColor = moduleTitle.onNormal.textColor;
                moduleDepsLabel = skin.GetStyle("Label_ModuleDepsLabel");
                moduleCurrentLabel = new GUIStyle(moduleDepsLabel) { fontSize = 11, alignment = TextAnchor.LowerCenter };
                moduleDepsList = skin.GetStyle("Label_ModuleDepsList");
                moduleDescription = skin.GetStyle("Label_ModuleDescription");
                moduleEmptyLabel = skin.GetStyle("Label_ModuleEmpty");
                moduleLabel = skin.GetStyle("Label_ModuleLabel");

                moduleEnableButton = skin.GetStyle("Toggle_ModuleEnable");

                labelBoolean = skin.GetStyle("Label_Boolean");

                acceptButton = skin.GetStyle("Button_Accept");
                repairButton = skin.GetStyle("Button_Repair");
                cancelButton = skin.GetStyle("Button_Cancel");

                logo = skin.GetStyle("Image_Logo");
            }
        }


        [MenuItem("WEAVR/Setup/Manage Extension", priority = 100)]
        public static void ShowExtensionsWindow()
        {
            ExtensionManager.ResetExtensions();
            ShowWindowAsUtility();
        }

        private static void ShowWindowAsUtility()
        {
            m_timeout = null;
            //Show existing window instance. If one doesn't exist, make one.
            var mainWindow = GetWindow<ManageExtensionsWindow>(true, "WEAVR Extensions Manager");
            mainWindow.minSize = new Vector2(500, 600);
        }

        private async static void ShowExtensionsWindowDelayed()
        {
            while (EditorApplication.isCompiling)
            {
                await Task.Delay(100);
            }
            await Task.Delay(500);
            ShowWindowAsUtility();
        }

        private Vector2 m_scrollPosition;

        private static readonly TimeSpan m_successTimeout = new System.TimeSpan(0, 0, 5);
        private static DateTime? m_timeout;

        public int callbackOrder => 2;

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
            EditorGUILayout.LabelField("WEAVR Extensions", s_styles.title);
            EditorGUILayout.LabelField("Manage extensions to be used in the project.", s_styles.subtitle);
            EditorGUILayout.LabelField($"Current Platform: <color=white>{EditorUserBuildSettings.activeBuildTarget}</color>", s_styles.subtitle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Extensions can be from third parties", s_styles.setupDescription);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (m_timeout != null)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Extensions are setting, wait until compilation is finished.", s_styles.successLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                if (DateTime.Now > m_timeout)
                {
                    Close();
                }
                else
                {
                    Repaint();
                }
                return;
            }

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            foreach (var wrapper in ExtensionManager.ExtensionWrappers)
            {
                if (wrapper.IsValid())
                {
                    DrawWrapper(wrapper);
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel", s_styles.cancelButton))
            {
                //ClearModules();
                Close();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Repair Extensions", s_styles.repairButton))
            {
                if (EditorUtility.DisplayDialog("Repair Extensions", "Repairing all extensions is a lengthy process. Are you sure?", "Yes", "No"))
                {
                    ExtensionManager.RepairExtensions();

                    m_timeout = DateTime.Now + m_successTimeout;
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", s_styles.acceptButton))
            {
                ExtensionManager.ApplyExtensionSetup();

                m_timeout = DateTime.Now + m_successTimeout;
            }

            EditorGUILayout.EndHorizontal();
        }


        private static void DrawWrapper(ExtensionManager.ExtensionWrapper wrapper)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(wrapper.extension?.name, s_styles.moduleTitle, GUILayout.MinWidth(250));
            GUILayout.FlexibleSpace();

            bool currentlyEnabled = wrapper.IsExtensionFullyEnabled();
            if (wrapper.IsEnabled != currentlyEnabled)
            {
                GUILayout.Label(currentlyEnabled ? "Currently ON" : "Currently OFF", s_styles.moduleCurrentLabel);
            }

            if (wrapper.CanBeEdited && !EditorApplication.isCompiling)
            {
                wrapper.IsEnabled = GUILayout.Toggle(wrapper.IsEnabled, wrapper.IsEnabled ? "ON" : "OFF", s_styles.moduleEnableButton);
            }
            else if (wrapper.ShouldPerformAction)
            {
                GUILayout.Toggle(wrapper.IsEnabled, wrapper.IsEnabled ? "Switching ON" : "Switching OFF", s_styles.moduleEnableButton);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    GUILayout.Toggle(wrapper.IsEnabled, wrapper.IsEnabled ? "ON" : "OFF", s_styles.moduleEnableButton);
                }
            }
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(wrapper.Description))
            {
                EditorGUILayout.LabelField(wrapper.Description, s_styles.moduleDescription);
            }
            //DrawWrapperInternals(extension);
            EditorGUILayout.EndVertical();
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            ShowExtensionsWindowDelayed();
            DestroyImmediate(this);
        }
    }
}
