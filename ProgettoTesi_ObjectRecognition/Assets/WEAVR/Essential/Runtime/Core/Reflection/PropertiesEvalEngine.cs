namespace TXT.WEAVR.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// This class is reponsible for evaluation of groups of properties. 
    /// Instead of evaluating each <see cref="Property"/> individually, 
    /// this class does it once per frame for a subset of properties.
    /// This in practice avoids any extra computation of any of property wrappers 
    /// and allows notifying (via callbacks) property changes
    /// </summary>
    [Stateless]
    [AddComponentMenu("WEAVR/Setup/Properties Evaluation Engine")]
    public class PropertiesEvalEngine : MonoBehaviour, IWeavrSingleton
    {
        public delegate void PropertyChangedSimpleCallback(object newValue);
        private static PropertiesEvalEngine _instance;

        private static PropertiesEvalEngine Instance {
            get {
                if(_instance == null) {
                    _instance = FindObjectOfType<PropertiesEvalEngine>();
                    if (_instance == null) {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("PropertyEvaluationEngine");
                        _instance = go.AddComponent<PropertiesEvalEngine>().InitializeInstance();
                        _instance.transform.SetParent(ObjectRetriever.WEAVR.transform, false);
                    }
                }
                return _instance;
            }
        }

        private Dictionary<string, EvaluationGroup> _evalGroups;
        private HashSet<EvaluationGroup> _activeGroups;
        private List<EvaluationGroup> _evaluationGroups;
        private bool _needsEvaluationGroupsRefresh;
        private List<EvaluationProperty> _obsoleteProperties;
        private int _evaluationCounter;

        // Use this for initialization
        void Awake() {
            if (_instance != null && _instance != this) {
                Debug.LogWarning(GetType().Name + " has been already started, thus this object (" + name + ") will be destroyed.");
                Destroy(this);
                return;
            }
            _instance = this;
            InitializeInstance();
        }

        private PropertiesEvalEngine InitializeInstance() {
            _evalGroups = new Dictionary<string, EvaluationGroup>();
            _activeGroups = new HashSet<EvaluationGroup>();
            _obsoleteProperties = new List<EvaluationProperty>();
            _evaluationGroups = new List<EvaluationGroup>();
            _evaluationCounter = 0;
            _needsEvaluationGroupsRefresh = true;
            return this;
        }

        // Update is called once per frame
        void Update() {
            if(_activeGroups.Count == 0) {
                return;
            }
            if (_needsEvaluationGroupsRefresh) {
                _evaluationGroups.Clear();
                _evaluationGroups.AddRange(_activeGroups);
                _needsEvaluationGroupsRefresh = false;
            }
            _evaluationCounter++;
            _obsoleteProperties.Clear();
            foreach(var group in _evaluationGroups) {
                group.RemoveObsoleteProperties(_obsoleteProperties);
                group.Evaluate(_evaluationCounter);
            }
        }

        /// <summary>
        /// Registers property for evaluation
        /// </summary>
        /// <param name="groupId">The group this property should be in</param>
        /// <param name="property">The property to evaluate</param>
        /// <param name="callbacks">The callbacks to be called when the property changes value</param>
        public static void RegisterProperty(string groupId, Property property, params PropertyChangedSimpleCallback[] callbacks) {
            EvaluationGroup group = null;
            if (!Instance._evalGroups.TryGetValue(groupId, out group)) {
                group = new EvaluationGroup(groupId);
                Instance._evalGroups.Add(groupId, group);
            }
            group.AddPropertyToEvaluate(new EvaluationProperty(property, callbacks));
        }

        /// <summary>
        /// Creates property wrapper and registers it for evaluation
        /// </summary>
        /// <param name="groupId">The group this property should be in</param>
        /// <param name="propertyPath">The path of the property</param>
        /// <param name="owner">The object with the specified property</param>
        /// <param name="callbacks">The callbacks to be called when the property changes value</param>
        /// <returns>The registered <see cref="Property"/> object</returns>
        public static Property RegisterProperty(string groupId, string propertyPath, Object owner, params PropertyChangedSimpleCallback[] callbacks) {
            var property = Property.Create(owner, propertyPath);
            if (property != null) {
                RegisterProperty(groupId, property, callbacks);
            }
            return property;
        }

        /// <summary>
        /// Loads an evaluation group into evaluation loop. All properties in the group will be evaluated
        /// </summary>
        /// <param name="groupId">The id of the group to evaluate</param>
        public static void LoadEvaluationGroup(string groupId) {
            EvaluationGroup group = null;
            if(Instance._evalGroups.TryGetValue(groupId, out group)) {
                Instance._activeGroups.Add(group);
                Instance._needsEvaluationGroupsRefresh = true;
            }
        }

        /// <summary>
        /// Unloads evaluation group from the evaluation loop.
        /// </summary>
        /// <param name="groupId">The id of the group to unload</param>
        public static void UnloadEvaluationGroup(string groupId) {
            EvaluationGroup group = null;
            if (Instance._evalGroups.TryGetValue(groupId, out group)) {
                Instance._activeGroups.Remove(group);
                Instance._needsEvaluationGroupsRefresh = true;
            }
        }

        private class EvaluationGroup
        {
            public string Id { get; private set; }
            public List<EvaluationProperty> PropertiesToEvaluate { get; private set; }

            public EvaluationGroup(string groupId) {
                Id = groupId;
                PropertiesToEvaluate = new List<EvaluationProperty>();
            }

            public void AddPropertyToEvaluate(EvaluationProperty property) {
                if (!PropertiesToEvaluate.Contains(property)) {
                    PropertiesToEvaluate.Add(property);
                }
                else {
                    var existingProperty = PropertiesToEvaluate[PropertiesToEvaluate.IndexOf(property)];
                    existingProperty.AddCallbacks(property.SimpleCallbacks);
                }
            }

            public void Evaluate(int evaluationId) {
                foreach(var propertyEval in PropertiesToEvaluate) {
                    propertyEval.Evaluate(evaluationId);
                }
            }

            /// <summary>
            /// Checks and removes the obsolete properties from the evaluation loop.
            /// </summary>
            /// <param name="obsoleteProperties">Used to help identify and simplify checks for next groups</param>
            public void RemoveObsoleteProperties(List<EvaluationProperty> obsoleteProperties) {
                for (int i = 0; i < PropertiesToEvaluate.Count; i++) {
                    if(PropertiesToEvaluate[i].Wrapper.Owner == null) {
                        PropertiesToEvaluate.RemoveAt(i--);
                    }
                }
                //obsoleteProperties.Clear();
                //foreach (var property in PropertiesToEvaluate) {
                //    if(property.Wrapper.Owner == null) {
                //        obsoleteProperties.Add(property);
                //    }
                //}
                //foreach(var property in obsoleteProperties) {
                //    PropertiesToEvaluate.Remove(property);
                //}
            }
        }

        private void OnDisable()
        {
            Weavr.UnregisterSingleton(this);
        }

        private class EvaluationProperty
        {
            public Property Wrapper { get; private set; }
            public HashSet<PropertyChangedSimpleCallback> SimpleCallbacks { get; private set; }

            public object value;

            private object _lastValue;
            private bool _lastOutcome;
            private int _lastEvaluationId;

            public EvaluationProperty(Property propertyWrapper, params PropertyChangedSimpleCallback[] callbacks) {
                Wrapper = propertyWrapper;
                SimpleCallbacks = new HashSet<PropertyChangedSimpleCallback>(callbacks);
                _lastValue = null;
                value = null;
            }

            public bool Evaluate(int evaluationId) {
                if (evaluationId == _lastEvaluationId) {
                    return _lastOutcome;
                }
                _lastEvaluationId = evaluationId;
                value = Wrapper.Value;
                if(!Equals(value, _lastValue)) {
                    foreach(var callback in SimpleCallbacks) {
                        callback(value);
                    }
                    _lastValue = value;
                    _lastOutcome = true;
                }
                else {
                    _lastOutcome = false;
                }
                return _lastOutcome;
            }
            
            public void AddCallbacks(IEnumerable<PropertyChangedSimpleCallback> callbacks) {
                foreach (var callback in callbacks) {
                    SimpleCallbacks.Add(callback);
                }
            }

            // override object.Equals
            public override bool Equals(object obj) {
                return (obj is EvaluationProperty || obj is Property) && Wrapper.GetHashCode() == obj.GetHashCode();
            }

            // override object.GetHashCode
            public override int GetHashCode() {
                return Wrapper.GetHashCode();
            }
        }
    }
}