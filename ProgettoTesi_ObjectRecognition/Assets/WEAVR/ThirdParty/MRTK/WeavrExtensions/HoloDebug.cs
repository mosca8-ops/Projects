#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.Common
{
    using UnityEngine;
    using UnityEngine.UI;

    public class HoloDebug : MonoBehaviour
    {
        public Text console;
        private static HoloDebug _instance;

        private void Start()
        {
            _instance = this;
        }

        public static void Log(object o)
        {
            if (_instance != null)
            {
                _instance.console.text += o != null ? o.ToString() + "\n" : "null\n";
            }
        }
    }
}
#endif
