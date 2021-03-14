using System.Collections.Generic;
using TXT.WEAVR.Communication.Entities;

using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;

namespace TXT.WEAVR.Communication.DTO
{
    public class UploadedProcedure
    {
        public ProcedureEntity Procedure { get; set; }
        public IEnumerable<Scene> AdditiveScenes { get; set; }
    }
}