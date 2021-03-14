using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TXT.WEAVR.LayoutSystem
{
    [Serializable]
    public class UnityEventVideoClip : UnityEvent<VideoClip> { }

    [Serializable]
    public class UnityEventTexture : UnityEvent<Texture> { }

    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(VideoPlayer))]
    [AddComponentMenu("WEAVR/Layout System/Layout Video")]
    public class LayoutVideo : BaseLayoutItem,  IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Space]
        [SerializeField]
        [Draggable]
        [Button(nameof(SaveDefaults), "Save")]
        protected RawImage m_image;

        [SerializeField]
        [Draggable]
        protected VideoPlayer m_videoPlayer;
        //[SerializeField]
        //[HiddenBy(nameof(m_videoPlayer))]
        protected RenderTexture m_videoRenderTexture;

        [SerializeField]
        protected OptionalColor m_playbackColor = Color.white;
        [SerializeField]
        protected OptionalColor m_emptyColor = Color.black;

        [SerializeField]
        [Button(nameof(ResetToDefaults), "Reset")]
        protected bool m_keepSize = false;

        [SerializeField]
        protected bool m_autoPlay = true;

        [SerializeField]
        protected VideoControls m_videoControls;
        protected VideoControls Controls
        {
            get
            {
                if(m_videoControls == null)
                {
                    m_videoControls = new VideoControls();
                }
                return m_videoControls;
            }
        }

        [Space]
        [SerializeField]
        protected UnityEventVideoClip m_onVideoChanged;
        [SerializeField]
        protected UnityEventTexture m_onTextureChanged;

        [SerializeField]
        [HideInInspector]
        private AspectRatioFitter m_aspectRatioFitter;

        [SerializeField]
        [HideInInspector]
        private ShadowRectTransformData m_shadowTransformData;

        public bool AutoPlay
        {
            get { return m_autoPlay; }
            set
            {
                if(m_autoPlay != value)
                {
                    m_autoPlay = value;
                    if(value && Video != null && !m_videoPlayer.isPlaying)
                    {
                        PlayVideo();
                    }
                }
            }
        }

        public bool HasDescription => Controls.description;

        public bool PinDescription
        {
            get { return Controls.keepDescriptionVisible; }
            set { Controls.keepDescriptionVisible = value; }
        }

        public bool LoopVideo
        {
            get { return m_videoPlayer == null ? false : m_videoPlayer.isLooping; }
            set { if(m_videoPlayer != null) { m_videoPlayer.isLooping = value; } }
        }

        public bool IsPlaying
        {
            get { return m_videoPlayer == null ? false : m_videoPlayer.isPlaying; }
            set
            {
                if(m_videoPlayer != null && m_videoPlayer.isPlaying != value)
                {
                    if (value)
                    {
                        PlayVideo();
                    }
                    else
                    {
                        StopVideo();
                    }
                }
            }
        }

        public VideoClip Video
        {
            get { return m_videoPlayer?.clip; }
            set
            {
                if (m_videoPlayer != null && m_videoPlayer.clip != value)
                {
                    m_videoPlayer.clip = value;
                    m_videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                    m_videoPlayer.enabled = value != null;

                    m_videoRenderTexture = null;

                    if(value != null)
                    {
                        m_videoRenderTexture = new RenderTexture((int)value.width, (int)value.height, 0);
                        m_videoPlayer.targetTexture = m_videoRenderTexture;
                        Texture = m_videoRenderTexture;

                        if (AutoPlay)
                        {
                            PlayVideo();
                        }
                    }
                    else
                    {
                        Controls.HideAll();
                        m_videoPlayer.Stop();
                        m_videoPlayer.targetTexture = null;
                        Texture = null;
                    }

                    m_onVideoChanged.Invoke(value);
                }
            }
        }

        public Texture Texture
        {
            get { return m_image?.texture as Texture; }
            set
            {
                if (m_image != null && m_image.texture != value)
                {
                    m_image.texture = value;
                    m_image.enabled = true;
                    if (value == null)
                    {
                        if (m_emptyColor.enabled)
                        {
                            m_image.color = m_emptyColor;
                        }
                        else
                        {
                            m_image.enabled = false;
                        }
                    }
                    else if(m_playbackColor.enabled)
                    {
                        m_image.color = m_playbackColor;
                    }
                    //m_image.enabled = value != null;

                    RestoreShadowTransform();
                    if (value != null && RatioFitter != null && value.height > 0 && value.width > 0)
                    {
                        if (m_keepSize && ShadowTransformData.isValidRect)
                        {
                            SmartResize(value.width, value.height);
                        }
                        else
                        {
                            RatioFitter.aspectRatio = (float)value.width / value.height;
                        }
                    }
                    m_onTextureChanged.Invoke(value);
                }
            }
        }

        private void SmartResize(float width, float height)
        {
            if (width / ShadowTransformData.rect.width > height / ShadowTransformData.rect.height)
            {
                RatioFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            }
            else
            {
                RatioFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            }
            RatioFitter.aspectRatio = width / height;
        }

        public AspectRatioFitter RatioFitter
        {
            get
            {
                if (m_aspectRatioFitter == null)
                {
                    m_aspectRatioFitter = GetComponent<AspectRatioFitter>();
                }
                return m_aspectRatioFitter;
            }
        }

        protected ShadowRectTransformData ShadowTransformData
        {
            get
            {
                if (m_shadowTransformData == null && RatioFitter != null)
                {
                    m_shadowTransformData = new ShadowRectTransformData();
                    m_shadowTransformData.Snapshot(transform as RectTransform);
                }
                return m_shadowTransformData;
            }
        }

        protected virtual void SnapshotShadowTransform()
        {
            ShadowTransformData?.Snapshot(transform as RectTransform);
        }

        protected virtual void RestoreShadowTransform()
        {
            if (RatioFitter != null && ShadowTransformData != null)
            {
                RatioFitter.aspectRatio = ShadowTransformData.aspectRatio;
                ShadowTransformData.Restore(transform as RectTransform);
            }
        }

        protected override void Reset()
        {
            base.Reset();
            if (m_image == null)
            {
                m_image = GetComponent<RawImage>();
            }
            if(m_videoPlayer == null)
            {
                m_videoPlayer = GetComponent<VideoPlayer>();
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (m_image == null)
            {
                m_image = GetComponent<RawImage>();
            }
            if(m_videoPlayer == null)
            {
                m_videoPlayer = GetComponent<VideoPlayer>();
            }
            if (m_aspectRatioFitter == null)
            {
                m_aspectRatioFitter = GetComponent<AspectRatioFitter>();
                SnapshotShadowTransform();
            }
            if (RatioFitter != null && Texture == null)
            {
                SnapshotShadowTransform();
            }
        }

        public override void Clear()
        {
            if (m_videoPlayer == null || m_videoRenderTexture == null)
            {
                Texture = null;
            }
            else
            {
                Video = null;
            }
        }

        private void Awake()
        {
            Controls.HideAll();
            Controls.PrepareControls(gameObject);
        }

        // Use this for initialization
        protected override void Start()
        {
            base.Start();
            OnValidate();
            UnregisterToControlsEvents();
            RegisterToControlsEvents();
        }

        protected virtual void RegisterToControlsEvents()
        {
            Controls.playButton?.onClick.AddListener(PlayVideo);
            Controls.pauseButton?.onClick.AddListener(PauseVideo);
            Controls.stopButton?.onClick.AddListener(StopVideo);
        }

        protected virtual void OnEnable()
        {
            if (m_videoPlayer != null)
            {
                m_videoPlayer.loopPointReached -= LoopPointReached;
                m_videoPlayer.loopPointReached += LoopPointReached;
            }
            if(Video != null && !m_videoPlayer.isPlaying)
            {
                if (AutoPlay)
                {
                    PlayVideo();
                }
                else
                {
                    ShowControls(true);
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (m_videoPlayer != null)
            {
                m_videoPlayer.loopPointReached -= LoopPointReached;
            }
            Video = null;
        }

        private void LoopPointReached(VideoPlayer source)
        {
            if (!LoopVideo)
            {
                StopVideo();
            }
        }

        protected virtual void UnregisterToControlsEvents()
        {
            Controls.playButton?.onClick.RemoveListener(PlayVideo);
            Controls.pauseButton?.onClick.RemoveListener(PauseVideo);
            Controls.stopButton?.onClick.RemoveListener(StopVideo);
        }

        public virtual void PlayVideo()
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            if(gameObject.activeInHierarchy && Video != null)
            {
                m_videoPlayer.Play();
                Controls.playButton?.gameObject.SetActive(false);
                Controls.pauseButton?.gameObject.SetActive(true);
                Controls.stopButton?.gameObject.SetActive(true);
                Controls.description?.SetActive(true);

                ShowControls(true);
            }
        }

        public virtual void PauseVideo()
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            if (gameObject.activeInHierarchy && Video != null)
            {
                m_videoPlayer.Pause();
                Controls.playButton?.gameObject.SetActive(true);
                Controls.pauseButton?.gameObject.SetActive(false);
                Controls.stopButton?.gameObject.SetActive(true);
                Controls.description?.SetActive(true);
            }
        }

        public virtual void StopVideo()
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            if (gameObject.activeInHierarchy && Video != null)
            {
                m_videoPlayer.Stop();
                Controls.playButton?.gameObject.SetActive(true);
                Controls.pauseButton?.gameObject.SetActive(false);
                Controls.stopButton?.gameObject.SetActive(false);
                Controls.description?.SetActive(true);
            }
        }

        public void SaveDefaults()
        {
            if (m_image == null)
            {
                m_image = GetComponent<RawImage>();
            }
            if(m_videoPlayer == null)
            {
                m_videoPlayer = GetComponent<VideoPlayer>();
            }
            if (m_aspectRatioFitter == null)
            {
                m_aspectRatioFitter = GetComponent<AspectRatioFitter>();
                SnapshotShadowTransform();
            }
            Clear();
        }

        public override void ResetToDefaults()
        {
            if (m_videoPlayer == null || m_videoRenderTexture == null)
            {
                Texture = null;
            }
            Video = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if(eventData.pointerId >= 0 // It is a touch input
                && !Controls.AnyIsVisible)
            {
                ShowControls(true);
                eventData.Use();
            }

            Controls.lastMousePosition = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowControls(true);
            eventData.Use();

            Controls.lastMousePosition = Input.mousePosition;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideControls();
            eventData.Use();

            Controls.lastMousePosition = null;
        }

        protected virtual void Update()
        {
            if (Video != null 
                && Controls.lastMousePosition.HasValue 
                && Vector3.Distance(Input.mousePosition, Controls.lastMousePosition.Value) > 4f)
            {
                ShowControls(true);
                Controls.lastMousePosition = Input.mousePosition;
            }
        }

        protected void HideControls()
        {
            StopAllCoroutines();
            if (Controls.UseAnimator && Controls.animatorHideTrigger.enabled)
            {
                Controls.animator.SetTrigger(Controls.animatorHideTrigger);
            }
            else
            {
                MoveAlphaTo(0, null);
            }
        }

        protected void ShowControls(bool useHideTimeout)
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            StopAllCoroutines();
            if (Controls.UseAnimator && Controls.animatorShowTrigger.enabled)
            {
                Controls.animator.SetTrigger(Controls.animatorShowTrigger);
            }
            else
            {
                MoveAlphaTo(1, null);
            }
            if (useHideTimeout)
            {
                StartCoroutine(KeepControlsVisibleFor(Controls.visibilityTime));
            }
        }

        protected IEnumerator KeepControlsVisibleFor(float time)
        {
            yield return new WaitForSeconds(time);
            HideControls();
        }

        protected void MoveAlphaTo(float alpha, Action endCallback)
        {
            StopAllCoroutines();
            StartCoroutine(CoroutineMoveAlphaTo(alpha, endCallback));
        }

        protected IEnumerator CoroutineMoveAlphaTo(float alpha, Action endCallback)
        {
            float remainingTime = Controls.transitionTime + 0.1f;
            while(remainingTime > 0)
            {
                Controls.MoveAlphaTo(alpha, Time.deltaTime / Controls.transitionTime);
                remainingTime -= Time.deltaTime;
                yield return null;
            }
            endCallback?.Invoke();
        }
        
        [Serializable]
        protected class VideoControls
        {
            [SerializeField]
            private Button m_playButton;
            [SerializeField]
            private Button m_pauseButton;
            [SerializeField]
            private Button m_stopButton;

            [SerializeField]
            private GameObject m_description;

            public Button playButton
            {
                get { return Get(m_playButton); }
                set { m_playButton = value; }
            }

            public Button pauseButton
            {
                get { return Get(m_pauseButton); }
                set { m_pauseButton = value; }
            }

            public Button stopButton
            {
                get { return Get(m_stopButton); }
                set { m_stopButton = value; }
            }

            public GameObject description
            {
                get { return Get(m_description); }
                set { m_description = value; }
            }

            public bool keepDescriptionVisible;
            
            [Range(1, 6)]
            public float visibilityTime = 3f;
            [Range(0.001f, 3f)]
            public float transitionTime = 0.5f;
            public OptionalString animatorShowTrigger;
            public OptionalString animatorHideTrigger;

            [HideInInspector]
            public CanvasGroup playButtonGroup;
            [HideInInspector]
            public CanvasGroup pauseButtonGroup;
            [HideInInspector]
            public CanvasGroup stopButtonGroup;
            [HideInInspector]
            public CanvasGroup descriptionGroup;
            [HideInInspector]
            public Animator animator;

            private Vector3? m_lastMousePosition;
            public Vector3? lastMousePosition
            {
                get { return m_lastMousePosition; }
                set { m_lastMousePosition = value; }
            }

            public bool AnyIsVisible => playButton?.gameObject.activeInHierarchy == true || Get(playButtonGroup)?.alpha > 0.01f
                                     || pauseButton?.gameObject.activeInHierarchy == true || Get(pauseButtonGroup)?.alpha > 0.01f
                                     || stopButton?.gameObject.activeInHierarchy == true || Get(stopButtonGroup)?.alpha > 0.01f
                                     || description?.activeInHierarchy == true || Get(descriptionGroup)?.alpha > 0.01f;

            public bool UseAnimator => animator != null && (animatorHideTrigger.enabled || animatorShowTrigger.enabled);

            public void HideAll()
            {
                playButton?.gameObject.SetActive(false);
                pauseButton?.gameObject.SetActive(false);
                stopButton?.gameObject.SetActive(false);
                description?.SetActive(false);
            }

            private T Get<T>(T obj) where T: UnityEngine.Object
            {
                return obj ? obj : null;
            }

            public void PrepareControls(GameObject owner)
            {
                playButtonGroup = GetOrCreateCanvasGroup(playButton?.gameObject);
                pauseButtonGroup = GetOrCreateCanvasGroup(pauseButton?.gameObject);
                stopButtonGroup = GetOrCreateCanvasGroup(stopButton?.gameObject);
                descriptionGroup = GetOrCreateCanvasGroup(description);
                animator = owner.GetComponentInParent<Animator>();
                visibilityTime = Mathf.Max(visibilityTime, 1);
            }

            private CanvasGroup GetOrCreateCanvasGroup(GameObject target)
            {
                if (target != null)
                {
                    var group = target.GetComponent<CanvasGroup>();
                    if (group == null)
                    {
                        group = target.AddComponent<CanvasGroup>();
                    }
                    return group;
                }
                return null;
            }

            public void MoveAlphaTo(float alpha, float delta)
            {
                MoveAlphaTo(playButtonGroup, alpha, delta);
                MoveAlphaTo(pauseButtonGroup, alpha, delta);
                MoveAlphaTo(stopButtonGroup, alpha, delta);
                MoveAlphaTo(descriptionGroup, keepDescriptionVisible ? 1 : alpha, delta);
            }

            private void MoveAlphaTo(Button button, float alpha, float delta)
            {
                if(button != null)
                {
                    var color = button.targetGraphic.color;
                    color.a = Mathf.MoveTowards(color.a, alpha, delta);
                    button.targetGraphic.color = color;
                }
            }

            private void MoveAlphaTo(CanvasGroup group, float alpha, float delta)
            {
                if(group != null)
                {
                    group.alpha = Mathf.MoveTowards(group.alpha, alpha, delta);
                    group.interactable = group.alpha > 0.01f;
                }
            }
        }

        [Serializable]
        protected class ShadowRectTransformData
        {
            public Vector2 anchoredPosition;
            public Vector3 anchoredPosition3D;
            public Vector2 anchorMax;
            public Vector2 anchorMin;
            public Vector2 offsetMax;
            public Vector2 offsetMin;
            public Vector2 pivot;
            public Vector2 sizeDelta;
            public Rect rect;
            public float aspectRatio;
            public bool isValidRect;

            public void Snapshot(RectTransform transform)
            {
                anchoredPosition = transform.anchoredPosition;
                anchoredPosition3D = transform.anchoredPosition3D;
                anchorMax = transform.anchorMax;
                anchorMin = transform.anchorMin;
                offsetMax = transform.offsetMax;
                offsetMin = transform.offsetMin;
                pivot = transform.pivot;
                sizeDelta = transform.sizeDelta;
                rect = transform.rect;

                isValidRect = rect.width > 0 && rect.height > 0;

                if (isValidRect)
                {
                    aspectRatio = rect.width / rect.height;
                }
            }

            public void Restore(RectTransform transform)
            {
                transform.anchoredPosition = anchoredPosition;
                transform.anchoredPosition3D = anchoredPosition3D;
                transform.anchorMax = anchorMax;
                transform.anchorMin = anchorMin;
                transform.offsetMax = offsetMax;
                transform.offsetMin = offsetMin;
                transform.pivot = pivot;
                transform.sizeDelta = sizeDelta;
            }
        }
    }
}