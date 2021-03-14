using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Camera System/WEAVR Camera")]
    public class WeavrCamera : MonoBehaviour, IWeavrSingleton
    {

        #region [  STATIC PART  ]

        public static event System.Action<Camera> CameraChanged;

        protected static List<WeavrCamera> m_cameraStack = new List<WeavrCamera>();
        protected static WeavrCamera m_mainCamera;

        public static WeavrCamera Current
        {
            get { return m_mainCamera; }
            set
            {
                if (m_mainCamera != value)
                {
                    if (value)
                    {
                        m_mainCamera = value;
                        m_cameraStack.Remove(m_mainCamera);
                        m_cameraStack.Add(m_mainCamera);
                    }
                    else
                    {
                        CleanUpUnusedCameras();
                        m_mainCamera = m_cameraStack.Count > 0 ? m_cameraStack[m_cameraStack.Count - 1] : null;
                    }
                    CameraChanged?.Invoke(m_mainCamera ? m_mainCamera.Camera : null);
                }
            }
        }

        public static Camera CurrentCamera => Current ? Current.Camera : WeavrManager.DefaultCamera;

        private static void CleanUpUnusedCameras()
        {
            int camIndex = m_cameraStack.Count - 1;
            while (camIndex >= 0 && (!m_cameraStack[camIndex] || !m_cameraStack[camIndex].isActiveAndEnabled))
            {
                m_cameraStack.RemoveAt(camIndex--);
            }
        }

        private static void UnregisterCamera(WeavrCamera weavrCamera)
        {
            if (weavrCamera == Current)
            {
                Current = null;
            }
            else if(!Current)
            {
                CleanUpUnusedCameras();
                m_mainCamera = m_cameraStack.Count > 0 ? m_cameraStack[m_cameraStack.Count - 1] : null;
                if (m_mainCamera && CameraChanged != null)
                {
                    CameraChanged(m_mainCamera.Camera);
                }
            }
            else if (m_cameraStack.Count > 0)
            {
                m_cameraStack.Remove(weavrCamera);
            }
        }

        public static Camera GetCurrentCamera(WeavrCameraType type)
        {
            if (Application.isPlaying)
            {
                var cam = m_cameraStack.LastOrDefault(c => c.m_type == type);
                return cam ? cam.m_camera : null;
            }

            var sceneCam = SceneTools.GetComponentsInScene<WeavrCamera>().FirstOrDefault(w => w.m_type == type);
            return sceneCam ? sceneCam.m_camera : null;
        }

        public static IEnumerable<Camera> GetAllCameras(WeavrCameraType type)
        {
            return Application.isPlaying ? 
                    m_cameraStack.Where(c => c && c.Type == type).Select(c => c.Camera) :
                    SceneTools.GetComponentsInScene<WeavrCamera>().Where(c => c && c.Type == type).Select(c => c.Camera);
        }

        #endregion

        public enum WeavrCameraType { PlayerControlled, Free }

        [SerializeField]
        private WeavrCameraType m_type;

        private Camera m_camera;
        private bool m_cameraWasEnabled;

        public Camera Camera
        {
            get
            {
                if (!m_camera)
                {
                    m_camera = GetComponent<Camera>();
                }
                return m_camera;
            }
        }

        public WeavrCameraType Type
        {
            get => m_type;
            set
            {
                if(m_type != value)
                {
                    m_type = value;
                }
            }
        }
        
        private void Start()
        {
            if (Camera.isActiveAndEnabled)
            {
                Current = this;
            }
        }

        private void OnEnable()
        {
            if (!Camera)
            {
                WeavrDebug.Log(this, $"No camera found on GameObject {name}");
                enabled = false;
                return;
            }
            Current = this;
        }

        private void OnDisable()
        {
            UnregisterCamera(this);
        }

        private void Update()
        {
            if (m_cameraWasEnabled != Camera.isActiveAndEnabled)
            {
                m_cameraWasEnabled = Camera.isActiveAndEnabled;
                if (Camera.isActiveAndEnabled)
                {
                    Current = this;
                }
                else
                {
                    UnregisterCamera(this);
                }
            }
        }
    }
}
