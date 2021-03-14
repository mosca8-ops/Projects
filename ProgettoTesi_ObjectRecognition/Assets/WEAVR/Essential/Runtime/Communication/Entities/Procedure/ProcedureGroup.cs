
using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    [Serializable]
    public class ProcedureGroup
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public IEnumerable<Procedure> Procedures { get; set; }
    }
}
