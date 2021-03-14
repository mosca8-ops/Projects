using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.TestsUtility
{

    public static class TestSceneManager
    {
        private static Scene? s_defaultFallbackScene;
        private static HashSet<string> s_createdSceneNames = new HashSet<string>();

        public static Scene CreateTestPreviewScene(string name)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            scene.name = name;
            return scene;
        }

        public static Scene[] PrepareScenes(bool additive, params string[] sceneNames)
        {
            List<string> scenePathsToLoad = new List<string>();
            if (additive)
            {
                // UNITY for now doesn't support opening additive new scenes in Test mode
                // So a workaround is being done here...
                // Basically save the already loaded scenes paths and load them additively after the NewScene
                for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
                {
                    var scene_i = EditorSceneManager.GetSceneAt(i);
                    if (!sceneNames.Contains(scene_i.name))
                    {
                        if (string.IsNullOrEmpty(scene_i.name))
                        {
                            scene_i.name = $"__TEST_SCENE_D_{UnityEngine.Random.Range(10, 999)}";
                            if (string.IsNullOrEmpty(scene_i.path))
                            {
                                s_createdSceneNames.Add(scene_i.name);
                                EditorSceneManager.SaveScene(scene_i, $"Assets/{scene_i.name}.unity");
                                scenePathsToLoad.Add($"Assets/{scene_i.name}.unity");
                            }
                            else
                            {
                                scenePathsToLoad.Add(scene_i.path);
                            }
                        }
                        else
                        {
                            scenePathsToLoad.Add(scene_i.path);
                        }
                    }
                }
            }

            for (int i = 0; i < sceneNames.Length; i++)
            {
                CreateOrGetTestScene(sceneNames[i], false);
            }

            if (additive)
            {
                foreach (var path_i in scenePathsToLoad)
                {
                    if (!string.IsNullOrEmpty(path_i))
                    {
                        try
                        {
                            EditorSceneManager.OpenScene(path_i, OpenSceneMode.Additive);
                        }
                        catch { }
                    }
                }
            }

            Scene[] scenes = new Scene[sceneNames.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                scenes[i] = EditorSceneManager.OpenScene($"Assets/{sceneNames[i]}.unity", OpenSceneMode.Additive);
            }

            return scenes;
        }

        public static Scene CreateOrGetTestScene(string name, bool additive = true)
        {
            if(!s_defaultFallbackScene.HasValue)
            {
                for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
                {
                    var scene_i = EditorSceneManager.GetSceneAt(i);
                    if (!string.IsNullOrEmpty(scene_i.path))
                    {
                        s_defaultFallbackScene = scene_i;
                        break;
                    }
                }
            }
            if (!TryGetAlreadySavedScene(name, additive, out Scene scene))
            {
                List<string> scenePathsToLoad = new List<string>();
                if (additive)
                {
                    // UNITY for now doesn't support opening additive new scenes in Test mode
                    // So a workaround is being done here...
                    // Basically save the already loaded scenes paths and load them additively after the NewScene
                    for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
                    {
                        var scene_i = EditorSceneManager.GetSceneAt(i);
                        if (scene_i.name != name)
                        {
                            if (string.IsNullOrEmpty(scene_i.name))
                            {
                                scene_i.name = $"__TEST_SCENE_D_{UnityEngine.Random.Range(10, 999)}";
                                if (string.IsNullOrEmpty(scene_i.path))
                                {
                                    s_createdSceneNames.Add(scene_i.name);
                                    EditorSceneManager.SaveScene(scene_i, $"Assets/{scene_i.name}.unity");
                                    scenePathsToLoad.Add($"Assets/{scene_i.name}.unity");
                                }
                                else
                                {
                                    scenePathsToLoad.Add(scene_i.path);
                                }
                            }
                            else
                            {
                                scenePathsToLoad.Add(scene_i.path);
                            }
                        }
                    }
                }

                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene.name = name;
                if (string.IsNullOrEmpty(scene.path))
                {
                    EditorSceneManager.SaveScene(scene, $"Assets/{name}.unity");
                }

                if (additive)
                {
                    foreach(var path_i in scenePathsToLoad)
                    {
                        if (!string.IsNullOrEmpty(path_i))
                        {
                            try
                            {
                                EditorSceneManager.OpenScene(path_i, OpenSceneMode.Additive);
                            }
                            catch { }
                        }
                    }
                }

                if (!scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene($"Assets/{name}.unity", additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
                }
            }
            s_createdSceneNames.Add(name);
            return scene;
        }

        private static bool TryGetAlreadySavedScene(string name, bool additive, out Scene scene)
        {
            if(File.Exists(Path.Combine(Application.dataPath, name + ".unity")))
            {
                scene = EditorSceneManager.OpenScene($"Assets/{name}.unity", additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
            }
            else
            {
                scene = new Scene();
            }
            return scene.IsValid();
        }

        public static void DeleteTestScene(string name)
        {
            if (!s_createdSceneNames.Contains(name)) { return; }

            var scene = SceneManager.GetSceneByName(name);
            if (scene.IsValid())
            {
                if(EditorSceneManager.loadedSceneCount == 1)
                {
                    if (s_defaultFallbackScene.HasValue && !string.IsNullOrEmpty(s_defaultFallbackScene.Value.path))
                    {
                        EditorSceneManager.OpenScene(s_defaultFallbackScene.Value.path, OpenSceneMode.Additive);
                    }
                    else
                    {
                        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                        s_defaultFallbackScene = null;
                    }
                }
                if (EditorSceneManager.IsPreviewScene(scene))
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
                else 
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            if (File.Exists(Path.Combine(Application.dataPath, name + ".unity")))
            {
                AssetDatabase.DeleteAsset($"Assets/{name}.unity");
                s_createdSceneNames.Remove(name);
            }
        }

        public static void CleanUp()
        {
            if (EditorSceneManager.loadedSceneCount == s_createdSceneNames.Count)
            {
                if (s_defaultFallbackScene.HasValue && !string.IsNullOrEmpty(s_defaultFallbackScene.Value.path))
                {
                    EditorSceneManager.OpenScene(s_defaultFallbackScene.Value.path, OpenSceneMode.Additive);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                    s_defaultFallbackScene = null;
                }
            }

            foreach(var file in Directory.GetFiles(Application.dataPath))
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                if (filename.StartsWith("__TEST_SCENE_"))
                {
                    s_createdSceneNames.Add(filename);
                }
            }

            foreach (var name in s_createdSceneNames)
            {
                var scene = SceneManager.GetSceneByName(name);
                if (scene.IsValid())
                {
                    if (EditorSceneManager.IsPreviewScene(scene))
                    {
                        EditorSceneManager.ClosePreviewScene(scene);
                    }
                    else
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
                if (File.Exists(Path.Combine(Application.dataPath, name + ".unity")))
                {
                    AssetDatabase.DeleteAsset($"Assets/{name}.unity");
                }
            }
            s_createdSceneNames.Clear();
        }
    }
}
