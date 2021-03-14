using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.InteractionUI
{

    [AddComponentMenu("WEAVR/VR/Advanced/Camera Screen")]
    public class VR_CameraScreen : MonoBehaviour
    {
        [SerializeField]
        protected VR_CameraFeed m_feed;
        [SerializeField]
        protected RawImage m_rawImage;
        [SerializeField]
        [HiddenBy(nameof(m_rawImage), hiddenWhenTrue: true)]
        protected Renderer m_backupRenderer;

        [Header("Components")]
        [SerializeField]
        protected Text m_descriptionText;


        public VR_CameraFeed Feed
        {
            get { return m_feed; }
            set
            {
                if(m_feed != value)
                {
                    m_feed = value;
                    if(m_descriptionText != null)
                    {
                        m_descriptionText.text = m_feed.title;
                    }
                }
            }
        }
        public RawImage Image => m_rawImage;

        protected virtual void Reset()
        {
            m_rawImage = GetComponentInChildren<RawImage>();
            if(m_rawImage == null)
            {
                m_backupRenderer = GetComponentInChildren<Renderer>();
            }
        }

        // Use this for initialization
        void Start()
        {
            if (m_rawImage == null)
            {
                m_rawImage = GetComponentInChildren<RawImage>();
            }
            if (m_rawImage == null)
            {
                m_backupRenderer = GetComponentInChildren<Renderer>();
            }
            if(m_descriptionText != null)
            {
                m_descriptionText.text = m_feed?.title;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(m_rawImage != null)
            {
                m_rawImage.texture = Feed.CurrentFrame;
            }
            else if(m_backupRenderer != null)
            {
                m_backupRenderer.material.mainTexture = Feed.CurrentFrame;
            }
        }

        public void Remove()
        {
            VR_CameraFeedManager.Instance.RemoveCameraFeed(m_feed);
        }

        public void Edit()
        {
            Teleport();
            VR_CameraFeedManager.Instance.CanSnapshot = true;
            VR_FlyMode.Instance.enabled = true;
        }

        public void Teleport()
        {
            VR_CameraFeedManager.Instance.MoveRigToFeed(m_feed);
        }

        public void Pin()
        {

        }
    }
}
