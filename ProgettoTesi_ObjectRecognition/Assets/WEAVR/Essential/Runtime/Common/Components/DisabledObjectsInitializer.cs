using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Setup/Disabled Objects Initializer")]
    public class DisabledObjectsInitializer : MonoBehaviour, IWeavrSingleton
    {
        [SerializeField]
        [Type(typeof(IExecuteDisabled))]
        private Behaviour[] m_objectsToInitialize;

        public void FindInScene()
        {
            m_objectsToInitialize = SceneTools.GetComponentsInScene<IExecuteDisabled>().Select(o => o as Behaviour).ToArray();
        }

        private void Start()
        {
            foreach(var obj in m_objectsToInitialize)
            {
                if (obj && obj.enabled && !obj.gameObject.activeInHierarchy)
                {
                    (obj as IExecuteDisabled)?.InitDisabled();
                }
            }
        }
    }
}
