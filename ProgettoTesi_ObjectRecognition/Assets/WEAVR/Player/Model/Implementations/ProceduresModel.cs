using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;
using ProcedureAsset = TXT.WEAVR.Procedure.Procedure;
using TXT.WEAVR.Communication.Entities;
using TXT.WEAVR.Player.DataSources;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace TXT.WEAVR.Player.Model
{

    public class ProceduresModel : IProceduresModel
    {
        private Dictionary<Guid, IProcedureProxy> m_proxies = new Dictionary<Guid, IProcedureProxy>();
        private Dictionary<Guid, Group> m_groups = new Dictionary<Guid, Group>();
        private Dictionary<Guid, ProcedureStatistics> m_statistics;
        private string m_statisticsPath;

        public ProcedureAsset RunningProcedure { get; set; }
        public IEnumerable<ProcedureStatistics> AllStatistics { get; set; }

        public event OnModelChangedDelegate OnChanged;

        public ProceduresModel(string statisticsFolder)
        {
            m_statisticsPath = Path.Combine(statisticsFolder, "procedures.stats");
        }

        private ProceduresModel()
        {

        }

        public void AddGroups(IEnumerable<Group> groups)
        {
            foreach(var group in groups)
            {
                m_groups[group.Id] = group;
            }
        }

        public void AddProcedures(IEnumerable<IProcedureProxy> procedures)
        {
            foreach(var proxy in procedures)
            {
                m_proxies[proxy.Id] = proxy;
            }
        }

        public void Clear()
        {
            m_proxies.Clear();
            m_groups.Clear();
        }

        public IModel Clone() => new ProceduresModel()
        {
            m_statisticsPath = m_statisticsPath,
            m_groups = new Dictionary<Guid, Group>(m_groups),
            m_proxies = new Dictionary<Guid, IProcedureProxy>(m_proxies),
            m_statistics = m_statistics != null ? new Dictionary<Guid, ProcedureStatistics>(m_statistics) 
                                                : new Dictionary<Guid, ProcedureStatistics>(),
        };

        public Group GetGroup(Guid groupId)
        {
            if(m_groups.TryGetValue(groupId, out Group group))
            {
                return group;
            }
            return null;
        }

        public async Task<ProcedureAsset> GetProcedureAsset(Guid id)
        {
            if(m_proxies.TryGetValue(id, out IProcedureProxy proxy))
            {
                return await proxy.GetAsset();
            }
            return null;
        }

        public async Task<ProcedureEntity> GetProcedureEntity(Guid id)
        {
            if (m_proxies.TryGetValue(id, out IProcedureProxy proxy))
            {
                return await proxy.GetEntity();
            }
            return null;
        }

        public IEnumerable<IProcedureProxy> GetProceduresByGroupIds(params Guid[] groupIds)
        {
            return m_proxies.Values.Where(p => p.GetAssignedGroupsIds().Any(g => groupIds.Contains(g)));
        }

        public IProcedureProxy GetProxy(Guid id)
        {
            if(m_proxies.TryGetValue(id, out IProcedureProxy proxy))
            {
                return proxy;
            }
            return null;
        }

        public ProcedureStatistics GetStatistics(Guid procedureVersionId)
        {
            ValidateStatistics();
            if(m_statistics.TryGetValue(procedureVersionId, out ProcedureStatistics statistics))
            {
                return statistics;
            }
            statistics = new ProcedureStatistics(procedureVersionId);
            m_statistics[procedureVersionId] = statistics;
            return statistics;
        }

        private void ValidateStatistics()
        {
            if (m_statistics == null)
            {
                try
                {
                    if (File.Exists(m_statisticsPath))
                    {
                        m_statistics = JsonConvert.DeserializeObject<ProcedureStatistics[]>(File.ReadAllText(m_statisticsPath)).ToDictionary(p => p.Id);
                    }
                    else
                    {
                        m_statistics = new Dictionary<Guid, ProcedureStatistics>();
                    }
                }
                catch (Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                    m_statistics = new Dictionary<Guid, ProcedureStatistics>();
                }
            }
        }

        public void RemoveGroups(IEnumerable<Group> groups)
        {
            foreach (var group in groups)
            {
                m_groups.Remove(group.Id);
            }
        }

        public void RemoveProcedures(IEnumerable<IProcedureProxy> procedures)
        {
            foreach (var proxy in procedures)
            {
                m_proxies.Remove(proxy.Id);
            }
        }

        public void SaveStatistics()
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(m_statisticsPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(m_statisticsPath));
                }
                File.WriteAllText(m_statisticsPath, JsonConvert.SerializeObject(m_statistics.Values.ToArray()));
            }
            catch(Exception ex)
            {
                WeavrDebug.LogException(this, ex);
            }
        }
    }
}
