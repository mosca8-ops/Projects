namespace TXT.WEAVR.Maintenance {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Interaction;
    using UnityEngine;

    [RequireComponent(typeof(AbstractConnectable))]
    [RequireComponent(typeof(AbstractGrabbable))]
    public abstract class MaintenanceTool : MonoBehaviour {
        [SerializeField]
        [HideInInspector]
        protected AbstractConnectable _connectable;
        [SerializeField]
        [HideInInspector]
        protected AbstractGrabbable _grabbable;
        [SerializeField]
        [HideInInspector]
        protected AbstractInteractionController _controller;

        protected void Start() {
            if (_connectable == null) {
                _connectable = GetComponent<AbstractConnectable>();
            }

            if (_grabbable == null) {
                _grabbable = GetComponent<AbstractGrabbable>();
            }
            if (_controller == null) {
                _controller = GetComponent<AbstractInteractionController>();
            }

            SetupEventHandlers();
        }

        protected abstract void SetupEventHandlers();

        protected virtual void Reset() {
            var defaultClass = GetDefaultObjectClass();
            var behaviours = new List<AbstractInteractiveBehaviour>(GetComponents<AbstractInteractiveBehaviour>());
            List<Type> requiredTypes = new List<Type>(GetRequiredComponentsTypes() ?? new Type[0]);
            // Find all Interactive Behaviours and update the default object type
            foreach (var behaviour in behaviours) {
                requiredTypes.Remove(behaviour.GetType());
                if (string.IsNullOrEmpty(behaviour.ObjectClass.type)) {
                    behaviour.ObjectClass = defaultClass;
                }
            }
            foreach(var type in requiredTypes) {
                var newBehaviour = gameObject.AddComponent(type);
                if (newBehaviour is AbstractInteractiveBehaviour) {
                    behaviours.Add((AbstractInteractiveBehaviour)newBehaviour);
                }
            }
            InitializeComponents(behaviours.ToArray());
        }

        protected abstract ObjectClass GetDefaultObjectClass();

        protected abstract Type[] GetRequiredComponentsTypes();

        protected virtual void InitializeComponents(AbstractInteractiveBehaviour[] components) {
            foreach (var component in components) {
                if (component is AbstractConnectable) {
                    InitializeConnectable((AbstractConnectable)component);
                }
                else if (component is AbstractGrabbable) {
                    InitializeGrabbable((AbstractGrabbable)component);
                }
            }
        }

        protected virtual void InitializeConnectable(AbstractConnectable connectable) {
            _connectable = connectable;

            _connectable.activeConnector = true;
            _connectable.connectorType = AbstractConnectable.ConnectorType.Male;
            _connectable.keepFixed = true;
            _connectable.instantConnection = false;
        }

        protected virtual void InitializeGrabbable(AbstractGrabbable grabbable) {
            _grabbable = grabbable;
        }
    }
}