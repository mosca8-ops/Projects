using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Procedure
{

    public abstract class ExecutionFlowsEngine : MonoBehaviour
    {
        private const int k_MaxIndexValue = 127; // It is something wrong if there are more than this number of parallel flows

        [Serializable]
        public class FlowUnityEvent : UnityEvent<ExecutionFlow> { }

        protected List<ExecutionFlow> m_runningExecutionFlows = new List<ExecutionFlow>();
        [SerializeField]
        private bool m_pauseFlowOnStart = false;
        [SerializeField]
        private FlowEvents m_flowEvents;

        public FlowUnityEvent OnFlowCreated => m_flowEvents.onFlowCreated;
        public FlowUnityEvent OnFlowToBeKilled => m_flowEvents.onFlowToBeKilled;
        public FlowUnityEvent OnFlowStart => m_flowEvents.onFlowStart;
        public FlowUnityEvent OnFlowEnded => m_flowEvents.onFlowEnded;

        public IReadOnlyList<ExecutionFlow> RunningFlows => m_runningExecutionFlows;

        private int m_nextId = 0;

        private List<WeakReference> m_lockers = new List<WeakReference>();
        public bool IsLocked { get; private set; }

        public bool PauseFlowOnStart
        {
            get => m_pauseFlowOnStart;
            set
            {
                if (m_pauseFlowOnStart != value)
                {
                    m_pauseFlowOnStart = value;
                }
            }
        }

        public bool IsSafeMode { get; internal set; }

        public void Lockdown(object locker)
        {
            if (!IsLocked)
            {
                IsLocked = true;
                if (m_lockers.Find(r => r.Target == locker) == null)
                {
                    m_lockers.Add(new WeakReference(locker, false));
                }
            }
        }

        public void ReleaseLockdown(object locker)
        {
            for (int i = 0; i < m_lockers.Count; i++)
            {
                if(!m_lockers[i].IsAlive || m_lockers[i].Target == locker)
                {
                    m_lockers.RemoveAt(i--);
                }
            }
            if(m_lockers.Count == 0)
            {
                IsLocked = false;
            }
        }

        public bool IsFlowRunning(ExecutionFlow executionFlow)
        {
            return m_runningExecutionFlows.Contains(executionFlow);
        }

        protected virtual void Start()
        {
            
        }

        public ExecutionFlow GetFlow(int flowId)
        {
            for (int i = 0; i < m_runningExecutionFlows.Count; i++)
            {
                if (m_runningExecutionFlows[i].Id == flowId)
                {
                    return m_runningExecutionFlows[i];
                }
            }
            return null;
        }

        public ExecutionFlow GetOrCreateFlow(int flowId, bool asPrimary)
        {
            var flow = GetFlow(flowId);
            if (!flow && !IsLocked)
            {
                int prevNextId = m_nextId;
                m_nextId = flowId;
                flow = CreateExecutionFlow(asPrimary);
                m_nextId = prevNextId;

                // Find the non yet used next id
                UpdateNextId();
            }
            return flow;
        }

        private void UpdateNextId()
        {
            bool nextIdSet = false;
            while (!nextIdSet)
            {
                nextIdSet = true;
                if(m_nextId > 127)
                {
                    m_nextId = 0;
                }
                for (int i = 0; i < RunningFlows.Count; i++)
                {
                    if (RunningFlows[i].Id == m_nextId)
                    {
                        m_nextId++;
                        nextIdSet = false;
                    }
                }
            }
        }

        public virtual ExecutionFlow CreateExecutionFlow(bool asPrimary)
        {
            if (IsLocked) { return null; }

            var newFlow = ScriptableObject.CreateInstance<ExecutionFlow>();
            newFlow.ExecutionEngine = this;
            m_runningExecutionFlows.Add(newFlow);
            newFlow.Id = m_nextId++;
            newFlow.IsPrimaryFlow = asPrimary;
            OnFlowCreated.Invoke(newFlow);

            return newFlow;
        }

        public virtual ExecutionFlow StartExecutionFlow(bool asPrimary, ExecutionMode mode, IEnumerable<IFlowContext> contexts)
        {
            if (IsLocked) { return null; }

            var newFlow = CreateExecutionFlow(asPrimary);
            newFlow.ExecutionMode = mode;
            bool autoAdvance = newFlow.AutoAdvance;
            newFlow.AutoAdvance = false;
            foreach (var context in contexts)
            {
                newFlow.EnqueueContext(context);
            }
            OnFlowStart.Invoke(newFlow);
            if (m_pauseFlowOnStart)
            {
                newFlow.Pause();
            }
            newFlow.AutoAdvance = autoAdvance;
            return newFlow;
        }

        public virtual ExecutionFlow StartExecutionFlow(bool asPrimary, ExecutionMode mode, IFlowContext entryPoint)
        {
            if (IsLocked) { return null; }

            var newFlow = CreateExecutionFlow(asPrimary);
            newFlow.ExecutionMode = mode;
            newFlow.SetStartContext(entryPoint);
            if (m_pauseFlowOnStart)
            {
                newFlow.Pause();
            }
            OnFlowStart.Invoke(newFlow);
            newFlow.StartExecution();
            return newFlow;
        }

        public virtual void StartExecutionFlow(ExecutionMode mode, ExecutionFlow flow)
        {
            if (m_runningExecutionFlows.Contains(flow) && !flow.Paused) { return; }

            flow.ExecutionEngine = this;
            m_runningExecutionFlows.Add(flow);
            flow.ExecutionMode = mode;
            OnFlowStart.Invoke(flow);
            if (m_pauseFlowOnStart)
            {
                flow.Pause();
            }
            flow.StartExecution();
        }

        public virtual void StartExecutionFlow(ExecutionFlow flow)
        {
            if (m_runningExecutionFlows.Contains(flow) && !flow.Paused) { return; }

            flow.ExecutionEngine = this;
            if (!m_runningExecutionFlows.Contains(flow))
            {
                m_runningExecutionFlows.Add(flow);
            }
            OnFlowStart.Invoke(flow);
            if (m_pauseFlowOnStart)
            {
                flow.Pause();
            }
            flow.StartExecution();
        }

        public virtual bool StopExecutionFlow(ExecutionFlow flow, bool raiseEvents = false)
        {
            if (m_runningExecutionFlows.Remove(flow))
            {
                flow.ForceStop();
                if (raiseEvents)
                {
                    FlowEnded(flow);
                }
                return true;
            }
            return false;
        }

        public virtual bool StopExecutionFlow(int flowId, bool raiseEvents = false)
        {
            for (int i = 0; i < m_runningExecutionFlows.Count; i++)
            {
                if (m_runningExecutionFlows[i].Id == flowId)
                {
                    return StopExecutionFlow(m_runningExecutionFlows[i], raiseEvents);
                }
            }
            return false;
        }

        public virtual void ReplayAllIn(ExecutionMode mode)
        {
            for (int i = 0; i < m_runningExecutionFlows.Count; i++)
            {
                if (m_runningExecutionFlows[i])
                {
                    m_runningExecutionFlows[i].ReplayInMode(mode, true);
                }
            }
        }

        public bool KillExecutionFlow(ExecutionFlow flow)
        {
            if (IsLocked) { return false; }

            if (m_runningExecutionFlows.Remove(flow))
            {
                flow.ForceStop();
                OnFlowToBeKilled.Invoke(flow);
                Destroy(flow);

                return true;
            }
            return false;
        }

        public bool KillExecutionFlow(int flowId)
        {
            for (int i = 0; i < m_runningExecutionFlows.Count; i++)
            {
                if (m_runningExecutionFlows[i].Id == flowId)
                {
                    return KillExecutionFlow(m_runningExecutionFlows[i]);
                }
            }
            return false;
        }

        public void StopAllExecutionFlows()
        {
            foreach (var flow in m_runningExecutionFlows)
            {
                flow.ForceStop();
            }
            m_runningExecutionFlows.Clear();
        }

        public void PauseAll()
        {
            foreach (var flow in m_runningExecutionFlows)
            {
                flow.Pause();
            }
        }

        public void ResumeAll()
        {
            foreach (var flow in m_runningExecutionFlows)
            {
                flow.Resume();
            }
        }

        protected virtual void Update()
        {
            ExecutionFlow flow;
            for (int i = 0; i < m_runningExecutionFlows.Count; i++)
            {
                flow = m_runningExecutionFlows[i];
                if (flow.Paused) { continue; }

                flow.Tick(Time.deltaTime);
                if (flow.ExecutionFinished)
                {
                    m_runningExecutionFlows.Remove(flow);
                    FlowEnded(flow);
                }
            }
        }

        protected virtual void FlowEnded(ExecutionFlow flow)
        {
            OnFlowEnded.Invoke(flow);
        }

        [Serializable]
        private struct FlowEvents
        {
            public FlowUnityEvent onFlowCreated;
            public FlowUnityEvent onFlowToBeKilled;
            public FlowUnityEvent onFlowStart;
            public FlowUnityEvent onFlowEnded;
        }
    }
}
