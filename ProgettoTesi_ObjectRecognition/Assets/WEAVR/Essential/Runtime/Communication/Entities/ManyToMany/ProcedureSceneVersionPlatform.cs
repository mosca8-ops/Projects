using System;

namespace TXT.WEAVR.Communication.Entities
{
    public class ProcedureSceneVersionPlatform : BaseEntity
    {
        public Guid ProcedureVersionPlatformId { get; set; }
        public ProcedureVersionPlatform ProcedureVersionPlatform { get; set; }

        public Guid SceneVersionPlatformId { get; set; }
        public SceneVersionPlatform SceneVersionPlatform { get; set; }
    }
}
