namespace TXT.WEAVR.Interaction
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.InputControls;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [SelectionBase]
    public abstract class AbstractInteractionController : MonoBehaviour, 
        IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public BagHolder bagHolder;
        [Tooltip("Show menu even if there is only one action available")]
        public bool alwaysShowMenu = true;
        public Color hoverColor = Color.green;
        public Color selectColor = Color.cyan;

        [SerializeField]
        protected List<AbstractInteractiveBehaviour> m_behaviours;
        [HideInInspector]
        [SerializeField]
        protected List<AbstractInteractiveBehaviour> m_activeBehaviours;

        [SerializeField]
        [HideInInspector]
        protected AbstractInteractiveBehaviour m_defaultBehaviour;
        
        [NonSerialized]
        private bool m_shouldUpdateBehaviours = true;

        private AbstractInteractiveBehaviour m_currentlyActiveBehaviour;
        private Coroutine m_activeBehaviourCoroutine;

        public AbstractInteractiveBehaviour CurrentlyActiveNonVRBehaviour
        {
            get => m_currentlyActiveBehaviour;
            set
            {
                if(m_currentlyActiveBehaviour != value)
                {
                    if (m_currentlyActiveBehaviour)
                    {
                        m_currentlyActiveBehaviour.EndInteraction(value);
                        if(m_activeBehaviourCoroutine != null)
                        {
                            StopCoroutine(m_activeBehaviourCoroutine);
                        }
                        m_activeBehaviourCoroutine = null;
                    }
                    m_currentlyActiveBehaviour = value;
                    CurrentBehaviour = value;
                    if (m_currentlyActiveBehaviour)
                    {
                        m_currentlyActiveBehaviour.Interact(bagHolder.Bag);
                        if (m_currentlyActiveBehaviour.RequiresContinuousInteractionCallback)
                        {
                            m_activeBehaviourCoroutine = StartCoroutine(ContinuousInteraction(m_currentlyActiveBehaviour));
                        }
                    }
                }
            }
        }
        
        public AbstractInteractiveBehaviour TemporaryMainBehaviour { get; set; }

        protected AbstractInteractiveBehaviour m_previousBehaviour;
        protected AbstractInteractiveBehaviour m_currentBehaviour;

        private Coroutine m_clearBehavioursCoroutine;

        [NonSerialized]
        private IInteractionHoverListener[] m_hoverListeners;

        public AbstractInteractiveBehaviour DefaultBehaviour {
            get {
                return m_defaultBehaviour;
            }
            set {
                if (m_defaultBehaviour == null)
                {
                    m_defaultBehaviour = value;
                }
            }
        }

        public AbstractInteractiveBehaviour PreviousBehaviour => m_previousBehaviour;
        public AbstractInteractiveBehaviour CurrentBehaviour {
            get { return m_currentBehaviour; }
            set {
                if (m_currentBehaviour != value)
                {
                    if (m_currentBehaviour != null && !m_currentBehaviour.IsPersistent)
                    {
                        m_currentBehaviour.Stopped -= InteractiveBehaviour_Stopped;
                        m_currentBehaviour.EndInteraction(value);
                    }
                    m_previousBehaviour = m_currentBehaviour;
                    m_currentBehaviour = value;
                    if (m_currentBehaviour != null)
                    {
                        m_currentBehaviour.Stopped -= InteractiveBehaviour_Stopped;
                        m_currentBehaviour.Stopped += InteractiveBehaviour_Stopped;
                    }
                }
            }
        }

        private void InteractiveBehaviour_Stopped(AbstractInteractiveBehaviour obj)
        {
            if (obj == CurrentBehaviour)
            {
                m_currentBehaviour.Stopped -= InteractiveBehaviour_Stopped;
                m_currentBehaviour.EndInteraction(null);
                m_currentBehaviour = null;
            }
            if(obj == CurrentlyActiveNonVRBehaviour)
            {
                CurrentlyActiveNonVRBehaviour = null;
            }
        }

        public void StopCurrentInteraction()
        {
            if (m_currentBehaviour != null)
            {
                m_currentBehaviour.EndInteraction(null);
            }
        }

        private IEnumerator ContinuousInteraction(AbstractInteractiveBehaviour behaviour)
        {
            while (behaviour && behaviour.CanInteract(bagHolder.Bag))
            {
                behaviour.Interact(bagHolder.Bag);
                yield return null;
            }
        }

        public void UpdateList(bool forced = false)
        {
            if (m_behaviours == null)
            {
                m_behaviours = new List<AbstractInteractiveBehaviour>();
            }
            // We need to wait for components destruction
            if (forced)
            {
                m_behaviours.Clear();
            }

            var components = new List<AbstractInteractiveBehaviour>(GetComponents<AbstractInteractiveBehaviour>());
            foreach (var component in components)
            {
                if (component != null && !m_behaviours.Contains(component))
                {
                    m_behaviours.Add(component);
                }
            }
            for (int i = 0; i < m_behaviours.Count; i++)
            {
                if (m_behaviours[i] == null || !components.Contains(m_behaviours[i]))
                {
                    m_behaviours.RemoveAt(i--);
                }
            }
            //if (m_defaultBehaviour == null || !m_behaviours.Contains(m_defaultBehaviour))
            //{
            //    m_defaultBehaviour = m_behaviours.FirstOrDefault(b => b.GetType().Name == "AbstractGrabbable");
            //}
        }

        public void RemoveFromList(AbstractInteractiveBehaviour behaviour)
        {
            m_behaviours.Remove(behaviour);
            m_defaultBehaviour = behaviour == m_defaultBehaviour ? null : m_defaultBehaviour;
        }
        
        public void SyncClassTypes()
        {
            ObjectClass? lastType = null;
            List<AbstractInteractiveBehaviour> toSync = new List<AbstractInteractiveBehaviour>();
            foreach (var behaviour in m_behaviours)
            {
                if (behaviour == null) { continue; }
                if (string.IsNullOrEmpty(behaviour.ObjectClass.type))
                {
                    toSync.Add(behaviour);
                }
                else
                {
                    lastType = behaviour.ObjectClass;
                }
            }
            if (!lastType.HasValue) { return; }
            foreach (var behaviour in toSync)
            {
                behaviour.ObjectClass = lastType.Value;
            }
        }

        private void DoBehaviourInteraction(AbstractInteractiveBehaviour behaviour)
        {
            CurrentBehaviour = behaviour;
            CurrentBehaviour.Interact(bagHolder.Bag);
        }

        private IEnumerator DelayedUpdate()
        {
            yield return new WaitForEndOfFrame();
            m_behaviours = new List<AbstractInteractiveBehaviour>();
            foreach (var component in GetComponents<AbstractInteractiveBehaviour>())
            {
                if (component != null)
                {
                    m_behaviours.Add(component);
                }
            }
        }

        private void OnValidate()
        {
            if (bagHolder == null)
            {
                bagHolder = BagHolder.Main;
            }
            UpdateList(false);
            //PropagateLayer(true);
        }

        private void PropagateLayer(bool forced)
        {
            int layerId = LayerMask.NameToLayer(Weavr.InteractionLayer);
            if(forced || gameObject.layer != layerId)
            {
                PropagateLayer(gameObject.transform, layerId);
            }
        }

        private void PropagateLayer(Transform t, int layerId)
        {
            t.gameObject.layer = layerId;
            for (int i = 0; i < t.childCount; i++)
            {
                PropagateLayer(t.GetChild(i), layerId);
            }
        }

        private void Reset()
        {
            // Get whether there are colliders or not
            if (GetComponentInChildren<Collider>(true) == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }
            if (m_behaviours == null)
            {
                m_behaviours = new List<AbstractInteractiveBehaviour>();
            }
            UpdateList();
            //PropagateLayer(true);
        }

        protected virtual void Start()
        {
            if (bagHolder == null)
            {
                bagHolder = BagHolder.Main;
            }
            if(m_hoverListeners == null)
            {
                m_hoverListeners = GetComponents<IInteractionHoverListener>();
            }
            //PropagateLayer(false);
        }

        protected virtual void OnDisable()
        {
            m_activeBehaviours.Clear();
            m_shouldUpdateBehaviours = true;
            CurrentBehaviour = null;
            foreach (var behaviour in m_behaviours)
            {
                if (behaviour.enabled) { behaviour.OnDisableInteraction(); }
            }
            Outliner.RemoveOutline(gameObject, hoverColor);
            Outliner.RemoveOutline(gameObject, selectColor);
        }

        protected virtual void OnDestroy()
        {
            OnDisable();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == 0)
            {
                if (m_shouldUpdateBehaviours)
                {
                    UpdateInteractiveBehaviours();
                }
                if (m_activeBehaviours.Count > 0)
                {
                    if (m_activeBehaviours.Count == 1 && !alwaysShowMenu && m_activeBehaviours[0].InteractTrigger == BehaviourInteractionTrigger.OnPointerDown)
                    {
                        CurrentlyActiveNonVRBehaviour = m_activeBehaviours[0];
                    }
                    ContextMenu3D.Instance.Hide();
                    if (WeavrManager.ShowInteractionHighlights)
                    {
                        Outliner.Outline(gameObject, selectColor, false);
                    }
                    eventData.Use();
                    return;
                }
            }

            ClearBehaviours();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != 0 || m_activeBehaviours.Count == 0)
            {
                return;
            }
            if (m_clearBehavioursCoroutine != null)
            {
                StopCoroutine(m_clearBehavioursCoroutine);
            }

            if (CurrentlyActiveNonVRBehaviour == this)
            {
                CurrentlyActiveNonVRBehaviour = null;
                Outliner.RemoveOutline(gameObject, selectColor);
            }
            else if (m_activeBehaviours.Count > 1 || alwaysShowMenu)
            {
                ContextMenu3D.Begin();
                foreach (var behaviour in m_activeBehaviours)
                {
                    ContextMenu3D.Instance.AddMenuItem(behaviour.GetInteractionName(bagHolder.Bag),
                                                        () => DoBehaviourInteraction(behaviour));
                }
                ContextMenu3D.Instance.Show(transform, true, onHideCallback: () => Outliner.RemoveOutline(gameObject, selectColor));
            }
            else if (m_activeBehaviours.Count == 1)
            {
                ContextMenu3D.Instance.Hide();
                DoBehaviourInteraction(m_activeBehaviours[0]);
                Outliner.RemoveOutline(gameObject, selectColor);
            }
            ClearBehaviours(false);
            eventData.Use();
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.used) { return; }

            if (m_shouldUpdateBehaviours)
            {
                UpdateInteractiveBehaviours();
            }

            if (m_activeBehaviours.Count > 0 && WeavrManager.ShowInteractionHighlights)
            {
                Outliner.Outline(gameObject, hoverColor, false);
            }
        }

        protected void HoverEnter()
        {
            if(m_hoverListeners != null && m_hoverListeners.Length > 0)
            {
                for (int i = 0; i < m_hoverListeners.Length; i++)
                {
                    m_hoverListeners[i].OnHoverEnter(this);
                }
            }
            else
            {
                Outliner.Outline(gameObject, hoverColor, false);
            }
        }

        protected void HoverExit()
        {
            if (m_hoverListeners != null && m_hoverListeners.Length > 0)
            {
                for (int i = 0; i < m_hoverListeners.Length; i++)
                {
                    m_hoverListeners[i].OnHoverExit(this);
                }
            }
            else
            {
                Outliner.RemoveOutline(gameObject, hoverColor);
            }
        }


        public void OnPointerExit(PointerEventData eventData)
        {
            if (CurrentlyActiveNonVRBehaviour)
            {
                CurrentlyActiveNonVRBehaviour = null;
            }
            if (eventData.used){ return; }
            if (Input.touchCount > 0 && eventData.delta.sqrMagnitude < 0.01f)
            {
                m_clearBehavioursCoroutine = StartCoroutine(DelayedClearBehaviours(2f));
            }
            else 
            {
                ClearBehaviours();
            }
            eventData.Use();
        }

        private IEnumerator DelayedClearBehaviours(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClearBehaviours();
            m_clearBehavioursCoroutine = null;
        }

        private void ClearBehaviours(bool hideSelection = true)
        {
            Outliner.RemoveOutline(gameObject, hoverColor);
            if (hideSelection)
            {
                Outliner.RemoveOutline(gameObject, selectColor);
            }
            m_activeBehaviours.Clear();
            m_shouldUpdateBehaviours = true;
        }

        
        private void UpdateInteractiveBehaviours()
        {
            m_activeBehaviours.Clear();
            foreach (var behaviour in m_behaviours)
            {
                if (behaviour.IsInteractive && behaviour.CanInteract(bagHolder.Bag))
                {
                    m_activeBehaviours.Add(behaviour);
                }
            }
            m_shouldUpdateBehaviours = false;
        }

        public bool Has<T>(bool includeNonInteractive = true)
        {
            foreach (var behaviour in m_behaviours)
            {
                if (behaviour is T && (includeNonInteractive || behaviour.IsInteractive))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
