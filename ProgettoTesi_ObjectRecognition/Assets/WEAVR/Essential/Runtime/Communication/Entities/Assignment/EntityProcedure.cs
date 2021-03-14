using System;

namespace TXT.WEAVR.Communication.Entities
{
    public class EntityProcedure : BaseEntity
    {
        public Guid ProcedureId { get; set; }
        public Procedure Procedure { get; set; }

        public Guid EntityId { get; set; }
        public Entity Entity { get; set; }
    }
}
