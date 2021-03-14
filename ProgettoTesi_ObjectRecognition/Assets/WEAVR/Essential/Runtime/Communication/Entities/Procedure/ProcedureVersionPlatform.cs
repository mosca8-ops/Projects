using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    public class ProcedureVersionPlatform : BaseContent
    {
        // O20
        public Guid ProcedureVersionPlatformFileId { get; set; }
        public ProcedureVersionPlatformFile ProcedureVersionPlatformFile { get; set; }

        // It is the build target
        // StandaloneWindows
        // StandaloneWindows64
        // StandaloneOSX
        // StandaloneLinux64
        // Android
        // iOS
        // WebGL
        // WSAPlayer
        public string BuildTarget { get; set; }
        public string Platform { get; set; }
        public string PlatformPlayer { get; set; }
        // It is the provider target
        // Standard
        // OpenVR
        // Oculus SDK
        // Pico SDK
        // Hololens MRTK
        public IEnumerable<string> Providers { get; set; }


        //TO ADD Collaboration bool
        public bool Collaboration { get; set; }

        // O2M
        public Guid ProcedureId { get; set; }

        // O2M
        public Guid ProcedureVersionId { get; set; }

        // O2M
        public Guid SceneVersionPlatformId { get; set; }
        public SceneVersionPlatform SceneVersionPlatform { get; set; }

        // M2M
        public IEnumerable<SceneVersionPlatform> AdditiveSceneVersionPlatforms { get; set; }
        
        public IEnumerable<ProcedureSceneVersionPlatform> ProcedureSceneVersionPlatforms { get; set; } // Used only for Tables Handling

        public IEnumerable<ProcedureMedia> ProcedureMedias { get; set; }

        // ---------------
    }
}
