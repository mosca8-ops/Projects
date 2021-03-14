using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TXT.WEAVR.Core;
using TXT.WEAVR.Interaction;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class GameObjectSerializableState
    {
        #region [ STATIC FIELDS ]
        private static Dictionary<Type, bool> s_statelessTypes = new Dictionary<Type, bool>();
        #endregion

        #region [ SERIALIZED FIELDS ]
        public string uniqueId;
        public bool isActive;
        public List<BaseSerializableState> componentsState;
        public BillboardAndOutlineState billboardOutlineState;
        #endregion

        #region [ SNAPSHOT ]
        public bool Snapshot(GameObject _gameObject)
        {
            if (_gameObject == null)
                return false;

            var uniqueID = _gameObject.GetComponent<UniqueID>();
            if (uniqueID == null)
                return false;

            if (_gameObject.GetComponent<StatelessGameObject>())
                return false;

            if (BagHolder.Main.Bag.IsInAnyHand(_gameObject))
                return false;

            uniqueId = uniqueID.ID;
            isActive = _gameObject.activeSelf;

            componentsState = new List<BaseSerializableState>();

            bool isComponentStateless;

            foreach (var component in _gameObject.GetComponents<Component>())
            {
                if (component == null)
                    continue;

                if (s_statelessTypes.TryGetValue(component.GetType(), out isComponentStateless))
                {
                    if (isComponentStateless)
                        continue;

                    SnaphotComponent(component);
                }
                else
                {
                    var componentType = component.GetType();
                    isComponentStateless = componentType.GetCustomAttribute<StatelessAttribute>() != null;
                    s_statelessTypes.Add(componentType, isComponentStateless);

                    if (isComponentStateless)
                        continue;

                    SnaphotComponent(component);
                }
            }

            billboardOutlineState = new BillboardAndOutlineState();
            billboardOutlineState.Snaphot(_gameObject);

            return true;
        }

        private void SnaphotComponent(Component _component)
        {
            BaseSerializableState componentSerializable = null;

            if (_component is RectTransform)
                componentSerializable = new RectTransformSerializableState();
            else if (_component is Transform)
                componentSerializable = new TransformSerializableState();
            else if (_component is Rigidbody)
                componentSerializable = new RigidbodySerializableState();
            else if (_component is Collider)
                componentSerializable = new ColliderSerializableState();
            else if (_component is Camera)
                componentSerializable = new CameraSerializableState();
            else if (_component is Renderer)
                componentSerializable = new RendererSerializableState();
            else if (_component is Animator)
                componentSerializable = new AnimatorSerializableState();
            else
                componentSerializable = new ComponentSerializableState();

            if (componentSerializable.Snapshot(_component))
                componentsState.Add(componentSerializable);
        }
        #endregion

        #region [ RESTORE ]
        public bool Restore()
        {
            var gameObjectToRestore = IDBookkeeper.Get(uniqueId);
            if (gameObjectToRestore == null)
                return false;

            gameObjectToRestore.SetActive(isActive);

            foreach (var componentState in componentsState)
                RestoreComponent(componentState, gameObjectToRestore);

            billboardOutlineState.Restore(gameObjectToRestore);

            return true;
        }

        private void RestoreComponent(BaseSerializableState _componentState, GameObject _gameobject)
        {
            var componentType = _componentState.GetComponentType();
            if (componentType == null)
                return;

            Component component = null;
            if (_gameobject.TryGetComponent(componentType, out component))
            {
                if (component is RectTransform)
                    _componentState = new RectTransformSerializableState(_componentState);
                else if (component is Transform)
                    _componentState = new TransformSerializableState(_componentState);
                else if (component is Rigidbody)
                    _componentState = new RigidbodySerializableState(_componentState);
                else if (component is Collider)
                    _componentState = new ColliderSerializableState(_componentState);
                else if (component is Camera)
                    _componentState = new CameraSerializableState(_componentState);
                else if (component is Renderer)
                    _componentState = new RendererSerializableState(_componentState);
                else if (component is Animator)
                    _componentState = new AnimatorSerializableState(_componentState);
                else
                    _componentState = new ComponentSerializableState(_componentState);

                _componentState.Restore(component);
            }
        }
        #endregion
    }
}
