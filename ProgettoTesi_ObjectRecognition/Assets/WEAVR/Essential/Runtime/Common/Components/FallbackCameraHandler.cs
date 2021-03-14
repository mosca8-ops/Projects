using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Setup/Fallback Camera Handler")]
    public class FallbackCameraHandler : MonoBehaviour
    {

        private static FallbackCameraHandler s_instance = null;
        private Camera m_camera = null;

        // Start is called before the first frame update
        protected virtual void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
                m_camera = GetComponent<Camera>();
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        public void EnterFallBackMode()
        {
            if (m_camera)
            {
                m_camera.enabled = true;
            }
        }

        public void ExitFallBackMode()
        {
            if (m_camera)
            {
                m_camera.enabled = false;
            }
        }

    }

}