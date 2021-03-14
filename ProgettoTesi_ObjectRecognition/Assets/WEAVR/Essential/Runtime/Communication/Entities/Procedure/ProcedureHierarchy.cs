
using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    [Serializable]
    public class ProcedureHierarchy
    {
        public IEnumerable<ProcedureGroup> Groups { get; set; }

        public IEnumerable<Procedure> Procedures { get; set; }
    }
}
