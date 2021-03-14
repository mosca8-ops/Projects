using System;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using TXT.WEAVR.License;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{

    public class ProcedureDefaultsWindow : EditorWindow
    {
        public const string ResourcesRelativePath = "Creator/Resources/";


        private ActionsCatalogueEditor m_actionsCatalogueEditor;
        private ActionsCatalogueEditor ActionsCatalogueEditor {
            get {
                if (m_actionsCatalogueEditor == null)
                {
                    m_actionsCatalogueEditor = UnityEditor.Editor.CreateEditor(ProcedureDefaults.Current.ActionsCatalogue) as ActionsCatalogueEditor;
                }
                return m_actionsCatalogueEditor;
            }
        }

        private ConditionsCatalogueEditor m_conditionsCatalogueEditor;
        private ConditionsCatalogueEditor ConditionsCatalogueEditor {
            get {
                if (m_conditionsCatalogueEditor == null)
                {
                    m_conditionsCatalogueEditor = UnityEditor.Editor.CreateEditor(ProcedureDefaults.Current.ConditionsCatalogue) as ConditionsCatalogueEditor;
                }
                return m_conditionsCatalogueEditor;
            }
        }

        private AnimationsCatalogueEditor m_animationsCatalogueEditor;
        private AnimationsCatalogueEditor AnimationsCatalogueEditor {
            get {
                if (m_animationsCatalogueEditor == null)
                {
                    m_animationsCatalogueEditor = UnityEditor.Editor.CreateEditor(ProcedureDefaults.Current.AnimationBlocksCatalogue) as AnimationsCatalogueEditor;
                }
                return m_animationsCatalogueEditor;
            }
        }

        private ColorPaletteEditor m_colorPaletteEditor;
        private ColorPaletteEditor ColorPaletteEditor {
            get {
                if (m_colorPaletteEditor == null)
                {
                    m_colorPaletteEditor = UnityEditor.Editor.CreateEditor(ProcedureDefaults.Current.ColorPalette) as ColorPaletteEditor;
                }
                return m_colorPaletteEditor;
            }
        }

        private VisualElement m_root;
        public VisualElement Root {
            get {
                if (m_root == null)
                {
                    m_root = rootVisualElement;
                }
                return m_root;
            }
        }

        private VisualElement m_mainPanelContainer;
        private VisualElement m_topLeftPanel;
        private VisualElement m_bottomLeftPanel;
        private Label m_mainPanelTitle;

        private Button m_selectAssetButton;
        private Button m_templatesButton;
        private Button m_execModesButton;
        private Button m_actionsButton;
        private Button m_conditionsButton;
        private Button m_animationsButton;
        private Button m_colorPaletteButton;

        private Button m_lastClickedButton;

 //#if WEAVR_INTERNAL_USE
        [MenuItem("WEAVR/Procedures/Catalogues", priority = 0)]
//#endif
        private static void ShowWindow()
        {
            GetWindow<ProcedureDefaultsWindow>(); // this spawns the window and OnEnable() is fired immediately.
        }

        static string UXMLResourceToPackage(string resourcePath)
        {
            return WeavrEditor.PATH + ResourcesRelativePath + resourcePath + ".uxml";
        }

        private void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }

            ProcedureDefaults.s_Persist = AssetDatabase.AddObjectToAsset;
            titleContent = new GUIContent("Procedure Defaults");
            minSize = new Vector2(800, 600);

            string uxmlPath = UXMLResourceToPackage("uxml/ProcedureDefaults");
            var tpl = EditorGUIUtility.Load(uxmlPath) as VisualTreeAsset;

            VisualElement thisWindow = new VisualElement();
            tpl?.CloneTree(thisWindow);
            Root.Add(thisWindow);

            thisWindow.StretchToParentSize();
            thisWindow.Q("window").StretchToParentSize();
            thisWindow.AddStyleSheetPath("ProcedureDefaults");

            m_mainPanelTitle = thisWindow.Q<Label>("panel-title");
            m_mainPanelContainer = thisWindow.Q("container");
            m_topLeftPanel = thisWindow.Q("top-panel");
            m_bottomLeftPanel = thisWindow.Q("bottom-panel");

            m_selectAssetButton = thisWindow.Q<Button>("selectAsset-button");
            m_execModesButton = thisWindow.Q<Button>("execModes-button");
            m_templatesButton = thisWindow.Q<Button>("templates-button");
            m_actionsButton = thisWindow.Q<Button>("actions-button");
            m_conditionsButton = thisWindow.Q<Button>("conditions-button");
            m_animationsButton = thisWindow.Q<Button>("animations-button");
            m_colorPaletteButton = thisWindow.Q<Button>("colorPalette-button");

            m_selectAssetButton.clicked += () =>
            {
                Selection.activeObject = ProcedureDefaults.Current;
                Clicked(m_selectAssetButton, "Main Asset", null, new IMGUIContainer(UnityEditor.Editor.CreateEditor(ProcedureDefaults.Current).OnInspectorGUI));
            };
            m_execModesButton.clickable.clicked += () => Clicked(m_execModesButton, "Execution Modes", 
                                                        null, 
                                                        CreateEditorsList(ProcedureDefaults.Current.ExecutionModes, ProcedureDefaults.Current.AddExecutionMode));
            m_templatesButton.clickable.clicked += () => Clicked(m_templatesButton, "Templates", 
                                                        null, 
                                                        CreateEditorsList(ProcedureDefaults.Current.Templates, ProcedureDefaults.Current.AddTemplate));
            m_actionsButton.clickable.clicked += () => Clicked(m_actionsButton, "Actions Catalogue",
                                                        new IMGUIContainer(ActionsCatalogueEditor.DrawMenuPanel),
                                                        new IMGUIContainer(ActionsCatalogueEditor.DrawMainPanel));
            m_conditionsButton.clickable.clicked += () => Clicked(m_conditionsButton, "Conditions Catalogue",
                                                        new IMGUIContainer(ConditionsCatalogueEditor.DrawMenuPanel),
                                                        new IMGUIContainer(ConditionsCatalogueEditor.DrawMainPanel));
            m_animationsButton.clickable.clicked += () => Clicked(m_animationsButton, "Animation Blocks Catalogue",
                                                        new IMGUIContainer(AnimationsCatalogueEditor.DrawMenuPanel),
                                                        new IMGUIContainer(AnimationsCatalogueEditor.DrawMainPanel));
            m_colorPaletteButton.clickable.clicked += () => Clicked(m_colorPaletteButton, "Color Palette",
                                                        null,
                                                        new IMGUIContainer(ColorPaletteEditor.OnInspectorGUI));
        }

        private VisualElement CreateEditorsList<T>(IEnumerable<T> list, Action<T> onAdd) where T : Object
        {
            VisualElement container = new VisualElement();
            CreateEditorsList(list, onAdd, container);

            var scrollView = new ScrollView();
            scrollView.Add(container);
            return scrollView;
        }

        private void CreateEditorsList<T>(IEnumerable<T> list, Action<T> onAdd, VisualElement container) where T : Object
        {
            foreach (var elem in list)
            {
                container.Add(new EditorContainer(elem));
            }

            var addButton = new Button(() => ListAndAddNew(Resources.FindObjectsOfTypeAll<T>().Except(list), e => { onAdd(e); container.Clear(); CreateEditorsList(list, onAdd, container); }));
            addButton.text = $"Add {EditorTools.NicifyName(typeof(T).Name)}";

            container.Add(addButton);
        }

        private void ListAndAddNew<T>(IEnumerable<T> elements, Action<T> onAdd) where T : Object
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add New.."), false, () => onAdd(null));
            menu.AddItem(new GUIContent("/"), false, delegate { });
            foreach(var elem in elements)
            {
                if (!string.IsNullOrEmpty(elem.name))
                {
                    menu.AddItem(new GUIContent(elem.name), false, () => onAdd(elem));
                }
            }
            menu.ShowAsContext();
        }

        private void Clicked(Button clickedButton, string titleLabel, VisualElement bottomPanel, VisualElement mainPanel)
        {
            if (m_lastClickedButton == clickedButton) { return; }

            foreach (var child in m_topLeftPanel.Children())
            {
                child.RemoveFromClassList("selected");
            }

            m_lastClickedButton = clickedButton;
            clickedButton.AddToClassList("selected");
            m_mainPanelTitle.text = titleLabel;
            m_bottomLeftPanel.Clear();
            if (bottomPanel != null)
            {
                m_bottomLeftPanel.Add(bottomPanel);
            }
            m_mainPanelContainer.Clear();
            if (mainPanel != null)
            {
                m_mainPanelContainer.Add(mainPanel);
            }
        }

        private void OnDisable()
        {
            if(ProcedureDefaults.s_Persist == AssetDatabase.AddObjectToAsset)
            {
                ProcedureDefaults.s_Persist = null;
            }
        }

        private class EditorContainer : VisualElement
        {
            public EditorContainer(Object obj)
            {
                AddToClassList("editor-container");
                TextField objectName = new TextField();
                objectName.value = obj.name;
                objectName.RegisterValueChangedCallback(s => obj.name = s.newValue);
                objectName.AddToClassList("object-name");
                Add(objectName);
                var container = new IMGUIContainer(UnityEditor.Editor.CreateEditor(obj).OnInspectorGUI);
                container.AddToClassList("object-gui");
                Add(container);
            }
        }

        private void OnLostFocus()
        {
            if (m_actionsCatalogueEditor)
            {
                DestroyImmediate(m_actionsCatalogueEditor);
            }
            m_actionsCatalogueEditor = null;
        }
    }
}
