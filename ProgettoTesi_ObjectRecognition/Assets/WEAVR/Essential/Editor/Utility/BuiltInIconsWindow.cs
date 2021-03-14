using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Utility
{

    public class BuiltInIconsWindow : EditorWindow
    {

#if !WEAVR_DLL && WEAVR_INTERNAL_USE
        [MenuItem("WEAVR/Utilities/Icons Browser")]
#endif
        public static void ShowWindow() {
            var window = GetWindow<BuiltInIconsWindow>();
            window.minSize = new Vector2(200, 800);
            window.titleContent = new GUIContent("Icons Browser");
            window.Show();
        }

        private Type[] _types;
        private KeyValuePair<string, Texture>[] _contents;
        private Vector2 _scrollPosition;
        int _viewOffset = 0;

        void GetTypes() {
            _types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => {
                    // Ugly hack to handle mis-versioned dlls
                    var innerTypes = new Type[0];
                    try {
                        innerTypes = t.GetTypes();
                    }
                    catch { }
                    return innerTypes;
                }).Where(t => t.IsSubclassOf(typeof(UnityEngine.Object))).ToArray();
        }

        private void GetContents() {
            List<KeyValuePair<string, Texture>> contents = new List<KeyValuePair<string, Texture>>();
            HashSet<Texture> images = new HashSet<Texture>();
            GUIContent tempContent;
            foreach (var type in _types) {
                tempContent = EditorGUIUtility.ObjectContent(null, type);
                if (tempContent.image != null && !images.Contains(tempContent.image)) {
                    contents.Add(new KeyValuePair<string, Texture>(tempContent.text ?? tempContent.image.name ?? "No name", tempContent.image));
                    images.Add(tempContent.image);
                }
            }
            _contents = contents.OrderBy(p => p.Key).ToArray();
        }

        void OnEnable() {
            GetTypes();
            GetContents();
        }

        void OnGUI() {

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Prev 50") && (_viewOffset - 50 >= 0)) {
                _viewOffset -= 50;
                _scrollPosition = Vector2.zero;
            }
            if (GUILayout.Button("Next 50") && (_viewOffset + 50 < _contents.Length)) {
                _viewOffset += 50;
                _scrollPosition = Vector2.zero;
            }
            EditorGUILayout.EndHorizontal();
            
            int lenToShow = Mathf.Min(_viewOffset + 50, _contents.Length);

            EditorGUILayout.LabelField(string.Format("Showing form {0} to {1} out of {2}", _viewOffset, lenToShow, _contents.Length), EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                for (int n = _viewOffset; n < lenToShow; ++n) {
                    var content = _contents[n];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(content.Key);
                    GUILayout.FlexibleSpace();
                    var imageRect = EditorGUILayout.GetControlRect(GUILayout.Width(content.Value.width), GUILayout.Height(content.Value.height));
                    GUI.DrawTexture(imageRect, content.Value);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Save")) {
                        string directory = GetDialogDirectory();
                        if (!string.IsNullOrEmpty(directory)) {
                            SaveTexture(CloneTexture(content.Value), directory);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static string GetDialogDirectory() {
            string path = EditorUtility.SaveFolderPanel("Save the textures", Application.dataPath + "/../", "Skin Textures");
            string directory = path.EndsWith("/") ? path : path + "/";
            return directory;
        }

        private static void SaveTexture(Texture2D texture, string directory) {
            if (texture == null || texture.width == 0 || texture.height == 0) { return; }
            directory = directory ?? (Application.dataPath + "/../");
            if (!directory.EndsWith("/")) { directory += "/"; }
            string path = directory + texture.name + ".png";
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(path, CloneTexture(texture).EncodeToPNG());
            Debug.Log("Saved texture to: " + path);
        }

        private static Texture2D CloneTexture(Texture source) {
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
