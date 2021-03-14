using System;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Player.View;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.UI
{
    [AddComponentMenu("WEAVR/VR/Interactions/Generic Menu")]
    public class VR_GenericMenu : VR_AbstractMenu
    {
        protected enum TriggerMode { Always, OnNothingHovered, OnNothingAttached }
        [SerializeField]
        protected Selectable m_defaultSelectable;

        [Header("Menu")]
        [SerializeField]
        protected bool m_disablePointer = true;
        [SerializeField]
        protected bool m_disableTeleport = true;
        [SerializeField]
        protected bool m_swapModels = true;
        [Space]
        [SerializeField]
        protected TriggerMode m_triggerMode = TriggerMode.OnNothingHovered;
        [SerializeField]
        protected VR_ControllerAction m_toggleButton;
        [SerializeField]
        protected VR_ControllerAction m_submitButton;
        [SerializeField]
        protected VR_ControllerAxis m_menuNavigation;

        [Space]
        [SerializeField]
        private Events m_events;

#if WEAVR_VR

        public VR_Skeleton_Poser m_menuPose = null;

        public event Action<VR_GenericMenu, Hand> OnBeforeShow;
        public event Action<VR_GenericMenu, Hand> OnShow;
        public event Action<VR_GenericMenu, Hand> OnBeforeHide;
        public event Action<VR_GenericMenu, Hand> OnHide;


        protected Selectable m_currentSelectable;
        protected PointerEventData m_eventData;
        protected VR_Hand m_hand;

        protected bool m_useController;

        protected Selectable CurrentSelectable
        {
            get
            {
                if (m_currentSelectable == null)
                {
                    m_currentSelectable = m_defaultSelectable ?? m_canvas.GetComponentInChildren<Selectable>();
                }
                return m_currentSelectable;
            }
        }

        protected virtual void Reset()
        {
            m_toggleButton.mAction = VR_ControllerAction.ActionType.Menu;
            m_submitButton.mAction = VR_ControllerAction.ActionType.TrackPadPressed;
        }

        // Use this for initialization
        protected override void Start()
        {
            base.Start();

            m_hand = GetComponentInParent<VR_Hand>();
            m_toggleButton.Initialize(m_hand);
            m_submitButton.Initialize(m_hand);
            m_menuNavigation.Initialize(m_hand);

            m_eventData = new PointerEventData(EventSystem.current);
            IsVisible = m_canvas != null && m_canvas.gameObject.activeInHierarchy;

            //ProcedureSceneVRUI procedureSceneVRUI = ProcedureSceneVRUI.Instance;

            //if (procedureSceneVRUI.VRCanvas != null && procedureSceneVRUI.VRParentUI != null)
            //{
            //    m_canvas = procedureSceneVRUI.VRCanvas;
            //    m_canvasWorldPoint = procedureSceneVRUI.VRParentUI.transform;
            //    if(m_menuPose == null && procedureSceneVRUI.CanvasMenuPoser != null)
            //    {
            //        m_menuPose = procedureSceneVRUI.CanvasMenuPoser.GetComponent<VR_Skeleton_Poser>();
            //    }
            //}
        }



        protected virtual void Update()
        {
            if (m_hand == null || !m_isActive || !CanToggle()) { return; }

            if (m_isVisible && m_useController)
            {
                if (CurrentSelectable == null || m_toggleButton.IsTriggered())
                {
                    if (m_isVisible) Hide();
                    else
                    {
                        //var genericMenu = m_canvas?.GetComponentInParent<VR_GenericMenu>();
                        //if (genericMenu != null && genericMenu != this)
                        //{
                        //    genericMenu.Hide();
                        //}
                        Show(m_hand.transform);
                    }
                    return;
                }

                if (!m_canvas.transform.IsChildOf(transform) || !m_canvas.gameObject.activeInHierarchy)
                {
                    m_isVisible = false;
                    RestoreModels();
                    return;
                }

                VR_Hand otherHand = m_hand.GetOtherHand();

                if(otherHand != null)
                {
                    return;
                }

                m_eventData.scrollDelta.Set(0, 0);

                if (m_menuNavigation.IsTriggered())
                {
                    m_eventData.scrollDelta = m_menuNavigation.GetScrollDelta();
                    ExecuteEvents.ExecuteHierarchy(m_canvas.gameObject, m_eventData, ExecuteEvents.scrollHandler);
                    Selectable nextSelectable = GetNextSelectable();
                    if (nextSelectable != null && nextSelectable.transform.IsChildOf(m_canvas.transform))
                    {
                        if (m_eventData.pointerEnter != nextSelectable.gameObject)
                        {
                            ExecuteEvents.Execute(m_eventData.pointerEnter, m_eventData, ExecuteEvents.pointerExitHandler);
                        }
                        nextSelectable.Select();
                        m_currentSelectable = nextSelectable;
                        return;
                    }
                }
                else if (m_submitButton.IsPressDown())
                {
                    m_eventData.Reset();
                    m_eventData.selectedObject = m_eventData.pointerEnter = m_eventData.pointerPress = CurrentSelectable.gameObject;
                    m_eventData.button = 0;
                    m_eventData.clickTime = Time.time;
                    m_eventData.eligibleForClick = true;
                    //if (m_eventData.hovered == null)
                    //{
                    //    m_eventData.hovered = new List<GameObject>();
                    //}
                    //else
                    //{
                    //    m_eventData.hovered.Clear();
                    //}
                    //m_eventData.hovered.Add(CurrentSelectable.gameObject);
                    if (ExecuteEvents.Execute(CurrentSelectable.gameObject, m_eventData, ExecuteEvents.pointerEnterHandler)
                    || ExecuteEvents.Execute(CurrentSelectable.gameObject, m_eventData, ExecuteEvents.pointerDownHandler))
                    {
                        //return;
                    };
                }
                else if (m_submitButton.IsPress())
                {
                    ExecuteEvents.Execute(CurrentSelectable.gameObject, m_eventData, ExecuteEvents.pointerDownHandler);
                }
                else if (m_submitButton.IsTriggered() && m_eventData.pointerEnter == CurrentSelectable.gameObject)
                {

                    if (ExecuteEvents.Execute(CurrentSelectable.gameObject, m_eventData, ExecuteEvents.pointerClickHandler)
                        || ExecuteEvents.Execute(CurrentSelectable.gameObject, m_eventData, ExecuteEvents.pointerUpHandler)
                        || ExecuteEvents.Execute(CurrentSelectable.gameObject, m_eventData, ExecuteEvents.submitHandler))
                    {
                        ExecuteEvents.Execute(CurrentSelectable.gameObject, m_eventData, ExecuteEvents.pointerExitHandler);
                        return;
                    }
                }
            }
            else if (m_isVisible && (!m_canvas.transform.IsChildOf(transform) || !m_canvas.gameObject.activeInHierarchy))
            {
                m_isVisible = false;
                RestoreModels();
                return;
            };

            if (m_toggleButton.IsTriggered())
            {
                if (m_isVisible) Hide();
                else
                {
                    // First check if other menu was used
                    //var genericMenu = m_canvas?.GetComponentInParent<VR_GenericMenu>();
                    //if(genericMenu != null && genericMenu != this)
                    //{
                    //    genericMenu.Hide();
                    //}
                    Show(m_hand.transform);
                }
            }

        }

        private Selectable GetNextSelectable()
        {
            //Selectable nextSelectable = CurrentSelectable.FindSelectable(m_menuNavigation.GetScrollDelta());
            //Debug.Log($"Moving to {nextSelectable} with {m_menuNavigation.GetScrollDelta()}");
            if (CurrentSelectable == null) { return null; }
            Selectable nextSelectable = null;
            var direction = m_menuNavigation.GetDirection();
            if (direction == VR_ControllerAxis.SwipeDirection.None) { return null; }
            if (direction.HasFlag(VR_ControllerAxis.SwipeDirection.Left))
            {
                nextSelectable = CurrentSelectable.FindSelectableOnLeft();
            }
            if (nextSelectable == null && direction.HasFlag(VR_ControllerAxis.SwipeDirection.Up))
            {
                nextSelectable = CurrentSelectable.FindSelectableOnUp();
            }
            if (nextSelectable == null && direction.HasFlag(VR_ControllerAxis.SwipeDirection.Right))
            {
                nextSelectable = CurrentSelectable.FindSelectableOnRight();
            }
            if (nextSelectable == null && direction.HasFlag(VR_ControllerAxis.SwipeDirection.Down))
            {
                nextSelectable = CurrentSelectable.FindSelectableOnDown();
            }

            return nextSelectable;
        }

        protected virtual bool CanToggle()
        {
            switch (m_triggerMode)
            {
                case TriggerMode.OnNothingAttached:
                    return m_hand.currentAttachedObject == null
                        || m_hand.currentAttachedObject.GetComponent<InteractionController>() == null;
                case TriggerMode.OnNothingHovered:
                    return m_hand.hoveringInteractable == null;
                default:
                    return true;
            }
        }

        public override void Show(Transform point = null)
        {
            m_events.OnBeforeShow.Invoke();
            OnBeforeShow?.Invoke(this, m_hand);

            //m_canvas.gameObject.SetActive(true);
            base.Show(point);

            m_useController = true;
            if (m_disablePointer)
            {
                var pointer = GetComponentInParent<WorldPointer>();
                if (pointer != null)
                {
                    pointer.enabled = false;
                }
            }
            m_hand.IsMenuHand = true;
            VR_Hand otherHand = m_hand.GetOtherHand();
           /* if (m_swapModels && otherHand != null
                && (otherHand.currentAttachedObject == null
                || otherHand.currentAttachedObject.GetComponent<InteractionController>() == null))
            {
                otherHand.EnterMenuMode(m_menuPose);
                
            }
*/
            if (EventSystem.current.currentSelectedGameObject == null
                || !EventSystem.current.currentSelectedGameObject.transform.IsChildOf(m_canvas.transform))
            {
                CurrentSelectable?.Select();
            }

            m_events.OnShow.Invoke();
            OnShow?.Invoke(this, m_hand);
        }

        public override void Hide()
        {
            m_events.OnBeforeHide.Invoke();
            OnBeforeHide?.Invoke(this, m_hand);

            RestoreModels();

            m_useController = true;

            base.Hide();

            if (m_disablePointer)
            {
                var pointer = GetComponentInParent<WorldPointer>();
                if (pointer != null)
                {
                    pointer.enabled = true;
                }
            }

            m_events.OnHide.Invoke();
            OnHide?.Invoke(this, m_hand);

        }

        private void RestoreModels()
        {
            if (m_swapModels && m_hand != null)
            {
                VR_Hand otherHand = m_hand.GetOtherHand();
                if (otherHand != null)
                {
                    otherHand.ExitMenuMode();
                   
                }
                m_hand.IsMenuHand = false;
            }
        }

#endif

        [Serializable]
        private class Events
        {
            public UnityEvent OnBeforeShow;
            public UnityEvent OnShow;
            public UnityEvent OnBeforeHide;
            public UnityEvent OnHide;
        }
    }
}
