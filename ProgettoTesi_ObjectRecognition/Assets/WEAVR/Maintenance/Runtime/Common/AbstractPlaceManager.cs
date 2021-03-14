namespace TXT.WEAVR.Maintenance
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Interaction;
    using UnityEngine;

    [Serializable]
    public abstract class AbstractPlaceManager : AbstractInteractiveBehaviour
    {
        [Space]
        public int slotRows = 2;
        public int slotColumns = 2;

        public bool animatePlacing = true;

        [SerializeField]
        [Draggable]
        public List<PlacePoint> slots = new List<PlacePoint>();

        private void Start()
        {
            foreach(var slot in slots)
            {
                slot.Manager = this;
            }
        }

        public override bool CanInteract(ObjectsBag currentBag) {
            return currentBag.Selected != null && HasFreeSlots();
        }

        public override string GetInteractionName(ObjectsBag currentBag) {
            return "Place";
        }

        public override void Interact(ObjectsBag currentBag) {
            if(currentBag == null || currentBag.Selected == null) {
                EndInteraction();
                return;
            }
            var objectToPlace = currentBag.Selected;
            var controller = objectToPlace.GetComponent<AbstractInteractionController>();
            if(controller != null && controller.CurrentBehaviour != null)
            {
                controller.CurrentBehaviour.EndInteraction();
                var placeable = controller.GetComponent<AbstractPlaceable>();
                if(placeable != null)
                {
                    placeable.PlaceItself(GetFreeSlot());
                    return;
                }
            }
            PlaceObject(objectToPlace);
        }

        public void PlaceObject(GameObject objectToPlace) {
            foreach (var slot in slots) {
                if (slot.IsFree) {
                    slot.PlaceObject(objectToPlace, 1);
                    Controller.CurrentBehaviour = this;
                    return;
                }
            }
        }

        public bool PlaceObject(GameObject objectToPlace, float placementVelocity) {
            foreach (var slot in slots) {
                if (slot.IsFree) {
                    slot.PlaceObject(objectToPlace, placementVelocity);
                    Controller.CurrentBehaviour = this;
                    return true;
                }
            }
            return false;
        }

        public bool PlaceObject(GameObject objectToPlace, Transform localPlacePoint, float placementVelocity)
        {
            foreach (var slot in slots)
            {
                if (slot.IsFree)
                {
                    slot.PlaceObject(objectToPlace, localPlacePoint, placementVelocity);
                    Controller.CurrentBehaviour = this;
                    return true;
                }
            }
            return false;
        }

        public PlacePoint GetFreeSlot()
        {
            foreach (var slot in slots)
            {
                if (slot.IsFree)
                {
                    return slot;
                }
            }
            return null;
        }

        public static PlacePoint GetFreeSlot(IEnumerable<AbstractPlaceManager> placeManagers)
        {
            foreach(var manager in placeManagers)
            {
                foreach (var slot in manager.slots)
                {
                    if (slot.IsFree)
                    {
                        return slot;
                    }
                }
            }
            return null;
        }

        public override bool CanBeDefault => true;

        public override bool UseStandardVRInteraction(ObjectsBag bag, object hand)
        {
            var placeable = bag.Selected != null ? bag.Selected.GetComponent<AbstractPlaceable>() : null;
            return placeable != null && placeable.enabled && placeable.IsInteractive;
        }

        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return CanInteract(bag);
        }

        public override void InteractVR(ObjectsBag bag, object hand)
        {
            Interact(bag);
        }

        public bool HasFreeSlots() {
            foreach(var slot in slots) {
                if (slot.IsFree) { return true; }
            }
            return false;
        }
    }
}
