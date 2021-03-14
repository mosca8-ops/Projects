using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    public class SceneVersion : BaseContent
    {
        public string Version { get; set; }

        // O2M
        public Guid SceneId { get; set; }

        // M2O
        public IEnumerable<SceneVersionPlatform> SceneVersionPlatforms { get; set; }

        // ---------------
    }
}
