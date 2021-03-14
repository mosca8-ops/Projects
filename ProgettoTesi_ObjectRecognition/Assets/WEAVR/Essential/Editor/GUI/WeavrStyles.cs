namespace TXT.WEAVR.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Graphs;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class WeavrStyles
    {
        private static GUISkin s_procedureEditorSkin;
        private static bool? s_isProSkin;

        public static GUISkin EditorSkin {
            get {
                if(s_procedureEditorSkin == null || s_isProSkin != EditorGUIUtility.isProSkin) {
                    s_isProSkin = EditorGUIUtility.isProSkin;
                    s_procedureEditorSkin = s_isProSkin.Value ? Resources.Load<GUISkin>("GUIStyles/ProcedureEditorSkin_dark")
                                                              : Resources.Load<GUISkin>("GUIStyles/ProcedureEditorSkin_light");
                }
                return s_procedureEditorSkin;
            }
        }

        private static bool? s_controlsIsProSkin;
        private static GUISkin s_controlsSkin;
        public static GUISkin ControlsSkin {
            get {
                if(s_controlsSkin == null || s_controlsIsProSkin != EditorGUIUtility.isProSkin) {
                    s_controlsIsProSkin = EditorGUIUtility.isProSkin;
                    s_controlsSkin = s_controlsIsProSkin.Value ? Resources.Load<GUISkin>("GUIStyles/WeavrControls_dark")
                                                               : Resources.Load<GUISkin>("GUIStyles/WeavrControls_light");
                }
                return s_controlsSkin;
            }
        }

        private static bool? s_procedure2IsProSkin;
        private static GUISkin s_procedure2EditorSkin;
        public static GUISkin EditorSkin2
        {
            get
            {
                if (s_procedure2EditorSkin == null || s_procedure2IsProSkin != EditorGUIUtility.isProSkin)
                {
                    s_procedure2IsProSkin = EditorGUIUtility.isProSkin;
                    s_procedure2EditorSkin = s_procedure2IsProSkin.Value ? Resources.Load<GUISkin>("GUIStyles/Procedure2EditorSkin_dark")
                                                                         : Resources.Load<GUISkin>("GUIStyles/Procedure2EditorSkin_light");
                }
                return s_procedure2EditorSkin;
            }
        }

        private static GUIStyle _miniToggleButtonOn;
        public static GUIStyle MiniToggleButtonOn {
            get {
                if(_miniToggleButtonOn == null) {
                    _miniToggleButtonOn = new GUIStyle(EditorStyles.miniButton);
                }
                return _miniToggleButtonOn;
            }
        }

        private static GUIStyle _miniToggleButtonOff;
        public static GUIStyle MiniToggleButtonOff {
            get {
                if (_miniToggleButtonOff == null) {
                    _miniToggleButtonOff = new GUIStyle(EditorStyles.miniButton);
                    _miniToggleButtonOff.normal.background = _miniToggleButtonOff.active.background;
                }
                return _miniToggleButtonOff;
            }
        }

        private static GUIStyle _miniToggleTextOn;
        public static GUIStyle MiniToggleTextOn {
            get {
                if (_miniToggleTextOn == null) {
                    _miniToggleTextOn = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                    _miniToggleTextOn.normal.textColor = Color.black;
                    _miniToggleTextOn.fontStyle = FontStyle.Bold;
                    _miniToggleTextOn.clipping = TextClipping.Overflow;
                }
                return _miniToggleTextOn;
            }
        }

        private static GUIStyle _miniToggleTextOff;
        public static GUIStyle MiniToggleTextOff {
            get {
                if (_miniToggleTextOff == null) {
                    _miniToggleTextOff = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                    _miniToggleTextOff.clipping = TextClipping.Overflow;
                }
                return _miniToggleTextOff;
            }
        }

        private static GUIStyle _leftGreyMiniLabel;
        public static GUIStyle LeftGreyMiniLabel {
            get {
                if (_leftGreyMiniLabel == null) {
                    _leftGreyMiniLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                    _leftGreyMiniLabel.alignment = TextAnchor.MiddleLeft;
                    _leftGreyMiniLabel.clipping = TextClipping.Overflow;
                }
                return _leftGreyMiniLabel;
            }
        }

        private static GUIStyle _rightGreyMiniLabel;
        public static GUIStyle RightGreyMiniLabel {
            get {
                if (_rightGreyMiniLabel == null) {
                    _rightGreyMiniLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                    _rightGreyMiniLabel.alignment = TextAnchor.MiddleRight;
                    _rightGreyMiniLabel.clipping = TextClipping.Overflow;
                }
                return _rightGreyMiniLabel;
            }
        }

        private static GUIStyle _leftWhiteMiniLabel;
        public static GUIStyle LeftWhiteMiniLabel {
            get {
                if (_leftWhiteMiniLabel == null) {
                    _leftWhiteMiniLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                    _leftWhiteMiniLabel.alignment = TextAnchor.MiddleLeft;
                    _leftWhiteMiniLabel.normal.textColor = Color.white;
                    _leftWhiteMiniLabel.clipping = TextClipping.Overflow;
                }
                return _leftWhiteMiniLabel;
            }
        }

        private static GUIStyle _rightWhiteMiniLabel;
        public static GUIStyle RightWhiteMiniLabel {
            get {
                if (_rightWhiteMiniLabel == null) {
                    _rightWhiteMiniLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                    _rightWhiteMiniLabel.alignment = TextAnchor.MiddleRight;
                    _rightWhiteMiniLabel.normal.textColor = Color.white;
                    _rightWhiteMiniLabel.clipping = TextClipping.Overflow;
                }
                return _rightWhiteMiniLabel;
            }
        }

        private static GUIStyle _titleBlackLabel;
        public static GUIStyle TitleBlackLabel {
            get {
                if (_titleBlackLabel == null) {
                    _titleBlackLabel = new GUIStyle(EditorStyles.boldLabel);
                    _titleBlackLabel.alignment = TextAnchor.MiddleLeft;
                    _titleBlackLabel.normal.textColor = Color.black;
                    _titleBlackLabel.clipping = TextClipping.Overflow;
                    _titleBlackLabel.fontSize = 14;
                    _titleBlackLabel.fixedHeight = 0;
                    _titleBlackLabel.padding.top = _titleBlackLabel.padding.bottom = 0;
                    _titleBlackLabel.margin.top = _titleBlackLabel.margin.bottom = 0;
                }
                return _titleBlackLabel;
            }
        }

        private static GUIStyle _centeredTitleBlackLabel;
        public static GUIStyle CenteredTitleBlackLabel {
            get {
                if (_centeredTitleBlackLabel == null) {
                    _centeredTitleBlackLabel = new GUIStyle(EditorStyles.boldLabel);
                    _centeredTitleBlackLabel.alignment = TextAnchor.MiddleCenter;
                    _centeredTitleBlackLabel.normal.textColor = Color.black;
                    _centeredTitleBlackLabel.clipping = TextClipping.Overflow;
                    _centeredTitleBlackLabel.fontSize = 14;
                    _centeredTitleBlackLabel.fixedHeight = 0;
                    _centeredTitleBlackLabel.padding.top = _centeredTitleBlackLabel.padding.bottom = 0;
                    _centeredTitleBlackLabel.margin.top = _centeredTitleBlackLabel.margin.bottom = 0;
                }
                return _centeredTitleBlackLabel;
            }
        }

        private static GUIStyle _foldoutBold;
        public static GUIStyle FoldoutBold {
            get {
                if (_foldoutBold == null) {
                    _foldoutBold = new GUIStyle(EditorStyles.foldout) {
                        fontStyle = FontStyle.Bold
                    };
                }
                return _foldoutBold;
            }
        }

        private static GUIStyle _foldoutBoldGreyBackground;
        public static GUIStyle FoldoutBoldWithBackground {
            get {
                if (_foldoutBoldGreyBackground == null) {
                    _foldoutBoldGreyBackground = new GUIStyle(EditorStyles.foldout) {
                        fontStyle = FontStyle.Bold,
                    };
                }
                return _foldoutBoldGreyBackground;
            }
        }

        private static GUIStyle _redLeftLabel;
        public static GUIStyle RedLeftLabel {
            get {
                if (_redLeftLabel == null) {
                    _redLeftLabel = new GUIStyle(EditorStyles.label);
                    _redLeftLabel.normal.textColor = Colors.error;
                }
                return _redLeftLabel;
            }
        }

        private static GUIStyle _redLeftBoldLabel;
        public static GUIStyle RedLeftBoldLabel {
            get {
                if (_redLeftBoldLabel == null) {
                    _redLeftBoldLabel = new GUIStyle(EditorStyles.boldLabel);
                    _redLeftBoldLabel.normal.textColor = Colors.error;
                }
                return _redLeftBoldLabel;
            }
        }

        private static GUIStyle _greenLeftBoldLabel;
        public static GUIStyle GreenLeftBoldLabel
        {
            get
            {
                if (_greenLeftBoldLabel == null)
                {
                    _greenLeftBoldLabel = new GUIStyle(EditorStyles.boldLabel);
                    _greenLeftBoldLabel.normal.textColor = Colors.transparentGreen;
                }
                return _greenLeftBoldLabel;
            }
        }

        private static GUIStyle _titleBlackHugeLabel;
        public static GUIStyle TitleBlackHugeLabel {
            get {
                if (_titleBlackHugeLabel == null) {
                    _titleBlackHugeLabel = new GUIStyle(EditorStyles.boldLabel);
                    _titleBlackHugeLabel.alignment = TextAnchor.UpperLeft;
                    _titleBlackHugeLabel.normal.textColor = Color.black;
                    _titleBlackHugeLabel.clipping = TextClipping.Overflow;
                    _titleBlackHugeLabel.fontSize = 28;
                    _titleBlackHugeLabel.fixedHeight = 0;
                    _titleBlackHugeLabel.border.top = _titleBlackHugeLabel.border.bottom = 0;
                    _titleBlackHugeLabel.padding.top = _titleBlackHugeLabel.padding.bottom = 0;
                    _titleBlackHugeLabel.margin.top = _titleBlackHugeLabel.margin.bottom = 0;
                }
                return _titleBlackHugeLabel;
            }
        }

        private static GUIStyle _titleBlackMediumLabel;
        public static GUIStyle TitleBlackMediumLabel {
            get {
                if (_titleBlackMediumLabel == null) {
                    _titleBlackMediumLabel = new GUIStyle(EditorStyles.boldLabel);
                    _titleBlackMediumLabel.alignment = TextAnchor.UpperLeft;
                    Color color = Color.black;
                    color.a = 0.5f;
                    _titleBlackMediumLabel.normal.textColor = color;
                    _titleBlackMediumLabel.clipping = TextClipping.Overflow;
                    _titleBlackMediumLabel.fontSize = 20;
                    _titleBlackMediumLabel.fixedHeight = 0;
                    _titleBlackMediumLabel.border.top = _titleBlackHugeLabel.border.bottom = 0;
                    _titleBlackMediumLabel.padding.top = _titleBlackHugeLabel.padding.bottom = 0;
                    _titleBlackMediumLabel.margin.top = _titleBlackHugeLabel.margin.bottom = 0;
                }
                return _titleBlackMediumLabel;
            }
        }

        private static GUIStyle _bigTransparentLabel;
        public static GUIStyle BigTransparentLabel {
            get {
                if (_bigTransparentLabel == null) {
                    _bigTransparentLabel = GUI.skin.FindStyle("bigTransparentLabel");
                    if (_bigTransparentLabel == null) {
                        var style = new GUIStyle(EditorStyles.whiteLargeLabel);
                        style.alignment = TextAnchor.UpperCenter;
                        return style;
                    }
                }
                return _bigTransparentLabel;
            }
        }

        private static GUIStyle _bigLeftTransparentLabel;
        public static GUIStyle BigLeftTransparentLabel {
            get {
                if (_bigLeftTransparentLabel == null) {
                    _bigLeftTransparentLabel = GUI.skin.FindStyle("bigTransparentLabel");
                    if(_bigLeftTransparentLabel == null) {
                        return EditorStyles.whiteLargeLabel;
                    }
                    _bigLeftTransparentLabel.alignment = TextAnchor.UpperLeft;
                }
                return _bigLeftTransparentLabel;
            }
        }

        private static GUIStyle _bigRightTransparentLabel;
        public static GUIStyle BigRightTransparentLabel {
            get {
                if (_bigRightTransparentLabel == null) {
                    _bigRightTransparentLabel = GUI.skin.FindStyle("bigTransparentLabel");
                    if (_bigRightTransparentLabel == null) {
                        return EditorStyles.whiteLargeLabel;
                    }
                    _bigRightTransparentLabel.alignment = TextAnchor.UpperRight;
                }
                return _bigRightTransparentLabel;
            }
        }

        private static GUIStyle _textFieldGreen;
        public static GUIStyle TextFieldGreen {
            get {
                if(_textFieldGreen == null) {
                    _textFieldGreen = new GUIStyle(EditorStyles.textField);
                    Color veryDarkGreen = Colors.darkGreen;
                    veryDarkGreen.g = 0.3f;
                    veryDarkGreen.r = veryDarkGreen.b = 0;
                    _textFieldGreen.normal.textColor = _textFieldGreen.onNormal.textColor =
                    _textFieldGreen.focused.textColor = _textFieldGreen.onFocused.textColor =
                    _textFieldGreen.active.textColor = _textFieldGreen.onActive.textColor = veryDarkGreen;
                }
                return _textFieldGreen;
            }
        }

        private static GUIStyle _textFieldBlue;
        public static GUIStyle TextFieldBlue {
            get {
                if (_textFieldBlue == null) {
                    _textFieldBlue = new GUIStyle(EditorStyles.textField);
                    Color veryDarkBlue = Color.blue;
                    veryDarkBlue.b = 0.3f;
                    veryDarkBlue.r = veryDarkBlue.g = 0;
                    _textFieldBlue.normal.textColor = _textFieldBlue.onNormal.textColor =
                    _textFieldBlue.focused.textColor = _textFieldBlue.onFocused.textColor =
                    _textFieldBlue.active.textColor = _textFieldBlue.onActive.textColor = veryDarkBlue;
                }
                return _textFieldBlue;
            }
        }

        private static GUIStyle _asyncIconOn;
        public static GUIStyle AsyncIconOn {
            get {
                if (_asyncIconOn == null) {
                    _asyncIconOn = EditorSkin.FindStyle("asyncIconOn");
                    if(_asyncIconOn == null) {
                        _asyncIconOn = new GUIStyle(EditorStyles.miniButton);
                    }
                }
                return _asyncIconOn;
            }
        }

        private static GUIStyle _asyncIconOff;
        public static GUIStyle AsyncIconOff {
            get {
                if (_asyncIconOff == null) {
                    _asyncIconOff = EditorSkin.FindStyle("asyncIconOff");
                    if (_asyncIconOff == null) {
                        _asyncIconOff = new GUIStyle(EditorStyles.miniButton);
                        _asyncIconOff.normal.background = _asyncIconOff.active.background;
                    }
                }
                return _asyncIconOff;
            }
        }

        private static GUIStyle _lockToggle;
        public static GUIStyle LockToggle {
            get {
                if (_lockToggle == null) {
                    _lockToggle = EditorSkin.FindStyle("lockToggle");
                    if (_lockToggle == null) {
                        _lockToggle = new GUIStyle(EditorStyles.toggle);
                    }
                }
                return _lockToggle;
            }
        }

        private static GUIStyle _inspectorHeader;
        public static GUIStyle InspectorHeader {
            get {
                if (_inspectorHeader == null) {
                    var builtinHeader = EditorGUIUtility.GetBuiltinSkin(UnityEditor.EditorSkin.Inspector).FindStyle("ProjectBrowserTopBarBg");
                    if (builtinHeader != null) {
                        _inspectorHeader = new GUIStyle(builtinHeader);
                    }
                    else {
                        _inspectorHeader = new GUIStyle();
                        _inspectorHeader.normal.background = EditorGUIUtility.whiteTexture;
                    }
                }
                return _inspectorHeader;
            }
        }

        public static class Colors
        {
            public readonly static Color focusedTransparent = new Color(89f / 255f, 137f / 255f, 207f / 255f, 0.5f);
            public readonly static Color focused = new Color(89f / 255f, 137f / 255f, 207f / 255f, 1f);
            public readonly static Color selection = new Color(0f / 255f, 131f / 255f, 214f / 255f, 0.4f);
            public readonly static Color selectionOpaque = new Color(0f / 255f, 131f / 255f, 214f / 255f, 1f);
            public readonly static Color error = new Color(250f / 255f, 50f / 255f, 50f / 255f);
            public readonly static Color errorDarkRed = new Color(180f / 255f, 10f / 255f, 10f / 255f);
            public readonly static Color errorTransparent = new Color(200f / 255f, 10f / 255f, 10f / 255f, 100f / 255f);
            public readonly static Color running = new Color(146f / 255f, 224f / 255f, 255f / 255f);
            public readonly static Color disabled = new Color(180f / 255f, 180f / 255f, 180f / 255f, 120f / 255f);
            public readonly static Color done = new Color(90f / 255f, 219f / 255f, 48f / 255f);

            public readonly static Color darkGreen = new Color(0.05f, 0.7f, 0.05f);
            public readonly static Color orange = new Color(0.9f, 0.45f, 0.05f);
            public readonly static Color darkYellow = new Color(0.7f, 0.7f, 0.05f);
            public readonly static Color green = new Color(10f / 255f, 200f / 255f, 10f / 255f);
            public readonly static Color yellow = new Color(255f / 255f, 244f / 255f, 94f / 255f);
            public readonly static Color cyan = new Color(146f / 255f, 224f / 255f, 255f / 255f);

            public readonly static Color faintGreen = new Color(0.1f, 0.9f, 0.1f, 0.3f);
            public readonly static Color transparentYellow = new Color(255f / 255f, 216f / 255f, 0f / 255f, 0.33f);
            public readonly static Color transparentGreen = new Color(0.1f, 0.9f, 0.1f, 0.5f);
            public readonly static Color faintGray = new Color(0.4f, 0.4f, 0.4f, 0.3f);
            public readonly static Color faintDarkGray = new Color(0.2f, 0.2f, 0.2f, 0.3f);

            public readonly static Color windowBackgroundLite = new Color(194f / 255f, 194f / 255f, 194f / 255f, 1);
            public readonly static Color windowBackgroundDark = new Color(56f / 255f, 56f / 255f, 56f / 255f, 1);

            public static Color WindowBackground => EditorGUIUtility.isProSkin ? windowBackgroundDark : windowBackgroundLite;
        }

        private static IconsContainer m_icons;
        public static IconsContainer Icons {
            get {
                if(m_icons == null) {
                    m_icons = new IconsContainer();
                }
                return m_icons;
            }
        }

        //[MenuItem("Assets/Refresh Icons")]
        public static void RefreshIcons() {
            Icons.RefreshIcons();
        }
        
        public class IconsContainer
        {
            private const int k_desiredHeight = 16;
            private readonly Dictionary<string, Texture2D> m_iconsDictionary;
            private readonly Dictionary<string, Texture2D> m_iconsOriginalsDictionary;
            public readonly Texture2D Rec;
            public readonly Texture2D RecActive;
            public readonly Texture2D Minus;
            public readonly Texture2D Plus;
            public readonly Texture2D Refresh;
            public readonly Texture2D Reset;
            public readonly Texture2D Close;
            public readonly Texture2D Delete;
            public readonly Texture2D DeleteBin;
            public readonly Texture2D Eye;
            public readonly Texture2D Visibility;
            public readonly Texture2D Ok;
            public readonly Texture2D PlayRange;

            internal IconsContainer() {
                m_iconsDictionary = new Dictionary<string, Texture2D>();
                m_iconsOriginalsDictionary = new Dictionary<string, Texture2D>();
                Rec = RetrieveTextureNormal("RecIcon") ?? Texture2D.blackTexture;
                RecActive = RetrieveTextureActive("RecIcon") ?? Texture2D.blackTexture;
                Minus = RetrieveTextureNormal("MinusIcon") ?? Texture2D.blackTexture;
                Plus = RetrieveTextureNormal("PlusIcon") ?? Texture2D.blackTexture;
                Refresh = RetrieveTextureNormal("RefreshIcon") ?? Texture2D.blackTexture;
                Reset = RetrieveTextureNormal("ResetIcon") ?? Texture2D.blackTexture;
                Close = RetrieveTextureNormal("CloseIcon") ?? Texture2D.blackTexture;
                Delete = RetrieveTextureNormal("DeleteIcon") ?? Texture2D.blackTexture;
                DeleteBin = RetrieveTextureNormal("DeleteBinIcon") ?? Texture2D.blackTexture;
                Eye = RetrieveTextureNormal("EyeIcon") ?? Texture2D.blackTexture;
                Visibility = RetrieveTextureNormal("VisibilityIcon") ?? Texture2D.blackTexture;
                Ok = RetrieveTextureNormal("OkIcon") ?? Texture2D.blackTexture;
                PlayRange = RetrieveTextureNormal("PlayRangeIcon") ?? Texture2D.blackTexture;

                RefreshIcons();
            }

            public Texture2D this[string name] {
                get {
                    if (m_iconsDictionary.TryGetValue(name.ToLower(), out Texture2D tex))
                    {
                        return tex;
                    }
                    Debug.LogFormat("Icon Texture '{0}' not found, returning white texture instead", name);
                    return Texture2D.whiteTexture;
                }
            }

            public IReadOnlyDictionary<string, Texture2D> Originals => m_iconsOriginalsDictionary;

            internal void RefreshIcons() {
                m_iconsDictionary.Clear();
                foreach(var icon in Resources.LoadAll<Texture2D>("Icons")) {
                    m_iconsOriginalsDictionary[icon.name.ToLower()] = icon;
                    m_iconsDictionary[icon.name.ToLower()] = icon.RescaleTexture(k_desiredHeight);
                }

                if (EditorGUIUtility.isProSkin) {
                    foreach (var icon in Resources.LoadAll<Texture2D>($"Editor/Icons/Dark"))
                    {
                        m_iconsOriginalsDictionary[icon.name.ToLower()] = icon;
                        m_iconsDictionary[icon.name.ToLower()] = icon.RescaleTexture(k_desiredHeight);
                    }
                }
                else
                {
                    foreach (var icon in Resources.LoadAll<Texture2D>($"Editor/Icons/Lite"))
                    {
                        m_iconsOriginalsDictionary[icon.name.ToLower()] = icon;
                        m_iconsDictionary[icon.name.ToLower()] = icon.RescaleTexture(k_desiredHeight);
                    }
                }
            }

            private static Texture2D RetrieveTextureNormal(string name) {
                var style = EditorSkin.GetStyle(name);
                if(style == null) {
                    Debug.LogFormat("WEAVRStyles: Unable to retrieve style '{0}'", name);
                    return null;
                }
                if (style.normal == null) {
                    Debug.LogFormat("WEAVRStyles: Style '{0}' does not have a '{1}' state", name, "normal");
                    return null;
                }
                if (style.normal.background == null) {
                    Debug.LogFormat("WEAVRStyles: Style '{0}' with '{1}' state does not have a background image", name, "normal");
                    return null;
                }
                return style.normal.background;
            }

            private static Texture2D RetrieveTextureActive(string name) {
                var style = EditorSkin.GetStyle(name);
                if (style == null) {
                    Debug.LogFormat("WEAVRStyles: Unable to retrieve style '{0}'", name);
                    return null;
                }
                if (style.active == null) {
                    Debug.LogFormat("WEAVRStyles: Style '{0}' does not have a '{1}' state", name, "active");
                    return null;
                }
                if (style.active.background == null) {
                    Debug.LogFormat("WEAVRStyles: Style '{0}' with '{1}' state does not have a background image", name, "active");
                    return null;
                }
                return style.active.background;
            }

            private static Texture2D RetrieveTextureHover(string name) {
                var style = EditorSkin.GetStyle(name);
                if (style == null) {
                    Debug.LogFormat("WEAVRStyles: Unable to retrieve style '{0}'", name);
                    return null;
                }
                if (style.hover == null) {
                    Debug.LogFormat("WEAVRStyles: Style '{0}' does not have a '{1}' state", name, "hover");
                    return null;
                }
                if (style.hover.background == null) {
                    Debug.LogFormat("WEAVRStyles: Style '{0}' with '{1}' state does not have a background image", name, "hover");
                    return null;
                }
                return style.hover.background;
            }

            private static Texture2D RetrieveTextureFocused(string name) {
                var style = EditorSkin.GetStyle(name);
                if (style == null) {
                    Debug.LogFormat("WEAVRStyles: Unable to retrieve style '{0}'", name);
                    return null;
                }
                if (style.focused == null) {
                    Debug.LogFormat("WEAVRStyles: Style '{0}' does not have a '{1}' state", name, "focused");
                    return null;
                }
                if (style.focused.background == null) {
                    Debug.LogFormat("WEAVRStyles: Style '{0}' with '{1}' state does not have a background image", name, "focused");
                    return null;
                }
                return style.focused.background;
            }
        }

        public static VisualElement CreateFromTemplate(string template)
        {
            var tpl = EditorGUIUtility.Load($"Assets/WEAVR/Essential/Editor/Resources/{template}.uxml") as VisualTreeAsset;

            VisualElement root = new VisualElement();
            tpl?.CloneTree(root);

            return root;
        }
        
        public static ResourceStyleSheets StyleSheets { get; } = new ResourceStyleSheets();

        public class ResourceStyleSheets
        {
            public const string CommonStylesheetPath = "Styles/CommonStylesheet";
            public const string DarkStylesheetPath = "Styles/CommonStylesheet_Dark";
            public const string LightStylesheetPath = "Styles/CommonStylesheet_Light";

            private StyleSheet m_common;
            private StyleSheet m_dark;
            private StyleSheet m_light;

            public StyleSheet Common
            {
                get
                {
                    if (!m_common)
                    {
                        m_common = Resources.Load<StyleSheet>(CommonStylesheetPath);
                    }
                    return m_common;
                }
            }

            public StyleSheet Dark
            {
                get
                {
                    if (!m_dark)
                    {
                        m_dark = Resources.Load<StyleSheet>(DarkStylesheetPath);
                    }
                    return m_dark;
                }
            }

            public StyleSheet Light
            {
                get
                {
                    if (!m_light)
                    {
                        m_light = Resources.Load<StyleSheet>(LightStylesheetPath);
                    }
                    return m_light;
                }
            }

            public StyleSheet Active => EditorGUIUtility.isProSkin ? Dark : Light;

            private Dictionary<(string, System.Type), StyleSheet> m_stylesheets;

            public ResourceStyleSheets()
            {
                m_stylesheets = new Dictionary<(string, System.Type), StyleSheet>();
            }

            public StyleSheet GetStyleSheet(string resource, System.Type type)
            {
                if(type != null)
                {
                    if (m_stylesheets.TryGetValue((resource, type), out StyleSheet ss) && ss)
                    {
                        return ss;
                    }
                    var styleSheet = Resources.Load<StyleSheet>(resource);
                    if (styleSheet)
                    {
                        m_stylesheets[(resource, type)] = styleSheet;
                        return styleSheet;
                    }
                }
                return m_stylesheets.FirstOrDefault(p => p.Key.Item1 == resource).Value;
            }
        }
    }
}