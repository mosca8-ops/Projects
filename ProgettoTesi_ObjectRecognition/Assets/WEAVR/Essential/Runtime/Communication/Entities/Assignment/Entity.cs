using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    public class Entity : BaseEntity
    {
        public Guid EntityId { get; set; }

        public EntityType Type { get; set; }

        public string Name { get; set; }

        public IEnumerable<EntityProcedure> EntitiesProcedures { get; set; }
    }

    public enum EntityType
    {
        USER,
        GROUP
    }
}
