using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace TXT.WEAVR.Procedure
{
    public abstract class SpecialSerializableState<T> : BaseSerializableState where T : Component
    {
        public override sealed bool Snapshot(Component _component)
        {
            return _component is T ? Snapshot(_component as T) : false;
        }
        public override sealed bool Restore(Component _component)
        {
            return _component is T ? Restore(_component as T) : false;
        }

        protected abstract bool Snapshot(T component);
        protected abstract bool Restore(T component);
    }

    [Serializable]
    public class RectTransformSerializableState : SpecialSerializableState<RectTransform>
    {
        public RectTransformSerializableState() { }

        public RectTransformSerializableState(BaseSerializableState _componentState)
        {
            componentID = _componentState.componentID;
            fieldValues = _componentState.fieldValues;
            propertyValues = _componentState.propertyValues;
        }

        protected override bool Snapshot(RectTransform _rectTransform)
        {
            if (_rectTransform == null)
                return false;

            var componentType = _rectTransform.GetType();
            var componentAssemblyName = componentType.AssemblyQualifiedName;

            m_membersData = GetComponentMembersData(componentAssemblyName);
            if (m_membersData == null)
                SetComponentMembersData(componentMembers.Count, componentType.AssemblyQualifiedName);

            componentID = m_membersData.componentID;

            if (_rectTransform.parent != null)
                propertyValues.Add(ValueSerialization.Serialize(SceneTools.GetGameObjectPath(_rectTransform.parent.gameObject)));
            else
                propertyValues.Add(ValueSerialization.Serialize(string.Empty));

            propertyValues.Add(ValueSerialization.Serialize(_rectTransform.localPosition));
            propertyValues.Add(ValueSerialization.Serialize(_rectTransform.localRotation));
            propertyValues.Add(ValueSerialization.Serialize(_rectTransform.localScale));
            propertyValues.Add(ValueSerialization.Serialize(_rectTransform.pivot));
            propertyValues.Add(ValueSerialization.Serialize(_rectTransform.anchoredPosition));
            propertyValues.Add(ValueSerialization.Serialize(_rectTransform.anchoredPosition3D));
            propertyValues.Add(ValueSerialization.Serialize(_rectTransform.offsetMax));
            propertyValues.Add(ValueSerialization.Serialize(_rectTransform.offsetMin));
            propertyValues.Add(ValueSerialization.Serialize(_rectTransform.anchorMax));
            propertyValues.Add(ValueSerialization.Serialize(_rectTransform.anchorMin));
            propertyValues.Add(ValueSerialization.Serialize(_rectTransform.sizeDelta));

            return true;
        }

        protected override bool Restore(RectTransform _rectTransform)
        {
            if (_rectTransform == null)
                return false;

            var path = (string)ValueSerialization.Deserialize(propertyValues[0], typeof(string));
            if (!string.IsNullOrEmpty(path))
            {
                var parentObject = SceneTools.GetGameObjectAtScenePath(path);
                if (parentObject != null && _rectTransform.parent.gameObject != parentObject)
                    _rectTransform.SetParent(parentObject.transform, false);
            }
            _rectTransform.localPosition = (Vector3)ValueSerialization.Deserialize(propertyValues[1], typeof(Vector3));
            _rectTransform.localRotation = (Quaternion)ValueSerialization.Deserialize(propertyValues[2], typeof(Quaternion));
            _rectTransform.localScale = (Vector3)ValueSerialization.Deserialize(propertyValues[3], typeof(Vector3));
            _rectTransform.localScale = (Vector3)ValueSerialization.Deserialize(propertyValues[3], typeof(Vector3));

            _rectTransform.pivot = (Vector2)ValueSerialization.Deserialize(propertyValues[4], typeof(Vector2));
            _rectTransform.anchoredPosition = (Vector2)ValueSerialization.Deserialize(propertyValues[5], typeof(Vector2));
            _rectTransform.anchoredPosition3D = (Vector3)ValueSerialization.Deserialize(propertyValues[6], typeof(Vector3));
            _rectTransform.offsetMax = (Vector2)ValueSerialization.Deserialize(propertyValues[7], typeof(Vector2));
            _rectTransform.offsetMin = (Vector2)ValueSerialization.Deserialize(propertyValues[8], typeof(Vector2));
            _rectTransform.anchorMax = (Vector2)ValueSerialization.Deserialize(propertyValues[9], typeof(Vector2));
            _rectTransform.anchorMin = (Vector2)ValueSerialization.Deserialize(propertyValues[10], typeof(Vector2));
            _rectTransform.sizeDelta = (Vector2)ValueSerialization.Deserialize(propertyValues[11], typeof(Vector2));

            return true;
        }
    }

    [Serializable]
    public class TransformSerializableState : SpecialSerializableState<Transform>
    {
        public TransformSerializableState() { }

        public TransformSerializableState(BaseSerializableState _componentState)
        {
            componentID = _componentState.componentID;
            fieldValues = _componentState.fieldValues;
            propertyValues = _componentState.propertyValues;
        }

        protected override bool Snapshot(Transform _transform)
        {
            if (_transform == null)
                return false;

            var componentType = _transform.GetType();
            var componentAssemblyName = componentType.AssemblyQualifiedName;

            m_membersData = GetComponentMembersData(componentAssemblyName);
            if (m_membersData == null)
                SetComponentMembersData(componentMembers.Count, componentType.AssemblyQualifiedName);

            componentID = m_membersData.componentID;

            if (_transform.parent != null)
                propertyValues.Add(ValueSerialization.Serialize(SceneTools.GetGameObjectPath(_transform.parent.gameObject)));
            else
                propertyValues.Add(ValueSerialization.Serialize(string.Empty));

            propertyValues.Add(ValueSerialization.Serialize(_transform.localPosition));
            propertyValues.Add(ValueSerialization.Serialize(_transform.localRotation));
            propertyValues.Add(ValueSerialization.Serialize(_transform.localScale));

            return true;
        }

        protected override bool Restore(Transform _transform)
        {
            if (_transform == null)
                return false;

            var path = (string)ValueSerialization.Deserialize(propertyValues[0], typeof(string));
            if (!string.IsNullOrEmpty(path))
            {
                var parentObject = SceneTools.GetGameObjectAtScenePath(path);
                if (parentObject != null && _transform.parent.gameObject != parentObject)
                    _transform.SetParent(parentObject.transform);
            }
            _transform.localPosition = (Vector3)ValueSerialization.Deserialize(propertyValues[1], typeof(Vector3));
            _transform.localRotation = (Quaternion)ValueSerialization.Deserialize(propertyValues[2], typeof(Quaternion));
            _transform.localScale = (Vector3)ValueSerialization.Deserialize(propertyValues[3], typeof(Vector3));

            return true;
        }
    }

    [Serializable]
    public class RigidbodySerializableState : SpecialSerializableState<Rigidbody>
    {
        public RigidbodySerializableState() { }

        public RigidbodySerializableState(BaseSerializableState _componentState)
        {
            componentID = _componentState.componentID;
            fieldValues = _componentState.fieldValues;
            propertyValues = _componentState.propertyValues;
        }

        protected override bool Snapshot(Rigidbody _rigidbody)
        {
            if (_rigidbody == null)
                return false;

            var componentType = _rigidbody.GetType();
            var componentAssemblyName = componentType.AssemblyQualifiedName;

            m_membersData = GetComponentMembersData(componentAssemblyName);
            if (m_membersData == null)
                SetComponentMembersData(componentMembers.Count, componentType.AssemblyQualifiedName);

            componentID = m_membersData.componentID;

            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.angularDrag));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.angularVelocity));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.centerOfMass));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.detectCollisions));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.drag));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.freezeRotation));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.isKinematic));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.mass));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.maxAngularVelocity));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.maxDepenetrationVelocity));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.position));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.rotation));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.sleepThreshold));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.solverIterations));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.solverVelocityIterations));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.useGravity));
            propertyValues.Add(ValueSerialization.Serialize(_rigidbody.velocity));

            return true;
        }

        protected override bool Restore(Rigidbody _rigidbody)
        {
            if (_rigidbody == null)
                return false;

            _rigidbody.angularDrag = (float)ValueSerialization.Deserialize(propertyValues[0], typeof(float));
            _rigidbody.angularVelocity = (Vector3)ValueSerialization.Deserialize(propertyValues[1], typeof(Vector3));
            _rigidbody.centerOfMass = (Vector3)ValueSerialization.Deserialize(propertyValues[2], typeof(Vector3));
            _rigidbody.detectCollisions = (bool)ValueSerialization.Deserialize(propertyValues[3], typeof(bool));
            _rigidbody.drag = (float)ValueSerialization.Deserialize(propertyValues[4], typeof(float));
            _rigidbody.freezeRotation = (bool)ValueSerialization.Deserialize(propertyValues[5], typeof(bool));
            _rigidbody.isKinematic = (bool)ValueSerialization.Deserialize(propertyValues[6], typeof(bool));
            _rigidbody.mass = (float)ValueSerialization.Deserialize(propertyValues[7], typeof(float));
            _rigidbody.maxAngularVelocity = (float)ValueSerialization.Deserialize(propertyValues[8], typeof(float));
            _rigidbody.maxDepenetrationVelocity = (float)ValueSerialization.Deserialize(propertyValues[9], typeof(float));
            _rigidbody.position = (Vector3)ValueSerialization.Deserialize(propertyValues[10], typeof(Vector3));
            _rigidbody.rotation = (Quaternion)ValueSerialization.Deserialize(propertyValues[11], typeof(Quaternion));
            _rigidbody.sleepThreshold = (float)ValueSerialization.Deserialize(propertyValues[12], typeof(float));
            _rigidbody.solverIterations = (int)ValueSerialization.Deserialize(propertyValues[13], typeof(int));
            _rigidbody.solverVelocityIterations = (int)ValueSerialization.Deserialize(propertyValues[14], typeof(int));
            _rigidbody.useGravity = (bool)ValueSerialization.Deserialize(propertyValues[15], typeof(bool));
            _rigidbody.velocity = (Vector3)ValueSerialization.Deserialize(propertyValues[16], typeof(Vector3));

            return true;
        }
    }

    [Serializable]
    public class ColliderSerializableState : SpecialSerializableState<Collider>
    {
        public ColliderSerializableState() { }

        public ColliderSerializableState(BaseSerializableState _componentState)
        {
            componentID = _componentState.componentID;
            fieldValues = _componentState.fieldValues;
            propertyValues = _componentState.propertyValues;
        }

        protected override bool Snapshot(Collider _collider)
        {
            if (_collider == null)
                return false;

            var componentType = _collider.GetType();
            var componentAssemblyName = componentType.AssemblyQualifiedName;

            m_membersData = GetComponentMembersData(componentAssemblyName);
            if (m_membersData == null)
                SetComponentMembersData(componentMembers.Count, componentType.AssemblyQualifiedName);

            componentID = m_membersData.componentID;

            propertyValues.Add(ValueSerialization.Serialize(_collider.enabled));
            propertyValues.Add(ValueSerialization.Serialize(_collider.isTrigger));

            if (_collider.material == null)
            {
                propertyValues.Add(ValueSerialization.Serialize(_collider.material));
            }
            else
            {
                PhysicMaterialState materialData = new PhysicMaterialState();
                materialData.bounciness = _collider.material.bounciness;
                materialData.dynamicFriction = _collider.material.dynamicFriction;
                materialData.staticFriction = _collider.material.staticFriction;
                materialData.frictionCombine = (int)_collider.material.frictionCombine;
                materialData.bounceCombine = (int)_collider.material.bounceCombine;

                var jsonState = JsonConvert.SerializeObject(materialData);
                propertyValues.Add(jsonState);

            }


            return true;
        }

        protected override bool Restore(Collider _collider)
        {
            if (_collider == null)
                return false;

            _collider.enabled = (bool)ValueSerialization.Deserialize(propertyValues[0], typeof(bool));
            _collider.isTrigger = (bool)ValueSerialization.Deserialize(propertyValues[1], typeof(bool));

            if (_collider.material == null)
            {
                if (propertyValues[2] == "null")
                    return true;
                else
                    _collider.material = new PhysicMaterial();
            }

            PhysicMaterialState materialData = JsonConvert.DeserializeObject<PhysicMaterialState>(propertyValues[2]);
            _collider.material.bounciness = materialData.bounciness;
            _collider.material.dynamicFriction = materialData.dynamicFriction;
            _collider.material.staticFriction = materialData.staticFriction;
            _collider.material.frictionCombine = (PhysicMaterialCombine)materialData.frictionCombine;
            _collider.material.bounceCombine = (PhysicMaterialCombine)materialData.bounceCombine;

            return true;
        }

        [Serializable]
        private struct PhysicMaterialState
        {
            public float bounciness;
            public float dynamicFriction;
            public float staticFriction;
            public int frictionCombine;
            public int bounceCombine;
        }
    }

    [Serializable]
    public class CameraSerializableState : SpecialSerializableState<Camera>
    {
        public CameraSerializableState() { }

        public CameraSerializableState(BaseSerializableState _componentState)
        {
            componentID = _componentState.componentID;
            fieldValues = _componentState.fieldValues;
            propertyValues = _componentState.propertyValues;
        }

        protected override bool Snapshot(Camera _camera)
        {
            if (_camera == null)
                return false;

            var componentType = _camera.GetType();
            var componentAssemblyName = componentType.AssemblyQualifiedName;

            m_membersData = GetComponentMembersData(componentAssemblyName);
            if (m_membersData == null)
                SetComponentMembersData(componentMembers.Count, componentType.AssemblyQualifiedName);

            componentID = m_membersData.componentID;

            propertyValues.Add(ValueSerialization.Serialize(_camera.aspect));
            propertyValues.Add(ValueSerialization.Serialize(_camera.backgroundColor));
            propertyValues.Add(ValueSerialization.Serialize(_camera.cullingMask));
            propertyValues.Add(ValueSerialization.Serialize(_camera.depth));
            propertyValues.Add(ValueSerialization.Serialize(_camera.enabled));
            propertyValues.Add(ValueSerialization.Serialize(_camera.farClipPlane));
            propertyValues.Add(ValueSerialization.Serialize(_camera.fieldOfView));
            propertyValues.Add(ValueSerialization.Serialize(_camera.nearClipPlane));
            propertyValues.Add(ValueSerialization.Serialize(_camera.rect));
            propertyValues.Add(ValueSerialization.Serialize(_camera.targetDisplay));

            return true;
        }

        protected override bool Restore(Camera _camera)
        {
            if (_camera == null)
                return false;

            _camera.aspect = (float)ValueSerialization.Deserialize(propertyValues[0], typeof(float));
            _camera.backgroundColor = (Color)ValueSerialization.Deserialize(propertyValues[1], typeof(Color));
            _camera.cullingMask = (int)ValueSerialization.Deserialize(propertyValues[2], typeof(int));
            _camera.depth = (float)ValueSerialization.Deserialize(propertyValues[3], typeof(float));
            _camera.enabled = (bool)ValueSerialization.Deserialize(propertyValues[4], typeof(bool));
            _camera.farClipPlane = (float)ValueSerialization.Deserialize(propertyValues[5], typeof(float));
            _camera.fieldOfView = (float)ValueSerialization.Deserialize(propertyValues[6], typeof(float));
            _camera.nearClipPlane = (float)ValueSerialization.Deserialize(propertyValues[7], typeof(float));
            _camera.rect = (Rect)ValueSerialization.Deserialize(propertyValues[8], typeof(Rect));
            _camera.targetDisplay = (int)ValueSerialization.Deserialize(propertyValues[9], typeof(int));

            return true;
        }
    }

    [Serializable]
    public class RendererSerializableState : SpecialSerializableState<Renderer>
    {
        public RendererSerializableState() { }

        public RendererSerializableState(BaseSerializableState _componentState)
        {
            componentID = _componentState.componentID;
            fieldValues = _componentState.fieldValues;
            propertyValues = _componentState.propertyValues;
        }

        protected override bool Snapshot(Renderer _renderer)
        {
            if (_renderer == null)
                return false;

            var componentType = _renderer.GetType();
            var componentAssemblyName = componentType.AssemblyQualifiedName;

            m_membersData = GetComponentMembersData(componentAssemblyName);
            if (m_membersData == null)
                SetComponentMembersData(componentMembers.Count, componentType.AssemblyQualifiedName);

            componentID = m_membersData.componentID;

            propertyValues.Add(ValueSerialization.Serialize(_renderer.renderingLayerMask));
            propertyValues.Add(ValueSerialization.Serialize(_renderer.rendererPriority));
            propertyValues.Add(ValueSerialization.Serialize(_renderer.sortingLayerName));
            propertyValues.Add(ValueSerialization.Serialize(_renderer.sortingLayerID));
            propertyValues.Add(ValueSerialization.Serialize(_renderer.sortingOrder));
            propertyValues.Add(ValueSerialization.Serialize(_renderer.receiveShadows));
            propertyValues.Add(ValueSerialization.Serialize(_renderer.enabled));

            var materialsIDs = new List<int>();
            var matID = default(int);
            for (int i = 0; i < _renderer.sharedMaterials.Length; i++)
            {
                matID = ProcedureStateManager.Instance.SnapshotMaterial(_renderer.sharedMaterials[i]);
                if(matID != -1)
                    materialsIDs.Add(matID);            
            }

            string jsonIDs = JsonConvert.SerializeObject(materialsIDs);
            propertyValues.Add(ValueSerialization.Serialize(jsonIDs));

            return true;
        }

        protected override bool Restore(Renderer _renderer)
        {
            if (_renderer == null)
                return false;

            _renderer.renderingLayerMask = (uint)ValueSerialization.Deserialize(propertyValues[0], typeof(uint));
            _renderer.rendererPriority = (int)ValueSerialization.Deserialize(propertyValues[1], typeof(int));
            _renderer.sortingLayerName = (string)ValueSerialization.Deserialize(propertyValues[2], typeof(string));
            _renderer.sortingLayerID = (int)ValueSerialization.Deserialize(propertyValues[3], typeof(int));
            _renderer.sortingOrder = (int)ValueSerialization.Deserialize(propertyValues[4], typeof(int));
            _renderer.receiveShadows = (bool)ValueSerialization.Deserialize(propertyValues[5], typeof(bool));
            _renderer.enabled = (bool)ValueSerialization.Deserialize(propertyValues[6], typeof(bool));

            var materialsIDs = JsonConvert.DeserializeObject<List<int>>(propertyValues[7]);
            var materials = new Material[materialsIDs.Count];

            for (int i = 0; i < materialsIDs.Count; i++)
                materials[i] = ProcedureStateManager.Instance.RestoreMaterial(materialsIDs[i]);

            _renderer.materials = materials;

            return true;
        }
    }

    [Serializable]
    public class AnimatorSerializableState : SpecialSerializableState<Animator>
    {
        public AnimatorSerializableState() { }

        public AnimatorSerializableState(BaseSerializableState _componentState)
        {
            componentID = _componentState.componentID;
            fieldValues = _componentState.fieldValues;
            propertyValues = _componentState.propertyValues;
        }

        protected override bool Snapshot(Animator _animator)
        {
            if (_animator == null)
                return false;

            var componentType = _animator.GetType();
            var componentAssemblyName = componentType.AssemblyQualifiedName;

            m_membersData = GetComponentMembersData(componentAssemblyName);
            if (m_membersData == null)
                SetComponentMembersData(componentMembers.Count, componentType.AssemblyQualifiedName);

            componentID = m_membersData.componentID;

            propertyValues.Add(ValueSerialization.Serialize(_animator.enabled));

            AnimatorState animatorState;
            AnimatorStateInfo stateInfo;
            var animatorStates = new List<AnimatorState>();

            for (int i = 0; i < _animator.layerCount; i++)
            {
                stateInfo = _animator.GetCurrentAnimatorStateInfo(i);

                animatorState = new AnimatorState();
                animatorState.layer = i;
                animatorState.stateNameHash = stateInfo.shortNameHash;
                animatorState.normalizedTime = stateInfo.normalizedTime;
                animatorStates.Add(animatorState);
            }

            string jsonStates = JsonConvert.SerializeObject(animatorStates);
            propertyValues.Add(jsonStates);

            return true;
        }

        protected override bool Restore(Animator _animator)
        {
            if (_animator == null)
                return false;

            _animator.enabled = (bool)ValueSerialization.Deserialize(propertyValues[0], typeof(bool));

            var animatorStates = JsonConvert.DeserializeObject<List<AnimatorState>>(propertyValues[1]);

            for (int i = 0; i < animatorStates.Count; i++)
                _animator.Play(animatorStates[i].stateNameHash, animatorStates[i].layer, animatorStates[i].normalizedTime);

            return true;
        }

        [Serializable]
        private struct AnimatorState
        {
            public int layer;
            public int stateNameHash;
            public float normalizedTime;
        }
    }
}
