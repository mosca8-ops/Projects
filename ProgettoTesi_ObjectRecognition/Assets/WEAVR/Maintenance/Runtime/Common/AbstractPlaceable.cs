namespace TXT.WEAVR.Maintenance
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Interaction;
    using UnityEngine;

    public abstract class AbstractPlaceable : AbstractInteractiveBehaviour
    {
        [Header("Placing")]
        [SerializeField]
        [Draggable]
        protected List<AbstractPlaceManager> m_placeManagers;
        [Draggable]
        public PlacePoint placePoint;
        [Tooltip("Places this object to a free place point when it is not connected or not grabbed")]
        public bool autoPlace = true;
        [DisabledBy(nameof(autoPlace))]
        public bool autoPlaceWhenReleased = true;
        [DisabledBy(nameof(autoPlace))]
        public bool autoPlaceIfInRadius = true;
        [SerializeField]
        [Draggable]
        [CanBeGenerated]
        protected Transform m_localPlacePoint;
        [Space]
        [Tooltip("If true, the object will be placed instantly")]
        public bool instantPlacement = false;
        [Tooltip("The velocity of the placement animation, in [m/s]")]
        [DisabledBy("instantPlacement", disableWhenTrue: true)]
        public float placementSpeed = 1;

        private float m_placementTime;

        [SerializeField]
        protected InputObjectClassArray m_validPlacements;

        [Space]
        public UnityEventGameObject onPlaced;

        public Transform PlacementOffset => m_localPlacePoint;

        private AbstractConnectable[] m_connectables;
        protected AbstractGrabbable m_grabbable;
        private bool m_isConnected;

        public List<AbstractPlaceManager> PlaceManagers => m_placeManagers;
        public InputObjectClassArray ValidPlacements => m_validPlacements;

        public bool IsPlaced {
            get { return placePoint && placePoint.placedObject == gameObject; }
        }

        public override bool CanInteract(ObjectsBag currentBag)
        {
            return base.CanInteract(currentBag) && CanBePlaced();
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return "Place";
        }

        public override void Interact(ObjectsBag currentBag)
        {
            PlaceItself();
        }

        void Start()
        {
            StartGrabbable();

            m_connectables = GetComponents<AbstractConnectable>();

            foreach (var connectable in m_connectables)
            {
                connectable.ConnectionChanged -= Connectable_ConnectionChanged;
                connectable.ConnectionChanged += Connectable_ConnectionChanged;
            }
        }

        internal void SetPlacement(PlacePoint placePoint)
        {
            if (!placePoint || this.placePoint == placePoint) { return; }
            if (this.placePoint && this.placePoint.placedObject == gameObject)
            {
                this.placePoint.placedObject = null;
            }
            this.placePoint = placePoint;
            onPlaced?.Invoke(gameObject);
        }

        protected abstract void StartGrabbable();

        private void Connectable_ConnectionChanged(AbstractConnectable source, AbstractInteractiveBehaviour previous, AbstractInteractiveBehaviour current, AbstractConnectable otherConnectable)
        {
            TryReturnToItsPlace();
        }

        protected void TryReturnToItsPlace()
        {
            if(Time.time < m_placementTime) { return; }

            m_isConnected = IsConnected();

            if (!m_isConnected && m_placeManagers != null 
                && !placePoint && autoPlace && ((autoPlaceWhenReleased && !m_grabbable.IsGrabbed) || !m_grabbable))
            {
                PlaceItself();
            }
        }

        internal void RemovePlacement(PlacePoint placePoint)
        {
            if(this.placePoint == placePoint)
            {
                this.placePoint = null;
            }
        }

        public void PlaceItself()
        {
            int index = 0;
            if (m_localPlacePoint)
            {
                while (index < m_placeManagers.Count
                    && m_placeManagers[index++].PlaceObject(gameObject, m_localPlacePoint, instantPlacement ? 0 : placementSpeed)) ;
            }
            else
            {
                while (index < m_placeManagers.Count
                    && m_placeManagers[index++].PlaceObject(gameObject, instantPlacement ? 0 : placementSpeed)) ;
            }

            m_placementTime = Time.time + (instantPlacement ? 0.1f : placementSpeed);
            if (m_grabbable && m_grabbable.IsGrabbed)
            {
                m_grabbable.Release();
            }
            Controller.CurrentBehaviour = this;
        }

        public bool CanBePlaced()
        {
            return m_placeManagers.Count > 0 && (!placePoint || placePoint.placedObject != gameObject);
        }

        public bool CanBePlaced(PlacePoint slot)
        {
            return m_placeManagers?.Contains(slot.Manager) == true && (!placePoint || placePoint.placedObject != gameObject);
        }

        public void PlaceItself(PlacePoint slot)
        {
            if (!slot || !slot.IsFree) { return; }
            if (m_localPlacePoint)
            {
                slot.PlaceObject(gameObject, m_localPlacePoint, instantPlacement ? 0 : placementSpeed);
            }
            else
            {
                slot.PlaceObject(gameObject, instantPlacement ? 0 : placementSpeed);
            }

            m_placementTime = Time.time + (instantPlacement ? 0.1f : placementSpeed);
            if (m_grabbable && m_grabbable.IsGrabbed)
            {
                m_grabbable.Release();
            }
            Controller.CurrentBehaviour = this;
        }

        private bool IsConnected()
        {
            foreach (var connectable in m_connectables)
            {
                if (connectable.IsConnected || connectable.IsConnecting)
                {
                    return true;
                }
            }
            return false;
        }
    }
}