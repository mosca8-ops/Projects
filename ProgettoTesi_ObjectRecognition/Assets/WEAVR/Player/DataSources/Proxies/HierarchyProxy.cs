using System;
using System.Collections.Generic;

using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;

using TXT.WEAVR.Communication.Entities;
using System.Threading.Tasks;
using JetBrains.Annotations;
using System.Linq;

namespace TXT.WEAVR.Player.DataSources
{
    public class HierarchyProxy : IHierarchyProxy
    {
        public IProcedureDataSource Source { get; private set; }

        public IEnumerable<IProcedureGroupProxy> Groups { get; set; }

        public IEnumerable<IProcedureProxy> UserProcedures { get; set; }

        // Temporary data
        private Dictionary<Guid, IProcedureProxy> m_proxyDictionary;
        private Func<ProcedureEntity, IProcedureProxy> m_generator;

        public HierarchyProxy(IProcedureDataSource source, 
                              [NotNull] ProcedureHierarchy hierarchy, 
                              [NotNull] Func<ProcedureEntity, IProcedureProxy> procedureConverter,
                              Func<Guid, Task<Group>> groupRetrieveFunctor)
            : this(source)
        {

            List<IProcedureGroupProxy> groups = new List<IProcedureGroupProxy>();
            List<IProcedureProxy> procedures = new List<IProcedureProxy>();

            m_proxyDictionary = new Dictionary<Guid, IProcedureProxy>();
            m_generator = procedureConverter;

            foreach(var entity in hierarchy.Procedures)
            {
                var proxy = GetProxy(entity);
                proxy.AssignGroup(Guid.Empty);
                procedures.Add(proxy);
            }

            foreach(var group in hierarchy.Groups)
            {
                var proxy = new ProcedureGroupProxy(source, group, GetProxy, groupRetrieveFunctor)
                {
                    Hierarchy = this,
                };
                groups.Add(proxy);
            }

            // Assign the values
            UserProcedures = procedures;
            Groups = groups;

            // Cleanup values
            m_generator = null;
        }

        public HierarchyProxy(IProcedureDataSource source)
        {
            Source = source;
        }

        private IProcedureProxy GetProxy(ProcedureEntity entity)
        {
            if(!m_proxyDictionary.TryGetValue(entity.Id, out IProcedureProxy proxy))
            {
                proxy = m_generator(entity);
                m_proxyDictionary[entity.Id] = proxy;
            }
            return proxy;
        }

        private IProcedureProxy GetProxy(Guid id, IProcedureProxy fallback)
        {
            if(m_proxyDictionary.TryGetValue(id, out IProcedureProxy proxy))
            {
                return proxy;
            }
            else if(fallback != null)
            {
                m_proxyDictionary[fallback.Id] = fallback;
            }
            return fallback;
        }

        public IProcedureProxy GetProxy(Guid id)
        {
            return m_proxyDictionary.TryGetValue(id, out IProcedureProxy proxy) ? proxy : default;
        }

        public void Merge(IHierarchyProxy other)
        {
            m_proxyDictionary = UserProcedures.ToDictionary(p => p.Id);
            foreach(var otherProxy in other.UserProcedures)
            {
                if(m_proxyDictionary.TryGetValue(otherProxy.Id, out IProcedureProxy existing))
                {
                    foreach(var groupId in otherProxy.GetAssignedGroupsIds())
                    {
                        existing.AssignGroup(groupId);
                        // How about other properties ???
                    }
                }
                else
                {
                    m_proxyDictionary[otherProxy.Id] = otherProxy;
                }
            }

            UserProcedures = m_proxyDictionary.Values.ToArray();

            var theseGroups = Groups.ToDictionary(g => g.Id);

            // Now Groups
            foreach(var otherGroup in other.Groups)
            {
                if(theseGroups.TryGetValue(otherGroup.Id, out IProcedureGroupProxy proxyGroup))
                {
                    proxyGroup.Merge(otherGroup);
                }
                else
                {
                    proxyGroup = new ProcedureGroupProxy(otherGroup.Source)
                    {
                        Hierarchy = this,
                        Id = otherGroup.Id,
                        Name = otherGroup.Name,
                        Procedures = otherGroup.Procedures.Select(p => GetProxy(p.Id, p)),
                        GroupRetriever = (otherGroup as ProcedureGroupProxy)?.GroupRetriever,
                        Group = (otherGroup as ProcedureGroupProxy)?.Group
                    };

                    (proxyGroup as ProcedureGroupProxy).AssignProcedures();
                    theseGroups[proxyGroup.Id] = proxyGroup;
                }
            }

            Groups = theseGroups.Values;
            m_proxyDictionary.Clear();
        }

        private class ProcedureGroupProxy : IProcedureGroupProxy
        {
            public IProcedureDataSource Source { get; private set; }

            public Guid Id { get; set; }

            public string Name { get; set; }

            public IEnumerable<IProcedureProxy> Procedures { get; set; }

            public Group Group { get; set; }

            public Func<Guid, Task<Group>> GroupRetriever { get; set; }

            public HierarchyProxy Hierarchy { get; set; }

            public ProcedureGroupProxy(IProcedureDataSource source,
                                  [NotNull] ProcedureGroup group,
                                  [NotNull] Func<ProcedureEntity, IProcedureProxy> procedureRetriever,
                                  Func<Guid, Task<Group>> groupRetrieveFunctor)
            {
                Source = source;
                GroupRetriever = groupRetrieveFunctor;

                Id = group.Id;
                Name = group.Name;

                List<IProcedureProxy> proxies = new List<IProcedureProxy>();
                foreach(var procedure in group.Procedures)
                {
                    var proxy = procedureRetriever(procedure);
                    proxy.AssignGroup(Id);
                    proxies.Add(proxy);
                }

                Procedures = proxies;
            }

            public ProcedureGroupProxy(IProcedureDataSource source)
            {
                Source = source;
            }

            public async Task<Group> GetGroup()
            {
                if (Group == null)
                {
                    Group = await GroupRetriever?.Invoke(Id);
                }
                return Group;
            }

            public void Merge(IProcedureGroupProxy otherGroup)
            {
                HashSet<IProcedureProxy> proxies = new HashSet<IProcedureProxy>(Procedures);
                foreach(var otherProxy in otherGroup.Procedures)
                {
                    var proxy = Hierarchy.GetProxy(otherProxy.Id, otherProxy);
                    proxy.AssignGroup(Id);
                    proxies.Add(proxy);
                }

                Procedures = proxies;
            }

            public void AssignProcedures()
            {
                foreach(var proxy in Procedures)
                {
                    proxy.AssignGroup(Id);
                }
            }
        }
    }
}
