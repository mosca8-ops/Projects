namespace TXT.WEAVR.Player.View

{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [AddComponentMenu("WEAVR/UI/Obscure Panel")]
    public class ObscurationPanel : MonoBehaviour, IWeavrSingleton
    {
        #region Static Part

        private static ObscurationPanel _instance = null;

        public static ObscurationPanel Instance
        {
            get
            {
                if (_instance == null)
                {
                    for(int i=0; i<SceneManager.sceneCount; i++)
                    {
                        var scene = SceneManager.GetSceneAt(i);
                        var obj = Weavr.TryGetInScene<ObscurationPanel>(scene);
                        if(obj != null)
                        {
                            _instance = obj;
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion
    }
}
