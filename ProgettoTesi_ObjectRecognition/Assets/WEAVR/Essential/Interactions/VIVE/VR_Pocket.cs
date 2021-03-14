using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Maintenance;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
#if WEAVR_VR
using Valve.VR;
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/VR/Interactions/Pocket")]
    public class VR_Pocket : Pocket
    {
        public OptionalInt hapticDurationInMs = 20;
        public UnityEvent OnEmptyHandIn;
        public UnityEvent OnEmptyHandOut;

#if WEAVR_VR

        private Dictionary<GameObject, Interactable.OnDetachedFromHandDelegate> m_detachedEvents = new Dictionary<GameObject, Interactable.OnDetachedFromHandDelegate>();
        private Dictionary<GameObject, Interactable.OnAttachedToHandDelegate> m_attachedEvents = new Dictionary<GameObject, Interactable.OnAttachedToHandDelegate>();
        private Dictionary<GameObject, ParentConstraint> m_constraints = new Dictionary<GameObject, ParentConstraint>();

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnNewObjInRange(GameObject go)
        {
            base.OnNewObjInRange(go);
            if (!m_detachedEvents.ContainsKey(go) || !m_attachedEvents.ContainsKey(go))
            {
                var vrObj = go.GetComponentInParent<VR_Object>();
                if (vrObj)
                {
                    if (!m_detachedEvents.TryGetValue(go, out Interactable.OnDetachedFromHandDelegate onDetachedAction))
                    {
                        onDetachedAction = h => TryPocketIn(h, go);
                        vrObj.onDetachedFromHand -= onDetachedAction;
                        vrObj.onDetachedFromHand += onDetachedAction;
                        m_detachedEvents[go] = onDetachedAction;
                    }

                    if (!m_attachedEvents.TryGetValue(go, out Interactable.OnAttachedToHandDelegate onAttachedAction))
                    {
                        onAttachedAction = h => TryPocketOut(h, go);
                        vrObj.onAttachedToHand -= onAttachedAction;
                        vrObj.onAttachedToHand += onAttachedAction;
                        m_attachedEvents[go] = onAttachedAction;
                    }

                    if (vrObj.attachedToHand && hapticDurationInMs.enabled)
                    {
                        vrObj.attachedToHand.TriggerHapticPulse((ushort)(hapticDurationInMs.value * 1000));
                    }
                }
                else if(PocketedObjects.Count > 0)
                {
                    var vr_hand = go.GetComponentInParent<VR_Hand>();
                    if (vr_hand)
                    {
                        OnEmptyHandIn?.Invoke();
                        if (hapticDurationInMs.enabled)
                        {
                            vr_hand.TriggerHapticPulse((ushort)(hapticDurationInMs.value * 1000));
                        }
                    }
                }
            }
        }

        protected override void OnObjOutOfRange(GameObject go)
        {
            base.OnObjOutOfRange(go);
            if (!PocketedObjects.Contains(go) && (m_attachedEvents.ContainsKey(go) || m_detachedEvents.ContainsKey(go)))
            {
                var vrObj = go.GetComponentInParent<VR_Object>();
                if (vrObj)
                {
                    RemoveEvents(go, vrObj);
                }
            }
            else
            {
                var vr_hand = go.GetComponentInParent<VR_Hand>();
                if (vr_hand)
                {
                    OnEmptyHandOut?.Invoke();
                    if (hapticDurationInMs.enabled)
                    {
                        vr_hand.TriggerHapticPulse((ushort)(hapticDurationInMs.value * 1000));
                    }
                }
            }
        }

        private void TryPocketOut(Hand hand, GameObject go)
        {
            if (PocketOut(go))
            {
                if(m_constraints.TryGetValue(go, out ParentConstraint constraint))
                {
                    Destroy(constraint);
                    m_constraints.Remove(go);
                }
            }
        }

        public override bool PocketOut(GameObject go)
        {
            if (base.PocketOut(go))
            {
                var vrObj = go.GetComponentInParent<VR_Object>();
                if (vrObj)
                {
                    RemoveEvents(go, vrObj);
                }
                return true;
            }
            return false;
        }

        private void RemoveEvents(GameObject go, VR_Object vrObj)
        {
            if (m_detachedEvents.TryGetValue(go, out Interactable.OnDetachedFromHandDelegate detachedAction))
            {
                vrObj.onDetachedFromHand -= detachedAction;
                m_detachedEvents.Remove(go);
            }
            if (m_attachedEvents.TryGetValue(go, out Interactable.OnAttachedToHandDelegate attachedAction))
            {
                vrObj.onAttachedToHand -= attachedAction;
                m_attachedEvents.Remove(go);
            }
        }

        private void TryPocketIn(Hand hand, GameObject go)
        {
            if (IsInRange(go) && PocketIn(go))
            {
                //if(!m_constraints.TryGetValue(go, out ParentConstraint constraint))
                //{
                //    constraint = go.AddComponent<ParentConstraint>();
                //    m_constraints[go] = constraint;
                //}
                //constraint.locked = false;
                //while(constraint.sourceCount > 0)
                //{
                //    constraint.RemoveSource(0);
                //}
                //constraint.AddSource(new ConstraintSource()
                //{
                //    sourceTransform = m_snapPoint,
                //    weight = 0.9f
                //});
                //constraint.constraintActive = true;
                //constraint.locked = true;
                //go.transform.SetParent(m_snapPoint, true);
            }
        }

#endif
    }
}
