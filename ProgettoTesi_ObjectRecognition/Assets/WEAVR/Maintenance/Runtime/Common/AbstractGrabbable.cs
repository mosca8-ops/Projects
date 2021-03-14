namespace TXT.WEAVR.Maintenance
{
    using System.Collections;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Interaction;
    using UnityEngine;
    using UnityEngine.Events;

    public abstract class AbstractGrabbable : AbstractInteractiveBehaviour
    {

        public Color grabColor = Color.red;

        [Header("Events")]
        public UnityEvent onGrab;
        public UnityEvent onUngrab;

        protected ObjectsBag.Hand m_grabbedHand;
        protected bool m_isGrabbed;

        public virtual bool IsGrabbedGlobally { get; set; }

        public virtual bool IsGrabbed {
            get {
                return m_isGrabbed;
            }
            protected set {
                if (m_isGrabbed != value)
                {
                    if (value) { Grab(); }
                    else { Release(); }
                }
            }
        }

        protected bool m_wasKinematic = false;

        protected ObjectsBag m_currentBag;

        public override bool CanBeDefault => true;

        private Transform m_transformToSync;

        protected bool m_syncTransforms;
        protected Transform TransformToSync {
            get {
                //if(m_transformToSync == null)
                //{
                //    m_transformToSync = new GameObject($"{name}_syncTransform").transform;
                //    m_transformToSync.gameObject.hideFlags = HideFlags.HideAndDontSave;
                //}
                return m_transformToSync;
            }
            set {
                if (m_transformToSync != value)
                {
                    //if (m_transformToSync != null)
                    //{
                    //    Destroy(m_transformToSync.gameObject);
                    //}
                    m_transformToSync = value;
                }
            }
        }

        protected override void Reset()
        {
            InteractTrigger = BehaviourInteractionTrigger.OnPointerDown;
            if (Controller != null)
            {
                Controller.DefaultBehaviour = this;
            }
        }

        // Use this for initialization
        protected virtual void Start()
        {
            if (m_currentBag == null)
            {
                m_currentBag = BagHolder.Main.Bag;
            }
            //if(m_snapAttachPoint == null)
            //{
            //    m_snapAttachPoint = transform;
            //}
        }

        [ExposeMethod]
        public virtual void Grab(bool highlight = true)
        {
            Grab(m_currentBag, highlight);
        }

        public void Grab(ObjectsBag bag, bool highlight = true, bool triggerEvents = true)
        {
            ChangeBag(bag, false);
            if (!IsGrabbed)
            {
                var selected = m_currentBag.GetSelected(this);
                if (selected != gameObject && selected)
                {
                    var otherGrabbable = selected.GetComponent<AbstractGrabbable>();
                    if (otherGrabbable != null) { otherGrabbable.Release(); }
                }
                m_currentBag.Selected = gameObject;
                m_grabbedHand = m_currentBag.GetOwningHand(gameObject);
                if (highlight && WeavrManager.ShowInteractionHighlights)
                {
                    Outliner.Outline(gameObject, grabColor, false);
                }
                m_isGrabbed = true;

                IsGrabbedGlobally = true;
                //if (Controller.Has<AbstractConnectable>())
                //{
                //    GetComponent<AbstractConnectable>().Disconnect();
                //}

                if (triggerEvents)
                {
                    onGrab.Invoke();
                }

                Controller.CurrentBehaviour = this;
            }
        }

        [ExposeMethod]
        public void Release(bool triggerEvents = true)
        {
            Release(m_currentBag, triggerEvents);
        }

        public virtual void Release(ObjectsBag bag, bool triggerEvents = true)
        {
            ChangeBag(bag, false);
            if (IsGrabbed)
            {
                if (m_grabbedHand?.Selected == gameObject)
                {
                    m_grabbedHand.Selected = null;
                }
                Outliner.RemoveOutline(gameObject, grabColor);
                m_isGrabbed = false;

                IsGrabbedGlobally = false;

                RestoreRigidBody();

                OnReleaseInternal();

                if (triggerEvents)
                    onUngrab.Invoke();

                EndInteraction();
            }
        }

        public override void OnDisableInteraction()
        {
            base.OnDisableInteraction();
            if (IsGrabbed)
            {
                Release();
            }
        }

        protected abstract void OnReleaseInternal();

        public void ChangeBag(ObjectsBag bag, bool removeFromPrevious)
        {
            if (removeFromPrevious && IsGrabbed)
            {
                m_currentBag.Remove(gameObject.name);
                m_isGrabbed = false;
            }
            if (m_currentBag != bag && bag != null)
            {
                m_currentBag = bag;
            }
        }

        protected override void StopInteraction(AbstractInteractiveBehaviour nextBehaviour)
        {
            base.StopInteraction(nextBehaviour);
            Release();
        }

        public override void Interact(ObjectsBag currentBag)
        {
            if (IsGrabbedInBag(currentBag)) { Release(currentBag); }
            else { Grab(currentBag); }
            //if(currentBag.Selected != gameObject) {
            //    currentBag.Selected = gameObject;
            //    BorderOutliner.Instance.Outline(gameObject, grabColor, false);
            //}
            //else /*if(currentBag.Selected == gameObject)*/{
            //    Release(currentBag);
            //}
            //_currentBag = currentBag;
        }

        public override bool CanInteract(ObjectsBag currentBag)
        {
            return base.CanInteract(currentBag); /*currentBag.Selected == null || currentBag.Selected == gameObject;*/
        }

        public bool IsGrabbedInBag(ObjectsBag bag)
        {
            return m_currentBag == bag && bag.Selected == gameObject;
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return IsGrabbedInBag(currentBag) ? "Release" : "Grab";
        }

        private void RestoreRigidBody()
        {
            var rigidBody = GetComponent<Rigidbody>();
            if (rigidBody != null)
            {
                rigidBody.isKinematic = m_wasKinematic;
            }
        }

        private void Update()
        {
        }

        #region [  VELOCITY ESTIMATION  ]

        private Coroutine routine;
        private int sampleCount;
        private Vector3[] velocitySamples;
        private Vector3[] angularVelocitySamples;

        //-------------------------------------------------
        public void BeginEstimatingVelocity()
        {


            FinishEstimatingVelocity();

            routine = StartCoroutine(EstimateVelocityCoroutine());
        }


        //-------------------------------------------------
        public void FinishEstimatingVelocity()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }


        //-------------------------------------------------
        public Vector3 GetVelocityEstimate()
        {
            // Compute average velocity
            Vector3 velocity = Vector3.zero;
            int velocitySampleCount = Mathf.Min(sampleCount, velocitySamples.Length);
            if (velocitySampleCount != 0)
            {
                for (int i = 0; i < velocitySampleCount; i++)
                {
                    velocity += velocitySamples[i];
                }
                velocity *= (1.0f / velocitySampleCount);
            }

            return velocity;
        }


        //-------------------------------------------------
        public Vector3 GetAngularVelocityEstimate()
        {
            // Compute average angular velocity
            Vector3 angularVelocity = Vector3.zero;
            int angularVelocitySampleCount = Mathf.Min(sampleCount, angularVelocitySamples.Length);
            if (angularVelocitySampleCount != 0)
            {
                for (int i = 0; i < angularVelocitySampleCount; i++)
                {
                    angularVelocity += angularVelocitySamples[i];
                }
                angularVelocity *= (1.0f / angularVelocitySampleCount);
            }

            return angularVelocity;
        }


        //-------------------------------------------------
        public Vector3 GetAccelerationEstimate()
        {
            Vector3 average = Vector3.zero;
            for (int i = 2 + sampleCount - velocitySamples.Length; i < sampleCount; i++)
            {
                if (i < 2)
                    continue;

                int first = i - 2;
                int second = i - 1;

                Vector3 v1 = velocitySamples[first % velocitySamples.Length];
                Vector3 v2 = velocitySamples[second % velocitySamples.Length];
                average += v2 - v1;
            }
            average *= (1.0f / Time.deltaTime);
            return average;
        }

        //-------------------------------------------------
        private IEnumerator EstimateVelocityCoroutine()
        {
            sampleCount = 0;

            Vector3 previousPosition = transform.position;
            Quaternion previousRotation = transform.rotation;
            while (true)
            {
                yield return new WaitForEndOfFrame();

                float velocityFactor = 1.0f / Time.deltaTime;

                int v = sampleCount % velocitySamples.Length;
                int w = sampleCount % angularVelocitySamples.Length;
                sampleCount++;

                // Estimate linear velocity
                velocitySamples[v] = velocityFactor * (transform.position - previousPosition);

                // Estimate angular velocity
                Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(previousRotation);

                float theta = 2.0f * Mathf.Acos(Mathf.Clamp(deltaRotation.w, -1.0f, 1.0f));
                if (theta > Mathf.PI)
                {
                    theta -= 2.0f * Mathf.PI;
                }

                Vector3 angularVelocity = new Vector3(deltaRotation.x, deltaRotation.y, deltaRotation.z);
                if (angularVelocity.sqrMagnitude > 0.0f)
                {
                    angularVelocity = theta * velocityFactor * angularVelocity.normalized;
                }

                angularVelocitySamples[w] = angularVelocity;

                previousPosition = transform.position;
                previousRotation = transform.rotation;
            }
        }

        #endregion

    }
}