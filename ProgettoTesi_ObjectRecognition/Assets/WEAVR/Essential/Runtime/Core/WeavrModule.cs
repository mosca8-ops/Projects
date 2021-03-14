namespace TXT.WEAVR
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using TXT.WEAVR.EditorBridge;
    using TXT.WEAVR.Utility;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Defines a Weavr Module
    /// </summary>
    public abstract class WeavrModule : ScriptableObject
    {
        public enum OperationMode { OperationsSupport, VirtualTraining }

        [SerializeField]
        protected bool m_startEnabled;
        [SerializeField]
        protected OperationMode m_operationMode = OperationMode.VirtualTraining;
        public OperationMode Mode
        {
            get => m_operationMode;
            set
            {
                if(m_operationMode != value)
                {
                    m_operationMode = value;
                    OnOperationsModeChanged();
                }
            }
        }
        
        protected float m_applyProgress = 0;
        public virtual float Progress {
            get { return m_applyProgress; }
        }

        protected bool m_isRunning = false;
        public virtual bool IsRunning {
            get { return m_isRunning; }
        }

        public bool StartEnabled => m_startEnabled;

        private List<GameObject> m_objectsToReference = new List<GameObject>();
        protected List<GameObject> ObjectsToFix => m_objectsToReference;

        private Dictionary<GameObject, GameObject> m_sceneReferences;
        protected Dictionary<GameObject, GameObject> SceneReferences => m_sceneReferences;

        public abstract void InitializeData(Scene scene);
        public abstract IEnumerator ApplyData(Scene scene, Dictionary<Type, WeavrModule> otherModules);

        public virtual IEnumerator FixModuleReferences(Scene scene, Dictionary<Type, WeavrModule> otherModules) {
            m_applyProgress = 0;
            m_sceneReferences = new Dictionary<GameObject, GameObject>();
            float progressQuant = Mathf.Max(10f / m_objectsToReference.Count, 1) * 0.5f;
            int counter = 0;
            while(counter < m_objectsToReference.Count) {
                for (int i = counter; i < counter + 10 && i < m_objectsToReference.Count; i++) {
                    var sceneObject = GetOrCreateSceneObject(scene, m_objectsToReference[i]);
                    if(sceneObject) {
                        m_sceneReferences[m_objectsToReference[i]] = sceneObject;
                    }
                }
                yield return new WaitForEndOfFrame();
                counter += 10;
                m_applyProgress += progressQuant;
            }

            m_applyProgress = 0.5f;
            counter = 0;
            foreach(var keyValuePair in m_sceneReferences) {
                counter++;
                PrefabUtility.FixReferences(keyValuePair.Key, keyValuePair.Value);
                if(counter >= 10) {
                    counter = 0;
                    yield return new WaitForEndOfFrame();
                    m_applyProgress += progressQuant;
                }
            }
            m_applyProgress = 1;
            OnSetupFinished();
            yield return null;
        }

        protected void RegisterObjectInScene(GameObject gameObject) {
            if (gameObject != null) {
                m_objectsToReference.Add(gameObject);
            }
        }

        protected virtual T AddComponentIfNotPresent<T>(GameObject go) where T : Component {
            return go?.GetComponent<T>() ?? go?.AddComponent<T>();
        }

        protected virtual GameObject GetOrCreateSceneObject(Scene scene, GameObject sample, string defaultName) {
            GameObject sceneObjectInstance;
            if (!SceneTools.ExistsInScene(scene, sample, out sceneObjectInstance)) {

                if (SceneTools.ExistsSimilarInScene(scene, sample, out sceneObjectInstance))
                {
                    SceneTools.RepairTree(sceneObjectInstance, sample, false);
                }
                else
                {
                    if (sample != null)
                    {
                        if (Application.isEditor && PrefabUtility.IsPrefabAsset(sample))
                        {
                            return PrefabUtility.InstantiatePrefabInScene(sample, scene) as GameObject;
                        }
                        sceneObjectInstance = Instantiate(sample);
                        sceneObjectInstance.name = sceneObjectInstance.name.Replace("(Clone)", "");
                        sceneObjectInstance.SetActive(sample.activeSelf);
                    }
                    else
                    {
                        // Create a new scene object
                        sceneObjectInstance = new GameObject(defaultName);
                    }
                }
            }

            return sceneObjectInstance;
        }

        protected virtual GameObject GetOrCreateSceneObject(Scene scene, GameObject sample) {
            GameObject sceneObjectInstance = null;
            if (sample != null && !SceneTools.ExistsInScene(scene, sample, out sceneObjectInstance)) {
                if (SceneTools.ExistsSimilarInScene(scene, sample, out sceneObjectInstance))
                {
                    SceneTools.RepairTree(sceneObjectInstance, sample, false);
                }
                else
                {
                    if (PrefabUtility.IsPrefabAsset(sample))
                    {
                        return PrefabUtility.InstantiatePrefabInScene(sample, scene) as GameObject;
                    }
                    sceneObjectInstance = Instantiate(sample);
                }
            }

            return sceneObjectInstance;
        }

        public virtual void OnUpdate()
        {

        }

        protected virtual void OnOperationsModeChanged()
        {

        }

        protected virtual void OnSetupFinished()
        {
            foreach (var pair in SceneReferences)
            {
                try
                {
                    PrefabUtility.UnpackInstance(pair.Value, PrefabUnpackMode.Completely);
                }
                catch { }
            }
        }
    }
}