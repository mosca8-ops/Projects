namespace TXT.WEAVR.Player.View
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [AddComponentMenu("")]
    public class AdvancedLoaderPanel : MonoBehaviour, IWeavrSingleton
    {
        #region Static Part

        private static AdvancedLoaderPanel _instance = null;

        public static AdvancedLoaderPanel Instance
        {
            get
            {
                if (_instance == null)
                {
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        var scene = SceneManager.GetSceneAt(i);
                        var obj = Weavr.TryGetInScene<AdvancedLoaderPanel>(scene);
                        if (obj != null)
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
