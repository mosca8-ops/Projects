using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Procedure;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Procedures/Visual Inspection/Cage Inspection Marker")]
    public class CageInspectionMarker : AbstractInspectionMarker
    {
        [Header("Components")]
        [SerializeField]
        [Draggable]
        private GameObject m_cage;
        [SerializeField]
        [Draggable]
        private GameObject m_magnifier;
        [SerializeField]
        [Draggable]
        private GameObject m_doneObject;
        [SerializeField]
        [Tooltip("Whether to keep the transparency when changing color or not")]
        private bool m_keepAlpha = false;
        [SerializeField]
        [Tooltip("Whether to hide the cage when the inspection is done or not")]
        private bool m_hideCageOnDone = true;
        [SerializeField]
        [Tooltip("Whether to hide the magnifier when the inspection is done or not")]
        private bool m_hideMagnifierOnDone = true;
        [SerializeField]
        private OptionalColor m_overrideColor;

        [Header("Animator parameters")]
        [SerializeField]
        [EnableIfComponentExists(typeof(Animator))]
        private OptionalString m_boolOutOfView = "OutOfView";
        [SerializeField]
        [EnableIfComponentExists(typeof(Animator))]
        private OptionalString m_floatInspecting = "Inspecting";
        [SerializeField]
        [EnableIfComponentExists(typeof(Animator))]
        private OptionalString m_triggerDone = "Done";

        [SerializeField]
        [HideInInspector]
        private Renderer m_cageRenderer;
        [SerializeField]
        [HideInInspector]
        private Renderer m_magnifierRenderer;
        [SerializeField]
        [HideInInspector]
        private Graphic m_magnifierGraphic;
        [SerializeField]
        [HideInInspector]
        private Renderer m_doneRenderer;

        private int m_boolOutOfViewId;
        private int m_floatInspectingId;
        private int m_triggerDoneId;

        [Space]
        [SerializeField]
        private StatesAndColors m_statesAndColors;
        [SerializeField]
        private Sounds m_sounds;
        [SerializeField]
        private Events m_events;

        private Animator m_animator;
        private Vector3 m_magnifierToCageRatio;
        private Vector3 m_doneToCageRatio;

        public override Color Color
        {
            get => CageColor;
            set => CageColor = MagnifierColor = DoneMessageColor = value;
        }

        public Color CageColor
        {
            get => GetColor(m_cageRenderer);
            set => SetColor(m_cageRenderer, value, m_keepAlpha);
        }

        public Color MagnifierColor
        {
            get => m_magnifierGraphic ? m_magnifierGraphic.color : GetColor(m_magnifierRenderer);
            set
            {
                if (m_magnifierGraphic) { SetColor(m_magnifierGraphic, value, m_keepAlpha); }
                else { SetColor(m_magnifierRenderer, value, m_keepAlpha); }
            }
        }

        public Color DoneMessageColor
        {
            get => GetColor(m_doneRenderer);
            set => SetColor(m_doneRenderer, value, m_keepAlpha);
        }

        public override float Progress
        {
            get => AnimatorInspecting;
            set => AnimatorInspecting = value;
        }

        private bool AnimatorOutOfView
        {
            get => m_animator && m_boolOutOfViewId != 0 ? m_animator.GetBool(m_boolOutOfViewId) : false;
            set
            {
                if (m_animator && m_boolOutOfViewId != 0)
                {
                    //m_animator.SetBool(m_boolOutOfViewId, value);
                    m_animator.SetBool(m_boolOutOfView, value);
                }
            }
        }

        private float AnimatorInspecting
        {
            get => m_animator ? m_animator.GetFloat(m_floatInspectingId) : 0;
            set
            {
                if (m_animator && m_floatInspectingId != 0)
                {
                    //m_animator.SetFloat(m_floatInspectingId, value);
                    m_animator.SetFloat(m_floatInspecting, value);
                }
            }
        }

        private void AnimatorSetDone()
        {
            if (m_animator && m_triggerDoneId != 0)
            {
                //m_animator.SetTrigger(m_triggerDoneId);
                m_animator.SetTrigger(m_triggerDone);
            }
        }

        private void AnimatorResetDone()
        {
            if (m_animator && m_triggerDoneId != 0)
            {
                //m_animator.ResetTrigger(m_triggerDoneId);
                m_animator.ResetTrigger(m_triggerDone);
            }
        }

        #region [  STATES  ]

        protected override void OnOutOfView()
        {
            base.OnOutOfView();
            if (m_statesAndColors.cageOutOfView.enabled)
            {
                CageColor = m_statesAndColors.cageOutOfView;
            }
            if (m_statesAndColors.magnifierOutOfView.enabled)
            {
                MagnifierColor = m_statesAndColors.magnifierOutOfView;
            }
            if (m_doneObject) { m_doneObject.SetActive(false); }
            AnimatorResetDone();
            AnimatorInspecting = 0;
            AnimatorOutOfView = true;

            m_sounds.PlayOutOfView();
            m_events.onOutOfView.Invoke();
        }

        protected override void OnInspecting()
        {
            base.OnInspecting();
            if (m_statesAndColors.cageInspecting.enabled)
            {
                CageColor = m_statesAndColors.cageInspecting;
            }
            if (m_statesAndColors.magnifierInspecting.enabled)
            {
                MagnifierColor = m_statesAndColors.magnifierInspecting;
            }
            if (m_doneObject) { m_doneObject.SetActive(false); }
            AnimatorResetDone();
            AnimatorOutOfView = false;

            m_sounds.PlayInspecting();
            m_events.onInspecting.Invoke();
        }

        protected override void OnInspectionDone()
        {
            base.OnInspectionDone();
            if (m_statesAndColors.cageDone.enabled)
            {
                CageColor = m_statesAndColors.cageDone;
            }
            if (m_statesAndColors.magnifierDone.enabled)
            {
                MagnifierColor = m_statesAndColors.magnifierDone;
            }
            if (m_statesAndColors.doneColor.enabled)
            {
                DoneMessageColor = m_statesAndColors.doneColor;
            }

            if (m_hideCageOnDone && m_cage) m_cage.SetActive(false);
            if (m_hideMagnifierOnDone && m_magnifier) m_magnifier.SetActive(false);

            if (m_doneObject)
            {
                m_doneObject.SetActive(true);
            }
            AnimatorSetDone();
            m_sounds.PlayDone();
            m_events.onInspectionDone.Invoke();
        }

        #endregion

        protected override void OnValidate()
        {
            base.OnValidate();
            m_cageRenderer = m_cage ? m_cage.GetComponentInChildren<Renderer>(true) : null;
            m_magnifierGraphic = m_magnifier ? m_magnifier.GetComponentInChildren<Graphic>(true) : null;
            m_magnifierRenderer = m_magnifier ? m_magnifier.GetComponentInChildren<Renderer>(true) : null;
            m_doneRenderer = m_doneObject ? m_doneObject.GetComponentInChildren<Renderer>(true) : null;
        }

        protected override void Awake()
        {
            base.Awake();
            m_sounds.transform = transform;
            if (!m_cageRenderer && m_cage)
            {
                m_cageRenderer = m_cage.GetComponentInChildren<Renderer>(true);
            }
            if ((!m_magnifierRenderer || !m_magnifierGraphic) && m_magnifier)
            {
                m_magnifierGraphic = m_magnifier.GetComponentInChildren<Graphic>(true);
                m_magnifierRenderer = m_magnifier.GetComponentInChildren<Renderer>(true);
            }
            if (!m_doneRenderer && m_doneObject)
            {
                m_doneRenderer = m_doneObject.GetComponentInChildren<Renderer>(true);
            }
            if (!m_animator)
            {
                m_animator = GetComponentInChildren<Animator>();
            }
            if (m_overrideColor.enabled)
            {
                Color = m_overrideColor.value;
            }
            if (m_animator)
            {
                m_boolOutOfViewId = m_boolOutOfView.enabled ? Animator.StringToHash(m_boolOutOfView) : 0;
                m_floatInspectingId = m_floatInspecting.enabled ? Animator.StringToHash(m_floatInspecting) : 0;
                m_triggerDoneId = m_triggerDone.enabled ? Animator.StringToHash(m_triggerDone) : 0;
            }
        }

        public override void OnUpdate(Transform inspector)
        {
            if (m_lookAtCamera && inspector)
            {
                if (m_doneObject)
                {
                    m_doneObject.transform.LookAt(inspector);
                }
                if (m_magnifier)
                {
                    m_magnifier.transform.LookAt(inspector);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_doneObject) { m_doneObject.SetActive(false); }
            if (m_cage) m_cage.SetActive(true);
            if (m_magnifier) m_magnifier.SetActive(true);
        }

        public override void Release()
        {
            if (m_cage) m_cage.SetActive(false);
            if (m_magnifier) m_magnifier.SetActive(false);
            if (m_doneObject) ToggleGameObjectDelayed(m_doneObject, false, 0.8f);
            base.Release();
        }

        public override void SetTarget(GameObject target, WEAVR.Pose localPose)
        {
            base.SetTarget(target, localPose);
            transform.SetParent(m_target, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (m_cage)
            {
                m_cage.SetActive(true);
                localPose.ApplyTo(m_cage.transform);
                if (LookAtInspector) {
                    var scaleAvg = (localPose.localScale.x + localPose.localScale.y + localPose.localScale.z) / 3f;
                    var scale = new Vector3(scaleAvg, scaleAvg, scaleAvg);
                    if (m_magnifier)
                    {
                        bool wasActive = m_magnifier.activeSelf;
                        m_magnifier.SetActive(true);
                        m_magnifier.transform.localPosition = localPose.localPosition;
                        m_magnifier.transform.localScale = scale;
                        m_magnifier.SetActive(wasActive);
                    }
                    if (m_doneObject)
                    {
                        bool wasActive = m_doneObject.activeSelf;
                        m_doneObject.SetActive(true);
                        m_doneObject.transform.localPosition = localPose.localPosition;
                        m_doneObject.transform.localScale = scale;
                        m_doneObject.SetActive(wasActive);
                    }
                }
                else
                {
                    if (m_magnifier)
                    {
                        localPose.ApplyEnabledTo(m_magnifier.transform);
                    }
                    if (m_doneObject)
                    {
                        localPose.ApplyEnabledTo(m_doneObject.transform);
                    }
                }
            }
        }

        public override void CopyValuesFrom(IVisualMarker otherMarker)
        {
            base.CopyValuesFrom(otherMarker);
            if(otherMarker is CageInspectionMarker marker)
            {
                Color = marker.Color;
                CageColor = marker.CageColor;
                MagnifierColor = marker.MagnifierColor;
                DoneMessageColor = marker.DoneMessageColor;
                Text = marker.Text;

                if(m_magnifier && marker.m_magnifier)
                {
                    m_magnifier.transform.localScale = marker.m_magnifier.transform.localScale;
                }
                if(m_doneObject && marker.m_doneObject)
                {
                    m_doneObject.transform.localScale = marker.m_doneObject.transform.localScale;
                }
            }
        }

        public override void StartInspection(IVisualInspectionLogic inspectionTarget, IVisualInspector inspector)
        {
            base.StartInspection(inspectionTarget, inspector);
            if (m_doneObject)
            {
                m_doneObject.SetActive(false);
            }
            if (m_magnifier)
            {
                m_magnifier.SetActive(true);
            }
            AnimatorOutOfView = true;
        }

        [Serializable]
        private struct StatesAndColors
        {
            public OptionalColor doneColor;

            [Space]
            public OptionalColor cageOutOfView;
            public OptionalColor magnifierOutOfView;

            [Space]
            public OptionalColor cageInspecting;
            public OptionalColor magnifierInspecting;

            [Space]
            public OptionalColor cageDone;
            public OptionalColor magnifierDone;
        }

        [Serializable]
        private struct Sounds
        {
            [NonSerialized]
            public Transform transform;

            public AudioSource audioSource;

            [Space]
            public AudioClip outOfView;
            public bool loopOutOfView;

            [Space]
            public AudioClip inspecting;
            public bool loopInspecting;

            [Space]
            public AudioClip done;
            public bool loopDone;

            public void PlayOutOfView() => Play(audioSource, outOfView, loopOutOfView);
            public void PlayInspecting() => Play(audioSource, inspecting, loopInspecting);
            public void PlayDone() => Play(audioSource, done, loopDone);

            private void Play(AudioSource source, AudioClip clip, bool loop)
            {
                if (source)
                {
                    source.Stop();
                    if (clip)
                    {
                        source.loop = loop;
                        source.clip = clip;
                        source.Play();
                    }
                }
                else if (clip)
                {
                    AudioSource.PlayClipAtPoint(clip, transform?.position ?? Vector3.zero);
                }
            }
        }

        [Serializable]
        private struct Events
        {
            public UnityEvent onOutOfView;
            public UnityEvent onInspecting;
            public UnityEvent onInspectionDone;
        }
    }
}
