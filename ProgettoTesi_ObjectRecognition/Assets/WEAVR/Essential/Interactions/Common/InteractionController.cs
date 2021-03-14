namespace TXT.WEAVR.Interaction
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Interaction;
    using TXT.WEAVR.Maintenance;
    using TXT.WEAVR.UI;
    using UnityEngine;

#if WEAVR_VR
    using Valve.VR.InteractionSystem;
#endif

    [DoNotExpose]
    [RequireComponent(typeof(VR_Object))]
    [SelectionBase]
    [AddComponentMenu("WEAVR/Interactions/Interactions Controller")]
    public class InteractionController : AbstractInteractionController
    {
        [SerializeField]
        [HideInInspector]
        private VR_Object m_vrObject;

        private void OnValidate() {
            AddVRObjectIfNeeded();
        }

        private void Reset() {
            AddVRObjectIfNeeded();
        }

        private void AddVRObjectIfNeeded() {
            if (m_vrObject == null) {
                m_vrObject = GetComponent<VR_Object>();
            }
            if (m_vrObject == null) {
                m_vrObject = gameObject.AddComponent<VR_Object>();
            }
            if(m_vrObject != null)
            {
                m_vrObject.enabled = enabled;
            }
            if (GetComponentInChildren<Collider>() == null)
            {
                var renderers = GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    foreach(var rend in renderers)
                    {
                        rend.gameObject.AddComponent<BoxCollider>();
                    }
                }
                else
                {
                    gameObject.AddComponent<BoxCollider>();
                }
            }
        }



        protected virtual void Awake()
        {
            AddVRObjectIfNeeded();
#if WEAVR_VR
            UpdateColliders();
            if (enabled)
            {
                RemoveIgnoreHovering();
            }
            else
            {
                AddIgnoreHovering();
            }
#endif
        }


        protected override void OnDisable()
        {
#if WEAVR_VR
            if (HoveringHand != null)
            {
                ControllerButtonHints.HideAllButtonHints(HoveringHand);
                ControllerButtonHints.HideAllTextHints(HoveringHand);
            }
            GetComponent<VR_Object>().enabled = false;
            AddIgnoreHovering();
            
#endif
            base.OnDisable();
        }

#if WEAVR_VR
        
        private AbstractInteractiveBehaviour m_stdInteractionBehaviour;
        private Hand m_hoveringHand;
        private Hand m_attachedHand;
        private List<Collider> m_colliders;
        [IgnoreStateSerialization]
        private bool m_interactionDisabled = false;

        public Hand HoveringHand {
            get { return m_hoveringHand; }
            private set {
                if(m_hoveringHand != value)
                {
                    //if (m_hoveringHand != null)
                    //{
                    //    BagHolder.Main.Bag.GetHand(m_hoveringHand)?.MakeActive(false);
                    //}
                    m_hoveringHand = value;
                    //if(m_hoveringHand != null)
                    //{
                    //    BagHolder.Main.Bag.GetHand(m_hoveringHand)?.MakeActive(true);
                    //}
                }
            }
        }

        public Hand AttachedHand
        {
            get { return m_attachedHand; }
            set
            {
                if(m_attachedHand != value)
                {
                    if(m_attachedHand != null && m_attachedHand.currentAttachedObject == gameObject)
                    {
                        m_attachedHand.DetachObject(gameObject);
                    }
                    m_attachedHand = value;
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AttachedHand = null;
            var commandMenu = (AttachedHand ?? HoveringHand)?.GetComponent<VR_CommandMenu>();
            if (commandMenu != null)
            {
                commandMenu.IsActive = false;
                commandMenu.OnHide -= CommandMenu_OnHide;
                commandMenu.OnBeforeShow -= CommandMenu_OnBeforeShow;
            }
        }

        private void UpdateColliders()
        {
            m_colliders = new List<Collider>();
            Collider wCollider = transform.GetComponent<Collider>();
            if (wCollider != null)
            {
                m_colliders.Add(wCollider);
            }
            foreach (Transform wTransfowm in transform)
            {
                RetrieveHandledColliders(wTransfowm, ref m_colliders);
            }
        }

        protected virtual void OnEnable()
        {
            var vrObject = GetComponent<VR_Object>();
            if(vrObject != null)
            {
                vrObject.enabled = true;
                RemoveIgnoreHovering();
            }
        }


        private void AddIgnoreHovering(Hand iHand = null)
        {
            UpdateColliders();
            m_interactionDisabled = true;
            foreach (Collider wCollider in m_colliders)
            {
                IgnoreHovering wIgnoreHovering = wCollider.gameObject.GetComponent<IgnoreHovering>();
                if (wIgnoreHovering == null)
                {
                    wIgnoreHovering = wCollider.gameObject.AddComponent<IgnoreHovering>();
                }
                if (wIgnoreHovering != null)
                {
                    wIgnoreHovering.enabled = true;
                    wIgnoreHovering.onlyIgnoreHand = iHand;
                }
            }
        }


        private void RetrieveHandledColliders(Transform iTransform, ref List<Collider> oColliders)
        { 
            InteractionController wInteractionController = iTransform.GetComponent<InteractionController>();
            VR_Object wVR_Object = iTransform.GetComponent<VR_Object>();
            //This GO do not have an interactable, it is handled by "this"
            if (wInteractionController == null && wVR_Object == null)
            {
                Collider wCollider = iTransform.GetComponent<Collider>();
                if (wCollider)
                {
                    //it has a collider, must be disabled/enabled by this
                    oColliders.Add(wCollider);
                    foreach (Transform wCurTransform in iTransform)
                    {
                        //iterate all childs
                        RetrieveHandledColliders(wCurTransform, ref oColliders);
                    }
                }
            }
        }

        private void RemoveIgnoreHovering()
        {
            UpdateColliders();
            if (m_interactionDisabled)
            {
                m_interactionDisabled = false;
                foreach (Collider wCollider in m_colliders)
                {
                    IgnoreHovering wIgnoreHovering = wCollider.gameObject.GetComponent<IgnoreHovering>();
                    if (wIgnoreHovering != null)
                    {
                        Destroy(wIgnoreHovering);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Trying to remove ignore hovering when not present, something went wrong...");
            }
        }

        public bool IsVRInteractable(Hand hand)
        {
            if (!enabled)
            {
                return false;
            }
            PushHand(hand);
            if (TemporaryMainBehaviour != null && TemporaryMainBehaviour.enabled && TemporaryMainBehaviour.CanInteractVR(bagHolder.Bag, hand))
            {
                PopHand(hand);
                return true;
            }
            else if (m_defaultBehaviour != null && m_defaultBehaviour.enabled && m_defaultBehaviour.CanInteractVR(bagHolder.Bag, hand))
            {
                PopHand(hand);
                return true;
            }

            foreach (var behaviour in m_behaviours)
            {
                if (behaviour.enabled
                    && behaviour.IsInteractive
                    && behaviour.CanInteractVR(bagHolder.Bag, hand))
                {
                    PopHand(hand);
                    return true;
                }
            }
            PopHand(hand);
            return false;
        }

        public  AbstractInteractiveBehaviour GetCurrentBehaviourVR()
        {
            if (enabled)
            {
                if (m_defaultBehaviour != null && m_defaultBehaviour.enabled)
                {
                    return m_defaultBehaviour;   
                }
                else
                {
                    foreach (AbstractInteractiveBehaviour wCurBehaviour in m_behaviours)
                    {
                        if (wCurBehaviour && wCurBehaviour.enabled)
                        {
                            return wCurBehaviour;
                        }
                    }
                }
            }
            return null;
        }

        private void UpdateInteractiveBehavioursVR(Hand hand)
        {
            m_activeBehaviours.Clear();
            PushHand(hand);
            if (TemporaryMainBehaviour != null 
                && TemporaryMainBehaviour.enabled
                && TemporaryMainBehaviour.CanInteractVR(bagHolder.Bag, hand)
                && TemporaryMainBehaviour.CanInteract(bagHolder.Bag))
            {
                m_stdInteractionBehaviour = TemporaryMainBehaviour;
            }
            else if (m_defaultBehaviour != null 
                && m_defaultBehaviour.enabled 
                && m_defaultBehaviour.CanInteract(bagHolder.Bag)
                && m_defaultBehaviour.CanInteractVR(bagHolder.Bag, hand))
            {
                m_stdInteractionBehaviour = m_defaultBehaviour;
            }
            else
            {
                m_stdInteractionBehaviour = null;
            }

            foreach (var behaviour in m_behaviours)
            {
                if (behaviour.enabled 
                    && behaviour.IsInteractive 
                    && behaviour.CanInteract(bagHolder.Bag) 
                    && behaviour.CanInteractVR(bagHolder.Bag, hand))
                {
                    m_activeBehaviours.Add(behaviour);
                    if (m_stdInteractionBehaviour == null 
                        && behaviour.UseStandardVRInteraction(bagHolder.Bag, hand))
                    {
                        m_stdInteractionBehaviour = behaviour;
                    }
                }
            }
            PopHand(hand);
        }

        //-------------------------------------------------
        // Called when a Hand starts hovering over this object
        //-------------------------------------------------
        private void OnHandHoverBegin(Hand hand)
        {
            if (!enabled) { return; }
            HoveringHand = hand;
            if (hand.currentAttachedObject == gameObject) { return; }

            //ControllerButtonHints.ShowTextHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger, "Grab");
            UpdateInteractiveBehavioursVR(hand);

            if (m_activeBehaviours.Count > 0)
            {
                // If already holding trigger down, cannot have another default action

                //if (m_stdInteractionBehaviour != null &&
                //    (hand.GetStandardInteractionButtonDown() || ((hand.controller != null) && hand.controller.GetHairTrigger())))
                //{
                //    m_stdInteractionBehaviour = null;
                //}
                ShowHints(hand);
                //ShowMenu(hand);
                if (WeavrManager.ShowInteractionHighlights)
                {
                    Outliner.Outline(gameObject, hoverColor, false);
                }

                var commandMenu = hand.GetComponent<VR_CommandMenu>();
                if(commandMenu != null)
                {
                    commandMenu.IsActive = true;
                    commandMenu.OnHide -= CommandMenu_OnHide;
                    commandMenu.OnHide += CommandMenu_OnHide;
                    commandMenu.OnBeforeShow -= CommandMenu_OnBeforeShow;
                    commandMenu.OnBeforeShow += CommandMenu_OnBeforeShow;
                }
            }
        }

        private void CommandMenu_OnHide(VR_GenericMenu commandMenu, Hand hand)
        {
            if(gameObject == null)
            {
                commandMenu.OnHide -= CommandMenu_OnHide;
                commandMenu.OnBeforeShow -= CommandMenu_OnBeforeShow;
                return;
            }
            Outliner.RemoveOutline(gameObject, hoverColor);
            Outliner.RemoveOutline(gameObject, selectColor);
        }

        private void CommandMenu_OnBeforeShow(VR_GenericMenu commandMenu, Hand hand)
        {
            UpdateMenu((VR_CommandMenu)commandMenu, hand);
        }

        private void ShowHints(Hand hand)
        {
            if (WeavrManager.ShowHints)
            {
                if (m_stdInteractionBehaviour)
                {
                    PushHand(hand);
                    ControllerButtonHints.ShowTextHint(hand,
                                                       hand.grabGripAction,
                                                       m_stdInteractionBehaviour.GetInteractionName(bagHolder.Bag));
                    PopHand(hand);
                }
            }
            else
            {
                ControllerButtonHints.ShowButtonHint(hand, hand.uiInteractAction);
                if (m_stdInteractionBehaviour)
                {
                    ControllerButtonHints.ShowButtonHint(hand, hand.grabGripAction);
                }
            }
        }

        private void ShowMenu(Hand hand)
        {
            if (WeavrManager.ShowHints)
            {
                //ControllerButtonHints.ShowTextHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger, "Execute Command");
                if (m_activeBehaviours.Count > 1)
                {
                    ControllerButtonHints.ShowTextHint(hand, hand.uiInteractAction, "Scroll/Select Action");
                }
                else
                {
                    ControllerButtonHints.ShowTextHint(hand, hand.uiInteractAction, "Select Action");
                }
            }
            else
            {
                ControllerButtonHints.ShowButtonHint(hand, hand.uiInteractAction);
            }

            var commandMenu = hand.GetComponent<VR_CommandMenu>();
            if (commandMenu != null)
            {
                UpdateMenu(commandMenu, hand);
                commandMenu.Show();
            }
            else
            {
                ContextMenu3D.Begin();
                foreach (var behaviour in m_activeBehaviours)
                {
                    ContextMenu3D.Instance.AddMenuItem(behaviour.GetInteractionName(bagHolder.Bag), () => DoBehaviourInteractionVR(behaviour, hand));
                }
                ContextMenu3D.Instance.Show(hand.transform, true, true);
            }
        }

        private void UpdateMenu(VR_CommandMenu commandMenu, Hand hand)
        {
            commandMenu.BeginMenu();
            foreach (var behaviour in m_activeBehaviours)
            {
                commandMenu.AddMenuItem(behaviour.GetInteractionName(bagHolder.Bag), () => DoBehaviourInteractionVR(behaviour, hand));
            }
        }

        private void DoBehaviourInteractionVR(AbstractInteractiveBehaviour behaviour, Hand hand)
        {
            PushHand(hand);
            CurrentBehaviour = behaviour;
            CurrentBehaviour.InteractVR(bagHolder.Bag, hand);
            PopHand(hand);
        }

        private void PopHand(Hand hand)
        {
            if (bagHolder) { bagHolder.Bag.RestoreActiveHand(hand); }
        }

        private void PushHand(Hand hand)
        {
            if (bagHolder) { bagHolder.Bag.SetActiveHand(hand); }
        }

        //-------------------------------------------------
        // Called when a Hand stops hovering over this object
        //-------------------------------------------------
        private void OnHandHoverEnd(Hand hand)
        {
            HoveringHand = null;
            if (!enabled) { return; }
            HideHints(hand);

            var commandMenu = hand.GetComponent<VR_CommandMenu>();
            if (commandMenu != null) {
                commandMenu.OnHide -= CommandMenu_OnHide;
                commandMenu.OnBeforeShow -= CommandMenu_OnBeforeShow;
                commandMenu.IsActive = false;
                commandMenu.Hide();
            }
            Outliner.RemoveOutline(gameObject, hoverColor);
            Outliner.RemoveOutline(gameObject, selectColor);
            m_activeBehaviours.Clear();
        }

        private void HideHints(Hand hand)
        {
            ControllerButtonHints.HideButtonHint(hand, hand.grabPinchAction);
            ControllerButtonHints.HideTextHint(hand, hand.grabPinchAction);
            ControllerButtonHints.HideTextHint(hand, hand.uiInteractAction);
            ControllerButtonHints.HideButtonHint(hand, hand.uiInteractAction);

            if (m_stdInteractionBehaviour != null)
            {
                ControllerButtonHints.HideTextHint(hand, hand.grabGripAction);
                ControllerButtonHints.HideButtonHint(hand, hand.grabGripAction);
            }
        }


        //-------------------------------------------------
        // Called every Update() while a Hand is hovering over this object
        //-------------------------------------------------
        private void HandHoverUpdate(Hand hand)
        {
            if (!enabled) { return; }

            // Check if default action
            if(m_stdInteractionBehaviour != null && m_stdInteractionBehaviour.enabled && m_stdInteractionBehaviour.CanInteractVR(bagHolder.Bag, hand))
            {
                if((m_stdInteractionBehaviour.InteractTrigger == BehaviourInteractionTrigger.OnPointerUp && IsTriggerUp(hand))
                    || (m_stdInteractionBehaviour.InteractTrigger == BehaviourInteractionTrigger.OnPointerDown && IsTriggerDown(hand)))
                {
                    StartBehaviourInteractionVR(hand);
                    return;
                }
            }
            //if (m_stdInteractionBehaviour != null &&
            //    (hand.GetStandardInteractionButtonDown() || ((hand.controller != null) && hand.controller.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))))
            //{
            //    StartBehaviourInteractionVR(hand);
            //    return;
            //}

            if (IsTriggerUp(hand))
            {
                HideHints(hand);
                UpdateInteractiveBehavioursVR(hand);
                ShowHints(hand);
                return;
            }

            //if (!commandMenu.IsVisible)
            //{
            //    if (m_activeBehaviours.Count > 0 && (hand.controller != null) && hand.controller.GetPressUp(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu))
            //    {
            //        ControllerButtonHints.HideTextHint(hand, Valve.VR.EVRButtonId.k_EButton_ApplicationMenu);
            //        ControllerButtonHints.HideButtonHint(hand, Valve.VR.EVRButtonId.k_EButton_ApplicationMenu);
            //        ShowMenu(hand);
            //    }
            //    return;
            //}

            //if ((hand.controller != null) && hand.controller.GetPressUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
            //{
            //    m_wasTouching = false;
            //    BorderOutliner.RemoveOutline(gameObject, hoverColor);
            //    BorderOutliner.RemoveOutline(gameObject, selectColor);
            //    commandMenu.TrySubmitSelected();
            //    //UpdateMenu(commandMenu);
            //    //if(m_activeBehaviours.Count > 0) { commandMenu.Show(); }
            //}

            //if (hand.controller != null)
            //{
            //    bool isCurrentlyTouching = hand.controller.GetTouch(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
            //    if (isCurrentlyTouching)
            //    {
            //        if (!m_wasTouching || commandMenu.TryTriggerMoveEvent(hand.controller.GetAxis() - m_lastAxisValue))
            //        {
            //            m_lastAxisValue = hand.controller.GetAxis();
            //        }
            //    }
            //    m_wasTouching = isCurrentlyTouching;
            //}
        }

        private void StartBehaviourInteractionVR(Hand hand)
        {
            ControllerButtonHints.HideTextHint(hand, hand.grabGripAction);
            ControllerButtonHints.HideButtonHint(hand, hand.grabGripAction);

            ValueChangerMenu.Instance?.Hide();
            ContextMenu3D.Instance?.Hide();

            //m_stdInteractionBehaviour.InteractVR(bagHolder.Bag, hand);
            DoBehaviourInteractionVR(m_stdInteractionBehaviour, hand);
            m_stdInteractionBehaviour = null;
        }

        private bool IsTriggerUp(Hand hand)
        {
            return VR_ControllerManager.GetStandardInteractionButtonUp(hand) ||
                   TXT.WEAVR.Interaction.VR_ControllerManager.getActionStateUp(TXT.WEAVR.Interaction.VR_ControllerAction.ActionType.TriggerPressed, hand);
        }

        private bool IsTriggerDown(Hand hand)
        {
            return VR_ControllerManager.GetStandardInteractionButtonDown(hand) ||
                   TXT.WEAVR.Interaction.VR_ControllerManager.getActionStateDown(TXT.WEAVR.Interaction.VR_ControllerAction.ActionType.TriggerPressed, hand);
        }

        private IEnumerator DelayedUpdateIgnoreHovering(Hand iHand)
        {
            yield return new WaitForSeconds(Time.fixedDeltaTime * 2.0f);
            UpdateIgnoreHovering(iHand);
        }

        private void UpdateIgnoreHovering(Hand iHand)
        {
            bool wAttachedHandVRInteractable = IsVRInteractable(iHand);
            bool wOtherHandVRInteractable = IsVRInteractable(iHand.otherHand);
            if (!wAttachedHandVRInteractable && !wOtherHandVRInteractable)
            {
                AddIgnoreHovering();
            }
            else if (!wAttachedHandVRInteractable)
            {
                AddIgnoreHovering(iHand);
            }
            else if (!wOtherHandVRInteractable)
            {
                AddIgnoreHovering(iHand.otherHand);
            }
            else
            {
                RemoveIgnoreHovering();
            }
        }
        //-------------------------------------------------
        // Called when this GameObject becomes attached to the hand
        //-------------------------------------------------
        private void OnAttachedToHand(Hand hand)
        {
            //textMesh.text = "Attached to hand: " + hand.name;
            //attachTime = Time.time;
            AttachedHand = hand;
            HideHints(hand);
            UpdateIgnoreHovering(hand);
        }


        //-------------------------------------------------
        // Called when this GameObject is detached from the hand
        //-------------------------------------------------
        private void OnDetachedFromHand(Hand hand)
        {
            m_attachedHand = null;
            //textMesh.text = "Detached from hand: " + hand.name;
            StartCoroutine(DelayedUpdateIgnoreHovering(hand));
        }


        ////-------------------------------------------------
        //// Called every Update() while this GameObject is attached to the hand
        ////-------------------------------------------------
        //private void HandAttachedUpdate(Hand hand)
        //{
        //    //textMesh.text = "Attached to hand: " + hand.name + "\nAttached time: " + (Time.time - attachTime).ToString("F2");
        //}


        ////-------------------------------------------------
        //// Called when this attached GameObject becomes the primary attached object
        ////-------------------------------------------------
        //private void OnHandFocusAcquired(Hand hand)
        //{
        //}


        ////-------------------------------------------------
        //// Called when another attached GameObject becomes the primary attached object
        ////-------------------------------------------------
        //private void OnHandFocusLost(Hand hand)
        //{
        //}

#endif
    }
}