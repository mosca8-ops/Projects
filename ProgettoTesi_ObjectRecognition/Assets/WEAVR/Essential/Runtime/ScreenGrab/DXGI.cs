using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace TXT.WEAVR.ScreenGrab
{

    [AddComponentMenu("WEAVR/Advanced/DXGI")]
    public class DXGI : MonoBehaviour
    {

        #region [  DLL CALLS  ]

        [DllImport("WeavrDesktopCapture")]
        private static extern void DesktopCapturePlugin_Initialize();
        [DllImport("WeavrDesktopCapture")]
        private static extern int DesktopCapturePlugin_GetNDesks();
        [DllImport("WeavrDesktopCapture")]
        private static extern int DesktopCapturePlugin_GetWidth(int iDesk);
        [DllImport("WeavrDesktopCapture")]
        private static extern int DesktopCapturePlugin_GetHeight(int iDesk);
        [DllImport("WeavrDesktopCapture")]
        private static extern int DesktopCapturePlugin_GetNeedReInit();
        [DllImport("WeavrDesktopCapture")]
        private static extern int DesktopCapturePlugin_SetTexturePtr(int iDesk, IntPtr ptr);
        [DllImport("WeavrDesktopCapture")]
        private static extern IntPtr DesktopCapturePlugin_GetRenderEventFunc();

        #endregion


        #region [  STATIC PART  ]

        private static DXGI s_instance;
        public static DXGI Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindObjectOfType<DXGI>();
                    if (!s_instance)
                    {
                        s_instance = new GameObject("DXGI_MANAGER").AddComponent<DXGI>();
                    }
                    s_instance.Awake();
                }

                return s_instance;
            }
        }

        public static DXGI UnsafeInstance => s_instance;

        #endregion

        [SerializeField]
        private TextureFormat m_textureFormat = TextureFormat.BGRA32;

        private Texture2D[] m_textures;
        private DXGI_Object m_dxgiObject;
        private CommandBuffer m_command;

        private string FilePath => Path.Combine(Application.streamingAssetsPath, "dxgi_config.json");
        private Dictionary<string, IDXGI_DataClient> m_handlers = new Dictionary<string, IDXGI_DataClient>();
        private Dictionary<string, IDXGI_GroupDataClient> m_groupHandlers = new Dictionary<string, IDXGI_GroupDataClient>();

        [NonSerialized]
        private bool m_initialized = false;
        [NonSerialized]
        private bool m_isBeingDestroyed = false;

        public event Action Changed;

        private void Awake()
        {
            if (s_instance && s_instance != this)
            {
                Destroy(this);
                return;
            }
            s_instance = this;

            if (!m_initialized)
            {
                m_initialized = true;
                Initialize();
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "dxgi_config.json"));
                    m_dxgiObject = JsonUtility.FromJson<DXGI_Object>(json);
                    if (m_dxgiObject != null && (m_dxgiObject.handlers?.Length > 0 || m_dxgiObject.groupHandlers?.Length > 0))
                    {
                        Debug.Log("<b>DXGI</b>: Configuration file read successfully");
                    }
                    else
                    {
                        Debug.LogError("<b>DXGI</b>: Something went wrong when reading configuration file");
                    }
                    m_dxgiObject?.Initialize();
                }
            }

            Display.onDisplaysUpdated -= Display_OnDisplaysUpdated;
            Display.onDisplaysUpdated += Display_OnDisplaysUpdated;

        }

        private void Display_OnDisplaysUpdated()
        {
            Clear();
            Initialize();
            Changed?.Invoke();
        }

        private void Initialize()
        {
            DesktopCapturePlugin_Initialize();
            int screens = NumberOfScreens;
            m_textures = new Texture2D[screens];
            for (int i = 0; i < screens; i++)
            {
                var (width, height) = GetScreenSize(i);
                var texture = new Texture2D(width, height, m_textureFormat, false);
                m_textures[i] = texture;
                DesktopCapturePlugin_SetTexturePtr(i, texture.GetNativeTexturePtr());
            }
        }

        private void OnDestroy()
        {
            Clear();
            Display.onDisplaysUpdated -= Display_OnDisplaysUpdated;
            if(s_instance == this)
            {
                s_instance = null;
            }
        }

        public void RegisterClient(IDXGI_DataClient client)
        {
            m_handlers[client.Id] = client;
            if(m_dxgiObject != null && m_dxgiObject.data.TryGetValue(client.Id, out DXGI_Data data) && data != null)
            {
                client.Data = data;
            }
            else if(client.Data == null)
            {
                client.Data = new DXGI_Data();
            }
        }

        public void RegisterClient(IDXGI_GroupDataClient client)
        {
            m_groupHandlers[client.Id] = client;
            if (m_dxgiObject != null && m_dxgiObject.groups.TryGetValue(client.Id, out DXGI_GroupData data) && data != null)
            {
                client.GroupData = data;
            }
            else if(client.GroupData == null)
            {
                client.GroupData = new DXGI_GroupData();
            }
        }

        public void UnregisterClient(IDXGI_DataClient client)
        {
            if(m_handlers.TryGetValue(client.Id, out IDXGI_DataClient other) && other == client)
            {
                m_handlers.Remove(client.Id);
            }
        }

        public void UnregisterClient(IDXGI_GroupDataClient client)
        {
            if (m_groupHandlers.TryGetValue(client.Id, out IDXGI_GroupDataClient other) && other == client)
            {
                m_groupHandlers.Remove(client.Id);
            }
        }

        public void SaveCurrentConfigurationToDisk()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            File.WriteAllText(FilePath, JsonUtility.ToJson(new DXGI_Object()
            {
                groupHandlers = m_groupHandlers.Values.Select(g => GetDataWithSameID(g)).ToArray(),
                handlers = m_handlers.Values.Select(h => GetDataWithSameID(h)).ToArray()
            }, true));
        }

        private DXGI_Data GetDataWithSameID(IDXGI_DataClient client)
        {
            client.Data.id = client.Id;
            return client.Data;
        }

        private DXGI_GroupData GetDataWithSameID(IDXGI_GroupDataClient client)
        {
            return new DXGI_GroupData(){
                groupId = client.Id,
                data = client.GroupData.data.Where(d => d.size != Vector2.zero).ToArray(),
            };
        }

        private void Clear()
        {
            if (m_textures != null)
            {
                for (int i = 0; i < m_textures.Length; i++)
                {
                    DesktopCapturePlugin_SetTexturePtr(i, IntPtr.Zero);
                    Destroy(m_textures[i]);
                }
            }
        }

        ~DXGI()
        {
            // TODO: Destroy the desktop capture...
        }

        private void Update()
        {
            //if (DesktopCapturePlugin_GetNeedReInit() != 0)
            //{
            //    Clear();
            //    Initialize();
            //}
            GL.IssuePluginEvent(RenderCallback, 0);
        }

        public TextureFormat TextureFormat => m_textureFormat;
        public (int width, int height) GetScreenSize(int screenId) => (DesktopCapturePlugin_GetWidth(screenId), DesktopCapturePlugin_GetHeight(screenId));
        public int NumberOfScreens => DesktopCapturePlugin_GetNDesks();
        public IntPtr RenderCallback => DesktopCapturePlugin_GetRenderEventFunc();
        public Texture2D GetTexture(int screenId)
        {
            //if (!m_textures[screenId])
            //{
            //    var (width, height) = GetScreenSize(screenId);
            //    var texture = new Texture2D(width, height, m_textureFormat, false);
            //    m_textures[screenId] = texture;
            //    DesktopCapturePlugin_SetTexturePtr(screenId, texture.GetNativeTexturePtr());
            //}
            return m_textures[screenId];
        }


        [Serializable]
        private class DXGI_Object
        {
            public DXGI_GroupData[] groupHandlers;
            public DXGI_Data[] handlers;

            public Dictionary<string, DXGI_Data> data;
            public Dictionary<string, DXGI_GroupData> groups;

            public void Initialize()
            {
                data = new Dictionary<string, DXGI_Data>();
                groups = new Dictionary<string, DXGI_GroupData>();

                foreach (var item in handlers)
                {
                    data[item.id] = item;
                }

                foreach (var item in groupHandlers)
                {
                    groups[item.groupId] = item;
                }
            }
        }
    }
}
