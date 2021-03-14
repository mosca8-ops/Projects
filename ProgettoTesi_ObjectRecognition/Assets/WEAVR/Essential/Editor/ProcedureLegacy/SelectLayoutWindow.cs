using System;
using TXT.WEAVR.LayoutSystem;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Editor
{

    public class SelectLayoutWindow : EditorWindow
    {

        private static Styles s_styles = new Styles();
        private Contents m_contents;

        #region [  GUIContents and Styles  ]

        private class Styles : BaseStyles
        {

            public GUIStyle layoutButton;
            public GUIStyle dropdownWindow;

            protected override void InitializeStyles(bool isProSkin)
            {
                layoutButton = WeavrStyles.EditorSkin.FindStyle("selectLayoutNode_Button") ?? "Button";
                dropdownWindow = WeavrStyles.EditorSkin.FindStyle("selectLayoutNode_DropDown") ?? new GUIStyle()
                {
                    fixedHeight = s_defaultSize.y,
                    fixedWidth = s_defaultSize.x
                };
            }
        }

        private class Contents
        {
            public readonly GUIContent buttonNew;
            public readonly GUIContent buttonLoad;
            public readonly GUIContent buttonXml;
            public readonly GUIContent buttonImportXml;
            public readonly GUIContent buttonImport;
            public readonly GUIContent buttonExport;
            public readonly GUIContent buttonAdvanced;
            public readonly GUIContent buttonClear;
            public readonly GUIContent buttonCopy;
            public readonly GUIContent buttonRun;
            public readonly GUIContent buttonSettings;
            public readonly GUIContent buttonCenter;
            public readonly GUIContent buttonResetColors;
            public readonly GUIContent buttonLayout;

            public readonly GUIContent forceSceneWarning;

            public Contents()
            {
                buttonNew = new GUIContent("New");
                buttonLoad = new GUIContent("Load");
                buttonXml = new GUIContent("XML");
                buttonImportXml = new GUIContent("Import Xml");
                buttonImport = new GUIContent("Import");
                buttonExport = new GUIContent("Export");
                buttonAdvanced = new GUIContent("Advanced");
                buttonClear = new GUIContent("Clear");
                buttonCopy = new GUIContent("Create Copy");
                buttonRun = new GUIContent(@"Test ▶");
                buttonSettings = new GUIContent("Settings");
                buttonCenter = new GUIContent("Center");
                buttonResetColors = new GUIContent("Reset");
                buttonLayout = new GUIContent("Layout");

                forceSceneWarning = new GUIContent("  Forcing current scene to this procedure will destroy all object references",
                                                    WeavrStyles.Icons["WarningIcon_32"]);
            }
        }

        #endregion;

        private static float? _defaultWindowTitleTabHeight;

        public static readonly Vector2 s_defaultSize = new Vector2(400, 400);
        public const int k_columns = 2;

        public class LayoutNode { }

        private Func<LayoutNode> m_createCallback;
        private Vector2 m_scrollPosition;

        // Add menu item named "My Window" to the Window menu
        //[MenuItem("WEAVR/Procedures/Open layout select window")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            var mainWindow = EditorWindow.GetWindow<SelectLayoutWindow>();

            mainWindow.Init();
        }

        public static void ShowAsDropDown(Vector2 position, Func<LayoutNode> createCallback)
        {
            //var window = SelectLayoutWindow.GetWindow<SelectLayoutWindow>(true, "Select Layout");
            var window = CreateInstance<SelectLayoutWindow>();
            window.titleContent = new GUIContent("Select Layout", WeavrStyles.Icons["procedureIcon"]);
            window.m_createCallback = createCallback;
            var size = new Vector2(s_styles.dropdownWindow.fixedWidth, s_styles.dropdownWindow.fixedHeight);
            Rect menuPoint = new Rect();
            menuPoint.size = Vector2.one;
            menuPoint.position = GUIUtility.GUIToScreenPoint(position - size * 0.5f);
            window.RefreshStyles();
            window.ShowAsDropDown(menuPoint, size);
            //window.ShowPopup();
        }

        private void OnEnable()
        {

            RefreshStyles();
            RefreshContents();
            LayoutContainer.RefreshAvailableContainers();
        }

        private void OnDestroy()
        {

        }

        private void RefreshStyles()
        {
            if (s_styles == null)
            {
                s_styles = new Styles();
            }
            s_styles.Refresh();
        }

        private void RefreshContents()
        {
            if (m_contents == null)
            {
                m_contents = new Contents();
            }
        }

        public void Init()
        {
            titleContent = new GUIContent("Select Layout", WeavrStyles.Icons["procedureIcon"]);
            ShowAuxWindow();
        }

        private void Update()
        {
            Repaint();
        }

        void OnGUI()
        {
            RefreshStyles();

            int index = 0;
            int elements = LayoutContainer.ContainersInScene.Count;
            Vector2 elemSize = position.size;
            elemSize.x -= 20;
            elemSize /= k_columns;
            elemSize.y = elemSize.x;

            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);

            for (int row = 0; row < elements / k_columns + 1 && index < elements; row++)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Space(5);
                for (int column = 0; column < k_columns && index < elements; column++, index++)
                {
                    var elemRect = GUILayoutUtility.GetRect(elemSize.x, elemSize.y);
                    var container = LayoutContainer.ContainersInScene[index];
                    if (GUI.Button(elemRect, container.name, s_styles.layoutButton))
                    {
                        var node = m_createCallback?.Invoke();
                        if (node != null)
                        {
                            //node.LayoutContainer = container;
                        }
                        Close();
                    }
                    elemRect.height -= s_styles.layoutButton.margin.top + s_styles.layoutButton.padding.vertical;
                    elemRect.y += s_styles.layoutButton.margin.top + s_styles.layoutButton.padding.top;
                    elemRect.x += s_styles.layoutButton.padding.left;
                    elemRect.width -= s_styles.layoutButton.padding.horizontal;
                    GUI.BeginClip(elemRect);
                    elemRect.y = elemRect.x = 0;
                    container.OnGUIDraw(elemRect);
                    GUI.EndClip();

                    GUILayout.Space(5);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }
    }
}
