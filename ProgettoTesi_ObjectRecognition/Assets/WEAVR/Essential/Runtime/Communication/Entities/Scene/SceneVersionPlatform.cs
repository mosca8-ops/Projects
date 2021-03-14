using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    public class SceneVersionPlatform : BaseContent
    {

        public Guid SceneVersionPlatformFileId { get; set; }
        public SceneVersionPlatformFile SceneVersionPlatformFile { get; set; }

        public string BuildTarget { get; set; }

        public string Platform { get; set; }
        public string PlatformPlayer { get; set; }

        public IEnumerable<string> Providers { get; set; }

        // O2M
        public Guid SceneId { get; set; }

        // O2M
        public Guid SceneVersionId { get; set; }

        // ---------------
    }
}
