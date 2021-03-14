using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TXT.WEAVR.Communication;
using System.Threading;
using System.IO;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Player.DataSources
{
    public class CacheManager : ICacheManager, IDownloadClient
    {
        private Dictionary<string, Texture2D> m_textures = new Dictionary<string, Texture2D>();
        private Dictionary<string, HashSet<Action<float>>> m_progressCallbacks = new Dictionary<string, HashSet<Action<float>>>();
        private Dictionary<string, FileMapping> m_fileMappings = new Dictionary<string, FileMapping>();
        private readonly string m_cacheFolder;

        public IDownloadManager DownloadManager { get; set; }

        public CacheManager(string cacheFolderPath)
        {
            m_cacheFolder = cacheFolderPath;
            if (!Directory.Exists(m_cacheFolder))
            {
                Directory.CreateDirectory(m_cacheFolder);
            }
            m_fileMappings = new Dictionary<string, FileMapping>();
            if(!Directory.Exists(Path.Combine(m_cacheFolder, "Textures")))
            {
                Directory.CreateDirectory(Path.Combine(m_cacheFolder, "Textures"));
            }
            try
            {
                var filepath = Path.Combine(m_cacheFolder, "mappings.json");
                if (File.Exists(filepath))
                {
                    var mappings = Newtonsoft.Json.JsonConvert.DeserializeObject<FileMapping[]>(File.ReadAllText(filepath));
                    if (mappings != null)
                    {
                        foreach (var mapping in mappings)
                        {
                            m_fileMappings[mapping.url] = mapping;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WeavrDebug.LogException(this, ex);
            }
        }

        public async Task<Texture2D> GetTexture(string url, Action<float> progresser = null)
        {
            if(m_textures.TryGetValue(url, out Texture2D texture))
            {
                return texture;
            }

            var newUrl = false;
            if(!m_fileMappings.TryGetValue(url, out FileMapping mapping))
            {
                newUrl = true;
                mapping.url = url;
                mapping.filepath = Path.Combine(m_cacheFolder, "Textures", Path.GetFileName(url));
            }

            if (!File.Exists(mapping.filepath))
            {
                try
                {
                    // Download it now
                    await DownloadManager.DownloadFileAsync(url, new Request(url), mapping.filepath, progresser);
                    if (newUrl)
                    {
                        mapping.size = new FileInfo(mapping.filepath).Length;
                        m_fileMappings[url] = mapping;
                        SaveMappingsToDisk();
                    }
                }
                catch(Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                }
            }
            else if (newUrl)
            {
                // Download and check if it is the same or not
                try
                {
                    var tempFilePath = "temp_" + UnityEngine.Random.Range(100, 100000) + mapping.filepath;
                    await DownloadManager.DownloadFileAsync(url, new Request(url), tempFilePath, progresser);
                    var fileInfo = new FileInfo(tempFilePath);
                    if (mapping.size == fileInfo.Length)
                    {
                        File.Delete(tempFilePath);
                    }
                    else
                    {
                        mapping.size = fileInfo.Length;
                        mapping.filepath = tempFilePath;
                    }

                    m_fileMappings[url] = mapping;
                    SaveMappingsToDisk();
                }
                catch (Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                }
            }

            try
            {
                texture = new Texture2D(2, 2);
                texture.LoadImage(File.ReadAllBytes(mapping.filepath));
                m_textures[url] = texture;
            }
            catch (Exception ex)
            {
                texture = null;
                WeavrDebug.LogException(this, ex);
            }

            return texture;
        }

        private void SaveMappingsToDisk()
        {
            try
            {
                var filepath = Path.Combine(m_cacheFolder, "mappings.json");
                File.WriteAllText(filepath, Newtonsoft.Json.JsonConvert.SerializeObject(m_fileMappings.Values.ToArray()));
            }
            catch(Exception ex)
            {
                WeavrDebug.LogException(this, ex);
            }
        }

        public void ClearCache()
        {
            if (Directory.Exists(m_cacheFolder))
            {
                Directory.Delete(m_cacheFolder, true);
            }
            Directory.CreateDirectory(m_cacheFolder);
            foreach(var pair in m_textures)
            {
                UnityEngine.Object.Destroy(pair.Value);
            }
            m_textures.Clear();
            m_fileMappings.Clear();
        }

        [Serializable]
        private struct FileMapping
        {
            public string url;
            public string filepath;
            public long size;
        }
    }
}
