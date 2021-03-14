using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Networking
{

    [AddComponentMenu("WEAVR/Network/Simple Network Procedure")]
    public class SimpleNetworkProcedure : MonoBehaviour, IOnEventCallback
    {
        [SerializeField]
        private ProcedureRunner m_runner;

        [Header("Configuration")]
        [SerializeField]
        private bool m_shareFlowCreation = false;
        [SerializeField]
        private bool m_shareFlowDestroy = false;
        [SerializeField]
        private bool m_onlyShareableContexts = true;
        [SerializeField]
        private bool m_forceCreateMissingFlows = false;

        private Dictionary<ExecutionFlow, (int localId, int maxId)> m_flowOperations = new Dictionary<ExecutionFlow, (int localId, int maxId)>();
        private HashSet<ExecutionFlow> m_mutedFlows = new HashSet<ExecutionFlow>();
        private int m_operationId = 0;
        private int m_maxOperationId = 0;

        private RaiseEventOptions m_raiseEventOptions;
        private SendOptions m_sendOptions;

        public enum OperationType
        {
            ProcedureStarted,
            ProcedureEnded,
            ContextAdded,
            ContextRemoved,
            ContextChanged,
            ContextFinished,
            FlowStarted,
            FlowEnded,
        }

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

        /* MESSAGE PROTOCOL
         * Parameters:
         *      OperationType
         *      OperationId
         *      CustomOperationData
         * 
         * 
         */

        private void OnValidate()
        {
            if (!m_runner)
            {
                m_runner = FindObjectOfType<ProcedureRunner>();
            }
        }

        private void Awake()
        {
            m_raiseEventOptions = new RaiseEventOptions()
            {
                Receivers = ReceiverGroup.Others,
                CachingOption = EventCaching.AddToRoomCache,
            };
            m_sendOptions = new SendOptions()
            {
                Reliability = true,
            };
        }

        // Start is called before the first frame update
        void OnEnable()
        {
            Runner.ProcedureStarted -= Runner_ProcedureStarted;
            Runner.ProcedureStarted += Runner_ProcedureStarted;

            Runner.ProcedureFinished -= Runner_ProcedureFinished;
            Runner.ProcedureFinished += Runner_ProcedureFinished;

            Runner.OnFlowCreated.RemoveListener(OnFlowCreated);
            Runner.OnFlowCreated.AddListener(OnFlowCreated);
            Runner.OnFlowEnded.RemoveListener(OnFlowEnded);
            Runner.OnFlowEnded.AddListener(OnFlowEnded);

            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);

            if (Runner)
            {
                Runner.ProcedureStarted -= Runner_ProcedureStarted;

                Runner.ProcedureFinished -= Runner_ProcedureFinished;

                Runner.OnFlowCreated.RemoveListener(OnFlowCreated);
                Runner.OnFlowEnded.RemoveListener(OnFlowEnded);
            }
        }

        private void OnFlowEnded(ExecutionFlow flow)
        {
            flow.ContextChanged -= Flow_ContextChanged;
            flow.ContextAdded -= Flow_ContextAdded;
            flow.ContextFinished -= Flow_ContextFinished;
            flow.ContextRemoved -= Flow_ContextRemoved;

            if(m_shareFlowDestroy && m_flowOperations.TryGetValue(flow, out (int localId, int maxId) ids) && ids.localId >= ids.maxId)
            {
                SendEvent(OperationType.FlowEnded, flow.Id);
            }
            m_flowOperations.Remove(flow);
        }
        
        private void OnFlowCreated(ExecutionFlow flow)
        {
            if (flow.IsPrimaryFlow)
            {
                flow.ContextChanged -= Flow_ContextChanged;
                flow.ContextChanged += Flow_ContextChanged;
                //flow.ContextAdded -= Flow_ContextAdded;
                //flow.ContextAdded += Flow_ContextAdded;
                //flow.ContextFinished -= Flow_ContextFinished;
                //flow.ContextFinished += Flow_ContextFinished;
                //flow.ContextRemoved -= Flow_ContextRemoved;
                //flow.ContextRemoved += Flow_ContextRemoved;
            }

            m_flowOperations[flow] = (0, 0);
        }

        
        private void Flow_ContextChanged(ExecutionFlow flow, IFlowContext newContext)
        {
            if (!m_onlyShareableContexts || newContext.CanBeShared)
            {
                SendEventIfNeeded(OperationType.ContextChanged, flow, newContext);
            }
        }

        private void Flow_ContextRemoved(ExecutionFlow flow, IFlowContext newContext)
        {
            if (!m_onlyShareableContexts || newContext.CanBeShared)
            {
                SendEventIfNeeded(OperationType.ContextRemoved, flow, newContext);
            }
        }

        private void Flow_ContextFinished(ExecutionFlow flow, IFlowContext context)
        {
            if (!m_onlyShareableContexts || context.CanBeShared)
            {
                SendEventIfNeeded(OperationType.ContextFinished, flow, context);
            }
        }

        private void Flow_ContextAdded(ExecutionFlow flow, IFlowContext newContext)
        {
            if (!m_onlyShareableContexts || newContext.CanBeShared)
            {
                SendEventIfNeeded(OperationType.ContextAdded, flow, newContext);
            }
        }

        private void Runner_ProcedureStarted(ProcedureRunner runner, Procedure.Procedure procedure, ExecutionMode mode)
        {
            if(m_operationId >= m_maxOperationId)
            {
                SendEvent(OperationType.ProcedureStarted, procedure.Guid, procedure.ExecutionModes.IndexOf(mode));
                m_maxOperationId = m_operationId;
            }
            m_operationId = m_maxOperationId;
        }

        private void Runner_ProcedureFinished(ProcedureRunner runner, Procedure.Procedure procedure)
        {
            if (m_operationId >= m_maxOperationId)
            {
                SendEvent(OperationType.ProcedureEnded, procedure.Guid);
                m_maxOperationId = m_operationId;
            }
            m_operationId = m_maxOperationId;
        }

        private void SendEventIfNeeded(OperationType opType, ExecutionFlow flow, IFlowContext context)
        {
            if (!m_mutedFlows.Contains(flow) && m_flowOperations.TryGetValue(flow, out (int localId, int maxId) ids))
            {
                ids.localId++;
                if (ids.localId > ids.maxId && context is ProcedureObject pObj){
                    ids.maxId = ids.localId;
                    SendEvent(opType, flow.Id, pObj.Guid, ids.localId);
                }
                //else
                //{
                //    ids.localId = ids.maxId;
                //}
                m_flowOperations[flow] = ids;
            }
            //ids.localId = ids.maxId;
        }

        protected void SendEvent(OperationType opType, params object[] values)
        {
            m_operationId++;

            // Create custom data
            object[] content = new object[values.Length + 2];
            content[0] = opType;
            content[1] = m_operationId;
            for (int i = 0; i < values.Length; i++)
            {
                content[i + 2] = values[i];
            }

            PhotonNetwork.RaiseEvent(NetworkEvents.SimpleNetworkProcedureEvent, content, m_raiseEventOptions, m_sendOptions);
        }

        private (ExecutionFlow flow, IFlowContext context, int opId) DeconstructData(object[] data)
        {
            return (Runner.GetFlow((int)data[2]), Runner.CurrentProcedure.Find(data[3] as string) as IFlowContext, (int)data[4]);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != NetworkEvents.SimpleNetworkProcedureEvent 
                || !(photonEvent.CustomData is object[] data) 
                || data.Length <= 1 
                || !(data[0] is int opType)
                || !(data[1] is int opId)) return;

            // OperationType, OperationId

            m_maxOperationId = Mathf.Max(m_maxOperationId, opId);
            (ExecutionFlow flow, IFlowContext context, int opId) values;
            (int localId, int maxId) ids;

            WeavrDebug.Log(this, $"Message Received: {(OperationType)opType}: {data.Skip(1).Select(i => i.ToString()).Aggregate((a, s) => a = a == null ? s : a + ", " + s)}");

            switch ((OperationType)opType)
            {
                case OperationType.ProcedureStarted:
                    // ProcedureGuid, ModeIndex
                    if(Runner.CurrentProcedure && Runner.CurrentProcedure.Guid == data[2] as string)
                    {
                        Runner.StartProcedure(Runner.CurrentProcedure, Runner.CurrentProcedure.ExecutionModes[(int)data[3]]);
                    }
                    else if(Procedure.Procedure.TryFind(data[2] as string, out Procedure.Procedure procedureToRun))
                    {
                        Runner.StartProcedure(procedureToRun, procedureToRun.ExecutionModes[(int)data[3]]);
                    }
                    break;
                case OperationType.ProcedureEnded:
                    // ProcedureGuid,
                    if(Runner.CurrentProcedure && Runner.CurrentProcedure.Guid == (data[2] as string))
                    {
                        Runner.StopCurrentProcedure();
                    }
                    break;
                case OperationType.ContextAdded:
                    // FlowId, ContextGuid, FlowOperationId
                    values = DeconstructData(data);
                    if(!m_flowOperations.TryGetValue(values.flow, out ids) || ids.maxId < values.opId)
                    {
                        try
                        {
                            m_mutedFlows.Add(values.flow);
                            m_flowOperations[values.flow] = (values.opId, values.opId);
                            values.flow.EnqueueContext(values.context);
                        }
                        finally
                        {
                            m_mutedFlows.Remove(values.flow);
                        }
                    }
                    break;
                case OperationType.ContextRemoved:
                    // FlowId, ContextGuid, FlowOperationId
                    values = DeconstructData(data);
                    if (!m_flowOperations.TryGetValue(values.flow, out ids) || ids.maxId < values.opId)
                    {
                        try
                        {
                            m_mutedFlows.Add(values.flow);
                            m_flowOperations[values.flow] = (values.opId, values.opId);
                            values.flow.Remove(values.context);
                        }
                        finally
                        {
                            m_mutedFlows.Remove(values.flow);
                        }
                    }
                    break;
                case OperationType.ContextChanged:
                    // FlowId, ContextGuid, FlowOperationId
                    values = DeconstructData(data);
                    if (!values.flow)
                    {
                        if (m_forceCreateMissingFlows)
                        {
                            values.flow = Runner.GetOrCreateFlow((int)data[2], true);
                            values.flow.ExecutionMode = Runner.ExecutionMode;
                        }
                        else
                        {
                            WeavrDebug.LogError(this, $"Unable to retrieve flow with id {(int)data[2]}");
                            break;
                        }
                    }
                    if (!m_flowOperations.TryGetValue(values.flow, out ids) || ids.maxId < values.opId)
                    {
                        try
                        {
                            m_mutedFlows.Add(values.flow);
                            m_flowOperations[values.flow] = (values.opId, values.opId);
                            Runner.IsControlledExternally = true;
                            values.flow.ExecuteInLine(values.context);
                        }
                        catch(Exception ex)
                        {
                            WeavrDebug.LogException(this, ex);
                        }
                        finally
                        {
                            Runner.IsControlledExternally = false;
                            m_mutedFlows.Remove(values.flow);
                        }
                    }
                    break;
                case OperationType.ContextFinished:
                    // FlowId, ContextGuid, FlowOperationId
                    values = DeconstructData(data);
                    if (!m_flowOperations.TryGetValue(values.flow, out ids) || ids.maxId < values.opId)
                    {
                        try
                        {
                            m_mutedFlows.Add(values.flow);
                            m_flowOperations[values.flow] = (values.opId, values.opId);
                            values.context.CurrentState = ContextState.Finished;
                        }
                        finally
                        {
                            m_mutedFlows.Remove(values.flow);
                        }
                    }
                    break;
                case OperationType.FlowStarted:
                    // FlowId
                    //Runner.StartExecutionFlow()
                    break;
                case OperationType.FlowEnded:
                    // FlowId
                    Runner.KillExecutionFlow((int)data[2]);
                    break;
                default:

                    break;
            }
        }
    }
}
