using System.Linq;

namespace TXT.WEAVR.Simulation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Utility;
    using UnityEngine;

    [AddComponentMenu("WEAVR/Simulation/Simulation Evaluation Engine")]
    public class SimulationEvalEngine : MonoBehaviour, IPropertyCache
    {
        #region [  Static Part  ]

        private static SimulationEvalEngine _instance;

        public static SimulationEvalEngine Instance {
            get {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SimulationEvalEngine>();
                    if (_instance == null)
                    {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("SimulationEvalEngine");
                        _instance = go.AddComponent<SimulationEvalEngine>();
                        _instance.transform.SetParent(ObjectRetriever.WEAVR.transform, false);
                    }
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        #endregion

        public enum UpdateGranularity
        {
            Global,
            Group,
            Individual
        }

        [Tooltip("Whether to start evaluation on startup or not")]
        public bool runOnStart = false;
        [Tooltip("Whether to poll variables in fixed update or in normal update")]
        public bool fixedUpdate = false;
        [Tooltip("Whether to create file mapping if open fails")]
        public bool createMapping = false;
        [Tooltip("Number of attempts to get the memory mapping")]
        public int attemptsToGetMemArea = 5;
        public bool continuousWrite = true;
        [Tooltip("Granularity at which to write variables. It should be known at compile time")]
        [DoNotExpose]
        public UpdateGranularity writeGranularity = UpdateGranularity.Group;
        [Tooltip("Granularity at which to read variables. It should be known at compile time")]
        [DoNotExpose]
        public UpdateGranularity readGranularity = UpdateGranularity.Individual;

        [DoNotExpose]
        public bool EvaluationEnabled { get; private set; }

        public string ModuleId {
            get {
                return "Simulation";
            }
        }

        private bool _allGroupsAreMapped;

        private SharedMemoryManager _smManager;

        private Dictionary<string, SimVarMeta> _propertiesVariables;
        private HashSet<object> _mappedObjects;
        private Dictionary<SharedMemoryAccess, List<SimVarGroup>> _groups;


        private Dictionary<string, Tuple<IntPtr, int>> _ShmPtrMap = new Dictionary<string, Tuple<IntPtr, int>>();

        private void Awake()
        {
            if (_instance != this)
            {
                _instance = this;
                Initialize();
            }
        }

        private void Initialize()
        {
            _groups = new Dictionary<SharedMemoryAccess, List<SimVarGroup>>();
            foreach (SharedMemoryAccess accessType in Enum.GetValues(typeof(SharedMemoryAccess)))
            {
                _groups.Add(accessType, new List<SimVarGroup>());
            }
            _propertiesVariables = new Dictionary<string, SimVarMeta>();
            _mappedObjects = new HashSet<object>();
            _smManager = SharedMemoryManager.Instance;

            _allGroupsAreMapped = false;

            // Register for property caches
            Property.RegisterPropertyCache(this);

            _instance = this;
        }

        public bool TryGetProperty(object owner, string propertyPath, out Property cachedProperty)
        {
            SimVarMeta meta = null;
            if (_propertiesVariables.TryGetValue(owner.ToString() + propertyPath, out meta))
            {
                if (meta.property == null)
                {
                    meta.property = Property.Create(owner, propertyPath);
                    meta.group.VariablesMetas[meta.property] = meta;
                    RegisterForUpdate(owner, meta);
                }
                cachedProperty = meta.property;
                return true;
            }
            cachedProperty = null;
            return false;
        }

        public Property GetProperty(object owner, string propertyPath)
        {
            SimVarMeta meta = null;
            if (_propertiesVariables.TryGetValue(owner.ToString() + propertyPath, out meta))
            {
                if (meta.property == null)
                {
                    meta.property = Property.Create(owner, propertyPath);
                    meta.group.VariablesMetas[meta.property] = meta;
                    RegisterForUpdate(owner, meta);
                }
                return meta.property;
            }
            return Property.Create(owner, propertyPath);
        }

        private void RegisterForUpdate(object owner, SimVarMeta meta)
        {
            if (meta.group.AccessType != SharedMemoryAccess.Write)
            {
                //meta.group.VariablesToRead.Add(meta);
                switch (readGranularity)
                {
                    case UpdateGranularity.Global:
                        meta.group.VariablesToRead.Add(meta.group);
                        break;
                    case UpdateGranularity.Group:
                        var parent = meta.parent ?? meta.group;
                        if (parent.property == null)
                        {
                            parent.property = Property.Create(owner, parent.propertyPath);
                            //RegisterForUpdate(owner, parent);
                        }
                        meta.group.VariablesToRead.Add(parent);
                        break;
                    case UpdateGranularity.Individual:
                        meta.group.VariablesToRead.Add(meta);
                        break;
                }
            }
            if (meta.group.AccessType != SharedMemoryAccess.Read)
            {
                switch (writeGranularity)
                {
                    case UpdateGranularity.Global:
                        meta.property.ValueChanged += meta.group.Property_GroupValueChanged;
                        break;
                    case UpdateGranularity.Group:
                        var parent = meta.parent ?? meta.group;
                        if (parent.property == null)
                        {
                            parent.property = Property.Create(owner, parent.propertyPath);
                            RegisterForUpdate(owner, parent);
                        }
                        meta.property.ValueChanged += meta.group.Property_ParentValueChanged;
                        break;
                    case UpdateGranularity.Individual:
                        meta.property.ValueChanged += meta.group.Property_IndividualValueChanged;
                        break;
                }
                if (continuousWrite)
                {
                    meta.group.VariablesToWrite.Add(meta);
                }
            }
        }

        public bool MapToSharedMemory(object simulationDataHolder)
        {
            if (_mappedObjects.Contains(simulationDataHolder))
            {
                return true;
            }

            _mappedObjects.Add(simulationDataHolder);

            string propertyPath = null;
            GameObject gmSimDataHolder = null;
            if (simulationDataHolder is Component)
            {
                propertyPath = string.Concat("[", simulationDataHolder.GetType().AssemblyQualifiedName, "].");
                gmSimDataHolder = ((Component)simulationDataHolder).gameObject;
            }

            foreach (var member in simulationDataHolder.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (member is FieldInfo || member is PropertyInfo)
                {
                    string currentPropertyPath = propertyPath + member.Name;
                    var simvarAttribute = member.GetCustomAttribute<SimulationVariableAttribute>();
                    if (simvarAttribute != null)
                    {
                        CreateGroup(simulationDataHolder, (gmSimDataHolder ?? simulationDataHolder).ToString(), currentPropertyPath, member, simvarAttribute);
                    }
                }
            }


            
            foreach (var wKvp in _ShmPtrMap.ToList())
            {
              var wShmInfo = wKvp.Value;
              if (wShmInfo.Item1 == IntPtr.Zero)
              {
                _ShmPtrMap[wKvp.Key] = new Tuple<IntPtr, int>(SharedMemory.WeavrShmCreateOrOpen(wKvp.Key, wShmInfo.Item2), wShmInfo.Item2);
              }
            }

            // Get the handles now
            foreach (var groupPair in _groups)
            {
                foreach (var group in groupPair.Value)
                {
                  group.MemoryMap(_ShmPtrMap[group.MemoryFileId].Item1);
                }
            }

            return true;
        }

        private void Start()
        {
            if (runOnStart)
            {
                StartEvaluation();
            }
        }

        private void OnDestroy()
        {
            Property.UnregisterPropertyCache(this);
            CloseAllSharedMemories();
        }

        public void StartEvaluation()
        {
            EvaluationEnabled = true;
        }

        public void StopEvaluation()
        {
            EvaluationEnabled = false;
        }

        private void Update()
        {
            if (EvaluationEnabled && !fixedUpdate)
            {
                ReflectAndUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (EvaluationEnabled && fixedUpdate)
            {
                ReflectAndUpdate();
            }
        }

        private void ReflectAndUpdate()
        {
            // Here is the main loop
            // For each access type update values accordingly
            // Update variables in READ
            foreach (var group in _groups[SharedMemoryAccess.Read])
            {
                foreach (var simVar in group.VariablesToRead)
                {
                    simVar.property.Value = _smManager.ReadVariable(simVar.handle, simVar.property.Value, simVar.type);
                }
            }

            // Update variables in WRITE
            foreach (var group in _groups[SharedMemoryAccess.Write])
            {
                foreach (var simVar in group.VariablesToWrite)
                {
                    _smManager.WriteVariable(simVar.handle, simVar.valueToSet ?? simVar.property.Value);
                    if (!continuousWrite)
                    {
                        simVar.valueToSet = null;
                    }
                }
                if (!continuousWrite)
                {
                    group.VariablesToWrite.Clear();
                }
            }

            // Update variables in READ-WRITE
            // For READ-WRITE first the variable writes and then reads
            foreach (var group in _groups[SharedMemoryAccess.ReadWrite])
            {
                foreach (var simVar in group.VariablesToWrite)
                {
                    _smManager.WriteVariable(simVar.handle, simVar.valueToSet ?? simVar.property.Value);
                    simVar.valueToSet = null;
                }
                group.VariablesToWrite.Clear();
                foreach (var simVar in group.VariablesToRead)
                {
                    simVar.property.Value = _smManager.ReadVariable(simVar.handle, simVar.property.Value, simVar.type);
                }
            }
        }

        IntPtr GetSharedMemoryPtr(string iName)
        {
          if (_ShmPtrMap.TryGetValue(iName, out var value))
          {
            return value.Item1;
          }
          return IntPtr.Zero;
        }

        private void CloseAllSharedMemories()
        {
          if (_ShmPtrMap != null)
          {
            foreach (var wKvp in _ShmPtrMap)
            {
              if (wKvp.Value.Item1 != IntPtr.Zero)
              {
                SharedMemory.WeavrShmClose(wKvp.Key);
              }
            }
          }
          _ShmPtrMap = new Dictionary<string, Tuple<IntPtr, int>>();
        }

        private int GetSharedMemoryIdSize(string iSharedMemoryId)
        {
          foreach (var wKvp in _ShmPtrMap)
          {
            if (wKvp.Key == iSharedMemoryId)
            {
              return wKvp.Value.Item2;
            }
          }
          return 0;
        }

        private void CreateGroup(object owner, string prefix, string propertyPath, MemberInfo memberInfo, SimulationVariableAttribute attribute)
        {
            Type type = memberInfo is PropertyInfo ? ((PropertyInfo)memberInfo).PropertyType : ((FieldInfo)memberInfo).FieldType;
            var group = new SimVarGroup(attribute.SharedMemoryId, attribute.AccessType)
            {
                owner = owner,
                offset = GetSharedMemoryIdSize(attribute.SharedMemoryId),
                size = Marshal.SizeOf(type),
                property = Property.Create(owner, propertyPath),
                type = type,
                propertyPath = propertyPath,
            };
            _ShmPtrMap[group.MemoryFileId] = new Tuple<IntPtr, int>(IntPtr.Zero, group.offset + group.size);
            _groups[attribute.AccessType].Add(group);
            int childOffset = group.offset;
            foreach (var childInfo in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (childInfo is PropertyInfo || childInfo is FieldInfo)
                {
                    childOffset = CreateMapTree(group, group, childOffset, prefix, propertyPath + "." + childInfo.Name, childInfo);
                }
            }
        }

        private int CreateMapTree(SimVarMeta parent, SimVarGroup group, int currentOffset, string prefix, string propertyPath, MemberInfo memberInfo)
        {
            var type = memberInfo is PropertyInfo ? ((PropertyInfo)memberInfo).PropertyType : ((FieldInfo)memberInfo).FieldType;
            var simVar = new SimVarMeta()
            {
                type = type,
                offset = currentOffset,
                size = Marshal.SizeOf(type),
                group = group,
                parent = parent,
                propertyPath = propertyPath
            };

            parent.children.Add(simVar);
            _propertiesVariables.Add(prefix + propertyPath, simVar);

            int childOffset = currentOffset;
            foreach (var childInfo in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (childInfo is PropertyInfo || childInfo is FieldInfo)
                {
                    childOffset = CreateMapTree(simVar, group, childOffset, prefix, propertyPath + "." + childInfo.Name, childInfo);
                }
            }

            return currentOffset + simVar.size;
        }

        private class SimVarGroup : SimVarMeta
        {
            public IntPtr fileHandle;
            public object owner;
            public string MemoryFileId { get; private set; }
            public SharedMemoryAccess AccessType { get; private set; }
            public Dictionary<Property, SimVarMeta> VariablesMetas { get; private set; }
            public HashSet<SimVarMeta> VariablesToWrite { get; private set; }
            public HashSet<SimVarMeta> VariablesToRead { get; private set; }
            public bool IsMapped { get; private set; }

            public SimVarGroup(string memoryFileId, SharedMemoryAccess access) : base()
            {
                MemoryFileId = memoryFileId;
                VariablesMetas = new Dictionary<Property, SimVarMeta>();
                if (access != SharedMemoryAccess.Read)
                {
                    VariablesToWrite = new HashSet<SimVarMeta>();
                }
                if (access != SharedMemoryAccess.Write)
                {
                    VariablesToRead = new HashSet<SimVarMeta>();
                }
                AccessType = access;
                IsMapped = false;
                group = this;
            }

            public void Property_IndividualValueChanged(Property property, object oldValue, object newValue)
            {
                SimVarMeta simVar = null;
                if (VariablesMetas.TryGetValue(property, out simVar))
                {
                    simVar.valueToSet = newValue;
                    VariablesToWrite.Add(simVar);
                }
            }

            public void Property_ParentValueChanged(Property property, object oldValue, object newValue)
            {
                SimVarMeta simVar = null;
                if (VariablesMetas.TryGetValue(property, out simVar))
                {
                    VariablesToWrite.Add(simVar.parent ?? simVar.group);
                }
            }

            public void Property_GroupValueChanged(Property property, object oldValue, object newValue)
            {
                SimVarMeta simVar = null;
                if (VariablesMetas.TryGetValue(property, out simVar))
                {
                    VariablesToWrite.Add(simVar.group);
                }
            }

            public void MemoryMap(IntPtr iShmPtr)
            {
                if (iShmPtr != IntPtr.Zero)
                {
                    handle = new IntPtr(iShmPtr.ToInt64() + offset);
                    Debug.Log("Mapping Handle " + handle);
                    UpdateHandles(iShmPtr);
                    IsMapped = true;
                }
            }
        }

        private class SimVarMeta
        {
            public Property property;
            public string propertyPath;
            public int offset;
            public int size;
            public Type type;
            public IntPtr handle;
            public SimVarGroup group;
            public object valueToSet;

            public SimVarMeta parent;
            public List<SimVarMeta> children;

            public SimVarMeta()
            {
                children = new List<SimVarMeta>();
            }

            public virtual void UpdateHandles(IntPtr iShmPtr)
            {
                if (parent != null)
                {
                    handle = new IntPtr(iShmPtr.ToInt64() + offset);
                }
                foreach (var child in children)
                {
                    child.UpdateHandles(iShmPtr);
                }
            }
        }
    }
}