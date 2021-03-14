#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR
{
    public class WeavrARGestureHandler : MonoBehaviour, IInputClickHandler
    {
        public bool debug = false;
        public WeavrARUIManager UIManager;

        [Tooltip("Set an internal var fot switch beahavious on/off")]
        public GameObject UiPanelInfo = null;

        [Tooltip("Set an internal var fot switch beahavious on/off")]
        public GameObject UiToActivate = null;

        [Tooltip("Set an internal var fot switch beahavious on/off")]
        public bool setInternalStateSwitch = false;

        [Tooltip("Add Box Collider at runtime")]
        public bool addBoxCollider = false;

        private bool isActive = false;

        private void Start()
        {
            // can't use on hololens
#if UNITY_EDITOR
            // this.prefabName = UnityEditor.PrefabUtility.GetPrefabParent(gameObject).name;
#endif
            if (addBoxCollider)
            {
                //BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                //MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                //meshCollider.convex = true;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (debug && isActive)
            {
                this.transform.Rotate(0, 1, 0);
            }
        }

        /// <summary>
        /// When the user clicked or tapped on this game object
        /// </summary>
        void OnAirTapped()
        {
            if (UiToActivate != null)
            {
                UIManager.showMenu(UiToActivate);
            }

            if (UiPanelInfo != null)
            {
                // Get the description from the prefab description
                string desc = GetComponent<WeavrPrefabDescription>().description;
                UiPanelInfo.GetComponentInChildren<Text>().text = desc; // show the description

                UIManager.showMenu(UiPanelInfo); // show panel info
            }
        }

        public void Action()
        {
            if (setInternalStateSwitch)
            {
                isActive = !isActive;
                gameObject.GetComponent<Animator>().SetTrigger("ChangeState");
            }
            this.OnAirTapped();
        }

        public void OnInputClicked(InputClickedEventData eventData)
        {
            //this.Action();
        }
    }

}
#endif
