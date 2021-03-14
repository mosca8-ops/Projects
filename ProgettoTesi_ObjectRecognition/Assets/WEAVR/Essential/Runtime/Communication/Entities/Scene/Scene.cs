using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Communication.Entities
{
    public class Scene : BaseContent
    {

        public Guid UnityId { get; set; }

        public string Name { get; set; }

        // O2M
        public IEnumerable<SceneVersion> SceneVersions { get; set; }
        // O2M
        public IEnumerable<SceneVersionPlatform> SceneVersionPlatforms { get; set; }


        // ---------------

    }
}
