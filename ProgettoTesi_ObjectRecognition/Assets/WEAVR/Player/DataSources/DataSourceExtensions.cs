using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TXT.WEAVR.Communication.Entities;

using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;
using ProcedureAsset = TXT.WEAVR.Procedure.Procedure;

namespace TXT.WEAVR.Player.DataSources
{
    public static partial class DataSourceExtensions
    {
        public static IEnumerable<IProcedureProxy> GetAllProcedureProxies(this IHierarchyProxy hierarchy)
        {
            HashSet<IProcedureProxy> proxies = new HashSet<IProcedureProxy>(hierarchy.UserProcedures);
            foreach(var group in hierarchy.Groups)
            {
                foreach(var proxy in group.Procedures)
                {
                    proxies.Add(proxy);
                }
            }
            return proxies;
        }
    }
}
