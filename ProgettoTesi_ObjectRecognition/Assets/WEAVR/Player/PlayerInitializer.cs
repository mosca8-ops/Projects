using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Communication;
using TXT.WEAVR.Player.Controller;
using TXT.WEAVR.Player.DataSources;
using TXT.WEAVR.Player.Model;
using TXT.WEAVR.Player.Views;
using UnityEngine;

namespace TXT.WEAVR.Player
{

    public class PlayerInitializer : MonoBehaviour, IDataProvider
    {
        [SerializeField]
        [Tooltip("Whether to dispose the controllers when this object is destroyed or not")]
        private bool m_disposeControllers = false;
        [SerializeField]
        [Tooltip("This URL is just for test, use the settings one in production")]
        private OptionalString m_testURL = "https://app-weavrmanager-dev-001.azurewebsites.net";
        [SerializeField]
        private WeavrPlayerOptions m_playerOptions;
        [SerializeField]
        [Type(typeof(IViewManager))]
        private Component m_viewManager;
        [SerializeField]
        [Type(typeof(IModelManager))]
        private Component m_modelManager;

        [SerializeField]
        [Type(typeof(IProcedureDataSource))]
        private MonoBehaviour[] m_dataSources;
        [SerializeField]
        [Type(typeof(IAnalyticsUnit))]
        private MonoBehaviour[] m_analyticsUnits;

        public IViewManager ViewManager { get; internal set; }
        public IModelManager ModelManager { get; internal set; }
        public IDownloadManager DownloadManager { get; internal set; }
        public ICacheManager CacheManager { get; internal set; }

        #region [  CONTROLLERS  ]
        public HashSet<IController> Controllers { get; private set; }
        public IAuthenticationController AuthController { get; private set; }
        public IAnalyticsController AnalyticsController { get; private set; }
        public ICoreController CoreController { get; private set; }
        public ISettingsController SettingsController { get; private set; }
        public ILibraryController LibraryController { get; private set; }
        public IProcedureController ProcedureDetailsController { get; private set; }
        public IProcedureRunController ProcedureRunController { get; private set; }
        public IARController ARController { get; private set; }
        #endregion

        public void ClearData(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public T GetView<T>() where T : IView => ViewManager.GetView<T>();

        public T GetModel<T>() where T : IModel => ModelManager.GetModel<T>();

        public T GetController<T>() where T : IController
        {
            foreach (var controller in Controllers)
            {
                if (controller is T tController)
                {
                    return tController;
                }
            }
            return default;
        }

        public T GetPersistentData<T>(string key)
        {
            if (PlayerPrefs.GetInt(key) is T tInt)
            {
                return tInt;
            }
            else if (PlayerPrefs.GetFloat(key) is T tFloat)
            {
                return tFloat;
            }

            var stringValue = PlayerPrefs.GetString(key);
            if (stringValue is T tString)
            {
                return tString;
            }

            try
            {
                return string.IsNullOrEmpty(stringValue) ? default : JsonConvert.DeserializeObject<T>(stringValue);
            }
            catch (Exception ex)
            {
                WeavrDebug.LogException(this, ex);
            }

            return default;
        }

        public void SavePersistentData<T>(string key, T value)
        {
            switch (value)
            {
                case int v:
                    PlayerPrefs.SetInt(key, v);
                    break;
                case float v:
                    PlayerPrefs.SetFloat(key, v);
                    break;
                case string v:
                    PlayerPrefs.SetString(key, v);
                    break;
                default:
                    PlayerPrefs.SetString(key, JsonConvert.SerializeObject(value));
                    break;
            }
        }

        private void Awake()
        {
            InitializeValues();
            InitializeDownloadsAndCaches();
            InitializeDataSources();
            InitializeAnalyticsUnits();
            InitializeViews();
            InitializeModels();
            InitializeControllers();
        }

        private void InitializeDownloadsAndCaches()
        {
            DownloadManager = new DownloadManager(System.IO.Path.Combine(Application.persistentDataPath, "Downloads"));
            CacheManager = new CacheManager(System.IO.Path.Combine(Application.persistentDataPath, "Cache"))
            {
                DownloadManager = DownloadManager,
            };
        }

        private void InitializeValues()
        {
            WeavrPlayer.Options = m_playerOptions;
            if (m_testURL.enabled)
            {
                WeavrPlayer.API.BASE_URL = m_testURL;
            }
            else
            {
                WeavrPlayer.API.BASE_URL = Weavr.Settings.GetValue("WeavrServerURL", "https://app-weavrmanager-dev-001.azurewebsites.net");
            }
        }

        private void InitializeDataSources()
        {
            for (int i = 0; i < m_dataSources.Length; i++)
            {
                if(m_dataSources[i] is IDownloadClient client)
                {
                    client.DownloadManager = DownloadManager;
                }
                if(m_dataSources[i] is ICacheUser cacheUser)
                {
                    cacheUser.CacheManager = CacheManager;
                }
            }
        }

        private void InitializeAnalyticsUnits()
        {
            for (int i = 0; i < m_analyticsUnits.Length; i++)
            {
                if (m_analyticsUnits[i] is IDownloadClient client)
                {
                    client.DownloadManager = DownloadManager;
                }
                if (m_analyticsUnits[i] is ICacheUser cacheUser)
                {
                    cacheUser.CacheManager = CacheManager;
                }
            }
        }

        private void InitializeControllers()
        {
            CoreController = new CoreController(this);
            AuthController = new AuthenticationController(null, null, this);
            AnalyticsController = new AnalyticsController(this, m_analyticsUnits.Select(m => m as IAnalyticsUnit).ToArray());
            SettingsController = new SettingsController(this);
            LibraryController = new LibraryController(null, this);
            ProcedureDetailsController = new ProcedureDetailsController(this);
            ProcedureRunController = new ProcedureRunController(this);
            ARController = new ARController(this);

            foreach(var dataSource in m_dataSources)
            {
                LibraryController.AddSource(dataSource as IProcedureDataSource);
            }

            Controllers = new HashSet<IController>()
            {
                CoreController,
                AuthController,
                AnalyticsController,
                SettingsController,
                LibraryController,
                ProcedureDetailsController,
                ProcedureRunController,
                ARController,
            };

            foreach(var controller in Controllers)
            {
                if(controller is IDownloadClient client)
                {
                    client.DownloadManager = DownloadManager;
                }
                if(controller is ICacheUser cacheUser)
                {
                    cacheUser.CacheManager = CacheManager;
                }
            }
        }

        private void InitializeModels()
        {
            ModelManager = m_modelManager as IModelManager;
        }

        private void InitializeViews()
        {
            ViewManager = m_viewManager as IViewManager;
        }

        private void Start()
        {
            CoreController.Start();
        }

        private void OnDestroy()
        {
            if (m_disposeControllers)
            {
                foreach (var controller in Controllers)
                {
                    (controller as IDisposable)?.Dispose();
                }
            }
        }
    }
}
