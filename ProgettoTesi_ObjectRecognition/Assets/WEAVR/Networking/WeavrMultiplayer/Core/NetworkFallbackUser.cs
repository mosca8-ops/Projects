using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Networking
{

    [AddComponentMenu("WEAVR/Network/Network Fallback User")]
    public class NetworkFallbackUser : MonoBehaviour
    {
        public WeavrNetwork.LocalUser user;
        public bool useAsTestUser = false;
        [HiddenBy(nameof(useAsTestUser))]
        public bool writeToJson;

        private void Reset()
        {
            user = new WeavrNetwork.LocalUser()
            {
                id = Guid.NewGuid().ToString(),
                firstName = "John",
                lastName = "Bush",
            };
        }
    }
}
