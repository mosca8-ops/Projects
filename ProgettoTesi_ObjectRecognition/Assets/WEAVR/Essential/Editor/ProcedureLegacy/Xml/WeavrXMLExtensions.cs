namespace TXT.WEAVR.Xml {
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;
    using TXT.WEAVR.Legacy;
    using UnityEngine;

    public partial class XmlProcedure {
        /// <summary>
        /// Gets the data objects dictionary where all data objects are listed by their id
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, DataBundleObject> DataObjectsDictionary { get; private set; }

        /// <summary>
        /// Gets the dictionary of steps
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, XmlStep> StepsDictionary { get; private set; }

        // <summary>
        /// Gets the dictionary of super steps
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, XmlSuperStep> SuperStepsDictionary { get; private set; }

        public XmlProcedure() {
            DataObjectsDictionary = new Dictionary<string, DataBundleObject>();
            SuperStepsDictionary = new Dictionary<string, XmlSuperStep>();
            StepsDictionary = new Dictionary<string, XmlStep>();
        }

        partial void OnXmlLoaded() {
            if (DataObjects != null && DataObjectsDictionary.Count != DataObjects.Length) {
                foreach (var dataObject in DataObjects) {
                    DataObjectsDictionary.Add(dataObject.Id, dataObject);
                }
            }
            if (Steps != null) {
                foreach (var step in Steps) {
                    var superStep = new XmlSuperStep(step);
                    step.SuperStep = superStep;
                    SuperStepsDictionary.Add(step.ID, superStep);
                    StepsDictionary.Add(step.ID, step);
                }
            }
            if(SuperSteps == null) {
                SuperSteps = new XmlSuperStep[0];
            }
            foreach(var superStep in SuperSteps) {
                SuperStepsDictionary.Add(superStep.ID, superStep);
                foreach(var step in superStep.Steps)
                {
                    step.SuperStep = superStep;
                    StepsDictionary.Add(step.ID, step);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="SceneObject"/> with the specified id
        /// </summary>
        /// <param name="id">The id of the <see cref="SceneObject"/></param>
        /// <returns>The <see cref="SceneObject"/> with the specified id, or null if not found</returns>
        public SceneObject GetSceneObject(string id) {
            DataBundleObject dataBundleObject = null;
            if (DataObjectsDictionary.TryGetValue(id, out dataBundleObject)) {
                return dataBundleObject as SceneObject;
            }
            return null;
        }

        /// <summary>
        /// Gets the <see cref="AssetObject"/> with the specified id
        /// </summary>
        /// <param name="id">The id of the <see cref="AssetObject"/></param>
        /// <returns>The <see cref="AssetObject"/> with the specified id, or null if not found</returns>
        public AssetObject GetAssetObject(string id) {
            DataBundleObject dataBundleObject = null;
            if (DataObjectsDictionary.TryGetValue(id, out dataBundleObject)) {
                return dataBundleObject as AssetObject;
            }
            return null;
        }
    }

    public partial class Condition {
        /// <summary>
        /// Returns as a link condition
        /// </summary>
        /// <returns></returns>
        public LinkCondition GetAsLinkCondition() {
            return new LinkCondition() {
                ConditionId = ConditionId
            };
        }
    }
    
    public partial class XmlStepAction {
        private ExecutionMode? _executionMode;

        [XmlIgnore]
        public ExecutionMode ExecutionMode {
            get {
                if (!_executionMode.HasValue) {
                    _executionMode = ExecutionModeHelper.Parse(ExecutionModeString);
                }
                return _executionMode.Value;
            }
            set {
                if (_executionMode != value) {
                    _executionMode = value;
                    ExecutionModeString = value.ToFullString();
                }
            }
        }
    }
}