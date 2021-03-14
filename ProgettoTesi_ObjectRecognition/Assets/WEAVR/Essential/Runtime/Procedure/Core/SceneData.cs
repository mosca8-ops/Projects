using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Procedure
{

    [Serializable]
    public class SceneData : IEquatable<Scene>
    {
        public delegate void GetSceneGuidDelegate(string scenePath, out string name, out string guid);
        public delegate Scene ResolveSceneDelegate(string scenePath, string sceneGuid);
        public delegate (string path, string name) GetSceneAssetDataDelegate(string guid);
        public delegate bool SceneExistsDelegate(string path);

        public static GetSceneGuidDelegate GetSceneGuid;
        public static ResolveSceneDelegate ResolveSceneEditor;
        public static GetSceneAssetDataDelegate GetScenePathAndName;
        public static SceneExistsDelegate SceneExists;

        [SerializeField]
        private string m_sceneName;
        [SerializeField]
        private string m_scenePath;
        [SerializeField]
        private string m_sceneGuid;

        public string Name => m_sceneName;
        public string Path
        {
            get => m_scenePath;
            set
            {
                if(Application.isEditor && m_scenePath != value)
                {
                    m_scenePath = value;
                    if (!string.IsNullOrEmpty(m_scenePath) && GetSceneGuid != null)
                    {
                        GetSceneGuid(m_scenePath, out m_sceneName, out m_sceneGuid);
                    }
                }
            }
        }

        //public string Guid => m_sceneGuid;
        public bool IsEmpty => string.IsNullOrEmpty(m_scenePath);

        public void ValidateSceneData()
        {
            if (!string.IsNullOrEmpty(m_sceneGuid) && SceneExists != null && !SceneExists(m_scenePath) && GetScenePathAndName != null)
            {
                var (newPath, newName) = GetScenePathAndName(m_sceneGuid);
                m_scenePath = newPath;
                m_sceneName = newName;
            }
        }

        public Scene ResolveScene()
        {
            if(IsEmpty || string.IsNullOrEmpty(m_sceneGuid))
            {
                GetSceneGuid?.Invoke(m_scenePath, out m_sceneName, out m_sceneGuid);
            }
            return ResolveSceneEditor != null ? ResolveSceneEditor(m_scenePath, m_sceneGuid) : ResolveSceneRuntime();
        }

        private Scene ResolveSceneRuntime()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path == m_scenePath)
                {
                    return scene;
                }
            }
            return default;
        }

        public SceneData(Scene scene)
        {
            m_sceneName = scene.name;
            m_scenePath = scene.path;
            GetSceneGuid?.Invoke(m_scenePath, out m_sceneName, out m_sceneGuid);
        }

        public SceneData(string path)
        {
            m_scenePath = path;
            GetSceneGuid?.Invoke(m_scenePath, out m_sceneName, out m_sceneGuid);
        }

        private SceneData()
        {

        }

        public static SceneData Empty { get; } = new SceneData();

        public static implicit operator SceneData(Scene scene)
        {
            return new SceneData(scene);
        }

        public bool Equals(Scene other)
        {
            return (m_sceneName == other.name && m_scenePath == other.path);
        }

        //public override bool Equals(object obj) {
        //    return obj is SceneWrapper && ((SceneWrapper)obj).fileId == fileId && ((SceneWrapper)obj).guid == guid;
        //}

        public override int GetHashCode()
        {
            return m_scenePath.GetHashCode();
        }

        public bool IsSame(SceneData other)
        {
            return m_scenePath == other.m_scenePath;
        }

        public void ForceUpdate(Scene scene, string sceneGuid)
        {
            m_sceneName = scene.name;
            m_scenePath = scene.path;
            if (string.IsNullOrEmpty(sceneGuid))
            {
                GetSceneGuid?.Invoke(m_scenePath, out m_sceneName, out m_sceneGuid);
            }
            else
            {
                m_sceneGuid = sceneGuid;
            }
        }
    }
}
