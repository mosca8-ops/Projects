using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    public class Procedure : BaseContent
    {
        public Guid UnityId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        //public string Status { get; set; }
        public ProcedureOwnerShipEnum Ownership { get; set; }

        // VT o OPS
        public string Configuration { get; set; }

        // M2O
        public Guid SceneId { get; set; }
        public Scene Scene { get; set; }

        // M2O
        public IEnumerable<ProcedureVersion> ProcedureVersions { get; set; }

        // M2O
        public IEnumerable<ProcedureVersionPlatform> ProcedureVersionPlatforms { get; set; }

        // M2O
        public IEnumerable<ProcedureStep> ProcedureSteps { get; set; }

        // M2O
        public Guid ProcedurePreviewId { get; set; }
        public ProcedureMedia ProcedurePreview { get; set; }

        public IEnumerable<EntityProcedure> EntitiesProcedures { get; set; }


        // ---------------

    }

    public enum ProcedureOwnerShipEnum
    {
        PUBLIC,
        PRIVATE
    }
}
