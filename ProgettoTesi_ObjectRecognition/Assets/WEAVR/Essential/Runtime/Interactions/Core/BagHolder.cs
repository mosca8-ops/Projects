namespace TXT.WEAVR.Interaction
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Holds an object bag, this class is a MonoBehaviour wrapper of the <see cref="ObjectsBag"/>
    /// </summary>
    [AddComponentMenu("")]
    public class BagHolder : MonoBehaviour
    {
        #region [  STATIC PART  ]
        private static BagHolder _main;
        /// <summary>
        /// Gets the first instantiated or found bag holder
        /// </summary>
        public static BagHolder Main {
            get {
                if(_main == null) {
                    _main = FindObjectOfType<BagHolder>();
                    if(_main == null) {
                        // Create a default one
                        var go = new GameObject("MaintenanceBag");
                        _main = go.AddComponent<BagHolder>();
                        _main.Awake();
                    }
                }
                return _main;
            }
        }

        #endregion

        public ObjectsBag Bag { get; private set; }

        // Use this for initialization
        void Awake() {
            if(_main == null) {
                _main = this;
            }
            Bag = new ObjectsBag();
        }
    }
}
