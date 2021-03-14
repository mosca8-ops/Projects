using UnityEngine;

namespace TXT.WEAVR.Common
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class PooledObject : MonoBehaviour
    {
        public IPool Pool { get; set; }
    }
}
