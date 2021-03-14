#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
using UnityEngine;

namespace TXT.WEAVR
{
    //[RequireComponent(typeof(Camera))]

    public class WeavrARTeleport : MonoBehaviour, IInputClickHandler
    {
        public bool debugConsole = false;
        public Camera HoloCamera = null;

        private GameObject parentCameraObj = null;
        private RaycastHit hit;
        private float nextClickTime = 0;
        private float intervalClick = 0.5f;

        // Use this for initialization
        void Start()
        {

            if (HoloCamera)
            {
                parentCameraObj = HoloCamera.transform.parent.gameObject;
            }
        }

        //#if UNITY_WSA_10_0
        public void OnInputClicked(InputClickedEventData eventData = null)
        {
            if (Time.time > nextClickTime)
            {
                Teleport();
                nextClickTime = Time.time + intervalClick;
            }
        }
        //#endif

        public void Teleport()
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
            if (Physics.Raycast(
                ray,
                out hit,
                1000f,
                Physics.AllLayers))
            {
                // If the object is has the layer selected the teleport is allowed
                if (hit.collider.gameObject.layer == gameObject.layer)
                {
                    // Change position of camera's parent because hololens overwrite position relative to the room
                    Vector3 v1 = new Vector3(hit.point.x, parentCameraObj.transform.position.y, hit.point.z);

                    // Test
                    v1 = new Vector3((hit.point.x + HoloCamera.transform.localPosition.x), parentCameraObj.transform.position.y, (hit.point.z + HoloCamera.transform.localPosition.z));

                    parentCameraObj.transform.position = v1;
                    // Reset local position camera

                    if (debugConsole)
                    {
                        Debug.Log("x: " + hit.point.x + ", y: " + hit.point.y + ", z: " + hit.point.z);
                    }
                }
            }
        }

        void Update()
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
            //            if (Input.GetMouseButtonUp(0))
            //            {
            //#if UNITY_EDITOR
            //                if (Time.time > nextClickTime)
            //                {
            //                    Teleport();
            //                    nextClickTime = Time.time + intervalClick;
            //                }

            //                Debug.DrawRay(ray.origin, ray.direction * 100, Color.green);
            //#endif
            //            }

        }
    }
}
#endif
