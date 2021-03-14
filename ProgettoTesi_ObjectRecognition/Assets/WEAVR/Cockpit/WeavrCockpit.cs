using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR
{
    /// <summary>
    /// Used as module identifier for TXT.WEAVR.Cockpit
    /// </summary>
    [GlobalModule("Cockpit", "This modules provides tools and various cockpit components to map and interact in a cockpit environment", "Simulation")]
    public class WeavrCockpit : WeavrModule
    {
        public override IEnumerator ApplyData(Scene scene, Dictionary<System.Type, WeavrModule> otherModules)
        {
            yield return null;
        }

        public override void InitializeData(Scene scene)
        {

        }
    }
}