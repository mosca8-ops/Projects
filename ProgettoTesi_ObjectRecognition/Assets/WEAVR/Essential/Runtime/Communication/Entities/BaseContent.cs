using System;

namespace TXT.WEAVR.Communication.Entities
{ 
    public class BaseContent : BaseEntity
    {
        public Guid ProcedureTokenId { get; set; }

        public ProcedureStatus Status { get; set; }
    }

    public enum ProcedureStatus
    {
        ACTIVE,
        TEMPORARY,
    }
}
