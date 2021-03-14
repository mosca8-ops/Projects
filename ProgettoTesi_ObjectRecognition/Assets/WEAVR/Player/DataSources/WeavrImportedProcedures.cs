using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TXT.WEAVR.Procedure;

using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;
using ProcedureAsset = TXT.WEAVR.Procedure.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Player.DataSources
{
    public class WeavrImportedProcedures : MonoBehaviour, IProcedureDataSource, IProcedureProvider
    {
        public bool IsAvailable => isActiveAndEnabled;


        #region [  IProcedureDataSource Implementation  ]

        public Task<IProcedureProxy> GetProcedureById(Guid procedureId)
        {
            throw new NotImplementedException();
        }

        public Task<IHierarchyProxy> GetProceduresHierarchy(Guid userId, params Guid[] groupsIds)
        {
            throw new NotImplementedException();
        }

        public Task<ISceneProxy> GetScene(Guid sceneId)
        {
            throw new NotImplementedException();
        }

        public void CleanUp()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool TryGetProcedure(string procedureGuid, out ProcedureAsset procedure)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
