using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR
{

    [AddComponentMenu("WEAVR/States Logic/Stateful GameObject")]
    public class StatefulGameObject : MonoBehaviour
    {
        public bool includeChildren = true;

        private void OnEnable()
        {
            var manager = this.TryGetSingleton<StateManager>();
            if (manager)
            {
                manager.Register(this);
            }
        }

        private void OnDisable()
        {
            var manager = this.TryGetSingleton<StateManager>();
            if (manager)
            {
                manager.Unregister(this);
            }
        }
    }
}