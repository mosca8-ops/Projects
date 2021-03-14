using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SceneEntity = TXT.WEAVR.Communication.Entities.Scene;
using Scene = UnityEngine.SceneManagement.Scene;

using UnityEngine.SceneManagement;
using TXT.WEAVR.Core;

namespace TXT.WEAVR.Player.DataSources
{
    public class SceneProxy : ISceneProxy, IDisposableProxy
    {
        protected string m_sceneFullPath;
        protected byte[] m_sceneBytes;
        protected string m_scene;
        private ProcedureFlags m_status;
        private WeavrAssetBundle m_assetBundle;


        public event OnValueChanged<ProcedureFlags> StatusChanged;

        public SceneEntity Scene { get; set; }

        public IEnumerable<ISceneProxy> AdditiveScenesProxies { get; set; }

        public Guid Id => Scene.Id;

        public IProcedureDataSource Source { get; set; }

        public ProcedureFlags Status
        {
            get => m_status;
            set
            {
                if (m_status != value)
                {
                    m_status = value;
                    StatusChanged?.Invoke(m_status);
                }
            }
        }

        public SceneProxy(IProcedureDataSource dataSource, SceneEntity scene)
        {
            Source = dataSource;
            Scene = scene;
        }

        public SceneProxy(IProcedureDataSource dataSource, SceneEntity scene, string sceneFilePath)
        {
            Source = dataSource;
            Scene = scene;
            m_sceneFullPath = sceneFilePath;
        }

        public SceneProxy(IProcedureDataSource dataSource, SceneEntity scene, byte[] sceneBytes)
        {
            Source = dataSource;
            Scene = scene;
            m_sceneBytes = sceneBytes;
        }

        public SceneProxy(IProcedureDataSource dataSource, SceneEntity scene, Scene unityScene)
        {
            Source = dataSource;
            Scene = scene;
            m_scene = unityScene.name;
        }

        public Task<SceneEntity> GetSceneEntity() => Task.FromResult(Scene);

        public async Task<string> GetUnityScene()
        {
            if (m_scene != null)
            {
                return m_scene;
            }

            if (!string.IsNullOrEmpty(m_sceneFullPath))
            {
                m_assetBundle = await WeavrAssetBundle.LoadFromFileAsync(m_sceneFullPath);
            }
            else if (m_sceneBytes != null && m_sceneBytes.Length > 0)
            {
                m_assetBundle = await WeavrAssetBundle.LoadFromMemoryAsync(m_sceneBytes);
            }

            var sceneEntity = await GetSceneEntity();

            if (m_assetBundle != null && sceneEntity != null)
            {
                var sceneNames = m_assetBundle.GetAllScenePaths();
                for (int i = 0; i < sceneNames.Length; i++)
                {
                    //sceneNames[i] = sceneNames[i].Replace(".unity", "");
                    if (sceneEntity.Name == sceneNames[i])
                    {
                        m_scene = sceneNames[i];
                        return m_scene;
                    }
                }
                for (int i = 0; i < sceneNames.Length; i++)
                {
                    if (sceneNames[i].Contains(sceneEntity.Name))
                    {
                        m_scene = sceneNames[i];
                        return m_scene;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(sceneEntity?.Name))
            {
                m_scene = sceneEntity.Name;
                return m_scene;
            }

            return default;
        }

        private static Scene GetSceneByName(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid())
            {
                return scene;
            }
            sceneName = sceneName.ToLower();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                scene = SceneManager.GetSceneByBuildIndex(i);
                if (scene.name.ToLower() == sceneName)
                {
                    return scene;
                }
                else if (scene.path.ToLower().Replace(".unity", string.Empty).EndsWith(sceneName))
                {
                    return scene;
                }
            }
            return default;
        }

        private static Scene GetSceneByPath(string scenePath)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid())
            {
                return scene;
            }
            scenePath = scenePath.ToLower();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                scene = SceneManager.GetSceneByBuildIndex(i);
                if (scene.path.ToLower().StartsWith(scenePath))
                {
                    return scene;
                }
            }
            return default;
        }

        public virtual Task Sync(Action<float> progressUpdate = null) => null;

        public virtual void Refresh()
        {

        }

        public void Dispose()
        {
            if (m_assetBundle != null)
            {
                m_assetBundle.Unload(true);
                m_assetBundle = null;
                m_scene = null;
            }
        }
    }
}
