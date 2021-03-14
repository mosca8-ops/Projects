using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Editor
{

    public class TestBuiltinStyles : EditorWindow
    {
#if !WEAVR_DLL && WEAVR_INTERNAL_USE
        [MenuItem("WEAVR/Utilities/Built-in Skin Browser")]
#endif
        static void OpenWindow()
        {
            TestBuiltinStyles window = EditorWindow.GetWindow<TestBuiltinStyles>(windowTitle);
            window.Focus();
        }

        const string windowTitle = "Built-in Skin Browser";

        GUISkin _nativeInpsectorSkin;

        Vector2 _scrollPosition = Vector2.zero;

        int _viewOffset = 0;

        System.Text.RegularExpressions.Regex regex;
        string _searchPattern = "";


        void GetNativeSkins()
        {
            _nativeInpsectorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
        }

        void OnEnable()
        {
            GetNativeSkins();
        }

        bool _testToggle = false;
        string _testText = "Testing as textfield";


        Texture2D _normalTextureSelected;
        Texture2D _activeTextureSelected;
        Texture2D _onNormalTextureSelected;
        Texture2D _onActiveTextureSelected;

        void DrawTextureAlpha(Vector2 pos, Texture2D t)
        {
            if (t != null)
            {
                EditorGUI.DrawPreviewTexture(new Rect(pos.x, pos.y, t.width, t.height), t);

                EditorGUI.DrawTextureAlpha(new Rect(pos.x + t.width + 1, pos.y, t.width, t.height), t);
            }
        }

        void OnGUI()
        {

            if (_normalTextureSelected != null || _activeTextureSelected != null || _onNormalTextureSelected != null || _onActiveTextureSelected != null)
            {
                const int padding = 5;

                int currentX = padding;
                int overallHeight = 0;


                if (_normalTextureSelected != null)
                {
                    DrawTextureAlpha(new Vector2(currentX, padding), _normalTextureSelected);
                    currentX += (_normalTextureSelected.width * 2) + 1 + padding;

                    overallHeight = Mathf.Max(overallHeight, _normalTextureSelected.height);
                }

                if (_activeTextureSelected != null)
                {
                    DrawTextureAlpha(new Vector2(currentX, padding), _activeTextureSelected);
                    currentX += (_activeTextureSelected.width * 2) + 1 + padding;

                    overallHeight = Mathf.Max(overallHeight, _activeTextureSelected.height);
                }

                if (_onNormalTextureSelected != null)
                {
                    DrawTextureAlpha(new Vector2(currentX, padding), _onNormalTextureSelected);
                    currentX += (_onNormalTextureSelected.width * 2) + 1 + padding;

                    overallHeight = Mathf.Max(overallHeight, _onNormalTextureSelected.height);
                }

                if (_onActiveTextureSelected != null)
                {
                    DrawTextureAlpha(new Vector2(currentX, padding), _onActiveTextureSelected);
                    currentX += (_onActiveTextureSelected.width * 2) + 1 + padding;

                    overallHeight = Mathf.Max(overallHeight, _onActiveTextureSelected.height);
                }

                GUILayout.Space(overallHeight + padding + padding);
            }


            _searchPattern = EditorGUILayout.TextField("Search Pattern:", _searchPattern);
            regex = new Regex(string.IsNullOrEmpty(_searchPattern) ? ".*" : _searchPattern);


            int lenToShow = Mathf.Min(_viewOffset + 50, _nativeInpsectorSkin.customStyles.Length);


            GUILayout.Label(
                string.Format("{0} custom styles in total. Viewing from {1} to {2}.",
                    _nativeInpsectorSkin.customStyles.Length,
                    _viewOffset, lenToShow)
            );



            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Prev 50") && (_viewOffset - 50 >= 0))
            {
                _viewOffset -= 50;
            }
            if (GUILayout.Button("Next 50") && (_viewOffset + 50 < _nativeInpsectorSkin.customStyles.Length))
            {
                _viewOffset += 50;
            }
            if (GUILayout.Button("Save Page Textures"))
            {
                string directory = GetDialogDirectory();
                if (!string.IsNullOrEmpty(directory))
                {
                    for (int i = _viewOffset; i < lenToShow; i++)
                    {
                        SaveStyleTextures(_nativeInpsectorSkin.customStyles[i], false, directory);
                    }
                }
            }
            if (GUILayout.Button("Save Whole Skin"))
            {
                string directory = GetDialogDirectory();
                if (!string.IsNullOrEmpty(directory))
                {
                    SaveSkinTextures(_nativeInpsectorSkin, false, directory);
                }
            }
            GUILayout.EndHorizontal();





            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                for (int n = _viewOffset; n < lenToShow; ++n)
                {
                    GUIStyle customSkinEntry = _nativeInpsectorSkin.customStyles[n];

                    if (string.IsNullOrEmpty(customSkinEntry.name) || !regex.IsMatch(customSkinEntry.name))
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(customSkinEntry.name);

                    GUILayout.FlexibleSpace();

                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(1000));
                    GUILayout.Space(2);
                    GUILayout.Box("               ", customSkinEntry);
                    GUILayout.Space(2);
                    GUILayout.Button("Testing as Button", customSkinEntry);
                    GUILayout.Space(2);
                    _testToggle = GUILayout.Toggle(_testToggle, "Testing as Toggle", customSkinEntry);
                    GUILayout.Space(2);
                    _testText = GUILayout.TextField(_testText, customSkinEntry);

                    GUILayout.Space(10);


                    GUILayout.BeginHorizontal();
                    GUILayout.Box(customSkinEntry.normal.background);
                    GUILayout.Space(2);
                    GUILayout.Box(customSkinEntry.active.background);
                    GUILayout.Space(2);
                    GUILayout.Box(customSkinEntry.onNormal.background);
                    GUILayout.Space(2);
                    GUILayout.Box(customSkinEntry.onActive.background);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(2);

                    GUILayout.EndHorizontal();

                    GUILayout.FlexibleSpace();


                    // since DrawTextureAlpha doesn't have a EditorGUILayout equivalent, we do this ass-backward way
                    if (GUILayout.Button("Show Alpha"))
                    {
                        _normalTextureSelected = customSkinEntry.normal.background;
                        _activeTextureSelected = customSkinEntry.active.background;
                        _onNormalTextureSelected = customSkinEntry.onNormal.background;
                        _onActiveTextureSelected = customSkinEntry.onActive.background;
                    }


                    // doesn't work since images from built-in skin isn't set to readable
                    if (GUILayout.Button("Save Textures") && customSkinEntry.normal.background != null)
                    {
                        string directory = GetDialogDirectory();
                        if (!string.IsNullOrEmpty(directory))
                        {
                            SaveStyleTextures(customSkinEntry, true, directory);
                        }
                        //    Texture2D normalH = customSkinEntry.normal.background;
                        ////haven't tested if this works since it's Pro only
                        ////Texture2D normal = Texture2D.CreateExternalTexture(normalH.width, normalH.height, normalH.format, false, true, normalH.GetNativeTexturePtr());
                        //var copyTex = CloneTextureAdvanced(normalH);
                        //var imageAsPng = copyTex.EncodeToPNG();
                        ////var imageAsPng = normalH.EncodeToPNG();
                        //Debug.Log(imageAsPng);
                        //File.WriteAllBytes(Application.dataPath + "/../Normal-" + normalH.name + ".png", imageAsPng);
                    }


                    GUILayout.EndHorizontal();

                    GUILayout.Space(10);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static string GetDialogDirectory()
        {
            string path = EditorUtility.SaveFolderPanel("Save the textures", Application.dataPath + "/../", "Skin Textures");
            string directory = path.EndsWith("/") ? path : path + "/";
            return directory;
        }

        private static void SaveSkinTextures(GUISkin skin, bool flat, string directory)
        {
            if (skin == null) { return; }
            if (!directory.EndsWith("/")) { directory += "/"; }
            directory += skin.name + "/";

            // Standard styles
            SaveStyleTextures(skin.box, flat, directory);
            SaveStyleTextures(skin.button, flat, directory);
            SaveStyleTextures(skin.horizontalScrollbar, flat, directory);
            SaveStyleTextures(skin.horizontalScrollbarLeftButton, flat, directory);
            SaveStyleTextures(skin.horizontalScrollbarRightButton, flat, directory);
            SaveStyleTextures(skin.horizontalScrollbarThumb, flat, directory);
            SaveStyleTextures(skin.horizontalSlider, flat, directory);
            SaveStyleTextures(skin.horizontalSliderThumb, flat, directory);
            SaveStyleTextures(skin.label, flat, directory);
            SaveStyleTextures(skin.scrollView, flat, directory);
            SaveStyleTextures(skin.textArea, flat, directory);
            SaveStyleTextures(skin.textField, flat, directory);
            SaveStyleTextures(skin.toggle, flat, directory);
            SaveStyleTextures(skin.verticalScrollbar, flat, directory);
            SaveStyleTextures(skin.verticalScrollbarDownButton, flat, directory);
            SaveStyleTextures(skin.verticalScrollbarThumb, flat, directory);
            SaveStyleTextures(skin.verticalScrollbarUpButton, flat, directory);
            SaveStyleTextures(skin.verticalSlider, flat, directory);
            SaveStyleTextures(skin.verticalSliderThumb, flat, directory);
            SaveStyleTextures(skin.window, flat, directory);

            // Custom styles
            foreach (var style in skin.customStyles)
            {
                SaveStyleTextures(style, flat, directory);
            }
        }

        private static void SaveStyleTextures(GUIStyle style, bool flat, string directory)
        {
            if (style == null) { return; }
            if (!directory.EndsWith("/")) { directory += "/"; }
            if (!flat) { directory += style.name + "/"; }
            SaveStyleStateTexture(style.normal, "normal", directory);
            SaveStyleStateTexture(style.active, "active", directory);
            SaveStyleStateTexture(style.hover, "hover", directory);
            SaveStyleStateTexture(style.focused, "focused", directory);
            SaveStyleStateTexture(style.onNormal, "onNormal", directory);
            SaveStyleStateTexture(style.onActive, "onActive", directory);
            SaveStyleStateTexture(style.onHover, "onHover", directory);
            SaveStyleStateTexture(style.onFocused, "normal", directory);
        }

        private static void SaveStyleStateTexture(GUIStyleState state, string name, string directory)
        {
            if (state != null)
            {
                SaveTexture(state.background, name, directory);
            }
        }

        private static void SaveTexture(Texture2D texture, string name, string directory)
        {
            if (texture == null || texture.width == 0 || texture.height == 0) { return; }
            directory = directory ?? (Application.dataPath + "/../");
            if (!directory.EndsWith("/")) { directory += "/"; }
            string path = directory + texture.name + "_" + (name ?? "") + ".png";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(path, CloneTexture(texture).EncodeToPNG());
            Debug.Log("Saved texture to: " + path);
        }

        private static Texture2D CloneTexture(Texture2D source)
        {
            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                                source.width,
                                source.height);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(source, tmp);
            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;
            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new Texture2D(source.width, source.height);
            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
            // Reset the active RenderTexture
            RenderTexture.active = previous;
            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);

            // "myTexture2D" now has the same pixels from "texture" and it's readable.
            return myTexture2D;
        }
    }
}