using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TXT.WEAVR.Procedure;

using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;
using ProcedureAsset = TXT.WEAVR.Procedure.Procedure;
using TXT.WEAVR.Communication.Entities;
using UnityEngine;
using TXT.WEAVR.Core;

namespace TXT.WEAVR.Player.DataSources
{
    public class BuiltinProcedures : MonoBehaviour, IProcedureDataSource, IProcedureProvider
    {
        private static Dictionary<string, Guid> s_guids = new Dictionary<string, Guid>();

        [SerializeField]
        [Draggable]
        private List<ProcedureAsset> m_commonProcedures;

        [Space]
        [SerializeField]
        private List<ProceduresByGroup> m_proceduresByGroups;

        private Dictionary<Guid, ProcedureProxy> m_proxies = new Dictionary<Guid, ProcedureProxy>();
        private Dictionary<Guid, Group> m_groups = new Dictionary<Guid, Group>();
        private Dictionary<ProcedureAsset, ProcedureEntity> m_conversions = new Dictionary<ProcedureAsset, ProcedureEntity>();
        private ProcedureHierarchy m_hierarchy;

        public List<ProcedureAsset> CommonProcedures => m_commonProcedures;

        public bool IsAvailable => isActiveAndEnabled;

        private ProceduresVisibilityJSON m_visibilities;

        private static Guid GetId(string s)
        {
            if (!s_guids.TryGetValue(s, out Guid id))
            {
                id = Guid.NewGuid();
                s_guids[s] = id;
            }
            return id;
        }

        public IEnumerable<ProcedureEntity> GetProceduresForGroup(string groupId) => m_proceduresByGroups.FirstOrDefault(p => p.groupName == groupId)?.procedures.Select(p => ConvertToEntity(p));

        public IEnumerable<ProcedureAsset> GetAllProcedureAssets()
        {
            List<ProcedureAsset> procedures = new List<ProcedureAsset>(m_commonProcedures);
            procedures.AddRange(m_proceduresByGroups.SelectMany(p => p.procedures).Where(p => p));
            return procedures.Distinct();
        }

        private void Awake()
        {
            SanitizeProcedureCollections();
        }

        private void SanitizeProcedureCollections()
        {
            if (m_commonProcedures?.Count > 0)
            {
                int j = 0;
                for (int i = 0; i < m_commonProcedures.Count; i++)
                {
                    if (!m_commonProcedures[i])
                    {
                        WeavrDebug.LogWarning(this, $"Unable to retrieve procedure in CommonProcedures at {i + j}");
                        m_commonProcedures.RemoveAt(i--);
                        j++;
                    }
                }
            }
            if (m_proceduresByGroups?.Count > 0 && m_proceduresByGroups.Any(g => g.procedures.Any(p => !p)))
            {
                WeavrDebug.LogWarning(this, $"Unable to retrieve some procedures in groups");
                foreach (var group in m_proceduresByGroups)
                {
                    group.procedures = group.procedures.Where(p => p).ToArray();
                }
            }
        }

        private void OnEnable()
        {
            ProcedureAsset.RegisterProvider(this);
        }

        private void OnDisable()
        {
            ProcedureAsset.UnregisterProvider(this);
        }

        private void Start()
        {
            if (!Weavr.TryGetConfig("persistent_procedures.json", out m_visibilities))
            {
                m_visibilities = new ProceduresVisibilityJSON();
            }
            m_visibilities.RefreshProcedures(GetAllProcedureAssets());
            Weavr.WriteToConfigFile("persistent_procedures.json", JsonUtility.ToJson(m_visibilities, true));
        }

        public ProcedureEntity ConvertToEntity(ProcedureAsset procedure)
        {
            if (!procedure) { return null; }

            if (!m_conversions.TryGetValue(procedure, out ProcedureEntity entity))
            {
                var updateTime = procedure.LastUpdate;
                entity = new ProcedureEntity()
                {
                    Id = new Guid(procedure.Guid),
                    UnityId = new Guid(procedure.Guid),
                    Configuration = procedure.Configuration.ShortName,
                    Name = procedure.ProcedureName,
                    Description = procedure.Description,
                    UpdatedAt = updateTime,
                    Scene = new Scene()
                    {
                        Name = procedure.SceneName,
                    },
                    ProcedureVersions = new ProcedureVersion[]
                    {
                        new ProcedureVersion()
                        {
                            Description = procedure.Description,
                            Version = 1.ToString(),
                            UpdatedAt = updateTime,
                            DefaultLanguage = procedure.LocalizationTable.DefaultLanguage.TwoLettersISOName,
                            AvailableLanguages = procedure.LocalizationTable.Languages.Select(l => l.TwoLettersISOName),
                            ExecutionModes = procedure.ExecutionModes.Select(e => e.ModeName).ToArray(),
                            ProcedureVersionPlatforms = new List<ProcedureVersionPlatform>()
                            {
                                new ProcedureVersionPlatform()
                                {
                                    Collaboration = false,
                                    CreatedAt = updateTime,
                                    UpdatedAt = updateTime,
                                    BuildTarget = WeavrPlayer.PLATFORM,
                                    PlatformPlayer = WeavrPlayer.PLATFORM,
                                }
                            },
                            ProcedureVersionSteps = procedure.ProcedureSteps.Select(s => new ProcedureVersionStep()
                            {
                                Id = new Guid(s.StepGUID),
                                Index = int.TryParse(s.Number, out int index) ? index : 0,
                                Description = s.Description,
                                Name = s.Title,
                            }),
                        }
                    },
                    ProcedureSteps = procedure.ProcedureSteps.Select(s => new ProcedureStep()
                    {
                        UnityId = new Guid(s.StepGUID),
                        ProcedureVersionSteps = new ProcedureVersionStep[]
                        {
                            new ProcedureVersionStep()
                            {
                                Index = int.TryParse(s.Number, out int index) ? index : 0,
                                Description = s.Description,
                                Name = s.Title,
                            }
                        }
                    }),


                    //Status = ProcedureStatus.Persistent,
                };

                m_conversions[procedure] = entity;
            }

            return entity;
        }

        public ProcedureAsset GetProcedureAsset(ProcedureEntity entity)
        {
            var procedure = m_commonProcedures.FirstOrDefault(p => p && p.Guid == entity.UnityId.ToString());
            if (!procedure)
            {
                procedure = m_proceduresByGroups.SelectMany(p => p.procedures).FirstOrDefault(p => p && p.Guid == entity.UnityId.ToString());
            }
            return procedure;
        }

        public bool TryGetProcedure(string procedureGuid, out ProcedureAsset procedure)
        {
            procedure = m_commonProcedures.FirstOrDefault(p => p.Guid == procedureGuid);
            if (!procedure)
            {
                procedure = m_proceduresByGroups.SelectMany(p => p.procedures).FirstOrDefault(p => p.Guid == procedureGuid);
            }
            return procedure;
        }


        [Serializable]
        private class ProceduresByGroup
        {
            public string groupName;
            public string groupDescription;
            [Draggable]
            public ProcedureAsset[] procedures;
        }

        [Serializable]
        private class ProcedureVisibility
        {
            public string guid;
            public string name;
            public bool visible;
        }

        [Serializable]
        private class ProceduresVisibilityJSON
        {
            public List<ProcedureVisibility> procedures = new List<ProcedureVisibility>();

            public bool IsProcedureVisible(ProcedureAsset procedure) => procedures.Any(p => p.name == procedure.ProcedureName && p.guid == procedure.Guid && p.visible);
            public bool IsProcedureVisible(ProcedureEntity procedure) => procedures.Any(p => p.name == procedure.Name && p.guid == procedure.UnityId.ToString() && p.visible);

            public void RefreshProcedures(IEnumerable<ProcedureAsset> proceduresToRefresh)
            {
                foreach (var procedure in proceduresToRefresh)
                {
                    if (procedure && !procedures.Any(v => v.guid == procedure.Guid && v.name == procedure.ProcedureName))
                    {
                        procedures.Add(new ProcedureVisibility()
                        {
                            name = procedure.ProcedureName,
                            guid = procedure.Guid,
                            visible = true,
                        });
                    }
                }
            }
        }

        #region [  IProcedureDataSource Implementation  ]

        public Task<IProcedureProxy> GetProcedureById(Guid procedureId)
        {
            if (!WeavrPlayer.Options.BuiltinProcedures) { return Task.FromResult<IProcedureProxy>(default); }

            if (!m_proxies.TryGetValue(procedureId, out ProcedureProxy proxy))
            {
                if (TryGetProcedure(procedureId.ToString(), out ProcedureAsset asset))
                {
                    proxy = new ProcedureProxy(this)
                    {
                        Asset = asset,
                        Id = procedureId,
                        Status = ProcedureFlags.Sync | ProcedureFlags.Ready,
                    };
                }
                m_proxies[procedureId] = proxy;
            }
            return Task.FromResult(proxy as IProcedureProxy);
        }

        public Task<IHierarchyProxy> GetProceduresHierarchy(Guid userId, params Guid[] groupsIds)
        {
            if (!WeavrPlayer.Options.BuiltinProcedures) { return Task.FromResult<IHierarchyProxy>(default); }

            return Task.FromResult(new HierarchyProxy(this, GetHierarchy(), CreateProxyFromEntity, RetrieveGroup) as IHierarchyProxy);
        }

        private ProcedureHierarchy GetHierarchy()
        {
            if (m_hierarchy == null)
            {
                m_hierarchy = new ProcedureHierarchy()
                {
                    Procedures = m_commonProcedures.Where(p => m_visibilities.IsProcedureVisible(p)).Select(p => ConvertToEntity(p)),
                    Groups = m_proceduresByGroups.Select(g => new ProcedureGroup()
                    {
                        Id = Guid.NewGuid(),
                        Name = g.groupName,
                        Procedures = g.procedures.Where(p => m_visibilities.IsProcedureVisible(p)).Select(p => ConvertToEntity(p)),
                    })
                };
            }
            return m_hierarchy;
        }

        private Task<Group> RetrieveGroup(Guid groupId)
        {
            if (!m_groups.TryGetValue(groupId, out Group group))
            {
                var hierarchyGroup = GetHierarchy().Groups.FirstOrDefault(g => g.Id == groupId);
                if (hierarchyGroup != null)
                {
                    group = new Group()
                    {
                        Id = groupId,
                        Name = hierarchyGroup.Name,
                        Description = string.Empty
                    };

                    m_groups[groupId] = group;
                }
            }
            return Task.FromResult(group);
        }

        private IProcedureProxy CreateProxyFromEntity(ProcedureEntity entity)
        {
            return new ProcedureProxy(this)
            {
                Id = entity.Id,
                Asset = GetProcedureAsset(entity),
                Entity = entity,
                Status = ProcedureFlags.Sync | ProcedureFlags.Ready,
            };
        }

        public async Task<ISceneProxy> GetScene(Guid sceneId)
        {
            if (!WeavrPlayer.Options.BuiltinProcedures) { return default; }

            foreach (var proxy in m_proxies.Values)
            {
                var sceneProxy = await proxy.GetSceneProxy();
                if (sceneProxy.Id == sceneId)
                {
                    return sceneProxy;
                }
            }
            return null;
        }

        public void CleanUp() => Clear();

        public void Clear()
        {
            m_proxies.Clear();
        }

        private class ProcedureProxy : IProcedureProxy
        {
            HashSet<Guid> m_groupsIds = new HashSet<Guid>();
            BuiltinProcedures m_source;
            ProcedureFlags m_status;

            public event OnValueChanged<ProcedureFlags> StatusChanged;

            public Guid Id { get; set; }

            public IProcedureDataSource Source => m_source;

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

            public ProcedureAsset Asset { get; set; }
            public ProcedureEntity Entity { get; set; }
            public SceneProxy SceneProxy { get; set; }

            public ProcedureProxy(BuiltinProcedures source)
            {
                m_source = source;
            }

            public void AssignGroup(Guid groupId)
            {
                m_groupsIds.Add(groupId);
            }

            public Task<ProcedureAsset> GetAsset() => Task.FromResult(Asset);

            public IEnumerable<Guid> GetAssignedGroupsIds() => m_groupsIds;

            public Task<ProcedureEntity> GetEntity()
            {
                if (Entity == null)
                {
                    Entity = m_source.ConvertToEntity(Asset);
                }
                return Task.FromResult(Entity);
            }

            public Task<ISceneProxy> GetSceneProxy()
            {
                if (SceneProxy == null)
                {
                    SceneProxy = new SceneProxy(Source, new Scene()
                    {
                        Id = GetId(Asset.ScenePath),
                        Name = Asset.ScenePath,
                    });
                }
                return Task.FromResult(SceneProxy as ISceneProxy);
            }

            public Task Sync(Action<float> progressUpdate = null)
            {
                return null;
            }

            public Task<Texture2D> GetPreviewImage() => Task.FromResult(Asset.Media.previewImage);

            public void Refresh()
            {

            }

            public Task<IEnumerable<ISceneProxy>> GetAdditiveScenesProxies()
            {
                return Task.FromResult(Asset.Environment.additiveScenes.Select(s => new SceneProxy(Source, new Scene()
                {
                    Id = GetId(s),
                    Name = s,
                }) as ISceneProxy));
            }

            public Task<bool> Delete()
            {
                return Task.FromResult(false);
            }
        }

        #endregion
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(BuiltinProcedures))]
    class BuiltinProceduresEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            using (new UnityEditor.EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(" Register Scenes "))
                {
                    var scenes = UnityEditor.EditorBuildSettings.scenes.ToList();
                    var procedureScenes = (target as BuiltinProcedures).GetAllProcedureAssets()
                                          .SelectMany(p => new string[] { p.ScenePath }.Concat(p.Environment.additiveScenes))
                                          .Distinct();

                    var addedCount = procedureScenes.Except(scenes.Select(s => s.path)).Count();

                    foreach (var s in procedureScenes)
                    {
                        if (!scenes.Any(sc => sc.path == s))
                        {
                            scenes.Add(new UnityEditor.EditorBuildSettingsScene(s, true));
                        }
                    }
                    UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();

                    UnityEditor.EditorWindow.focusedWindow.ShowNotification(new GUIContent($"Added {addedCount} new scenes to Build Settings"));
                }
            }
        }
    }
#endif
}
