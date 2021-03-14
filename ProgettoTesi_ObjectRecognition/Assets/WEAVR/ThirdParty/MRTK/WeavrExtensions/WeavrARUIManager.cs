#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// This script follow the camera, and manage the menus inside _UIManager
/// </summary>
namespace TXT.WEAVR
{
    public class WeavrARUIManager : MonoBehaviour
    {
        public List<GameObject> UIMenu;
        public Camera cameraToFollow;

        [Range(0.0f, 5.0f)]
        public float followSpeed = 1.8f;

        private GameObject prevUIelem = null;
        private GameObject mainCanvas;
        public bool followCamera;

        // Use this for initialization
        void Start()
        {
            // hide all menu
            if (UIMenu != null)
            {
                mainCanvas = UIMenu[0].transform.parent.gameObject;
                mainCanvas.SetActive(false);

                foreach (GameObject uiElem in UIMenu)
                {
                    uiElem.SetActive(false);
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (cameraToFollow != null)
            {
                // calculate delta
                float delta = Vector3.Distance(transform.position, cameraToFollow.transform.position);
                transform.position = cameraToFollow.transform.position;

                if (followCamera)//&& delta > 0.5)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, cameraToFollow.transform.rotation, followSpeed * Time.deltaTime);
                }
            }
        }

        public void ShowMenuByName(string name)
        {
            bool flagFind = false;

            foreach (GameObject uiElem in UIMenu)
            {
                if (uiElem.name == name)
                {
                    showMenu(uiElem);
                    flagFind = true;
                    break;
                }
            }

            if (!flagFind)
            {
                Debug.Log("menu not found " + name);
            }

        }

        public void CloseMenuByName(string name)
        {
            bool flagFind = false;

            foreach (GameObject uiElem in UIMenu)
            {
                if (uiElem.name == name)
                {
                    hideMenu(uiElem);
                    flagFind = true;
                    break;
                }
            }

            if (!flagFind)
            {
                Debug.Log("menu not found " + name);
            }
        }

        public void showMenu(GameObject obj)
        {
            foreach (GameObject uiElem in UIMenu)
            {
                if (uiElem == obj)
                {
                    mainCanvas.SetActive(true);
                    obj.SetActive(true);
                    break;
                }
            }
        }

        public void hideMenu(GameObject obj)
        {
            foreach (GameObject uiElem in UIMenu)
            {
                if (uiElem == obj)
                {
                    obj.SetActive(false);
                    mainCanvas.SetActive(false);
                    break;
                }
            }
        }

    }
}
#endif
