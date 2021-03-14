#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
namespace TXT.WEAVR.Common
{
    using UnityEngine;

    public class TextInput : MonoBehaviour
    {
        public string TextValue {
            get { return textComponent.text; }
            set { textComponent.text = value; }
        }

        public virtual void Clear()
        {
            TextValue = "";
        }

        public UnityEngine.UI.Text textComponent;

        public void OnValidate()
        {
            if (textComponent == null)
            {
                throw new MissingComponentException("Text component is missing for this component to operate!");
            }
        }

        // Used for inheritance objects initialization
        protected virtual void Initialize() { }

        // Use this for initialization
        void Start()
        {
            if (textComponent == null)
            {
                textComponent = GetComponentInChildren<UnityEngine.UI.Text>();
            }
            Initialize();
        }
    }
}
#endif
