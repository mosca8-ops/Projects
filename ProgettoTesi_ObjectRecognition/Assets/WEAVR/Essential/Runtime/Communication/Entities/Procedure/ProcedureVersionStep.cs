using System;

namespace TXT.WEAVR.Communication.Entities
{
    public class ProcedureVersionStep : BaseContent
    {
        public int Index { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        // O2M
        public Guid ProcedureStepId { get; set; }

        // O2M
        public Guid ProcedureVersionId { get; set; }


        // ---------------
    }
}
