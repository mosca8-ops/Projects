using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    public class ProcedureVersion : BaseContent
    {
        public string Version { get; set; }
        public string Description { get; set; }
        public string EditorVersion { get; set; }
        public IEnumerable<string> ExecutionModes { get; set; }
        public IEnumerable<string> AvailableLanguages { get; set; }
        public string DefaultLanguage { get; set; }

        // O2M
        public Guid ProcedureId { get; set; }

        // O2M
        public Guid SceneVersionId { get; set; }
        public SceneVersion SceneVersion { get; set; }

        // M2O
        public IEnumerable<ProcedureVersionPlatform> ProcedureVersionPlatforms { get; set; }

        // M2O
        public IEnumerable<ProcedureVersionStep> ProcedureVersionSteps { get; set; }


        // ---------------
    }
}
