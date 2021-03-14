using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.Events;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#else
using TXT.WEAVR.Core;
#endif
 
namespace TXT.WEAVR.InteractionUI
{
    [Serializable]
    public class UnityEventFeed : UnityEvent<VR_CameraFeed> { }

    [AddComponentMenu("WEAVR/VR/Advanced/Camera Feed Manager")]
    public class VR_CameraFeedManager : MonoBehaviour
    {

        #region [  STATIC PART  ]

        private static VR_CameraFeedManager _instance;

        public static VR_CameraFeedManager Instance {
            get {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<VR_CameraFeedManager>();
                    if (_instance == null)
                    {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("VR_CameraFeedManager");
                        _instance = go.AddComponent<VR_CameraFeedManager>();
                    }
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        #endregion

        [SerializeField]
        private VR_CameraFeed m_cameraFeedSample;
        [SerializeField]
        private Camera m_vrCamera;
        [SerializeField]
        private VR_ControllerAction m_snapshotButton;
        [SerializeField]
        private bool m_canSnapshot;
        [SerializeField]
        private Color m_snapshotFade = Color.white;
        [SerializeField]
        [HideInInspector]
        private List<VR_CameraFeed> m_feeds;

        [Space]
        [SerializeField]
        private UnityEventFeed m_onFeedAdded;
        [SerializeField]
        private UnityEventFeed m_onFeedRemoved;

        public event Action<VR_CameraFeed> OnFeedAdded;
        public event Action<VR_CameraFeed> OnFeedRemoved;

        [SerializeField]
        private Dictionary<VR_CameraFeed, RigPose> m_rigPoses;

        public IReadOnlyList<VR_CameraFeed> Feeds => m_feeds;

        public bool CanSnapshot {
            get { return m_canSnapshot; }
            set {
                m_canSnapshot = value;
            }
        }

        private void Initialize()
        {
            if (m_feeds == null)
            {
                m_feeds = new List<VR_CameraFeed>();
            }
            m_rigPoses = new Dictionary<VR_CameraFeed, RigPose>();
        }

        private void Awake()
        {
            if (_instance != this)
            {
                Initialize();
            }
        }

        public VR_CameraFeed CreateCameraFeed()
        {
            VR_CameraFeed newFeed = null;
            if (m_cameraFeedSample != null)
            {
                newFeed = Instantiate(m_cameraFeedSample.gameObject).GetComponent<VR_CameraFeed>();
                newFeed.transform.SetParent(transform, false);
                newFeed.gameObject.SetActive(true);
            }
            else
            {
                newFeed = new GameObject($"CameraFeed_{m_feeds.Count}").AddComponent<VR_CameraFeed>();
                newFeed.transform.SetParent(transform, false);
            }

            SetCurrentPose(newFeed);

            return newFeed;
        }

        public void RemoveCameraFeed(VR_CameraFeed feed)
        {
            if (m_feeds.Remove(feed))
            {
                m_rigPoses.Remove(feed);
                Destroy(feed.gameObject);
            }
        }

        public void Register(VR_CameraFeed feed)
        {
            if (!m_feeds.Contains(feed))
            {
                m_feeds.Add(feed);
                m_rigPoses[feed] = null;

                m_onFeedAdded.Invoke(feed);
                OnFeedAdded?.Invoke(feed);
            }
        }

        public void Unregister(VR_CameraFeed feed)
        {
            if (m_feeds.Remove(feed))
            {
                m_rigPoses.Remove(feed);
                m_onFeedRemoved.Invoke(feed);
                OnFeedRemoved?.Invoke(feed);
            }
        }

        public void MoveRigToFeed(int index)
        {
            MoveRigToFeed(m_feeds[index]);
        }

        protected void InvokeDelayed(Action action, float delay)
        {
            StartCoroutine(DelayedCoroutine(action, delay));
        }

        IEnumerator DelayedCoroutine(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action();
        }

#if WEAVR_VR

        // Use this for initialization
        void Start()
        {
            m_snapshotButton.OnTriggered += SnapshotButton_OnTriggered;
        }

        private void SnapshotButton_OnTriggered()
        {
            if (CanSnapshot)
            {
                //SteamVR_Fade.Start(Color.clear, 0);
                //SteamVR_Fade.Start(m_snapshotFade, 0.1f);

                CreateCameraFeed();

                InvokeDelayed(() =>
                {
                    //SteamVR_Fade.Start(Color.clear, 0.1f);
                },
                0.1f);
            }
        }

        private void OnValidate()
        {
            if (m_vrCamera == null)
            {
                m_vrCamera = Valve.VR.InteractionSystem.Player.instance.GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            if (m_canSnapshot && m_snapshotButton.IsTriggered())
            {

            }
        }

        protected virtual void SetCurrentPose(VR_CameraFeed newFeed)
        {
            var cam = m_vrCamera ?? Valve.VR.InteractionSystem.Player.instance.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                newFeed.Camera?.CopyFrom(cam);
                newFeed.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
            }
            else
            {
                var head = Valve.VR.InteractionSystem.Player.instance.hmdTransform;
                newFeed.transform.SetPositionAndRotation(head.position, head.rotation);
            }

            m_feeds.Add(newFeed);
            m_rigPoses[newFeed] = new RigPose(Valve.VR.InteractionSystem.Player.instance.trackingOriginTransform.position);
        }

        public void MoveRigToFeed(VR_CameraFeed feed)
        {
            RigPose pose = null;
            if (!m_rigPoses.TryGetValue(feed, out pose) || pose == null)
            {
                if (feed.VRRigPosition.HasValue)
                {
                    pose = new RigPose(feed.VRRigPosition.Value, Valve.VR.InteractionSystem.Player.instance.hmdTransform.position - Valve.VR.InteractionSystem.Player.instance.trackingOriginTransform.position);
                }
                else
                {
                    float headHeight = Valve.VR.InteractionSystem.Player.instance.hmdTransform.position.y;
                    float feetHeight = (Valve.VR.InteractionSystem.Player.instance.trackingOriginTransform ?? Valve.VR.InteractionSystem.Player.instance.transform).position.y;
                    pose = new RigPose(feed.transform.position - Vector3.up * (headHeight - feetHeight));
                }
            }

            //SteamVR_Fade.Start(Color.clear, 0);
            //SteamVR_Fade.Start(Color.black, 0.1f);

            InvokeDelayed(() =>
            {
                Valve.VR.InteractionSystem.Player.instance.trackingOriginTransform.position = pose.position;
                //SteamVR_Fade.Start(Color.clear, 0.1f);
            },
            0.1f);
        }

#else

        public void MoveRigToFeed(VR_CameraFeed feed)
        {

        }

        protected virtual void SetCurrentPose(VR_CameraFeed newFeed)
        {
            var cam = m_vrCamera ?? WeavrCamera.CurrentCamera;
            if (cam != null)
            {
                newFeed.Camera?.CopyFrom(cam);
                newFeed.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
            }
            m_feeds.Add(newFeed);
            m_rigPoses[newFeed] = new RigPose(newFeed.transform.position);
        }
#endif

        [SerializeField]
        private class RigPose
        {
            public Vector3 position;

            public RigPose(Vector3 position)
            {
                this.position = position;
            }

            public RigPose(Vector3 rigPosition, Vector3 headPosition)
            {
                position = new Vector3(rigPosition.x - headPosition.x, rigPosition.y, rigPosition.z - headPosition.z);
            }
        }
    }
}
