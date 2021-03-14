using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    public class ProcedureStep : BaseContent
    {

        public Guid UnityId { get; set; }

        // O2M
        public Guid ProcedureId { get; set; }

        // M2O
        public IEnumerable<ProcedureVersionStep> ProcedureVersionSteps { get; set; }

        // ---------------
    }
}
