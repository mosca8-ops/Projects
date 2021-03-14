using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Procedure;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Procedure
{

    public abstract class AbstractInspectionMarker : MonoBehaviour, IVisualInspectionMarker, IActiveProgressElement
    {
        private Transform m_originalParent;
        private VisualInspectionState m_state = VisualInspectionState.NotReady;


        [SerializeField]
        protected bool m_lookAtCamera;

        protected Transform m_target;
        private IVisualInspector m_inspector;
        protected Transform m_inspectorTransform;
        protected IVisualInspectionLogic m_inspectionLogic;


        public event VisualMarkerDelegates.OnReleaseMarker Released;
        public event Action OnInspectionStarted;
        public event Action<float> OnOngoingInspectionNormalized;
        public event Action OnInspectionLost;

        public IVisualInspector CurrentInspector => m_inspector;
        public virtual Color Color { get; set; }
        public virtual string Text { get; set; }
        public virtual VisualInspectionState State {
            get => m_state;
            set
            {
                if(m_state != value)
                {
                    m_state = value;
                    switch (m_state)
                    {
                        case VisualInspectionState.NotReady:
                            OnNotReady();
                            break;
                        case VisualInspectionState.OutOfView:
                            OnOutOfView();
                            break;
                        case VisualInspectionState.Inspecting:
                            OnInspecting();
                            break;
                        case VisualInspectionState.Inspected:
                            OnInspectionDone();
                            break;
                    }
                }
            }
        }

        public virtual float Progress { get; set; }

        public bool AutoDisableOnRelease => true;

        public bool LookAtInspector { get => m_lookAtCamera; set => m_lookAtCamera = value; }
        
        protected virtual void OnNotReady() { }

        protected virtual void OnOutOfView() { }

        protected virtual void OnInspecting() { }

        protected virtual void OnInspectionDone() { }

        protected virtual void OnValidate() { }

        protected virtual void Awake()
        {
            m_originalParent = transform.parent;
            OnNotReady();
        }

        protected virtual void OnEnable()
        {

        }

        public virtual void Release()
        {
            if (m_inspectionLogic is IVisualInspectionEvents tEvents)
            {
                tEvents.OnOngoingInspectionNormalized -= InspectionTarget_OnOngoingInspectionNormalized;
                tEvents.OnInspectionStarted -= InspectionTarget_OnInspectionStarted;
                tEvents.OnInspectionLost -= InspectionTarget_OnInspectionLost;
                tEvents.OnInspectionDone -= InspectionTarget_OnInspectionDone;
            }
            DelayedRelease(1);
        }

        private void ReleaseInternal()
        {
            transform.SetParent(m_originalParent, false);
            m_inspector = null;
            gameObject.SetActive(false);
            Released?.Invoke(this);
            m_target = null;
        }

        public void ResetProgress()
        {
            Progress = 0;
        }

        public virtual void Update()
        {
            if (m_inspectorTransform)
            {
                OnUpdate(m_inspectorTransform);
            }
        }

        public virtual void OnUpdate(Transform inspector)
        {
        }

        protected void DelayedRelease(float delay)
        {
            StartCoroutine(DelayedReleaseCoroutine(delay));
        }

        protected IEnumerator DelayedReleaseCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            ReleaseInternal();
        }

        protected void ToggleGameObjectDelayed(GameObject obj, bool enable, float delay)
        {
            StartCoroutine(ToggleDelayed(obj, enable, delay));
        }

        protected IEnumerator ToggleDelayed(GameObject obj, bool enable, float delay)
        {
            yield return new WaitForSeconds(delay);
            obj.SetActive(enable);
        }

        protected Color GetColor(Renderer renderer)
        {
            if(renderer is SpriteRenderer sprite) { return sprite.color; }
            if (Application.isPlaying && renderer.gameObject.scene.isLoaded)
            {
                return renderer && renderer.material ? renderer.material.color : Color.clear;
            }
            else
            {
                return renderer && renderer.sharedMaterial ? renderer.sharedMaterial.color : Color.clear;
            }
        }

        protected void SetColor(Graphic graphic, Color value, bool keepAlpha)
        {
            if (graphic && graphic.color != value)
            {
                graphic.color = keepAlpha ? new Color(value.r, value.g, value.b, graphic.color.a) : value;
            }
        }

        protected void SetColor(Renderer renderer, Color value, bool keepAlpha)
        {
            if(renderer is SpriteRenderer sprite)
            {
                SetColor(sprite, value, keepAlpha);
                return;
            }
            if (Application.isPlaying)
            {
                if (renderer && renderer.material && renderer.material.color != value)
                {
                    renderer.material.color = keepAlpha ? new Color(value.r, value.g, value.b, renderer.material.color.a) : value;
                }
            }
            else if (renderer && renderer.sharedMaterial && renderer.sharedMaterial.color != value)
            {
                renderer.sharedMaterial.color = keepAlpha ? new Color(value.r, value.g, value.b, renderer.sharedMaterial.color.a) : value;
            }
        }

        protected void SetColor(SpriteRenderer renderer, Color value, bool keepAlpha)
        {
            if (renderer && renderer.color != value)
            {
                renderer.color = keepAlpha ? new Color(value.r, value.g, value.b, renderer.color.a) : value;
            }
        }

        public virtual void SetTarget(GameObject target, Pose localPose)
        {
            StopAllCoroutines();
            m_target = target.transform;
        }

        public virtual void StartInspection(IVisualInspectionLogic inspectionLogic, IVisualInspector inspector)
        {
            ResetProgress();
            State = VisualInspectionState.NotReady;

            m_inspectionLogic = inspectionLogic;
            m_inspector = inspector;
            m_inspectorTransform = (inspector as Component)?.transform;

            if(m_inspectionLogic is IVisualInspectionEvents tEvents)
            {
                tEvents.OnOngoingInspectionNormalized -= InspectionTarget_OnOngoingInspectionNormalized;
                tEvents.OnOngoingInspectionNormalized += InspectionTarget_OnOngoingInspectionNormalized;

                tEvents.OnInspectionStarted -= InspectionTarget_OnInspectionStarted;
                tEvents.OnInspectionStarted += InspectionTarget_OnInspectionStarted;

                tEvents.OnInspectionLost -= InspectionTarget_OnInspectionLost;
                tEvents.OnInspectionLost += InspectionTarget_OnInspectionLost;

                tEvents.OnInspectionDone -= InspectionTarget_OnInspectionDone;
                tEvents.OnInspectionDone += InspectionTarget_OnInspectionDone;

                if (m_inspectionLogic.TargetIsVisible)
                {
                    State = VisualInspectionState.Inspecting;
                }
            }
        }

        private void InspectionTarget_OnInspectionDone()
        {
            State = VisualInspectionState.Inspected;
        }

        private void InspectionTarget_OnInspectionLost()
        {
            State = VisualInspectionState.OutOfView;
        }

        private void InspectionTarget_OnInspectionStarted()
        {
            State = VisualInspectionState.Inspecting;
        }

        private void InspectionTarget_OnOngoingInspectionNormalized(float value)
        {
            Progress = value;
        }

        public virtual void EndInspection()
        {
            Release();
        }

        public virtual void CopyValuesFrom(IVisualMarker otherMarker)
        {
            
        }
    }
}
