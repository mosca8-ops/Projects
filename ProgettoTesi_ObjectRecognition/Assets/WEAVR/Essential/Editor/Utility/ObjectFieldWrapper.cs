namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Utility;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// An object wrapper which allows for hierarchy objects persistence
    /// </summary>
    [Serializable]
    public class ObjectFieldWrapper : ScriptableObject, ICopyable, IRemovable<ObjectFieldWrapper>
    {
        [SerializeField]
        private UnityEngine.Object _object;
        private Type _type;

        [SerializeField]
        private bool _isRuntimeReady;   // Tells if it is not coming from deserialization

        [SerializeField]
        private string _typename;
        [SerializeField]
        private string _uniqueID;
        [SerializeField]
        private string _objectScenePath;    // Fallback in case unique id is not valid
        [SerializeField]
        private bool _isComponent;
        [SerializeField]
        private bool _isPrefab;

        /// <summary>
        /// The wrapped object, which could be of any <see cref="UnityEngine.Object"/> type
        /// </summary>
        public UnityEngine.Object Object {
            get {
                if (!_isRuntimeReady || _object.IsDestroyed())
                {
                    UpdateTypeInfo(_object);
                    _isRuntimeReady = true;
                    if (_object == null)
                    {
                        _object = TryRetrieveObject();
                    }
                }
                return _object;
            }
            set {
                if (_object != value)
                {
                    Undo.RecordObject(this, "Changed Object Field");
                    _object = value;
                    UpdateTypeInfo(value);
                    TryCreateUniqueID(value);
                    UpdateObjectInfos();
                    EditorUtility.SetDirty(this);
                }
            }
        }

        /// <summary>
        /// Gets the unique id, if any, of the wrapped object
        /// </summary>
        public string UniqueId {
            get {
                return _uniqueID;
            }
        }

        /// <summary>
        /// Gets the type, if any, of the wrapped object
        /// </summary>
        public Type WrappedOjectType {
            get {
                return _type;
            }
        }

        /// <summary>
        /// A commodity getter for <see cref="_type"/> variable
        /// </summary>
        private Type Type {
            get {
                if (_type == null && _typename != null)
                {
                    _type = Type.GetType(_typename);
                }
                return _type;
            }
        }

        /// <summary>
        /// Set which type di wrapper should handle
        /// </summary>
        /// <param name="type">The type to handle</param>
        public void SetType(Type type)
        {
            _type = type;
            _typename = type.AssemblyQualifiedName;
            _isComponent = type.DerivesFrom(typeof(Component));

            if (_isComponent && _object != null)
            {
                _object = (_object as GameObject).GetComponent(type);
            }
        }

        /// <summary>
        /// Gets the object converted to the specified type
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <returns>The converted type</returns>
        public T Get<T>() where T : UnityEngine.Object
        {
            return Object as T;
        }

        /// <summary>
        /// Constructor. Hidden to allow clean serialization (Unity is very unstable with ScriptableObject derived classes)
        /// </summary>
        private ObjectFieldWrapper()
        {

        }

        private void OnEnable()
        {
            //_isRuntimeReady = false;
        }

        /// <summary>
        /// Initializes the instance with default type
        /// </summary>
        /// <param name="type">The type to wrap</param>
        /// <returns>This instance initialized</returns>
        private ObjectFieldWrapper Initialize(Type type)
        {
            _type = type;
            Undo.RecordObject(this, "Set typename");
            _typename = type.AssemblyQualifiedName;
            return this;
        }

        /// <summary>
        /// Creates an Object wrapper for the specific type
        /// </summary>
        /// <param name="type">The type to wrap</param>
        /// <returns>The newly created and initialized object wrapper</returns>
        public static ObjectFieldWrapper Create(Type type)
        {
            ObjectFieldWrapper wrapper = CreateInstance<ObjectFieldWrapper>();
            wrapper.Initialize(type);
            Undo.RegisterCreatedObjectUndo(wrapper, "Created Wrapper");
            return wrapper;
        }

        /// <summary>
        /// Creates a wrapper out of specified details
        /// </summary>
        /// <param name="typename">The type name of the wrapped object</param>
        /// <param name="uniqueId">The unique id of the wrapped object</param>
        /// <param name="hierarchyPath">The path of the object in the scene</param>
        /// <returns>The created wrapper</returns>
        public static ObjectFieldWrapper Create(string typename, string uniqueId, string hierarchyPath)
        {
            ObjectFieldWrapper wrapper = CreateInstance<ObjectFieldWrapper>();
            wrapper._typename = string.IsNullOrEmpty(typename) ? typeof(GameObject).AssemblyQualifiedName : typename;
            wrapper._uniqueID = uniqueId;
            wrapper._objectScenePath = hierarchyPath;

            return wrapper;
        }

        /// <summary>
        /// Creates a wrapper out of specified details
        /// </summary>
        /// <param name="type">The type of the wrapped object</param>
        /// <param name="uniqueId">The unique id of the wrapped object</param>
        /// <param name="hierarchyPath">The path of the object in the scene</param>
        /// <returns>The created wrapper</returns>
        public static ObjectFieldWrapper Create(Type type, string uniqueId, string hierarchyPath)
        {
            ObjectFieldWrapper wrapper = CreateInstance<ObjectFieldWrapper>();
            wrapper._type = type;
            wrapper._typename = type.AssemblyQualifiedName;
            wrapper._uniqueID = uniqueId;
            wrapper._objectScenePath = hierarchyPath;

            return wrapper;
        }

        /// <summary>
        /// Copies all data from the specified other Object wrapper
        /// </summary>
        /// <param name="other">The other <see cref="ObjectFieldWrapper"/> where to copy data from </param>
        public void CopyDataFrom(ObjectFieldWrapper other)
        {
            name = other.name;
            _object = other._object;
            _type = other.Type;
            _isRuntimeReady = other._isRuntimeReady;
            _typename = other._typename;
            _uniqueID = other._uniqueID;
            _objectScenePath = other._objectScenePath;
            _isComponent = other._isComponent;
            _isPrefab = other._isPrefab;
        }

        /// <summary>
        /// Draws the field and updates internally the object. 
        /// Use <see cref="Object"/> to retrieve the wrapped object.
        /// </summary>
        /// <param name="rect">The Rect where to draw the field</param>
        /// <param name="label">The label of the field. If null, no label will be shown</param>
        public void DrawField(Rect rect, string label)
        {
            Object = !string.IsNullOrEmpty(label) ?
                                      EditorGUI.ObjectField(rect, label, Object, Type, true) as UnityEngine.Object :
                                      EditorGUI.ObjectField(rect, Object, Type, true) as UnityEngine.Object;
        }

        /// <summary>
        /// Draws the field and updates internally the object. 
        /// Use <see cref="Object"/> to retrieve the wrapped object.
        /// </summary>
        /// <param name="rect">The Rect where to draw the field</param>
        /// <param name="label">The label of the field. If null, no label will be shown</param>
        public void DrawFieldWithContent(Rect rect, GUIContent label)
        {
            Object = EditorGUI.ObjectField(rect, label, Object, Type, true) as UnityEngine.Object;
        }

        /// <summary>
        /// Updates the object of the specified wrapper
        /// </summary>
        /// <param name="wrapper">The wrapper to update</param>
        /// <param name="newObject">The new object ot assign</param>
        public static void UpdateObject(ObjectFieldWrapper wrapper, UnityEngine.Object newObject)
        {
            wrapper.Object = newObject;
        }

        /// <summary>
        /// Draws the field and updates internally the object and returns it as specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object to update</typeparam>
        /// <param name="rect">The Rect where to draw the field</param>
        /// <param name="label">The label of the field. If null, no label will be shown</param>
        /// <returns>The updated wrapped object converted to <typeparamref name="T"/></returns>
        public T DrawField<T>(Rect rect, string label) where T : UnityEngine.Object
        {
            DrawField(rect, label);
            return _object as T;
        }

        /// <summary>
        /// Tries to retrieve object either by unique id or by scene path
        /// </summary>
        /// <returns>If retrieval was successful, returns the object, otherwise null</returns>
        private UnityEngine.Object TryRetrieveObject()
        {
            // If it is a prefab, then return the object, 
            // because the following logic is only for hierarchy objects
            if (_isPrefab)
            {
                return _object;
            }
            // If no type has been saved, then the object was null during serialization
            if (_type == null && _typename == null)
            {
                return null;
            }
            // Same goes for unique id and object scene path, it is impossible to retrieve the object without it
            if (string.IsNullOrEmpty(_uniqueID) && string.IsNullOrEmpty(_objectScenePath))
            {
                if (!string.IsNullOrEmpty(name) && name != "Null")
                {
                    Debug.Log($"{GetType().Name}[{name}] UniqueId is null");
                }
                return null;
            }
            _object = TryGetWithUniqueId(_uniqueID);
            if (_object == null)
            { // Unique id failed
                if (!BuildPipeline.isBuildingPlayer)
                {
                    Debug.LogErrorFormat("{0}[{1}]: Unable to retrieve object with id [{2}]", GetType().Name, name, _uniqueID);
                }
                _object = string.IsNullOrEmpty(_objectScenePath) ?
                            null :
                            SceneTools.GetGameObjectAtScenePath(_objectScenePath); // Try to get it with scene path
                if (_object == null)
                {
                    Debug.LogErrorFormat("{0}[{1}]: Unable to retrieve object with path [{2}]", GetType().Name, name, _objectScenePath);
                }
            }

            if (_isComponent && _object != null)
            {
                _object = (_object as GameObject).GetComponent(Type);
            }

            UpdateObjectInfos();
            return _object;
        }

        private UnityEngine.Object TryGetWithUniqueId(string uniqueId)
        {
            var returnValue = IDBookkeeper.Get(uniqueId);
            if (returnValue == null)
            {
                var component = SceneTools.GetComponentInScene<UniqueID>(u => u.ID == uniqueId);
                returnValue = component != null ? component.gameObject : null;
            }
            return returnValue;
        }

        /// <summary>
        /// Updates from the <see cref="_object"/> all needed information for serialization purposes
        /// </summary>
        private void UpdateObjectInfos()
        {
            _isRuntimeReady = true;
            if (_object == null)
            {
                // Reset all values
                if (!string.IsNullOrEmpty(name) && name != "Null")
                {
                    Debug.Log($"{GetType().Name}[{name}] Object is null and UniqueId is set to null");
                }
                _uniqueID = null;
                _objectScenePath = null;
                name = "Null";
                return;
            }
            // Get all the correct values which will help retrieve the object later
            name = _object.name;
            _isComponent = _object is Component;
            _isPrefab = EditorTools.IsPrefab(_object);
            if (_isPrefab)
            {
                Debug.Log($"{GetType().Name}[{name}] Object is PREFAB and UniqueId is set to null");
                _uniqueID = null;
                _objectScenePath = null;
            }
            else
            {
                _uniqueID = IDBookkeeper.GetUniqueID(_isComponent ? (_object as Component).gameObject.GetInstanceID() : _object.GetInstanceID());
                _objectScenePath = SceneTools.GetGameObjectPath(_isComponent ? (_object as Component).gameObject : _object as GameObject);
            }
        }

        /// <summary>
        /// Tries and updates the type of the wrapped object
        /// </summary>
        /// <param name="newObject">The object to get the type from</param>
        /// <returns>True if the type has been updated</returns>
        private bool UpdateTypeInfo(UnityEngine.Object newObject)
        {
            //if (_type == null && _typename != null)
            //{
            //    _type = Type.GetType(_typename);
            //}
            if (newObject == null)
            {
                return false;
            }
            if (Type != newObject.GetType() && !newObject.GetType().DerivesFrom(_type))
            {
                _type = newObject.GetType();
                Undo.RecordObject(this, "Changed type");
                _typename = _type.AssemblyQualifiedName;
                Undo.RecordObject(this, "Changed type");
                _isComponent = newObject is Component;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to obtain a unique id (either existing or create a new one)
        /// </summary>
        /// <param name="value">The object to obtain a unique id</param>
        private static void TryCreateUniqueID(UnityEngine.Object value)
        {
            GameObject gameObject = value is Component ? (value as Component).gameObject : value as GameObject;
            if (gameObject != null)
            {
                IDBookkeeper.Register(gameObject);
            }
        }

        public bool CopyFrom(object source)
        {
            if (source is ObjectFieldWrapper)
            {
                CopyDataFrom(source as ObjectFieldWrapper);
            }
            return false;
        }

        public ObjectFieldWrapper OnRemove()
        {
            Undo.RecordObject(this, GetType().Name + " removed");

            return this;
        }
    }
}
