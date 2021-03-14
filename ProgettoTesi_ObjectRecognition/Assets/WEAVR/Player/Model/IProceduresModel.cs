using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Communication.Entities;
using UnityEngine;

using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;
using ProcedureAsset = TXT.WEAVR.Procedure.Procedure;
using TXT.WEAVR.Player.DataSources;
using System.Threading.Tasks;

namespace TXT.WEAVR.Player.Model
{
    public interface IProceduresModel : IModel
    {
        ProcedureAsset RunningProcedure { get; set; }
        Task<ProcedureEntity> GetProcedureEntity(Guid id);
        Task<ProcedureAsset> GetProcedureAsset(Guid id);
        IProcedureProxy GetProxy(Guid id);

        IEnumerable<IProcedureProxy> GetProceduresByGroupIds(params Guid[] groupIds);
        Group GetGroup(Guid groupId);

        void Clear();
        void AddProcedures(IEnumerable<IProcedureProxy> procedures);
        void RemoveProcedures(IEnumerable<IProcedureProxy> procedures);
        void AddGroups(IEnumerable<Group> groups);
        void RemoveGroups(IEnumerable<Group> groups);

        ProcedureStatistics GetStatistics(Guid procedureId);
        void SaveStatistics();
    }
}
