using UnityEngine;

namespace TXT.WEAVR.InteractionUI
{
    [AddComponentMenu("WEAVR/VR/Advanced/Camera Feed")]
    [RequireComponent(typeof(Camera))]
    public class VR_CameraFeed : MonoBehaviour
    {
        public string title;
        [SerializeField]
        [Range(0.1f, 30)]
        private float m_targetFps = 5;
        [SerializeField]
        private Vector2Int m_frameSize = new Vector2Int(320, 240);
        [SerializeField]
        [HideInInspector]
        protected float m_updateRate;
        [SerializeField]
        protected Camera m_camera;
        [Space]
        [SerializeField]
        protected Transform m_vrRigPoint;

        private bool m_realtime;
        private bool m_paused;
        private float m_nextUpdate;
        protected int m_frameCount;
        protected Rect m_frameRect;
        protected Texture2D m_texture;

        public Vector3? VRRigPosition {
            get { return m_vrRigPoint?.position; }
            set {
                if (m_vrRigPoint == null)
                {
                    m_vrRigPoint = new GameObject("ShadowVRRIG").transform;
                    m_vrRigPoint.SetParent(transform, false);
                    m_vrRigPoint.hideFlags = HideFlags.DontSave;
                }
                m_vrRigPoint.position = value ?? Vector3.zero;
            }
        }

        public Texture2D CurrentFrame {
            get {
                if (m_paused && m_texture != null)
                {
                    return m_texture;
                }
                if (!m_realtime && m_nextUpdate > Time.time)
                {
                    return m_texture;
                }
                PrepareTexture();
                m_nextUpdate = Time.time + m_updateRate;
                return m_texture;
            }
        }

        public Camera Camera => m_camera;

        public bool IsRealtime {
            get { return m_realtime; }
            set { m_realtime = value; }
        }

        public bool IsPaused {
            get { return m_paused; }
            set { m_paused = value; }
        }

        public Vector2Int FrameSize {
            get { return m_frameSize; }
            set {
                if (m_frameSize != value)
                {
                    m_frameSize = value;
                    UpdateFrameSizeRelatedData();
                }
            }
        }

        private void UpdateFrameSizeRelatedData()
        {
            CleanupTexture(m_camera.targetTexture);
            m_camera.targetTexture = new RenderTexture(m_frameSize.x, m_frameSize.y, 24);
            if (m_texture != null)
            {
                m_texture.Resize(m_frameSize.x, m_frameSize.y);
            }
            m_frameRect.size = m_frameSize;
        }

        private void CleanupTexture(Texture texture)
        {
            if (texture != null)
            {
                if (texture is RenderTexture)
                {
                    ((RenderTexture)texture).Release();
                }
                if (Application.isPlaying)
                {
                    Destroy(texture);
                }
                else
                {
                    DestroyImmediate(texture);
                }
            }
        }

        public int FrameCount => m_frameCount;

        protected virtual void Reset()
        {
            m_camera = GetComponent<Camera>();
        }

        protected virtual void OnValidate()
        {
            InitializeCamera();
            UpdateFrameSizeRelatedData();
            m_updateRate = 1 / m_targetFps;
        }

        private void InitializeCamera()
        {
            if (m_camera == null)
            {
                m_camera = GetComponent<Camera>();
            }
            m_camera.enabled = false;
            if (m_camera.targetTexture == null)
            {
                m_camera.targetTexture = new RenderTexture(m_frameSize.x, m_frameSize.y, 24);
            }
        }

        // Use this for initialization
        protected virtual void Start()
        {
            InitializeCamera();
            m_updateRate = 1 / m_targetFps;
            if (m_texture == null)
            {
                m_texture = new Texture2D(m_frameSize.x, m_frameSize.y);
            }
            m_frameRect.size = m_frameSize;
        }

        Texture2D PrepareTexture(Camera cam)
        {
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = cam.targetTexture;
            cam.Render();
            Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
            image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
            image.Apply();
            RenderTexture.active = currentRT;
            return image;
        }

        protected virtual void PrepareTexture()
        {
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = m_camera.targetTexture;
            m_camera.Render();
            //Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
            //image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
            //image.Apply();
            if (m_texture == null)
            {
                m_texture = new Texture2D(m_frameSize.x, m_frameSize.y);
            }
            m_texture.ReadPixels(m_frameRect, 0, 0);
            m_texture.Apply();
            RenderTexture.active = currentRT;
        }

        private void OnEnable()
        {
            InitializeCamera();
            VR_CameraFeedManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            VR_CameraFeedManager.Instance.Unregister(this);
        }

        private void OnDestroy()
        {
            VR_CameraFeedManager.Instance.Unregister(this);
            CleanupTexture(m_texture);
            CleanupTexture(m_camera.targetTexture);
        }
    }
}
