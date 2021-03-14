using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.License
{
    public class LicenseInfoWindow : EditorWindow
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

        [MenuItem("WEAVR/Licensing/Information", priority = 95)]
        public static void ShowLicenseInfoAsWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            var mainWindow = EditorWindow.GetWindow<LicenseInfoWindow>(true, "WEAVR License Info");
            mainWindow.maxSize = new Vector2(500, s_styles.logo.fixedHeight > 0 ? s_styles.logo.fixedHeight : 100);
            mainWindow.minSize = mainWindow.maxSize;
            //mainWindow.ShowPopup();
        }

        private void OnEnable()
        {
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
            EditorGUILayout.LabelField("WEAVR Editor License Info", s_styles.title);

            if (WeavrLE.IsValid())
            {
                EditorGUILayout.LabelField($"Valid license for {RLMLicenserEditor.RLM_PRODUCT_NAME} version {RLMLicenserEditor.RLM_PRODUCT_VERSION}", s_styles.subtitle);

                GUILayout.FlexibleSpace();

                foreach (var licenser in WeavrLE.Licensers)
                {
                    foreach (var detail in licenser.GetDetails())
                    {
                        EditorGUILayout.LabelField(detail, s_styles.subtitle);
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField($"Error in license {RLMLicenserEditor.RLM_PRODUCT_NAME} with version {RLMLicenserEditor.RLM_PRODUCT_VERSION}", s_styles.subtitle);

                GUILayout.FlexibleSpace();

                foreach (var licenser in WeavrLE.Licensers)
                {
                    foreach (var detail in licenser.GetDetails())
                    {
                        EditorGUILayout.LabelField(detail, s_styles.subtitle);
                    }
                }
            }

            GUILayout.FlexibleSpace();

            //if (!File.Exists(RLMLicensingEditor.RlmPathLicense))
            //{
            //    EditorGUILayout.LabelField("No License Found", s_styles.subtitle);
            //}
            //else
            //{
            //    int stat = RLM.rlm_stat(RLMLicensingEditor.RlmHandle);
            //    if (stat != 0)
            //    {
            //        EditorGUILayout.LabelField($"Error in license: {stat}", s_styles.subtitle);
            //    }
            //    else
            //    {
            //        // Check out a license
            //        IntPtr license = RLM.rlm_checkout(RLMLicensingEditor.RlmHandle, RLMLicensingEditor.RLM_PRODUCT_NAME, RLMLicensingEditor.RLM_PRODUCT_VERSION, 1);
            //        stat = RLM.rlm_license_stat(license);
            //        if (stat != 0)
            //        {
            //            EditorGUILayout.LabelField($"Error in license {RLMLicensingEditor.RLM_PRODUCT_NAME} with version {RLMLicensingEditor.RLM_PRODUCT_VERSION}", s_styles.subtitle);
            //            GUILayout.FlexibleSpace();
            //            EditorGUILayout.LabelField(RLM.marshalToString(RLM.rlm_errstring(license, RLMLicensingEditor.RlmHandle, new byte[RLM.RLM_ERRSTRING_MAX])), s_styles.setupDescription);
            //        }
            //        else
            //        {
            //            EditorGUILayout.LabelField($"Valid license for {RLMLicensingEditor.RLM_PRODUCT_NAME} version {RLMLicensingEditor.RLM_PRODUCT_VERSION}", s_styles.subtitle);
            //            GUILayout.FlexibleSpace();

            //            var expiration = RLM.marshalToString(RLM.rlm_license_exp(license));
            //            EditorGUILayout.LabelField($"Expiration: {expiration}", s_styles.setupDescription);

            //            if (expiration != "permanent")
            //            {
            //                EditorGUILayout.LabelField($"Days until expiration: {RLM.rlm_license_exp_days(license)}", s_styles.setupDescription);
            //            }
            //        }

            //        // Check it back in
            //        RLM.rlm_checkin(license);

            //        // Clean up the handle
            //        RLM.rlm_close(RLMLicensingEditor.RlmHandle);
            //    }

            //    RLMLicensingEditor.ResetRlmHandle();
            //}

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }
}