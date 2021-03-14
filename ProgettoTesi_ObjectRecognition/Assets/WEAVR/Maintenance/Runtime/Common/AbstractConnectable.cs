namespace TXT.WEAVR.Maintenance
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Animation;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Interaction;
    using UnityEngine;
    using UnityEngine.Animations;
    using UnityEngine.Events;

    public abstract class AbstractConnectable : AbstractInteractiveBehaviour
    {
        public delegate void OnConnectionChanged(AbstractConnectable source, AbstractInteractiveBehaviour previous, AbstractInteractiveBehaviour current, AbstractConnectable otherConnectable);

        private float m_sqrConnectEpsilon = 0.000001f;
        public enum ConnectorType { Male, Female, Neutral }

        [Header("Initialization")]
        [Tooltip("Object to connect on start")]
        [Draggable]
        public AbstractInteractiveBehaviour startConnectedTo;

        [Header("Connector Related")]
        [Tooltip("The connector type, if the type is neutral, it can connect to any type of InteractiveBehaviour")]
        public ConnectorType connectorType = ConnectorType.Neutral;

        [Tooltip("If true, then this connector moves to other objects")]
        public bool activeConnector = true;
        [Tooltip("Whether to keep the object fixed when connected")]
        [DisabledBy(nameof(activeConnector))]
        public bool keepFixed = false;
        //[Tooltip("Whether to fix also the rotation")]
        //[HiddenBy(nameof(keepFixed))]
        //public bool keepRotationFixed = true;
        [Tooltip("Whether to set constraints when connected or not")]
        [HiddenBy(nameof(keepFixed))]
        public bool useConstraints = false;
        [HiddenBy(nameof(useConstraints))]
        public float contraintWeight = 0.9f;
        [HiddenBy(nameof(useConstraints))]
        public float breakForce = 20000;
        [Tooltip("Whether to allow rotation around an axis or make it completely rigid")]
        [HiddenBy(nameof(keepFixed))]
        public bool fixOnlyOneAxis = false;
        [Tooltip("The axis to rotate around")]
        [HiddenBy(nameof(keepFixed), nameof(fixOnlyOneAxis))]
        public Vector3 localAxisToFix = Vector3.forward;
        [Tooltip("When true, the connection will be instantaneous, otherwise an animation will be performed")]
        [HideInInspector]
        public bool instantConnection = false; // TODO
        [Space]
        [Tooltip("The speed of the connection animation [m/s]")]
        [HiddenBy("instantConnection", hiddenWhenTrue: true)]
        public float connectionSpeed = 1;
        [Tooltip("At which distance this connects to valid connectors")]
        public float connectDistance = 0.01f;
        [Draggable]
        [Tooltip("The point where this object connects to other connectors")]
        [SerializeField]
        [CanBeGenerated(nameof(_objectClass) + "." + nameof(Interaction.ObjectClass.type), FallbackName = "ConnectionPoint")]
        protected Transform m_connectionPoint;

        [SerializeField]
        protected InputObjectClassArray m_validConnectionClasses;

        [Header("Events")]
        public UnityEvent OnConnected;
        public UnityEvent OnDisconnected;

        public event OnConnectionChanged ConnectionChanged;

        private AbstractConnectable m_lastValidConnectable;

        private bool m_animateConnection;
        private Transform m_targetPoint;
        private Transform m_thisPoint;
        protected Rigidbody m_rigidBody;
        protected bool m_wasKinematic;

        private Vector3 m_fixedLocalPosition;
        private Quaternion m_fixedLocalRotation;

        private Joint m_joint;
        private PositionConstraint m_positionConstraint;
        private RotationConstraint m_rotationConstraint;

        private readonly Collider[] m_overlapping = new Collider[4];

        protected AbstractConnectable m_potentialConnectable;

        public AbstractConnectable PotentialConnectable => m_potentialConnectable;

        public Transform ConnectionPoint => m_connectionPoint != null ? m_connectionPoint : transform;

        [SerializeField]
        [ShowAsReadOnly]
        private AbstractInteractiveBehaviour m_connectedObject;
        public AbstractInteractiveBehaviour ConnectedObject
        {
            get
            {
                return m_connectedObject;
            }
            set
            {
                if (m_connectedObject != value)
                {
                    var previousConnected = m_connectedObject;
                    m_animateConnection = false;
                    m_targetPoint = null;
                    IsConnected = false;

                    if (value != null)
                    {
                        m_thisPoint = ConnectionPoint;
                        m_animateConnection = activeConnector;
                        m_lastValidConnectable = GetValidConnectable(value.gameObject);
                        if (m_lastValidConnectable != null)
                        {
                            m_targetPoint = m_lastValidConnectable.ConnectionPoint;
                        }
                        else
                        {
                            m_targetPoint = value.transform;
                        }
                        if (activeConnector)
                        {
                            gameObject.Animate(AnimationFactory.MoveAndRotate(m_targetPoint, m_thisPoint, instantConnection ? 0 : connectionSpeed),
                                            (g, d) => DestinationReached(m_lastValidConnectable ?? value));
                        }
                    }
                    else
                    {
                        m_connectedObject = null;
                        m_lastValidConnectable = null;
                        if (ConnectionChanged != null)
                        {
                            ConnectionChanged(this, previousConnected, null, null);
                        }
                        //OnDisconnected.Invoke();
                    }
                }
            }
        }

        private bool m_isConnected;
        public bool IsConnected
        {
            get
            {
                return m_isConnected;
            }
            set
            {
                if (m_isConnected != value)
                {
                    m_potentialConnectable = null;
                    m_isConnected = value;
                    if (ConnectedObject != null && ConnectedObject is AbstractConnectable)
                    {
                        ((AbstractConnectable)ConnectedObject).IsConnected = value;
                        float connDist = value ? Mathf.Min(connectDistance, ((AbstractConnectable)ConnectedObject).connectDistance) : connectDistance;
                        connDist = Mathf.Abs(connDist);
                        m_sqrConnectEpsilon = connDist * connDist;
                    }
                    if (value)
                    {
                        OnConnected.Invoke();
                        Controller.CurrentBehaviour = this;
                    }
                    else
                    {
                        m_sqrConnectEpsilon = connectDistance * connectDistance;
                        ConnectedObject = null;
                        OnDisconnected.Invoke();

                        IsDisconnectedInternal();
                    }

                    if (!m_isConnected)
                    {
                        EndInteraction();
                    }
                }
            }
        }

        protected abstract void IsDisconnectedInternal();

        public bool IsConnecting { get { return m_targetPoint != null; } }

        public override bool CanBeDefault => true;

        // Update is called once per frame
        void LateUpdate()
        {
            if (keepFixed && IsConnected && activeConnector)
            {
                if (m_rigidBody != null)
                {
                    m_rigidBody.isKinematic = true;
                }
                //transform.localPosition = m_fixedLocalPosition;
                //transform.localRotation = m_fixedLocalRotation;
            }
            else if (m_targetPoint == null)
            {
                IsConnected = false;
            }
            else if (IsConnected && !keepFixed && (m_targetPoint.position - m_thisPoint.position).sqrMagnitude > m_sqrConnectEpsilon)
            {
                IsConnected = false;
            }
             
        }

        private void OnEnable()
        {
            if (m_thisPoint != null && keepFixed && !IsConnected && activeConnector && startConnectedTo != null 
                && Vector3.Distance(ConnectionPoint.position, GetValidConnectionPoint(startConnectedTo).position) < m_sqrConnectEpsilon)
            {
                TryConnectToStartingConnector();
            }
        }

        private void DestinationReached(AbstractInteractiveBehaviour connectingObject)
        {
            if (keepFixed && activeConnector)
            {
                if (m_rigidBody != null)
                {
                    m_wasKinematic = m_rigidBody.isKinematic;
                    m_rigidBody.isKinematic = true;
                }
                m_fixedLocalPosition = transform.localPosition;
                m_fixedLocalRotation = transform.localRotation;
            }
            var previousConnected = m_connectedObject;
            m_connectedObject = connectingObject;
            IsConnected = true;
            if (m_lastValidConnectable != null)
            {
                m_lastValidConnectable.IsConnected = true;
            }

            if (ConnectionChanged != null)
            {
                ConnectionChanged(this, previousConnected, m_connectedObject, m_lastValidConnectable);
            }
            //OnConnected.Invoke();

            if (connectingObject is AbstractConnectable)
            {
                ((AbstractConnectable)connectingObject).m_connectedObject = this;
                ((AbstractConnectable)connectingObject).m_targetPoint = ConnectionPoint;
            }
        }

        private void OnValidate()
        {
            connectionSpeed = Mathf.Clamp(connectionSpeed, 0.01f, 10f);
            if (startConnectedTo != null && startConnectedTo is AbstractConnectable)
            {
                ((AbstractConnectable)startConnectedTo).startConnectedTo = this;
            }
            connectDistance = Mathf.Abs(connectDistance);
            m_sqrConnectEpsilon = connectDistance * connectDistance;
        }

        public virtual bool CanConnect(AbstractInteractiveBehaviour other)
        {
            return other != null && connectorType == ConnectorType.Neutral && m_validConnectionClasses.HasInputClass(other.ObjectClass);
        }

        public virtual bool CanConnect(AbstractConnectable other)
        {
            if (other != null && ((other.connectorType == ConnectorType.Male && connectorType == ConnectorType.Female)
                   || (other.connectorType == ConnectorType.Female && connectorType == ConnectorType.Male)
                   || (other.connectorType == ConnectorType.Neutral || connectorType == ConnectorType.Neutral))
                   //&& m_validConnectionClasses.HasInputClass(other.ObjectClass) 
                   && other.m_validConnectionClasses.HasInputClass(ObjectClass))
            {
                m_potentialConnectable = other;
                return true;
            }
            return false;
        }

        public virtual bool CanConnect(GameObject other)
        {
            return other && other != this && (CanConnect(other.GetComponent<AbstractConnectable>()) || CanConnect(other.GetComponent<AbstractInteractiveBehaviour>()));
        }

        [ExposeMethod]
        public virtual bool Connect(AbstractInteractiveBehaviour other)
        {
            if (CanConnect(other))
            {
                ConnectedObject = other;
                return true;
            }
            return false;
        }

        [ExposeMethod]
        public virtual bool Connect(AbstractConnectable other)
        {
            if (CanConnect(other))
            {
                if (other.activeConnector)
                {
                    other.ConnectedObject = this;
                }
                else
                {
                    ConnectedObject = other;
                }
                //other.ConnectedObject = this;
                return true;
            }
            return false;
        }

        [ExposeMethod]
        public virtual void Disconnect()
        {
            IsConnected = false;
        }

        public virtual bool CheckEnvironmentForConnectables(bool connectOnSight)
        {
            if (!IsConnected)
            {
                int colliders = Physics.OverlapSphereNonAlloc(ConnectionPoint.position, connectDistance, m_overlapping);
                for (int i = 0; i < colliders; i++)
                {
                    var connectable = m_overlapping[i].GetComponent<AbstractConnectable>();
                    if ((connectOnSight && Connect(connectable)) || CanConnect(connectable))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected virtual void Start()
        {
            connectDistance = Mathf.Abs(connectDistance);
            m_sqrConnectEpsilon = connectDistance * connectDistance;
            m_rigidBody = GetComponent<Rigidbody>();
            m_thisPoint = ConnectionPoint;

            TryConnectToStartingConnector();
        }

        private void TryConnectToStartingConnector()
        {
            if (startConnectedTo != null && startConnectedTo.gameObject.activeInHierarchy)
            {
                var validConnector = GetValidConnectable(startConnectedTo.gameObject) ?? startConnectedTo;
                if (validConnector != null)
                {
                    m_targetPoint = GetValidConnectionPoint(validConnector);
                    gameObject.Animate(AnimationFactory.MoveAndRotate(m_targetPoint, m_thisPoint, 0), (g, d) => DestinationReached(validConnector));
                }
            }
        }

        private Transform GetValidConnectionPoint(AbstractInteractiveBehaviour behaviour)
        {
            return (behaviour is AbstractConnectable) ?
                                    ((AbstractConnectable)behaviour).ConnectionPoint : behaviour.transform;
        }

        public override bool CanInteract(ObjectsBag currentBag)
        {
            if (!base.CanInteract(currentBag)) { return false; }
            if (IsConnected)
            {
                return currentBag.Selected == null;
            }
            if (currentBag.Selected == null)
            {
                return false;
            }
            var connectables = currentBag.Selected.GetComponents<AbstractConnectable>();
            foreach (var connectable in connectables)
            {
                if (CanConnect(connectable))
                {
                    return true;
                }
            }
            var interactive = currentBag.Selected.GetComponent<AbstractInteractiveBehaviour>();
            return interactive != null && CanConnect(interactive);
        }

        protected override void StopInteraction(AbstractInteractiveBehaviour nextBehaviour)
        {
            if (Controller.enabled)
            {
                base.StopInteraction(nextBehaviour);
                IsConnected = false;
            }
        }

        public override void Interact(ObjectsBag currentBag)
        {
            if (IsConnected)
            {
                Disconnect();
                return;
            }
            if (currentBag.Selected == null)
            {
                //currentBag.Selected = gameObject;
                return;
            }
            var connectables = currentBag.Selected.GetComponents<AbstractConnectable>();
            foreach (var connectable in connectables)
            {
                if (CanConnect(connectable))
                {
                    Connect(connectable);
                    return;
                }
            }
            var interactive = currentBag.Selected.GetComponent<AbstractInteractiveBehaviour>();
            if (interactive != null)
            {
                Connect(interactive);
            }
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            if (IsConnected)
            {
                return "Disconnect " + ConnectedObject.ObjectClass.type;
            }
            else
            {
                var otherBehaviour = currentBag.Selected.GetComponent<AbstractInteractiveBehaviour>();
                return "Connect " + (otherBehaviour != null ? otherBehaviour.ObjectClass.type : currentBag.Selected.name);
            }
        }

        protected AbstractConnectable GetValidConnectable(GameObject go)
        {
            foreach (var connectable in go.GetComponents<AbstractConnectable>())
            {
                if (CanConnect(connectable))
                {
                    return connectable;
                }
            }
            return null;
        }

    }
}
