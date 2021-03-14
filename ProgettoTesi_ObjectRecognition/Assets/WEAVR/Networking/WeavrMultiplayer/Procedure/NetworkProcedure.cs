using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Procedure;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace TXT.WEAVR.Networking
{

    [AddComponentMenu("WEAVR/Network/Network Procedure")]
    public class NetworkProcedure : MonoBehaviour, IOnEventCallback, IInRoomCallbacks, IProcedureStartValidator
    {
        #region [  STATIC PART  ]

        private static List<NetworkProcedure> s_activeComponents = new List<NetworkProcedure>();
        private static byte s_progressiveNetworkId = 1;
        private const byte k_ProcedureValidationNetworkId = 0;

        #endregion

        #region [  INSPECTOR PART  ]

        [SerializeField]
        private ProcedureRunner m_runner;

        [Header("Configuration")]
        [SerializeField]
        private bool m_onlyShareableContexts = false;
        [SerializeField]
        private bool m_preciseResume = true;

        private bool m_inlineResume = true;

        [Header("Components")]
        [SerializeField]
        private GameObject m_outOfSync;
        [SerializeField]
        private GameObject m_waitingPlayers;

        #endregion

        #region [  PRIVATE FIELDS  ]

        private Dictionary<ExecutionFlow, FlowInfo> m_flows = new Dictionary<ExecutionFlow, FlowInfo>();
        private Dictionary<int, int> m_deadFlowsIDs = new Dictionary<int, int>();

        private bool m_hasSyncStarted;
        private bool m_isRebuildingPath;
        private int m_rebuildStartIndex;
        private bool m_isOfflineProcedure;
        private ProcedureInfo m_procedureInfo;
        private ProcedureValidation m_ongoingProcedureValidation;

        private RaiseEventOptions m_defaultRaiseEventOptions;
        private RaiseEventOptions m_clearOptions;
        private RaiseEventOptions m_tempOptions;
        private SendOptions m_defaultSendOptions;

        private Dictionary<OperationType, Options> m_operationOptions = new Dictionary<OperationType, Options>()
        {
            { OperationType.ConditionEvaluationChanged, new Options(true, true, EventCaching.DoNotCache) },
            { OperationType.ProcedureStarted,           new Options(true, true, EventCaching.DoNotCache) },
            { OperationType.ProcedureEnded,             new Options(true, true, EventCaching.DoNotCache) },
            { OperationType.IsConnectedToProcedure,     new Options(true, true, EventCaching.DoNotCache) },
            { OperationType.ContextChanged,             new Options(true, true, EventCaching.DoNotCache) },
            { OperationType.ActiveNetObjValueChanged,   new Options(true, true, EventCaching.DoNotCache) },
            { OperationType.FullPathUpdate,             new Options(false, true, EventCaching.DoNotCache) },
            { OperationType.PreciseResumePoint,         new Options(false, true, EventCaching.DoNotCache) },
            { OperationType.ReadyToStart,               new Options(false, true, EventCaching.DoNotCache) },
            { OperationType.WaitingForPlayers,          new Options(false, true, EventCaching.DoNotCache) },
        };

        private HashSet<INetworkProcedureObject> m_networkObjects = new HashSet<INetworkProcedureObject>();

        private List<FlowPoint> m_flowsPaths = new List<FlowPoint>();
        private List<FlowPoint> m_prevFlowsPaths = new List<FlowPoint>();
        private List<FlowPoint> m_tempFlowsPaths = new List<FlowPoint>();

        public bool IsProcedureMaster => PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer.IsMasterClient;

        #endregion

        #region [  ENUMS DEFINITIONS  ]

        public enum OperationType
        {
            ReadyToStart = 1,
            WaitingForPlayers = 2,
            ProcedureStarted = 10,
            ProcedureEnded = 11,
            IsConnectedToProcedure = 15,
            FlowStarted = 20,
            FlowEnded = 21,
            ContextChanged = 22,
            ConditionEvaluationChanged = 30,
            ActiveNetObjValueChanged = 31,
            FullPathUpdate = 50,
            PreciseResumePoint = 51,
        }

        public enum SyncContextType
        {
            Unknown = 0,
            Node = 1,
            Transition = 2,
            FlowEnded = 15,
        }

        #endregion

        #region [  INTERNAL CLASSES  ]

        private class Options
        {
            public RaiseEventOptions raiseEventOptions;
            public SendOptions sendOptions;
            public bool immediateSend;

            public Options(bool immediateSend = false,
                bool reliability = true,
                EventCaching cachingOptions = EventCaching.DoNotCache,
                ReceiverGroup receivers = ReceiverGroup.Others)
            {
                this.immediateSend = immediateSend;
                raiseEventOptions = new RaiseEventOptions()
                {
                    CachingOption = cachingOptions,
                    Receivers = receivers,
                };
                sendOptions = new SendOptions()
                {
                    Reliability = reliability
                };
            }
        }

        private class FlowInfo
        {
            public ExecutionFlow Flow { get; private set; }
            public IFlowContext prevContext;
            public IFlowContext context;
            public IFlowContext nextContext;
            public IFlowContext networkContext;
            public int operationId;
            public int networkOperationId;
            public int restoredFromPathId;
            public bool willAlignOperationIdWithNetwork;

            public FlowInfo(ExecutionFlow flow)
            {
                Flow = flow;
                context = flow.CurrentContext;
                nextContext = flow.NextContext;
                operationId = 0;
            }

            public void Update(bool incrementOperation)
            {
                if (Flow.CurrentContext != context)
                {
                    prevContext = context;
                }
                context = Flow.CurrentContext;
                nextContext = Flow.NextContext;
                if (incrementOperation)
                {
                    operationId++;
                }
            }
        }

        private struct FlowPoint
        {
            public int flowId;
            public IFlowContext context;

            public FlowPoint(int flowId, IFlowContext context)
            {
                this.flowId = flowId;
                this.context = context;
            }
        }

        private class ProcedureInfo
        {
            public readonly Procedure.Procedure procedure;
            public readonly string procedureGuid;
            public readonly byte networkId;
            public readonly byte executionModeIndex;
            public int? procedureStarter;
            public bool procedureIsRunning;
            public HashSet<int> players { get; } = new HashSet<int>();

            public ExecutionMode ExecutionMode => procedure.ExecutionModes[executionModeIndex];

            public ProcedureInfo(Procedure.Procedure procedure, ExecutionMode mode, byte networkId)
            {
                this.procedure = procedure;
                procedureGuid = procedure.Guid;
                this.networkId = networkId;
                executionModeIndex = (byte)procedure.ExecutionModes.IndexOf(mode);
            }
        }

        private class ProcedureValidation
        {
            public Procedure.Procedure procedure;
            public ExecutionMode mode;
        }

        #endregion

        #region [  PROPERTIES  ]

        public ProcedureRunner Runner
        {
            get
            {
                if (!m_runner)
                {
                    m_runner = this.TryGetSingleton<ProcedureRunner>();
                }
                return m_runner;
            }
        }

        #endregion

        #region [  UNITY CALLBACKS  ]

        private void OnValidate()
        {
            if (!m_runner)
            {
                m_runner = FindObjectOfType<ProcedureRunner>();
            }
        }

        private void Awake()
        {
            m_defaultRaiseEventOptions = new RaiseEventOptions()
            {
                Receivers = ReceiverGroup.Others,
                CachingOption = EventCaching.DoNotCache,
            };
            m_defaultSendOptions = new SendOptions()
            {
                Reliability = true,
            };
            m_clearOptions = new RaiseEventOptions()
            {
                CachingOption = EventCaching.RemoveFromRoomCache,
                Receivers = ReceiverGroup.All,
                InterestGroup = 0,
            };
            m_tempOptions = new RaiseEventOptions()
            {
                Receivers = ReceiverGroup.Others,
                CachingOption = EventCaching.DoNotCache,
            };

            m_hasSyncStarted = false;
        }

        // Start is called before the first frame update
        void OnEnable()
        {
            if (!s_activeComponents.Contains(this))
            {
                s_activeComponents.Add(this);
            }

            Runner.RegisterStartValidator(this);

            Runner.ProcedureStarted -= Runner_ProcedureStarted;
            Runner.ProcedureStarted += Runner_ProcedureStarted;

            Runner.ProcedureFinished -= Runner_ProcedureFinished;
            Runner.ProcedureFinished += Runner_ProcedureFinished;

            Runner.OnFlowCreated.RemoveListener(OnFlowCreated);
            Runner.OnFlowCreated.AddListener(OnFlowCreated);
            Runner.OnFlowEnded.RemoveListener(OnFlowEnded);
            Runner.OnFlowEnded.AddListener(OnFlowEnded);

            foreach (var flow in Runner.RunningFlows)
            {
                OnFlowCreated(flow);
            }

            m_hasSyncStarted = false;

            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            s_activeComponents.Remove(this);

            PhotonNetwork.RemoveCallbackTarget(this);

            if (Runner)
            {
                Runner.UnregisterStartValidator(this);

                Runner.ProcedureStarted -= Runner_ProcedureStarted;

                Runner.ProcedureFinished -= Runner_ProcedureFinished;

                Runner.OnFlowCreated.RemoveListener(OnFlowCreated);
                Runner.OnFlowEnded.RemoveListener(OnFlowEnded);
            }
        }

        #endregion

        #region [  FLOW CREATED/ENDED PART  ]

        private void OnFlowEnded(ExecutionFlow flow)
        {
            if (m_isOfflineProcedure || !flow.IsPrimaryFlow) { return; }

            flow.ContextChanged -= Flow_ContextChanged;

            if (m_flows.TryGetValue(flow, out FlowInfo info))
            {
                if (info.context is INetworkProcedureObjectsContainer container && !IsContextUsedByOtherFlows(flow, info.context))
                {
                    UnregisterNetworkObjects(container);
                }
                //SendEvent(OperationType.FlowEnded, flow.Id);
            }
            m_flows.Remove(flow);
            m_flowsPaths.Add(new FlowPoint(flow.Id, null));

            UpdateFlowsPaths(flow);
            m_deadFlowsIDs[flow.Id] = 0;
        }

        private void OnFlowCreated(ExecutionFlow flow)
        {
            if (m_isOfflineProcedure || !flow.IsPrimaryFlow) { return; }

            flow.ContextChanged -= Flow_ContextChanged;
            flow.ContextChanged += Flow_ContextChanged;

            if (!m_isRebuildingPath)
            {
                m_flows[flow] = new FlowInfo(flow);
            }

            m_deadFlowsIDs.Remove(flow.Id);
        }

        #endregion
        
        #region [  CONTEXTS PART  ]

        private void Flow_ContextChanged(ExecutionFlow flow, IFlowContext newContext)
        {
            if (m_flows.TryGetValue(flow, out FlowInfo info)
                && info.context is INetworkProcedureObjectsContainer prevContainer
                && !IsContextUsedByOtherFlows(flow, info.context))
            {
                UnregisterNetworkObjects(prevContainer);
            }

            info?.Update(true);

            if (newContext is INetworkProcedureObjectsContainer container)
            {
                RegisterNetworkObjects(container);
            }

            m_flowsPaths.Add(new FlowPoint(flow.Id, info.context));
            UpdateFlowsPaths(flow);

            if (!m_onlyShareableContexts || newContext.CanBeShared)
            {
                info.networkContext = info.context;
                SendFlowEventIfNeeded(OperationType.ContextChanged, flow, newContext);
            }

            if (info != null)
            {
                //AutoEnableOutOfSyncObject();
                EnableOutOfSyncObject(info.operationId < info.networkOperationId);
            }
        }

        private bool IsContextUsedByOtherFlows(ExecutionFlow flow, IFlowContext context)
        {
            foreach (var f in Runner.RunningFlows)
            {
                if (f != flow && f.CurrentContext == context)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region [  NETWORK OBJECTS PART  ]

        private void RegisterNetworkObjects(INetworkProcedureObjectsContainer container)
        {
            foreach (var obj in container.NetworkObjects)
            {
                if (obj is IActiveNetworkProcedureObject activeObj)
                {
                    activeObj.OnLocalValueChanged -= ActiveObject_OnLocalValueChanged;
                    activeObj.OnLocalValueChanged += ActiveObject_OnLocalValueChanged;

                    m_networkObjects.Add(activeObj);
                }
                else if (obj is BaseCondition condition)
                {
                    condition.LocalEvaluationChanged -= Condition_EvaluationChanged;
                    condition.LocalEvaluationChanged += Condition_EvaluationChanged;

                    m_networkObjects.Add(condition);
                }
            }
        }

        private void UnregisterNetworkObjects(INetworkProcedureObjectsContainer container)
        {
            foreach (var obj in container.NetworkObjects)
            {
                if (obj is IActiveNetworkProcedureObject activeObj)
                {
                    activeObj.OnLocalValueChanged -= ActiveObject_OnLocalValueChanged;

                    m_networkObjects.Remove(activeObj);
                }
                else if (obj is BaseCondition condition)
                {
                    condition.LocalEvaluationChanged -= Condition_EvaluationChanged;

                    m_networkObjects.Remove(condition);
                }
            }
        }

        private void ClearNetworkObjects()
        {
            foreach (var obj in m_networkObjects)
            {
                if (obj is IActiveNetworkProcedureObject activeObj)
                {
                    activeObj.OnLocalValueChanged -= ActiveObject_OnLocalValueChanged;
                }
                else if (obj is BaseCondition condition)
                {
                    condition.LocalEvaluationChanged -= Condition_EvaluationChanged;
                }
            }
            m_networkObjects.Clear();
            m_hasSyncStarted = false;
            m_prevFlowsPaths.Clear();
            m_flowsPaths.Clear();
        }

        private void ActiveObject_OnLocalValueChanged(IActiveNetworkProcedureObject obj)
        {
            SendObjectEvent(OperationType.ActiveNetObjValueChanged, obj.ID, obj.SerializeForNetwork());

            //EnableOutOfSyncObject(m_operationId >= m_maxOperationId);
        }

        private void Condition_EvaluationChanged(BaseCondition condition, bool value)
        {
            if (condition.NetworkValue != value)
            {
                SendObjectEvent(OperationType.ConditionEvaluationChanged, condition.Guid, value);
            }

            //ToggleOutOfSync(m_operationId >= m_maxOperationId);
        }

        #endregion

        #region [  PROCEDURE START/END PART  ]

        public bool ValidateProcedureStart(Procedure.Procedure procedure, ExecutionMode mode)
        {
            if (!procedure.IsNetworkEnabled || !PhotonNetwork.InRoom || !procedure.MinNumberOfPlayers.HasValue) { return true; }

            if(procedure.MinNumberOfPlayers.Value <= PhotonNetwork.CurrentRoom.PlayerCount)
            {
                m_ongoingProcedureValidation = null;
                EnableWaitPlayersObject(false);
                return true;
            }
            else
            {
                if (m_ongoingProcedureValidation?.procedure != procedure || m_ongoingProcedureValidation.mode != mode)
                {
                    m_ongoingProcedureValidation = new ProcedureValidation()
                    {
                        procedure = procedure,
                        mode = mode,
                    };
                }

                NotificationManager.NotificationWarning("Procedure will start when other players will join");
                EnableWaitPlayersObject(procedure.MinNumberOfPlayers.Value - PhotonNetwork.CurrentRoom.PlayerCount);
                SendEvent(OperationType.WaitingForPlayers, TryCompactGuid(procedure.Guid), (byte)(procedure.MinNumberOfPlayers.Value - PhotonNetwork.CurrentRoom.PlayerCount));
                return false;
            }
        }

        private void Runner_ProcedureStarted(ProcedureRunner runner, Procedure.Procedure procedure, ExecutionMode mode)
        {
            if (!procedure.IsNetworkEnabled) {
                m_isOfflineProcedure = true;
                return; 
            }

            m_isOfflineProcedure = false;

            m_ongoingProcedureValidation = null;
            EnableWaitPlayersObject(false);

            if (m_procedureInfo?.procedureIsRunning != true)
            {
                m_hasSyncStarted = true;
                EnableOutOfSyncObject(false);
                m_procedureInfo = new ProcedureInfo(procedure, mode, GetNextAvailableId())
                {
                    procedureStarter = PhotonNetwork.LocalPlayer.ActorNumber,
                    procedureIsRunning = true,
                };
                // Check if online and update the master when this player connects after the procedure has started
                SendEvent(OperationType.ReadyToStart, TryCompactGuid(procedure.Guid));
                SendEvent(OperationType.ProcedureStarted, TryCompactGuid(procedure.Guid), m_procedureInfo.networkId, m_procedureInfo.executionModeIndex);
            }
            else
            {
                EnableOutOfSyncObject(true);
            }

            m_procedureInfo?.players.Add(PhotonNetwork.LocalPlayer.ActorNumber);
            SendEvent(OperationType.IsConnectedToProcedure, m_procedureInfo.networkId);
        }

        private void Runner_ProcedureFinished(ProcedureRunner runner, Procedure.Procedure procedure)
        {
            if (IsProcedureMaster && m_procedureInfo != null)
            {
                SendEvent(OperationType.ProcedureEnded, m_procedureInfo.networkId);
                ClearAllCachedEvents();
                ClearNetworkObjects();
            }
            if(m_procedureInfo != null)
            {
                m_procedureInfo.procedureIsRunning = false;
            }
            EnableOutOfSyncObject(false);
        }

        #endregion

        #region [  NETWORK EVENT MANAGEMENT  ]

        private void ClearAllCachedEvents()
        {
            PhotonNetwork.RaiseEvent(NetworkEvents.ProcedureEvent, null, m_clearOptions, m_defaultSendOptions);
        }

        private void RemoveCachedEvent(OperationType opType)
        {
            if (m_operationOptions.TryGetValue(opType, out Options options)
                && (options.raiseEventOptions.CachingOption == EventCaching.AddToRoomCache
                 || options.raiseEventOptions.CachingOption == EventCaching.AddToRoomCacheGlobal))
            {
                PhotonNetwork.RaiseEvent(NetworkEvents.ProcedureEvent, new object[] { opType }, m_clearOptions, m_defaultSendOptions);
            }
        }

        private void RemoveCachedEvent(OperationType opType, object param1)
        {
            if (m_operationOptions.TryGetValue(opType, out Options options)
                && (options.raiseEventOptions.CachingOption == EventCaching.AddToRoomCache
                 || options.raiseEventOptions.CachingOption == EventCaching.AddToRoomCacheGlobal))
            {
                PhotonNetwork.RaiseEvent(NetworkEvents.ProcedureEvent, new object[] { opType, param1 }, m_clearOptions, m_defaultSendOptions);
            }
        }

        private void SendFlowEventIfNeeded(OperationType opType, ExecutionFlow flow, IFlowContext context)
        {
            if (m_flows.TryGetValue(flow, out FlowInfo info))
            {
                if (info.operationId > info.networkOperationId && context is ProcedureObject pObj)
                {
                    SendEvent(opType, m_procedureInfo.networkId, (byte)flow.Id, TryCompactGuid(pObj.Guid), info.operationId);
                }
            }
        }

        protected void SendEvent(OperationType opType, params object[] values) => SendEvent(opType, null, values);

        protected void SendEvent(OperationType opType, int[] targetActors, params object[] values)
        {
            if (!PhotonNetwork.InRoom) { return; }

            // Create custom data
            object[] content = new object[values.Length + 1];
            content[0] = opType;
            for (int i = 0; i < values.Length; i++)
            {
                content[i + 1] = values[i];
            }

            SendEventWithContent(opType, targetActors, content);
        }

        protected void SendObjectEvent(OperationType opType, string objId, params object[] values) => SendObjectEvent(opType, objId, null, values);

        protected void SendObjectEvent(OperationType opType, string objId, int[] targetActors, params object[] values)
        {
            if (!PhotonNetwork.InRoom) { return; }

            // Create custom data
            object[] content = new object[values.Length + 3];
            content[0] = opType;
            content[1] = m_procedureInfo.networkId;
            content[2] = TryCompactGuid(objId);
            for (int i = 0; i < values.Length; i++)
            {
                content[i + 3] = values[i];
            }

            SendEventWithContent(opType, targetActors, content);
        }

        private void SendEventWithContent(OperationType opType, int[] targetActors, object[] content)
        {
            if (m_operationOptions.TryGetValue(opType, out Options options))
            {
                SendEventWithContent(content, targetActors, options.immediateSend, options.raiseEventOptions, options.sendOptions);
            }
            else
            {
                SendEventWithContent(content, targetActors, false, m_defaultRaiseEventOptions, m_defaultSendOptions);
            }
        }

        private void SendEventWithContent(object[] content, int[] targetActors, bool immediateSend, RaiseEventOptions eventOptions, SendOptions sendOptions)
        {
            m_tempOptions.CachingOption = eventOptions.CachingOption;
            m_tempOptions.Receivers = eventOptions.Receivers;
            m_tempOptions.InterestGroup = eventOptions.InterestGroup;
            m_tempOptions.TargetActors = targetActors ?? eventOptions.TargetActors;

            PhotonNetwork.RaiseEvent(NetworkEvents.ProcedureEvent, content, m_tempOptions, sendOptions);
            if (immediateSend)
            {
                PhotonNetwork.SendAllOutgoingCommands();
            }
        }

        #endregion

        #region [  IOnEventCallback IMPLEMENTATION  ]

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != NetworkEvents.ProcedureEvent
                || !(photonEvent.CustomData is object[] data)
                || data.Length < 1) return;

            // OperationType, OperationId
            var opType = (OperationType)data[0];

            FlowInfo flowInfo;
            (ExecutionFlow flow, IFlowContext context, int opId) values;

            //WeavrDebug.Log(this, $"Message Received: {opType}: {data.Skip(1).Select(i => i.ToString()).Aggregate((a, s) => a = a == null ? s : a + ", " + s)}");

            if(opType != OperationType.ProcedureStarted 
                && opType != OperationType.ReadyToStart 
                && opType != OperationType.WaitingForPlayers
                && m_procedureInfo?.networkId != (byte)data[1])
            {
                // Here we are pointing to another procedure
                return;
            }

            switch (opType)
            {
                case OperationType.ReadyToStart:
                    m_hasSyncStarted = true;
                    break;
                case OperationType.WaitingForPlayers:
                    EnableWaitPlayersObject((byte)data[2]);
                    break;
                case OperationType.IsConnectedToProcedure:
                    if (m_procedureInfo != null)
                    {
                        m_procedureInfo.players.Add(photonEvent.Sender);
                        EnableOutOfSyncObject(m_procedureInfo.procedure.MinNumberOfPlayers > m_procedureInfo.players.Count);
                    }
                    break;
                case OperationType.ProcedureStarted:
                    // ProcedureGuid, NetworkId, ModeIndex
                    StartProcedure(photonEvent, TryParseGuid(data[1]), (byte)data[2], (byte)data[3]);
                    break;
                case OperationType.ProcedureEnded:
                    // ProcedureNetworkID,
                    Runner.StopCurrentProcedure();
                    ClearNetworkObjects();
                    break;
                case OperationType.FullPathUpdate:
                    if (data[2] is byte[] bytes)
                    {
                        if (!m_hasSyncStarted)
                        {
                            m_flowsPaths.Clear();
                            RestorePaths(bytes, m_flowsPaths);
                            AlignFlowPaths();
                            try
                            {
                                if (!m_preciseResume)
                                {
                                    ReapplyAndResumePath(0, null);
                                    m_rebuildStartIndex = 0;
                                }
                            }
                            finally
                            {
                                m_hasSyncStarted = true;
                            }
                        }
                        else
                        {
                            List<FlowPoint> receivedPath = new List<FlowPoint>();
                            RestorePaths(bytes, receivedPath);

                            // Get the part where to start reapplying the path
                            m_rebuildStartIndex = receivedPath.Count;
                            for (int i = 0; i < receivedPath.Count; i++)
                            {
                                if (i >= m_flowsPaths.Count || !m_flowsPaths[i].Equals(receivedPath[i]))
                                {
                                    m_rebuildStartIndex = i;
                                    break;
                                }
                            }

                            m_flowsPaths = receivedPath;

                            if (m_rebuildStartIndex == receivedPath.Count)
                            {
                                // Nothing to update
                                break;
                            }

                            if (!m_preciseResume)
                            {
                                ReapplyAndResumePath(m_rebuildStartIndex, null);
                                m_rebuildStartIndex = 0;
                            }
                        }
                    }
                    break;
                case OperationType.PreciseResumePoint:
                    if(m_preciseResume && data[2] is byte[] byteArray)
                    {
                        ReapplyAndResumePath(m_rebuildStartIndex, byteArray);
                        m_rebuildStartIndex = 0;
                        //ResumeCurrentExecution(DeserializeResumePoints(byteArray));
                    }
                    break;
                case OperationType.ContextChanged:
                    // ProcedureNetworkID, FlowId, ContextGuid, FlowOperationId
                    values = DeconstructFlowOperationData(data);
                    if (!values.flow)
                    {
                        //if (!m_deadFlowsIDs.ContainsKey((byte)data[2]))
                        //{
                        //    WeavrDebug.LogError(this, $"Unable to retrieve flow with id {data[2]}");
                        //}
                        break;
                    }
                    if (m_flows.TryGetValue(values.flow, out flowInfo) && flowInfo.networkOperationId < values.opId)
                    {
                        flowInfo.networkOperationId = values.opId;
                        flowInfo.networkContext = values.context;
                    }
                    if (flowInfo?.willAlignOperationIdWithNetwork == true)
                    {
                        flowInfo.operationId = values.opId;
                    }
                    EnableOutOfSyncObject(flowInfo.operationId < flowInfo.networkOperationId);
                    break;
                case OperationType.ConditionEvaluationChanged:
                    // ProcedureNetworkID, ConditionId, Value
                    if (Runner.CurrentProcedure.Find(TryParseGuid(data[2])) is BaseCondition localCondition)
                    {
                        localCondition.NetworkValue = (bool)data[3];
                    }
                    break;
                case OperationType.ActiveNetObjValueChanged:
                    // ProcedureNetworkID, ObjID, Values[] -> starting from data[3]
                    if (Runner.CurrentProcedure.Find(TryParseGuid(data[2])) is IActiveNetworkProcedureObject activeLocalObj)
                    {
                        object[] valuesToConsume = new object[data.Length - 3];
                        for (int i = 0; i < valuesToConsume.Length; i++)
                        {
                            valuesToConsume[i] = data[i + 3];
                        }
                        activeLocalObj.ConsumeFromNetwork(valuesToConsume);
                    }
                    break;
                case OperationType.FlowStarted:
                    // ProcedureNetworkID, FlowId
                    //Runner.StartExecutionFlow()
                    break;
                case OperationType.FlowEnded:
                    // ProcedureNetworkID, FlowId
                    Runner.StopExecutionFlow((int)data[2], raiseEvents: true);
                    break;
                default:

                    break;
            }
        }
        
        private (ExecutionFlow flow, IFlowContext context, int opId) DeconstructFlowOperationData(object[] data)
        {
            return (Runner.GetFlow((byte)data[2]), Runner.CurrentProcedure.Find(TryParseGuid(data[3])) as IFlowContext, (int)data[4]);
        }

        private void StartProcedure(EventData photonEvent, string guid, byte networkId, byte executionModeIndex)
        {
            Procedure.Procedure procedureToRun = Runner.CurrentProcedure && Runner.CurrentProcedure.Guid == guid ?
                                                    Runner.CurrentProcedure :
                                                    Procedure.Procedure.TryFind(guid, out Procedure.Procedure procedure) ?
                                                    procedure : null;

            if (procedureToRun && m_procedureInfo?.procedureIsRunning != true)
            {
                m_procedureInfo = new ProcedureInfo(procedureToRun, procedureToRun.ExecutionModes[executionModeIndex], networkId)
                {
                    procedureStarter = photonEvent.Sender,
                    procedureIsRunning = true,
                };
                if (m_hasSyncStarted)
                {
                    Runner.StartProcedure(procedureToRun, m_procedureInfo.ExecutionMode);
                }
                else
                {
                    Runner.PrepareProcedure(procedureToRun, m_procedureInfo.ExecutionMode);
                }
            }
            else if(m_procedureInfo?.procedureGuid == guid && m_procedureInfo.networkId != networkId && !procedureToRun.MaxNumberOfPlayers.HasValue)
            {
                m_procedureInfo = new ProcedureInfo(procedureToRun, procedureToRun.ExecutionModes[executionModeIndex], networkId)
                {
                    procedureStarter = photonEvent.Sender,
                    procedureIsRunning = true,
                };
            }
        }

        #endregion

        #region [  SERIALIZATION PART  ]

        private short PackToInt16(BaseGraph graph, IFlowContext context)
        {
            var (contextType, contextIndex) = GetContextShortID(graph, context);
            return (short)(((int)contextType << 12) | (contextIndex & 0x0FFF));
        }

        private IFlowContext UnpackFromInt16(BaseGraph graph, short value)
        {
            var contextType = (SyncContextType)((value & 0xF000) >> 12);
            int contextIndex = value & 0x0FFF;

            switch (contextType)
            {
                case SyncContextType.Node: return graph.Nodes[contextIndex];
                case SyncContextType.Transition: return graph.Transitions[contextIndex];
            }

            return null;
        }

        private (SyncContextType contextType, int contextIndex) GetContextShortID(BaseGraph graph, IFlowContext context)
        {
            switch (context)
            {
                case BaseNode node: return (SyncContextType.Node, graph.Nodes.IndexOf(node));
                case BaseTransition transition: return (SyncContextType.Transition, graph.Transitions.IndexOf(transition));
            }

            return (SyncContextType.Unknown, -1);
        }
        
        private byte[] SerializePaths(List<FlowPoint> flowsPaths)
        {
            // 3 bytes per context (12bit per context id --> max 4096 contexts of the same type!!)
            // byte 1: flow id
            // byte 2: context type (4bit) + most significant bits of context id (4bit)
            // byte 3: least significant bits of context id (8bit)

            var graph = Runner.CurrentProcedure.Graph;
            byte[] data = new byte[flowsPaths.Count * 3];
            for (int i = 0, j = 0; i < flowsPaths.Count; i++, j += 3)
            {
                int contextIndex;
                SyncContextType contextType;
                switch (flowsPaths[i].context)
                {
                    case BaseNode node:
                        contextType = SyncContextType.Node;
                        contextIndex = graph.Nodes.IndexOf(node);
                        break;
                    case BaseTransition transition:
                        contextType = SyncContextType.Transition;
                        contextIndex = graph.Transitions.IndexOf(transition);
                        break;
                    case null:
                        contextType = SyncContextType.FlowEnded;
                        contextIndex = 0;
                        break;
                    default:
                        contextType = SyncContextType.Unknown;
                        contextIndex = 0;
                        break;
                }

                data[j] = (byte)flowsPaths[i].flowId;
                data[j + 1] = (byte)(((int)contextType << 4) | (contextIndex & 0x0F00) >> 4);
                data[j + 2] = (byte)(contextIndex & 0xFF);
            }

            return data;
        }

        private void RestorePaths(byte[] data, List<FlowPoint> destination)
        {
            // Deserialize First, --> use the serialization logic to deserialize
            var graph = Runner.CurrentProcedure.Graph;
            for (int i = 0; i < data.Length; i += 3)
            {
                SyncContextType contextType = (SyncContextType)((data[i + 1] & 0xF0) >> 4);
                if (contextType == SyncContextType.Unknown)
                {
                    continue;
                }

                int flowId = data[i];
                int contextIndex = ((data[i + 1] & 0x0F) << 8) | data[i + 2];

                switch (contextType)
                {
                    case SyncContextType.Node:
                        destination.Add(new FlowPoint(flowId, graph.Nodes[contextIndex]));
                        break;
                    case SyncContextType.Transition:
                        destination.Add(new FlowPoint(flowId, graph.Transitions[contextIndex]));
                        break;
                    case SyncContextType.FlowEnded:
                        destination.Add(new FlowPoint(flowId, null));
                        break;
                    default:

                        break;
                }
            }

            //// Update current flows situation
            //UpdateFlowsInfosFromPaths();

            //// Reapply the path
            //ReapplyPath();
        }

        private byte[] SerializeResumePoints()
        {
            List<byte> data = new List<byte>();
            IFlowElement elem = null;
            foreach(var flow in m_flows.Keys)
            {
                if (flow.CurrentContext is IFlowProvider provider && (elem = flow.TryGetCurrentElement()) != null)
                {
                    var elemIndex = provider.GetFlowElements()?.IndexOf(elem);
                    if (elemIndex >= 0)
                    {
                        data.Add((byte)flow.Id);
                        data.Add((byte)elemIndex.Value);
                    }
                }
            }

            return data.ToArray();
        }

        private Dictionary<ExecutionFlow, IFlowElement> DeserializeResumePoints(byte[] data)
        {
            Dictionary<ExecutionFlow, IFlowElement> values = new Dictionary<ExecutionFlow, IFlowElement>();
            for (int i = 0; i < data.Length; i+=2)
            {
                var flow = Runner.GetFlow(data[i]);
                if (flow && m_flows.TryGetValue(flow, out FlowInfo info) && info.context is IFlowProvider provider)
                {
                    values[flow] = provider.GetFlowElements()[data[i + 1]];
                }
            }

            return values;
        }

        #endregion

        #region [  FLOWS PATHS RELATED  ]

        private void UpdateFlowsPaths(ExecutionFlow lastUpdatedFlow)
        {
            FindAndRemoveRepeatingPattern(lastUpdatedFlow, out int newCleanIndex);
            AlignFlowPaths();
        }
        
        private void AlignFlowPaths()
        {
            for (int i = m_prevFlowsPaths.Count; i < m_flowsPaths.Count; i++)
            {
                m_prevFlowsPaths.Add(m_flowsPaths[i]);
            }
        }

        private List<FlowPoint> ComputeDeltaPaths(List<FlowPoint> pathA, List<FlowPoint> pathB)
        {
            m_tempFlowsPaths.Clear();
            for (int i = pathA.Count; i < pathB.Count; i++)
            {
                m_tempFlowsPaths.Add(pathB[i]);
            }
            return m_tempFlowsPaths;
        }

        private bool FindAndRemoveRepeatingPattern(ExecutionFlow flow, out int newCleanIndex, int maxSteps = 10)
        {
            // Remove anything which repeats for 2 or more elements
            if (FindRepeatingPattern(flow, out int[] longestPattern, maxSteps))
            {
                for (int i = 0; i < longestPattern.Length; i++)
                {
                    m_flowsPaths.RemoveAt(longestPattern[i]);
                    if (i < m_prevFlowsPaths.Count)
                    {
                        m_prevFlowsPaths.RemoveAt(longestPattern[i]);
                    }
                }
                newCleanIndex = longestPattern[longestPattern.Length - 1];
                return true;
            }
            newCleanIndex = 0;
            return false;
        }

        private bool FindRepeatingPattern(ExecutionFlow flow, out int[] patternIndices, int maxSteps = 10)
        {
            List<int> flowIndices = new List<int>(maxSteps);
            IFlowContext lastContext = null;
            for (int i = m_flowsPaths.Count - 1; i > 0; i--)
            {
                if (m_flowsPaths[i].flowId != flow.Id) { continue; }
                if (lastContext == null)
                {
                    lastContext = m_flowsPaths[i].context;
                    continue;
                }

                flowIndices.Add(i);
            }

            int length = 0;
            int start = 0;
            for (int i = 0; i < flowIndices.Count && maxSteps > 0; i++, maxSteps--)
            {
                if (lastContext == m_flowsPaths[flowIndices[i]].context)
                {
                    // Potential pattern starting
                    int j = i + 1;
                    int k = 0;
                    while (j < flowIndices.Count && m_flowsPaths[flowIndices[j]].Equals(m_flowsPaths[flowIndices[k]]))
                    {
                        j++;
                        k++;
                    }

                    if (k > length)
                    {
                        start = i + 1;
                        length = k;
                    }
                }
            }

            // A pattern is considered repeated when 2 or more elements are repeating in the path
            if (length >= 2)
            {
                patternIndices = new int[length];
                int endIndex = start + length;
                int patternIndex = 0;
                for (int i = start; i < endIndex; i++)
                {
                    patternIndices[patternIndex++] = flowIndices[i];
                }
            }
            else
            {
                patternIndices = null;
            }
            return length >= 2;
        }

        private void ReapplyAndResumePath(int startIndex, byte[] resumePointsRawData)
        {
            try
            {
                m_isRebuildingPath = true;
                UpdateFlowsInfosFromPaths();
                var resumePoints = DeserializeResumePoints(resumePointsRawData);

                if (m_inlineResume)
                {
                    ReapplyPath(startIndex, resumExecution: true, resumePoints);
                }
                else
                {
                    ReapplyPath(startIndex);
                    ResumeCurrentExecution(resumePoints);
                }
                EnableOutOfSyncObject(false);
            }
            finally
            {
                m_isRebuildingPath = false;
            }
        }

        private void UpdateFlowsInfosFromPaths()
        {
            // First compute flows which are alive
            HashSet<int> aliveIDs = new HashSet<int>();
            for (int i = 0; i < m_flowsPaths.Count; i++)
            {
                if(m_flowsPaths[i].context == null)
                {
                    aliveIDs.Remove(m_flowsPaths[i].flowId);
                }
                else
                {
                    aliveIDs.Add(m_flowsPaths[i].flowId);
                }
            }

            // Then starting from the end reconstruct the current situation
            m_flows.Clear();
            for (int i = m_flowsPaths.Count - 1; i >= 0; i--)
            {
                if (!aliveIDs.Contains(m_flowsPaths[i].flowId))
                {
                    continue;
                }

                var flow = Runner.GetOrCreateFlow(m_flowsPaths[i].flowId, true);
                if(!m_flows.TryGetValue(flow, out FlowInfo info))
                {
                    info = new FlowInfo(flow);
                    info.context = m_flowsPaths[i].context;
                    info.networkContext = info.context;
                    info.willAlignOperationIdWithNetwork = true;
                    info.restoredFromPathId = i;
                    m_flows[flow] = info;
                }
                else if(info.prevContext == null)
                {
                    info.prevContext = m_flowsPaths[i].context;
                }
            }
        }

        private void ReapplyPath(int startIndex = 0, bool resumExecution = false, Dictionary<ExecutionFlow, IFlowElement> resumePoints = null)
        {
            ExecutionFlow previousFlow = null;
            List<IFlowContext> contextsToExecute = new List<IFlowContext>();
            HashSet<ExecutionFlow> flowsToKill = new HashSet<ExecutionFlow>();

            // Work on a snapshot of the path
            List<FlowPoint> flowPaths = new List<FlowPoint>(m_flowsPaths);
            HashSet<FlowInfo> flowsToResume = resumExecution ? new HashSet<FlowInfo>(m_flows.Values) : null;

            // Need to lock the flows engine, otherwise the traffic nodes will generate new flows
            Runner.Lockdown(this);

            for (int i = startIndex; i < flowPaths.Count; i++)
            {
                var flowPoint = flowPaths[i];

                var flow = Runner.GetFlow(flowPoint.flowId);

                if (!flow)
                {
                    // First need to unlock the flows engine to allow flows creation
                    Runner.ReleaseLockdown(this);
                    flow = Runner.GetOrCreateFlow(flowPoint.flowId, true);
                    Runner.Lockdown(this);
                }

                // Prevent automatic execution
                flow.AutoAdvance = false;

                // The execution mode can change during execution, so assign it only if not present
                if (!flow.ExecutionMode)
                {
                    flow.ExecutionMode = m_procedureInfo?.ExecutionMode ? m_procedureInfo.ExecutionMode : Runner.ExecutionMode;
                }

                if (flow != previousFlow)
                {
                    // Execute previously stored contexts
                    if (previousFlow && contextsToExecute.Count > 0)
                    {
                        previousFlow.ExecuteNetworkPartOnly(true, contextsToExecute.ToArray());
                    }
                    contextsToExecute.Clear();
                    previousFlow = flow;
                }

                // Do not execute if it is the latest context, it should be executed normally
                if (m_flows.TryGetValue(flow, out FlowInfo info))
                {
                    if (info.restoredFromPathId == i && info.context == flowPoint.context)
                    {
                        // Or execute it inline if the option is enabled
                        if (resumExecution)
                        {
                            // Execute the previous contexts
                            if (previousFlow && contextsToExecute.Count > 0)
                            {
                                previousFlow.ExecuteNetworkPartOnly(true, contextsToExecute.ToArray());
                                contextsToExecute.Clear();
                            }

                            Runner.ReleaseLockdown(this);
                            flowsToResume.Remove(info);
                            try
                            {
                                flow.AutoAdvance = false;
                                ResumeCurrentExecution(info, resumePoints);
                            }
                            finally
                            {
                                Runner.Lockdown(this);
                            }
                        }
                        continue;
                    }
                }
                else
                {
                    flowsToKill.Add(flow);
                }

                // Start piling up contexts for the same flow
                if (flowPoint.context != null)
                {
                    if (!m_onlyShareableContexts || flowPoint.context?.CanBeShared == true)
                    {
                        contextsToExecute.Add(flowPoint.context);
                    }
                    else
                    {
                        flowPoint.context.CurrentState = ContextState.Finished;
                    }
                }

            }

            // Execute the remaining ones
            if (previousFlow && contextsToExecute.Count > 0)
            {
                previousFlow.ExecuteNetworkPartOnly(true, contextsToExecute.ToArray());
            }

            // Release the flows engine
            Runner.ReleaseLockdown(this);

            // Resume the remaining ones
            if (resumExecution)
            {
                foreach(var info in flowsToResume)
                {
                    ResumeCurrentExecution(info, resumePoints);
                }
            }
            
            // Kill all unused flows
            foreach (var flow in flowsToKill)
            {
                Runner.StopExecutionFlow(flow);
            }
        }

        private void ResumeCurrentExecution(Dictionary<ExecutionFlow, IFlowElement> resumePoints = null)
        {
            foreach (var pair in m_flows)
            {
                ResumeCurrentExecution(pair.Value, resumePoints);
            }
        }

        private void ResumeCurrentExecution(FlowInfo flowInfo, Dictionary<ExecutionFlow, IFlowElement> resumePoints = null)
        {
            if (flowInfo.Flow.CurrentContext != flowInfo.context)
            {
                flowInfo.Flow.EnqueueContext(flowInfo.context);
            }
            flowInfo.Flow.AutoAdvance = true;

            if (resumePoints != null && resumePoints.TryGetValue(flowInfo.Flow, out IFlowElement elem))
            {
                if (flowInfo.Flow.TryGetCurrentElement() is IFlowElement currentElement)
                {
                    var resumeIndex = (flowInfo.context as IFlowProvider)?.GetFlowElements().IndexOf(elem);
                    var currentIndex = (flowInfo.context as IFlowProvider)?.GetFlowElements().IndexOf(currentElement);
                    if (currentIndex >= resumeIndex)
                    {
                        return;
                    }
                }

                var context = flowInfo.context;
                flowInfo.Flow.FastForward(f => f.CurrentContext != context || f.TryGetCurrentElement() == elem);
            }
        }

        #endregion

        #region [  IRoomCallbacks IMPLEMENTATION  ]

        public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            if (m_procedureInfo?.procedureIsRunning == true)
            {
                if ((m_procedureInfo.procedure.MaxNumberOfPlayers >= m_procedureInfo.players.Count) == false)
                {
                    // Send all the paths to the newcomer
                    if (IsProcedureMaster)
                    {
                        int[] targetActors = new int[] { newPlayer.ActorNumber };
                        SendEvent(OperationType.ProcedureStarted, targetActors, TryCompactGuid(m_procedureInfo.procedureGuid), m_procedureInfo.networkId, m_procedureInfo.executionModeIndex);
                        SendEvent(OperationType.FullPathUpdate, targetActors, m_procedureInfo.networkId, SerializePaths(m_flowsPaths));
                        if (m_preciseResume)
                        {
                            SendEvent(OperationType.PreciseResumePoint, targetActors, m_procedureInfo.networkId, SerializeResumePoints());
                        }
                    }

                    SendEvent(OperationType.IsConnectedToProcedure, new int[] { newPlayer.ActorNumber }, m_procedureInfo.networkId, PhotonNetwork.LocalPlayer.ActorNumber);
                }
                else
                {
                    WeavrDebug.LogError(this, $"Maximum amount of players reached for the procedure {m_procedureInfo.procedure.ProcedureName}");
                }
            }
            else if(m_ongoingProcedureValidation != null && IsProcedureMaster)
            {
                Runner.StartProcedure(m_ongoingProcedureValidation.procedure, m_ongoingProcedureValidation.mode);
            }
        }
        
        public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            // Remove from players list if present
            if(m_procedureInfo?.procedureIsRunning == true)
            {
                m_procedureInfo.players.Remove(otherPlayer.ActorNumber);
            }
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            // Nothing for now
        }

        public void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
        {
            // Nothing for now
        }

        public void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            // Nothing for now
        }

        #endregion

        #region [  HELPER FUNCTIONS  ]

        private static byte GetNextAvailableId()
        {
            while(s_activeComponents.Any(n => n.m_procedureInfo?.networkId == s_progressiveNetworkId))
            {
                s_progressiveNetworkId++;
                if (s_progressiveNetworkId > byte.MaxValue)
                {
                    s_progressiveNetworkId = byte.MinValue;
                }
            }
            return s_progressiveNetworkId;
        }

        private void EnableOutOfSyncObject(bool enable)
        {
            if (m_outOfSync)
            {
                m_outOfSync.SetActive(enable);
            }
        }

        private void EnableWaitPlayersObject(bool enable)
        {
            if (m_waitingPlayers)
            {
                m_waitingPlayers.SetActive(enable);
                if (enable && m_procedureInfo != null)
                {
                    var text = m_waitingPlayers.GetComponentInChildren<Text>();
                    if (text)
                    {
                        text.text = $"Waiting for {m_procedureInfo.procedure.MinNumberOfPlayers - m_procedureInfo.players.Count} more players";
                    }
                }
            }
        }

        private void EnableWaitPlayersObject(int playersToWait)
        {
            if (m_waitingPlayers)
            {
                m_waitingPlayers.SetActive(playersToWait > 0);
                if (playersToWait > 0)
                {
                    var text = m_waitingPlayers.GetComponentInChildren<Text>();
                    if (text)
                    {
                        text.text = playersToWait == 1 ? "Waiting for 1 more player" : $"Waiting for {playersToWait} more players";
                    }
                }
            }
        }

        private void AutoEnableOutOfSyncObject()
        {
            EnableOutOfSyncObject(!CheckIfProcedureIsInSync());
        }

        private bool CheckIfProcedureIsInSync()
        {
            foreach(var info in m_flows.Values)
            {
                if(info.context?.CanBeShared == true && info.context != info.networkContext)
                {
                    return false;
                }
            }
            return true;
        }

        private object TryCompactGuid(string guid) => Guid.TryParse(guid, out Guid result) ? result.ToByteArray() : guid as object;

        private string TryParseGuid(object data) => data is Guid guid ? guid.ToString() : data is byte[] byteArray && byteArray.Length == 16 ? new Guid(byteArray).ToString() : data?.ToString();

        #endregion

    }
}
