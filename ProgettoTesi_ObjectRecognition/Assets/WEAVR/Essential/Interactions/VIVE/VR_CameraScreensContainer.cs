using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.InteractionUI
{

    [AddComponentMenu("WEAVR/VR/Advanced/Camera Screens Container")]
    public class VR_CameraScreensContainer : MonoBehaviour
    {
        public VR_CameraScreen screenSample;
        public ScrollRect scrollView;
        public RectTransform screensContainer;

        private List<VR_CameraScreen> m_screens;

        private void Reset()
        {
            scrollView = GetComponentInParent<ScrollRect>();
            if (transform is RectTransform)
            {
                screensContainer = transform as RectTransform;
            }
        }

        private VR_CameraScreen AddScreen()
        {
            var screen = Instantiate(screenSample.gameObject).GetComponent<VR_CameraScreen>();
            screen.transform.SetParent(screensContainer, false);
            m_screens.Add(screen);
            return screen;
        }

        private void RemoveScreen(VR_CameraScreen screen)
        {
            if (m_screens.Remove(screen))
            {
                VR_CameraFeedManager.Instance.RemoveCameraFeed(screen.Feed);
                Destroy(screen.gameObject);
            }
        }

        // Use this for initialization
        void Start()
        {
            m_screens = new List<VR_CameraScreen>();
            foreach(var feed in VR_CameraFeedManager.Instance.Feeds)
            {
                AddScreen().Feed = feed;
            }

            VR_CameraFeedManager.Instance.OnFeedAdded -= Instance_OnFeedAdded;
            VR_CameraFeedManager.Instance.OnFeedAdded += Instance_OnFeedAdded;
            VR_CameraFeedManager.Instance.OnFeedRemoved -= Instance_OnFeedRemoved; ;
            VR_CameraFeedManager.Instance.OnFeedRemoved += Instance_OnFeedRemoved; ;
        }

        private void Instance_OnFeedRemoved(VR_CameraFeed feed)
        {
            for (int i = 0; i < m_screens.Count; i++)
            {
                if(m_screens[i].Feed == feed)
                {
                    m_screens.RemoveAt(i--);
                }
            }
        }

        private void Instance_OnFeedAdded(VR_CameraFeed feed)
        {
            AddScreen().Feed = feed;
        }

        public void CreateScreen()
        {

        }

        //// Update is called once per frame
        //void Update()
        //{

        //}
    }
}
