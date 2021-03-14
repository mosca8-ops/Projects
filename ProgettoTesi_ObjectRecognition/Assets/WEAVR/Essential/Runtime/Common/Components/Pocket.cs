using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Interaction;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/Pocket")]
    public class Pocket : MonoBehaviour
    {
        private const int k_BufferSize = 16;
        public enum PocketMode { FirstInFirstOut, LastInFirstOut }
        
        [SerializeField]
        protected LayerMask m_layerMask;
        [SerializeField]
        protected PocketMode m_pocketMode = PocketMode.LastInFirstOut;
        [SerializeField]
        protected BoxCollider m_collider;
        [SerializeField]
        protected bool m_automaticPocketIn;
        [SerializeField]
        protected GameObject[] m_whiteList;

        [Space]
        [SerializeField]
        [Range(1, 10)]
        protected int m_slots = 1;
        [SerializeField]
        protected OptionalFloat m_snapTime = 2;
        [SerializeField]
        protected Transform m_snapPoint;

        [Space]
        [SerializeField]
        protected Animator m_animator;
        [SerializeField]
        [HiddenBy(nameof(m_animator))]
        protected string m_onNormalTrigger = "OnNormal";
        [SerializeField]
        [HiddenBy(nameof(m_animator))]
        protected string m_onHoverTrigger = "OnHover";
        [SerializeField]
        [HiddenBy(nameof(m_animator))]
        protected string m_onPocketInTrigger = "PocketIn";
        [SerializeField]
        [HiddenBy(nameof(m_animator))]
        protected string m_onPocketOutTrigger = "PocketOut";

        [Space]
        [SerializeField]
        protected Events m_events;

        public UnityEventGameObject OnPockedInEvent => m_events.OnPocketedIn;
        public UnityEventGameObject OnPockedOutEvent => m_events.OnPocketedOut;
        public UnityEventGameObject OnInRangeEvent => m_events.IsInRange;
        public UnityEventGameObject OnOutOfRangeEvent => m_events.IsOutOfRange;


        private List<GameObject> m_pocketObjects = new List<GameObject>();
        public IReadOnlyList<GameObject> PocketedObjects => m_pocketObjects;
        public bool HasObjects => m_pocketObjects.Count > 0;

        private List<LockTarget> m_lockTargets = new List<LockTarget>();

        private List<AbstractInteractionController> m_lastInRange = new List<AbstractInteractionController>();
        private List<AbstractInteractionController> m_currentInRange = new List<AbstractInteractionController>();

        private Collider[] m_castColliders = new Collider[k_BufferSize];

        protected virtual void Reset()
        {
            m_layerMask = LayerMask.NameToLayer("Default");
            m_collider = GetComponentInChildren<BoxCollider>();
            if (!m_collider)
            {
                m_collider = gameObject.AddComponent<BoxCollider>();
            }
        }

        protected virtual void OnValidate()
        {
            if (!m_snapPoint)
            {
                m_snapPoint = transform;
            }
        }

        protected virtual void Start()
        {
            if (!m_snapPoint)
            {
                m_snapPoint = transform;
            }

            int colliders = PerformPhysicsTest();

            if (colliders > 0)
            {
                for (int i = 0; i < colliders; i++)
                {
                    var interactionController = m_castColliders[i].GetComponentInParent<AbstractInteractionController>();
                    if (interactionController && CanPocketIn(interactionController.gameObject))
                    {
                        PocketIn(interactionController.gameObject);
                    }
                }
            }
        }

        protected virtual void OnNewObjInRange(GameObject go)
        {

        }

        protected virtual void OnObjOutOfRange(GameObject go)
        {

        }

        protected void ResetAnimatorTriggers()
        {
            if (m_animator)
            {
                m_animator.ResetTrigger(m_onNormalTrigger);
                m_animator.ResetTrigger(m_onHoverTrigger);
                m_animator.ResetTrigger(m_onPocketInTrigger);
                m_animator.ResetTrigger(m_onPocketOutTrigger);
            }
        }

        protected void AnimatorOnNormal()
        {
            if (m_animator) { m_animator.SetTrigger(m_onNormalTrigger); }
        }

        protected void AnimatorOnHover()
        {
            if (m_animator) { m_animator.SetTrigger(m_onHoverTrigger); }
        }

        protected void AnimatorOnPocketIn()
        {
            if (m_animator) { m_animator.SetTrigger(m_onPocketInTrigger); }
        }

        protected void AnimatorOnPocketOut()
        {
            if (m_animator) { m_animator.SetTrigger(m_onPocketOutTrigger); }
        }

        protected virtual void Update()
        {
            int colliders = PerformPhysicsTest();

            if (colliders > 0)
            {
                m_currentInRange.Clear();

                bool wasHovered = m_lastInRange.Count > 0;

                for (int i = 0; i < colliders; i++)
                {
                    var interactionController = m_castColliders[i].GetComponentInParent<AbstractInteractionController>();
                    if (interactionController && CanPocketIn(interactionController.gameObject))
                    {
                        m_currentInRange.Add(interactionController);
                        if (!m_lastInRange.Contains(interactionController))
                        {
                            m_lastInRange.Add(interactionController);
                            OnInRangeEvent?.Invoke(interactionController.gameObject);
                            OnNewObjInRange(interactionController.gameObject);
                        }
                        if (m_automaticPocketIn)
                        {
                            PocketIn(interactionController.gameObject);
                        }
                    }
                }

                for (int i = 0; i < m_lastInRange.Count; i++)
                {
                    if (!m_currentInRange.Contains(m_lastInRange[i]))
                    {
                        if (!IsInPocket(m_lastInRange[i].gameObject))
                        {
                            OnOutOfRangeEvent?.Invoke(m_lastInRange[i].gameObject);
                            OnObjOutOfRange(m_lastInRange[i].gameObject);
                        }
                        m_lastInRange.RemoveAt(i--);
                    }
                }

                if (!wasHovered && m_lastInRange.Count > 0)
                {
                    //ResetAnimatorTriggers();
                    AnimatorOnHover();
                }
                else if (wasHovered && m_lastInRange.Count == 0)
                {
                    // Not hovered anymore
                    //ResetAnimatorTriggers();
                    AnimatorOnNormal();
                }

                if (m_automaticPocketIn)
                {
                    for (int i = 0; i < m_pocketObjects.Count; i++)
                    {
                        bool remove = true;
                        for (int j = 0; j < colliders; j++)
                        {
                            if (m_castColliders[j].transform.IsChildOf(m_pocketObjects[i].transform))
                            {
                                remove = false;
                                break;
                            }
                        }

                        if (remove && PocketOut(m_pocketObjects[i]))
                        {
                            i--;
                        }
                    }
                }
            }
        }

        private int PerformPhysicsTest()
        {
            if (m_collider.enabled)
            {
                return Physics.OverlapBoxNonAlloc(m_collider.transform.TransformPoint(m_collider.center), Vector3.Scale(m_collider.size * 0.5f, m_collider.transform.lossyScale), m_castColliders, transform.rotation, m_layerMask);
            }
            m_collider.enabled = true;
            int colliders = Physics.OverlapBoxNonAlloc(m_collider.transform.TransformPoint(m_collider.center), Vector3.Scale(m_collider.size * 0.5f, m_collider.transform.lossyScale), m_castColliders, transform.rotation, m_layerMask);
            m_collider.enabled = false;
            return colliders;
        }

        public bool IsInRange(GameObject go)
        {
            int colliders = PerformPhysicsTest();
            for (int i = 0; i < colliders; i++)
            {
                if (m_castColliders[i].transform.IsChildOf(go.transform))
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanPocketIn(GameObject go)
        {
            return PocketedObjects.Count < m_slots && !go.transform.IsChildOf(transform) && !IsInPocket(go) && (m_whiteList.Length == 0 || m_whiteList.Any(g => go.transform.IsChildOf(g.transform)));
        }

        public bool IsInPocket(GameObject go) => PocketedObjects.Any(s => go.transform.IsChildOf(s.transform));

        public virtual bool PocketIn(GameObject go)
        {
            var controller = go.GetComponentInParent<AbstractInteractionController>();
            if (controller && CanPocketIn(go))
            {
                for (int i = 0; i < m_pocketObjects.Count; i++)
                {
                    if (m_pocketObjects[i].transform.IsChildOf(go.transform))
                    {
                        UnregisterFromLock(m_pocketObjects[i]);
                        m_pocketObjects.RemoveAt(i--);
                    }
                }
                RegisterForLock(controller);
                m_pocketObjects.Add(go);
                OnPockedInEvent?.Invoke(go);

                ResetAnimatorTriggers();
                AnimatorOnPocketIn();
                return true;
            }
            return false;
        }

        public virtual bool PocketOut(GameObject go)
        {
            if (PocketedObjects.Contains(go))
            {
                UnregisterFromLock(go);
            }
            var key = PocketedObjects.FirstOrDefault(s => go.transform.IsChildOf(s.transform));
            if (key)
            {
                bool removed = m_pocketObjects.Remove(key);
                if (removed)
                {
                    OnPockedOutEvent?.Invoke(go);
                    ResetAnimatorTriggers();
                    AnimatorOnPocketOut();
                    return removed;
                }
            }
            return false;
        }

        public virtual GameObject PocketOutFirst()
        {
            var first = m_pocketObjects.Count > 0 ? m_pocketObjects[m_pocketMode == PocketMode.FirstInFirstOut ? 0 : m_pocketObjects.Count - 1] : null;
            return first && PocketOut(first) ? first : null;
        }

        protected virtual void FixedUpdate()
        {
            if (m_snapTime.enabled)
            {
                for (int i = 0; i < m_lockTargets.Count; i++)
                {
                    LockInteractable(m_lockTargets[i]);
                }
            }
        }

        protected void RegisterForLock(AbstractInteractionController controller)
        {
            if(m_lockTargets.Any(t => t.transform == controller.transform))
            {
                return;
            }
            
            if (controller)
            {
                var body = controller.GetComponent<Rigidbody>();
                if (body)
                {
                    m_lockTargets.Add(new LockTarget()
                    {
                        controller = controller,
                        body = body,
                        transform = body.transform,
                        dropTimer = -1
                    });
                }
            }
        }

        protected void UnregisterFromLock(GameObject obj)
        {
            for (int i = 0; i < m_lockTargets.Count; i++)
            {
                if(m_lockTargets[i].transform == obj.transform)
                {
                    m_lockTargets.RemoveAt(i--);
                }
            }
        }

        protected void LockInteractable(LockTarget lockTarget)
        {
            bool used = lockTarget.controller && lockTarget.controller.CurrentBehaviour;

            if (used)
            {
                lockTarget.body.isKinematic = false;
                lockTarget.dropTimer = -1;
            }
            else
            {
                lockTarget.dropTimer += Time.deltaTime / (m_snapTime / 2);

                lockTarget.body.isKinematic = lockTarget.dropTimer > 1;

                if (lockTarget.dropTimer > 1)
                {
                    //transform.parent = snapTo;
                    lockTarget.transform.position = m_snapPoint.position;
                    lockTarget.transform.rotation = m_snapPoint.rotation;
                }
                else
                {
                    float t = Mathf.Pow(35, lockTarget.dropTimer);

                    lockTarget.body.velocity = Vector3.Lerp(lockTarget.body.velocity, Vector3.zero, Time.fixedDeltaTime * 4);
                    if (lockTarget.body.useGravity)
                        lockTarget.body.AddForce(-Physics.gravity);

                    lockTarget.transform.position = Vector3.Lerp(lockTarget.transform.position, m_snapPoint.position, Time.fixedDeltaTime * t * 3);
                    lockTarget.transform.rotation = Quaternion.Slerp(lockTarget.transform.rotation, m_snapPoint.rotation, Time.fixedDeltaTime * t * 2);
                }
            }
        }

        protected class LockTarget
        {
            public AbstractInteractionController controller;
            public Transform transform;
            public Rigidbody body;
            public float dropTimer;
        }

        [Serializable]
        protected struct Events {
            public UnityEventGameObject OnPocketedIn;
            public UnityEventGameObject OnPocketedOut;
            public UnityEventGameObject IsInRange;
            public UnityEventGameObject IsOutOfRange;
        }
    }
}
