using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TXT.WEAVR.Core;
using TXT.WEAVR.Interaction;
using Newtonsoft.Json;
using System.IO;

namespace TXT.WEAVR.Procedure
{
    [AddComponentMenu("WEAVR/Setup/Procedure State Manager")]
    public class ProcedureStateManager : MonoBehaviour, IWeavrSingleton
    {
        #region [ STATIC FIELDS ]
        private static ProcedureStateManager s_instance;
        public static ProcedureStateManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = Weavr.GetInCurrentScene<ProcedureStateManager>();
                    if (s_instance == null)
                    {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("ProcedureStateManager");
                        s_instance = go.AddComponent<ProcedureStateManager>();
                        s_instance.transform.SetParent(ObjectRetriever.WEAVR.transform, false);
                    }
                }
                return s_instance;
            }
        }

        public static UnityAction<string> CheckFolders;
        public static UnityAction<ProcedureMaterialData, string> CreateAsset;
        public static UnityAction SuccessfulSaveState;
        #endregion

        #region [ SERIALIZED FIELDS ]
        [SerializeField]
        [HideInInspector]
        private bool m_saveState;
        public bool SaveState { get => m_saveState; set => m_saveState = value; }
        #endregion

        #region [ RUNTIME FIELDS ]
        private ProcedureRunner m_procedureRunner;
        private Procedure m_runningProcedure;
        private ExecutionMode m_executionMode;

        private ProcedureState m_procedureState;

        private int m_snapshotCount;
        private List<GameObject> m_serializedGameObjects;
        #endregion

        private void Start()
        {
            ProcedureRunner.Current.ProcedureStarted -= Initialize;
            ProcedureRunner.Current.ProcedureStarted += Initialize;
        }

        private async void Initialize(ProcedureRunner _runner, Procedure _procedure, ExecutionMode _mode)
        {
            m_procedureRunner = _runner;
            m_runningProcedure = _procedure;
            m_executionMode = _mode;

            CheckFolders?.Invoke("ProcedureStateData");

            if (SaveState)
            {
                m_procedureRunner.ProcedureFinished -= WriteState;
                m_procedureRunner.ProcedureFinished += WriteState;

                m_procedureRunner.OnFlowCreated.RemoveListener(OnFlowCreated);
                m_procedureRunner.OnFlowCreated.AddListener(OnFlowCreated);

                m_procedureRunner.OnFlowEnded.RemoveListener(OnFlowEnded);
                m_procedureRunner.OnFlowEnded.AddListener(OnFlowEnded);

                m_serializedGameObjects = FindObjectToSerialize();
                m_procedureState = new ProcedureState();
                m_procedureState.materialData = InstantiateMaterialData();
            }
            else
            {
                await LoadState();             
            }
        }

        #region [ SAVE STATE ]
        private List<GameObject> FindObjectToSerialize()
        {
            var gameObjectsToSerialize = m_runningProcedure.Graph.ReferencesTable.GetGameObjects().ToList();

            var rigidbodiesInScene = SceneTools.GetComponentsInScene<Rigidbody>().ToList();
            var interactionsInScene = SceneTools.GetComponentsInScene<AbstractInteractiveBehaviour>().ToList();

            for (int i = 0; i < rigidbodiesInScene.Count; i++)
            {
                if (!gameObjectsToSerialize.Contains(rigidbodiesInScene[i].gameObject))
                {
                    if (rigidbodiesInScene[i].GetComponent<UniqueID>() != null)
                        gameObjectsToSerialize.Add(rigidbodiesInScene[i].gameObject);
                    else
                        Debug.LogWarning("Missing UniqueID on a GameObject with Rigibody not in procedure." +
                            "ProcedureStateManager won't serialize the state of the object." +
                            " - " + rigidbodiesInScene[i].gameObject.name);
                }
            }

            for (int i = 0; i < interactionsInScene.Count; i++)
            {
                if (!gameObjectsToSerialize.Contains(interactionsInScene[i].gameObject))
                {
                    if (interactionsInScene[i].GetComponent<UniqueID>() != null)
                        gameObjectsToSerialize.Add(interactionsInScene[i].gameObject);
                    else
                        Debug.LogWarning("Missing UniqueID on a Interactive Object not in procedure." +
                            "ProcedureStateManager won't serialize the state of the object." +
                            " - " + interactionsInScene[i].gameObject.name);
                }
            }

            return gameObjectsToSerialize;
        }

        private void OnFlowCreated(ExecutionFlow _executionFlow)
        {
            _executionFlow.ContextChanged -= OnContextChange;
            _executionFlow.ContextChanged += OnContextChange;
        }

        private void OnFlowEnded(ExecutionFlow _executionFlow)
        {
            _executionFlow.ContextChanged -= OnContextChange;
        }

        private void OnContextChange(ExecutionFlow _executionFlow, IFlowContext _newContext)
        {
            if (_newContext is GenericNode genericNode)
                SnapshotStep(genericNode, _executionFlow);
        }

        private void SnapshotStep(GenericNode _node, ExecutionFlow _executionFlow)
        {
            if (_node == null)
                return;

            if (m_procedureState.stepsStates == null)
                m_procedureState.stepsStates = new List<StepSerializableState>();

            var stepState = GetStepSerializableState(_node.StepGUID);
            if (stepState != null)
            {
                if (IsConcurrentNode(_executionFlow, stepState))
                    stepState.AddConcurrentNode(_node.Guid);
            }
            else
            {
                stepState = new StepSerializableState();
                if (stepState.Snapshot(_node, m_snapshotCount, m_serializedGameObjects))
                {
                    m_procedureState.stepsStates.Add(stepState);
                    m_snapshotCount++;
                    SerializeConcurrentSteps(_node, _executionFlow, stepState);
                }
            }
        }

        private bool IsConcurrentNode(ExecutionFlow _executionFlow, StepSerializableState _stepState)
        {
            if (_executionFlow.ExecutionEngine.RunningFlows.Count > 1)
            {
                foreach (var flow in _executionFlow.ExecutionEngine.RunningFlows)
                {
                    if (flow.CurrentContext is GenericNode otherNode && otherNode.Guid == _stepState.nodeGUID)
                        return true;
                }
            }

            return false;
        }

        private void SerializeConcurrentSteps(GenericNode _node, ExecutionFlow _executionFlow, StepSerializableState _currentStepState)
        {
            if (_executionFlow.ExecutionEngine.RunningFlows.Count > 1)
            {
                StepSerializableState serializedStep = null;
                foreach (var flow in _executionFlow.ExecutionEngine.RunningFlows)
                {
                    if (flow.CurrentContext is GenericNode step && step != _node)
                    {
                        _currentStepState.AddConcurrentNode(step.Guid);
                        serializedStep = GetStepSerializableState(step.Guid);
                        if (serializedStep != null)
                            serializedStep.AddConcurrentNode(step.Guid);
                    }
                }
            }
        }

        private async void WriteState(ProcedureRunner _runner, Procedure _procedure)
        {
            await WriteStateAsync(_runner, _procedure);
        }

        private async Task WriteStateAsync(ProcedureRunner _runner, Procedure _procedure)
        {
            m_procedureState.componentMembers = BaseSerializableState.componentMembers;

            CreateMaterialDataAsset(m_procedureState.materialData);

            /*
             * If this function doesn't modify a file or create a new one,
             * comment the Task for showing serializarion errors.
            */
            await Task.Run(() =>
            {
                string json = JsonConvert.SerializeObject(m_procedureState);
                Weavr.WriteToConfigFile(m_runningProcedure.ProcedureName + "_State.json", json);
            });

            SuccessfulSaveState?.Invoke();
        }
        #endregion

        #region [ LOAD STATE ]
        private async Task LoadState()
        {
            if (Weavr.TryGetConfigFilePath(m_runningProcedure.ProcedureName + "_State.json", out string configFilePath))
            {
                m_procedureState = await Task.Run(() =>
                {
                    ProcedureState procedureState = null;
                    try
                    {
                        string fileContent = File.ReadAllText(configFilePath);
                        procedureState = JsonUtility.FromJson<ProcedureState>(fileContent);
                        if (procedureState != null)
                        {
                            BaseSerializableState.componentMembers = procedureState.componentMembers;
                        }
                    }
                    catch (Exception ex)
                    {
                        WeavrDebug.LogException(this, ex);
                    }
                    return procedureState;
                });

                if (m_procedureState != null)
                {
                    m_procedureState.materialData = LoadMaterialData();
                }
            }
        }

        public void GoToStep(string _stepGUID)
        {
            var stepToRestore = m_procedureState.stepsStates.FirstOrDefault(s => s.stepGUID == _stepGUID);
            RestoreStep(stepToRestore);
        }

        private void RestoreStep(StepSerializableState _stepToRestore)
        {
            if (_stepToRestore == null)
                return;

            var statesToRestore = new Dictionary<string, StepSerializableState>() { { _stepToRestore.nodeGUID, _stepToRestore } };
            if (_stepToRestore.concurrentNodes != null && _stepToRestore.concurrentNodes.Count > 0)
            {
                foreach (var stepGUID in _stepToRestore.concurrentNodes)
                {
                    statesToRestore.Add(stepGUID, GetStepSerializableState(stepGUID));
                }
            }

            var steps = new List<IFlowContext>();
            ProcedureObject step = null;
            foreach (var state in statesToRestore)
            {
                step = m_runningProcedure.Find(state.Key);
                if (step != null)
                {
                    steps.Add(step as IFlowContext);
                    if (state.Value != null)
                        state.Value.Restore();
                }
            }

            StartExecutionFLows(steps);
            //RestartExecutionFLows(steps);
        }

        private void StartExecutionFLows(List<IFlowContext> _steps)
        {
            for (int i = m_procedureRunner.RunningFlows.Count - 1; i >= 0; i--)
            {
                m_procedureRunner.RunningFlows[i].EndCurrentContext();
                m_procedureRunner.StopExecutionFlow(m_procedureRunner.RunningFlows[i]);
            }

            ResetSteps(_steps);

            foreach (var step in _steps)
                m_procedureRunner.StartExecutionFlow(!(step is BaseNode node) || m_procedureRunner.CurrentProcedure.Graph.StartingNodes.Contains(node), m_executionMode, step);
        }

        private void RestartExecutionFLows(List<IFlowContext> _steps)
        {
            ResetSteps(_steps);

            var flowsDelta = _steps.Count - m_procedureRunner.RunningFlows.Count;
            if (flowsDelta == 0)
            {
                for (int i = 0; i < m_procedureRunner.RunningFlows.Count; i++)
                {
                    m_procedureRunner.RunningFlows[i].ExecuteExclusive(_steps[i]);
                }
            }
            else if (flowsDelta > 0)
            {
                for (int i = m_procedureRunner.RunningFlows.Count - 1; i >= 0; i--)
                {
                    m_procedureRunner.RunningFlows[i].ExecuteExclusive(_steps[i]);
                    _steps.Remove(_steps[i]);
                }

                foreach (var step in _steps)
                {
                    m_procedureRunner.StartExecutionFlow(!(step is BaseNode node) || m_procedureRunner.CurrentProcedure.Graph.StartingNodes.Contains(node), m_executionMode, step);
                }
            }
            else if (flowsDelta < 0)
            {
                flowsDelta = Mathf.Abs(flowsDelta);
                int flowToStopIndex;
                for (int i = 0; i < flowsDelta; i++)
                {
                    flowToStopIndex = m_procedureRunner.RunningFlows.Count - 1 - i;
                    m_procedureRunner.RunningFlows[flowToStopIndex].EndCurrentContext();
                    m_procedureRunner.StopExecutionFlow(m_procedureRunner.RunningFlows[flowToStopIndex]);
                }

                for (int i = m_procedureRunner.RunningFlows.Count - 1; i >= 0; i--)
                {
                    m_procedureRunner.RunningFlows[i].ExecuteExclusive(_steps[i]);
                    _steps.Remove(_steps[i]);
                }
            }
        }

        private void ResetSteps(List<IFlowContext> _steps)
        {
            var supersteps = _steps.Select(s => s is BaseNode node ? node.Step : s is LocalTransition transition ? transition.NodeA.Step : null)
                                    .Where(s => s != null).Distinct();
            foreach (var sStep in supersteps)
            {
                ResetNodes(sStep);
            }
        }

        private void ResetNodes(BaseStep _superStep)
        {
            foreach (var node in _superStep.Nodes)
            {
                if (node is TrafficNode trafficNode)
                    trafficNode.Reset();
            }
        }
        #endregion

        #region [ MATERIAL STATE ]
        private ProcedureMaterialData InstantiateMaterialData()
        {
            return ScriptableObject.CreateInstance<ProcedureMaterialData>();
        }

        private void CreateMaterialDataAsset(ProcedureMaterialData _materialData)
        {
            var path = "Assets/Resources/ProcedureStateData/" + m_runningProcedure.ProcedureName + "_MaterialState.asset";
            CreateAsset?.Invoke(_materialData, path);
        }

        private ProcedureMaterialData LoadMaterialData()
        {
            return Resources.Load<ProcedureMaterialData>("ProcedureStateData/" + m_runningProcedure.ProcedureName + "_MaterialState");
        }

        public int SnapshotMaterial(Material _material)
        {
            return m_procedureState.materialData.Snapshot(_material);
        }

        public Material RestoreMaterial(int _id)
        {
            return m_procedureState.materialData.Restore(_id);
        }
        #endregion

        private StepSerializableState GetStepSerializableState(string _stepGUID)
        {
            return m_procedureState.stepsStates.FirstOrDefault(s => s.stepGUID == _stepGUID);
        }

        [Serializable]
        class ProcedureState
        {
            public List<ComponentMembersData> componentMembers;
            public List<StepSerializableState> stepsStates;

            [NonSerialized]
            public ProcedureMaterialData materialData;
        }
    }
}
