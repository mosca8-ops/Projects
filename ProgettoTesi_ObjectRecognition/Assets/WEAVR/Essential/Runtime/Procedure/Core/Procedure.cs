using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    //[CreateAssetMenu(fileName = "Procedure", menuName = "WEAVR/Procedure")]
    [DoNotClone]
    public class Procedure : ProcedureObject
    {
        public static Func<Procedure, IEnumerable<ProcedureObject>> s_AssetProcedureOjects;
        public static Func<Procedure, string, ProcedureObject> s_FindProcedureOject;
        public static Action<Procedure> s_SaveUpdateTime;

        private static HashSet<IProcedureProvider> s_procedureProviders = new HashSet<IProcedureProvider>();
        
        public static void RegisterProvider(IProcedureProvider provider)
        {
            s_procedureProviders.Add(provider);
        }
        
        public static void UnregisterProvider(IProcedureProvider provider)
        {
            s_procedureProviders.Remove(provider);
        }

        public static bool TryFind(string procedureGuid, out Procedure procedure)
        {
            foreach(var provider in s_procedureProviders)
            {
                if(provider != null && provider.TryGetProcedure(procedureGuid, out procedure))
                {
                    return true;
                }
            }
            procedure = null;
            return false;
        }

        [SerializeField]
        private string m_procedureName;
        [SerializeField]
        [LongText]
        private string m_description;
        [SerializeField]
        private ProcedureConfig m_config;
        [SerializeField]
        private NetworkConfiguration m_networkConfig;
        [SerializeField]
        private LocalizationTable m_localizationTable;
        [SerializeField]
        private CapabilityOptions m_capabilities;
        [SerializeField]
        private MediaItems m_media;
        [SerializeField]
        private EnvironmentItems m_environment;
        [SerializeField]
        private long m_lastUpdate;
        [SerializeField]
        private BaseGraph m_graph;

        [System.NonSerialized]
        private ExecutionMode m_currentExecutionMode;
        
        [System.NonSerialized]
        private Dictionary<string, ProcedureObject> m_procedureObjects = new Dictionary<string, ProcedureObject>();

#if UNITY_2019_3
        public bool RequiresReset { get; set; }
        internal void ShouldReset()
        {
            RequiresReset = true;
        }
#endif

        public string ProcedureName => m_procedureName;
        public string Description => m_description;
        public ExecutionMode DefaultExecutionMode { get => m_config.DefaultExecutionMode; set => m_config.DefaultExecutionMode = value; }
        public ExecutionMode ValidDefaultExecutionMode
        {
            get
            {
                if (!m_config.ExecutionModes.Contains(m_config.DefaultExecutionMode) && m_config.ExecutionModes.Count > 0)
                {
                    m_config.DefaultExecutionMode = m_config.ExecutionModes[0];
                }
                return m_config.DefaultExecutionMode;
            }
        }

        public ExecutionMode HintsReplayExecutionMode { get => m_config.HintsReplayExecutionMode; set => m_config.HintsReplayExecutionMode = value; }

        public ExecutionMode CurrentExecutionMode
        {
            get
            {
                if (!m_currentExecutionMode)
                {
                    m_currentExecutionMode = DefaultExecutionMode;
                }
                return m_currentExecutionMode;
            }
            set
            {
                if (m_currentExecutionMode != value && value && ExecutionModes.Contains(value))
                {
                    m_currentExecutionMode = value;
                }
            }
        }

        public CapabilityOptions Capabilities => m_capabilities;

        public IEnumerable<IProcedureStep> ProcedureSteps => Graph.Steps.Select(s => s as IProcedureStep).Concat(Graph.Nodes.Select(n => n.ProcedureStep)).Distinct();

        public MediaItems Media => m_media;

        public EnvironmentItems Environment => m_environment;

        public IExposedPropertyTable ReferencesResolver => Graph.ReferencesTable?.Resolver;

        public bool IsFullyLoaded => Graph.ReferencesTable?.IsReady ?? false;
        
        public string ScenePath => Graph.ReferencesTable?.SceneData?.Path;
        public string SceneName => Graph.ReferencesTable?.SceneData?.Name;

        public ProcedureConfig Configuration => m_config;

        public bool IsNetworkEnabled => m_networkConfig?.isNetworkEnabled == true;
        public int? MinNumberOfPlayers => m_networkConfig?.minNumberOfPlayers;
        public int? MaxNumberOfPlayers => m_networkConfig?.m_maxNumberOfPlayers;

        public LocalizationTable LocalizationTable => m_localizationTable;
        public List<ExecutionMode> ExecutionModes => m_config.ExecutionModes;
        public BaseGraph Graph => m_graph;

        public DateTime LastUpdate => new DateTime(m_lastUpdate);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_config == null)
            {
                m_config = CreateInstance<ProcedureConfig>();
            }
            if (m_graph == null)
            {
                m_graph = Create<BaseGraph>(this);
            }
            if (string.IsNullOrEmpty(Guid))
            {
                Guid = System.Guid.NewGuid().ToString();
            }
        }

        protected override void OnNotifyModified()
        {
            base.OnNotifyModified();
            m_lastUpdate = DateTime.Now.Ticks;
        }

        public void Register(ProcedureObject obj)
        {
            if (Application.isEditor)
            {
                var keysToRemove = m_procedureObjects.Where(p => p.Value == obj).Select(p => p.Key).ToArray();
                foreach (var key in keysToRemove)
                {
                    m_procedureObjects.Remove(key);
                }
                if (string.IsNullOrEmpty(obj.Guid))
                {
                    obj.ChangeGUID();
                }
                if (m_procedureObjects.TryGetValue(obj.Guid, out ProcedureObject other) && other != obj)
                {
                    do
                    {
                        obj.ChangeGUID();
                    }
                    while (m_procedureObjects.ContainsKey(obj.Guid));
                }
            }

            m_procedureObjects[obj.Guid] = obj;
        }

        public void Unregister(ProcedureObject obj)
        {
            if (Application.isEditor)
            {
                var keysToRemove = m_procedureObjects.Where(p => p.Value == obj).Select(p => p.Key).ToArray();
                foreach (var key in keysToRemove)
                {
                    m_procedureObjects.Remove(key);
                }
            }

            m_procedureObjects.Remove(obj.Guid);
        }

        public virtual void SoftReset()
        {
            if (!Application.isEditor) { return; }
            foreach(var po in m_procedureObjects.Values)
            {
                if(po is IFlowElement fe)
                {
                    fe.CurrentState = ExecutionState.NotStarted;
                }
                if(po is IFlowContext fc)
                {
                    fc.CurrentState = ContextState.Standby;
                }
            }
        }

        public ProcedureObject Find(string guid)
        {
            if(!m_procedureObjects.TryGetValue(guid, out ProcedureObject obj) || !obj)
            {
                if(s_AssetProcedureOjects != null)
                {
                    var objs = s_AssetProcedureOjects(this);
                    foreach(var procObj in objs)
                    {
                        if(procObj && !string.IsNullOrEmpty(procObj.Guid))
                        {
                            Register(procObj);
                            if(procObj.Guid == guid)
                            {
                                obj = procObj;
                            }
                        }
                    }
                }
                if(!obj && s_FindProcedureOject != null)
                {
                    obj = s_FindProcedureOject(this, guid);
                }
            }
            return obj;
        }

        public void PrepareForLaunch()
        {
            m_graph.ReferencesTable?.AutoResolve();
        }

        [Serializable]
        public class CapabilityOptions
        {
            public bool usesAR;
            public bool useStepInfo = true;
            public bool useAnalytics = true;
        }

        [Serializable]
        public class EnvironmentItems
        {
            public string[] additiveScenes;
        }

        [Serializable]
        public class MediaItems
        {
            public Texture2D previewImage;

            public MediaItems Clone() => new MediaItems()
            {
                previewImage = previewImage,
            };

            public void CopyFrom(MediaItems media)
            {
                previewImage = media?.previewImage;
            }
        }

        [System.Serializable]
        public class NetworkConfiguration
        {
            public bool isNetworkEnabled = true;
            [HiddenBy(nameof(isNetworkEnabled))]
            public OptionalInt minNumberOfPlayers = new OptionalInt()
            {
                enabled = false,
                value = 2
            };
            [HiddenBy(nameof(isNetworkEnabled))]
            public OptionalInt m_maxNumberOfPlayers = new OptionalInt()
            {
                enabled = false,
                value = 12
            };
        }
    }
}
