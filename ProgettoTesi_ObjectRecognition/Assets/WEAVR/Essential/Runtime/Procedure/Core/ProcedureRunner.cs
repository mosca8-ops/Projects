using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.Events;
using IStatesStack = TXT.WEAVR.Core.StateManager.IStatesStack;

namespace TXT.WEAVR.Procedure
{

    [AddComponentMenu("WEAVR/Procedures/Procedures Runner")]
    public class ProcedureRunner : ExecutionFlowsEngine, IWeavrSingleton
    {
        public delegate void ValueChanged<T>(object source, T newValue);
        public delegate void StepEvent(IProcedureStep step);
        public delegate void ProcedureEvent(ProcedureRunner runner, Procedure procedure);
        public delegate void ProcedureEventWithExecutionMode(ProcedureRunner runner, Procedure procedure, ExecutionMode mode);

        public class ProcedureRunnerSettings : IWeavrSettingsClient
        {
            public string SettingsSection => "WEAVR Procedure";

            public IEnumerable<ISettingElement> Settings => new ISettingElement[]
            {
                new Setting()
                {
                    name = "SafeModeProcedure",
                    description = "Whether to execute the procedure in safe mode or not",
                    flags = SettingsFlags.EditableInPlayer,
                    Value = false,
                }
            };
        }

        [Serializable]
        public class UnityEventStep : UnityEvent<ProcedureObject> { }
        [Serializable]
        public class UnityEventProcedure : UnityEvent<Procedure> { }

        #region [  STATIC PART  ]

        private static ProcedureRunner s_instance;

        public static ProcedureRunner Current
        {
            get
            {
                if (!s_instance)
                {
                    s_instance = Weavr.GetInCurrentScene<ProcedureRunner>();
                    if (!s_instance)
                    {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("ProcedureRunner");
                        s_instance = go.AddComponent<ProcedureRunner>();
                        s_instance.transform.SetParent(ObjectRetriever.WEAVR.transform, false);
                    }
                    s_instance.Awake();
                }
                return s_instance;
            }
        }

        #endregion

        [Space]
        [SerializeField]
        private bool m_startWhenReady = false;
        [SerializeField]
        [HideInInspector]
        private bool m_startFromDebugStep = false;
        [SerializeField]
        [Draggable]
        private Procedure m_currentProcedure;

        [Space]
        [SerializeField]
        private Events m_events;

        private Dictionary<ExecutionFlow, FlowStack> m_statesStacks = new Dictionary<ExecutionFlow, FlowStack>();
        private List<StepStack> m_stacks = new List<StepStack>();
        private IStatesStack m_initialStates;

        private HashSet<IProcedureStartValidator> m_startValidators = new HashSet<IProcedureStartValidator>();

        private int m_currentStepIndex;

        private bool m_movingBack;
        private bool m_redoingNext;
        private bool m_redoingCurrent;
        private bool m_externalControl;

        private Action m_moveNextOverride;
        public Action MoveNextOverride {
            get => m_moveNextOverride;
            set
            {
                if(m_moveNextOverride == null || value == null)
                {
                    m_moveNextOverride = value;
                    RequiresNextToContinue?.Invoke(this, value != null);
                }
            }
        }

        private Action m_movePreviousOverride;
        public Action MovePreviousOverride
        {
            get => m_movePreviousOverride;
            set
            {
                if (m_movePreviousOverride == null || value == null)
                {
                    m_movePreviousOverride = value;
                }
            }
        }

        private bool m_canRedo;
        public bool CanRedo {
            get => m_canRedo;
            private set
            {
                if(m_canRedo != value)
                {
                    m_canRedo = value;
                    CanRedoStepChanged?.Invoke(this, value);
                }
            }
        }

        private bool m_canMoveNext;
        public bool CanMoveNext {
            get => m_canMoveNext;
            private set
            {
                if(m_canMoveNext != value)
                {
                    m_canMoveNext = value;
                    CanMoveNextStepChanged?.Invoke(this, value);
                }
            }
        }

        public bool IsControlledExternally
        {
            get => m_externalControl;
            set
            {
                if(m_externalControl != value)
                {
                    m_externalControl = value;
                }
            }
        }

        public ExecutionMode ExecutionMode
        {
            get => m_currentProcedure && 0 <= m_currentStepIndex && m_currentStepIndex < m_stacks.Count 
                ? m_stacks[m_currentStepIndex].stack.flow.ExecutionMode : null;
            set
            {
                if(m_currentProcedure && ExecutionMode != value)
                {
                    foreach (var flow in m_runningExecutionFlows)
                    {
                        if (flow)
                        {
                            flow.ExecutionMode = value;
                        }
                    }
                }
            }
        }

        public ExecutionMode ExecutionModeWithoutRestart
        {
            get => m_currentProcedure && 0 <= m_currentStepIndex && m_currentStepIndex < m_stacks.Count
                ? m_stacks[m_currentStepIndex].stack.flow.ExecutionMode : null;
            set
            {
                if (m_currentProcedure && ExecutionMode != value)
                {
                    foreach (var flow in m_runningExecutionFlows)
                    {
                        if (flow)
                        {
                            flow.SetExecutionMode(value, false);
                        }
                    }
                }
            }
        }

        public bool IsWaitingForNextButton => MoveNextOverride != null;

        [NonSerialized]
        private Procedure m_runningProcedure;
        public Procedure RunningProcedure {
            get => m_runningProcedure;
            private set
            {
                if(m_runningProcedure != value)
                {
                    m_runningProcedure = value;
                    if (m_runningProcedure)
                    {
                        m_initialStates = this.GetSingleton<StateManager>()
                            .CreateInitialSnapshot(m_runningProcedure.Graph.ReferencesTable.GetGameObjects(), true);
                    }
                }
            }
        }

        public bool RestartProcedure
        {
            get => false;
            set
            {
                if (value)
                {
                    if(m_initialStates != null)
                    {
                        m_initialStates.Restore(0);
                        m_initialStates = null;
                    }
                    else
                    {
                        foreach (var stackPair in m_statesStacks)
                        {
                            stackPair.Value.statesStack.Restore(0);
                        }
                    }
                    StopCurrentProcedure();
                    m_statesStacks.Clear();
                    m_stacks.Clear();

                    if (m_currentProcedure)
                    {
                        m_currentProcedure.SoftReset();
                    }
                    StartCurrentProcedure();
                }
            }
        }

        public event ValueChanged<bool> CanMoveNextStepChanged;
        public event ValueChanged<bool> CanRedoStepChanged;
        public event ValueChanged<bool> RequiresNextToContinue;

        public event StepEvent StepStarted;
        public event StepEvent StepFinished;

        public event ProcedureEventWithExecutionMode ProcedureStarted;
        public event ProcedureEvent ProcedureFinished;
        public event ProcedureEvent ProcedureStopped;

        private IProcedureStep m_lastStep;
        public IProcedureStep CurrentStep => 0 <= m_currentStepIndex && m_currentStepIndex < m_stacks.Count ? m_stacks[m_currentStepIndex].step : null;
        public IProcedureStep PreviousStep => 0 < m_currentStepIndex && m_currentStepIndex <= m_stacks.Count ? m_stacks[m_currentStepIndex - 1].step : null;

        public bool StartWhenReady
        {
            get => m_startWhenReady;
            set
            {
                if(m_startWhenReady != value)
                {
                    StopAllCoroutines();
                    m_startWhenReady = value;
                    if (m_currentProcedure && m_startWhenReady && Application.isPlaying)
                    {
                        StartProcedure(m_currentProcedure);
                    }
                }
            }
        }

        public bool StartFromDebugStep { get => m_startFromDebugStep; set => m_startFromDebugStep = value; }

        public Procedure CurrentProcedure
        {
            get => m_currentProcedure;
            set
            {
                if(m_currentProcedure != value)
                {
                    StopCurrentProcedure();
                    m_currentProcedure = value;
                    if(m_currentProcedure && m_startWhenReady && Application.isPlaying)
                    {
                        StartProcedure(m_currentProcedure);
                    }
                }
            }
        }

        public void RegisterStartValidator(IProcedureStartValidator validator)
        {
            m_startValidators.Add(validator);
        }

        public void UnregisterStartValidator(IProcedureStartValidator validator)
        {
            m_startValidators.Remove(validator);
        }

        public bool CanStartProcedure(Procedure procedure, ExecutionMode mode)
        {
            foreach(var validator in m_startValidators)
            {
                if(validator != null && !validator.ValidateProcedureStart(procedure, mode))
                {
                    return false;
                }
            }
            return true;
        }

        public void StartCurrentProcedure()
        {
            if (m_currentProcedure)
            {
                StartProcedure(m_currentProcedure);
            }
        }

        public void StartProcedure(Procedure procedure)
        {
            StartProcedure(procedure, procedure.DefaultExecutionMode);
        }

        public void StartCurrentProcedureMode(int execModeIndex)
        {
            if (m_currentProcedure)
            {
                StartProcedure(m_currentProcedure, m_currentProcedure.ExecutionModes[Mathf.Clamp(execModeIndex, 0, m_currentProcedure.ExecutionModes.Count - 1)]);
            }
        }

        public void StartProcedure(Procedure procedure, int executionModeIndex)
        {
            if (procedure && 0 <= executionModeIndex && executionModeIndex < procedure.ExecutionModes.Count)
            {
                StartProcedure(procedure, procedure.ExecutionModes[executionModeIndex]);
            }
        }

        public void StartProcedure(Procedure procedure, ExecutionMode executionMode)
        {
            if(!CanStartProcedure(procedure, executionMode))
            {
                return;
            }

            if (StartFromDebugStep && Application.isEditor && procedure.Graph.DebugStartNodes.Count > 0)
            {
                StartProcedureFromDebugStep(procedure, executionMode);
                return;
            }

            ExecutionMode mode = PrepareProcedure(procedure, executionMode);

            foreach (var startPoint in procedure.Graph.StartingNodes)
            {
                StartExecutionFlow(true, mode, startPoint);
            }

            foreach(var flowStart in procedure.Graph.FlowStartNodes)
            {
                StartExecutionFlow(false, mode, flowStart);
            }
        }

        public ExecutionMode PrepareProcedure(Procedure procedure, ExecutionMode executionMode)
        {
            m_startWhenReady = false;
            m_statesStacks.Clear();
            m_stacks.Clear();
            m_currentStepIndex = -1;
            m_currentProcedure = procedure;
            procedure.PrepareForLaunch();
            var mode = executionMode ? executionMode : procedure.DefaultExecutionMode;

            if (procedure.LocalizationTable)
            {
                this.GetSingleton<LocalizationManager>().Table = procedure.LocalizationTable;
            }

            RaiseProcedureStarted(procedure, mode);

            RefreshMovingCapabilities();
            return mode;
        }

        public void StartProcedureFromDebugStep(Procedure procedure, ExecutionMode executionMode)
        {
            StopAllCoroutines();

            ExecutionMode mode = PrepareProcedure(procedure, executionMode);

            var debugPath = procedure.Graph.ShortestDebugPath();

            StartExecutionFlow(true, mode, debugPath.Convert<GraphObject>().Select(g => g as IFlowContext).Where(f => f != null))
                .FastForward(f => Equals(f.CurrentContext, debugPath.Destination));
            
            //foreach (var startPoint in procedure.Graph.StartingNodes)
            //{
            //    StartExecutionFlow(mode, startPoint);
            //}
        }

        public bool IsStartingStep(IProcedureStep step)
        {
            return CurrentProcedure && CurrentProcedure.Graph.StartingNodes.Any(n => n.ProcedureStep == step);
        }

        public void StopCurrentProcedure()
        {
            StopAllCoroutines();
            if (m_currentProcedure && m_runningProcedure == m_currentProcedure)
            {
                m_initialStates = null;
                foreach(var flow in m_runningExecutionFlows)
                {
                    flow.ContextChanged -= Flow_ContextChanged;
                }
                StopAllExecutionFlows();
                RaiseProcedureStopped(m_currentProcedure);
            }
        }

        private void RefreshMovingCapabilities()
        {
            m_canMoveNext = !m_canMoveNext;
            CanMoveNext = !m_canMoveNext;
            m_canRedo = !m_canRedo;
            CanRedo = !m_canRedo;
        }

        protected virtual void Awake()
        {
            if (m_statesStacks == null)
            {
                m_statesStacks = new Dictionary<ExecutionFlow, FlowStack>();
            }
            if (m_stacks == null)
            {
                m_stacks = new List<StepStack>();
            }
        }

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            IsSafeMode = Application.isEditor || Weavr.Settings.GetValue("SafeModeProcedure", false);
            if (m_currentProcedure && m_startWhenReady)
            {
                StartCoroutine(DelayedStart(0.5f, () => StartProcedure(m_currentProcedure)));
            }
        }

        private IEnumerator DelayedStart(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback();
        }

        public void MovePreviousStep()
        {
            MoveNextOverride = null;
            if(MovePreviousOverride != null)
            {
                MovePreviousOverride();
                return;
            }
            var prevStepIndex = m_currentStepIndex <= 0 ? 0 : m_currentStepIndex - 1;
            if(m_currentStepIndex >= m_stacks.Count)
            {
                Debug.Log($"Unable to move to previous step...");
                return;
            }

            var prevStepStack = m_stacks[prevStepIndex];
            CanRedo = true;
            m_movingBack = true;
            prevStepStack.stack.TryPop();
            m_movingBack = false;
        }

        public void ReplayLastStepCompletely()
        {
            if (0 <= m_currentStepIndex && m_currentStepIndex < m_stacks.Count)
            {
                m_stacks[m_currentStepIndex].stack.flow.RestartCurrentContext();
            }
        }

        public void ReplayLastStep()
        {
            if (m_currentProcedure)
            {
                ReplayLastStep(m_currentProcedure.HintsReplayExecutionMode);
            }
        }

        public void ReplayLastStep(ExecutionMode mode)
        {
            if(0 <= m_currentStepIndex && m_currentStepIndex < m_stacks.Count)
            {
                m_stacks[m_currentStepIndex].stack.flow.ReplayInMode(mode, true);
            }
        }

        public void ReplayLastStep(int executionModeIndex)
        {
            if(m_currentProcedure && 0 <= executionModeIndex && executionModeIndex < m_currentProcedure.ExecutionModes.Count)
            {
                ReplayLastStep(m_currentProcedure.ExecutionModes[executionModeIndex]);
            }
        }

        public void ReplayAllLastSteps()
        {
            if (m_currentProcedure)
            {
                ReplayAllLastSteps(m_currentProcedure.HintsReplayExecutionMode);
            }
        }

        public void ReplayAllLastSteps(ExecutionMode mode)
        {
            if (0 <= m_currentStepIndex && m_currentStepIndex < m_stacks.Count)
            {
                foreach(var flow in m_runningExecutionFlows.ToArray())
                {
                    if (flow)
                    {
                        flow.ReplayInMode(mode, true);
                    }
                }
            }
        }

        public void ReplayAllLastSteps(int executionModeIndex)
        {
            if (m_currentProcedure && 0 <= executionModeIndex && executionModeIndex < m_currentProcedure.ExecutionModes.Count)
            {
                ReplayLastStep(m_currentProcedure.ExecutionModes[executionModeIndex]);
            }
        }

        public void RedoNextStep()
        {
            MoveNextOverride = null;
            int nextIndex = m_currentStepIndex;
            if(0 > nextIndex || nextIndex >= m_stacks.Count)
            {
                return;
            }

            var nextStepStack = m_stacks[nextIndex];
            var lastStep = m_stacks.Count > nextIndex + 1 ? m_stacks[nextIndex + 1] : null;
            m_redoingNext = true;
            nextStepStack.stack.TryRestoreNext();
            m_redoingNext = false;
        }

        public void MoveNextStep()
        {
            if (CurrentStep == null) { return; }

            if(MoveNextOverride != null)
            {
                MoveNextOverride();
                return;
            }
            
            var lastFlow = m_stacks[m_currentStepIndex]?.stack.flow;

            if (lastFlow == null || (CurrentStep.IsMandatory && !lastFlow.ExecutionMode.RequiresNextToContinue)) { return; }

            if(lastFlow != null)
            {
                var currentStepGuid = CurrentStep?.StepGUID;
                var currentContext = lastFlow.CurrentContext;
                if (lastFlow.FastForward(f => f.CurrentContext is IProcedureStep step
                                          && (step.StepGUID != currentStepGuid 
                                          || (step.IsMandatory && (!f.ExecutionMode.RequiresNextToContinue || step != currentContext)))))
                {
                    return;
                }
            }

            for (int i = 0; i < m_runningExecutionFlows.Count; i++)
            {
                var currentStepGuid = CurrentStep?.StepGUID;
                var currentContext = lastFlow.CurrentContext;
                if (m_runningExecutionFlows[i].FastForward(f => f.CurrentContext is IProcedureStep step
                                          && (step.StepGUID != currentStepGuid
                                          || (step.IsMandatory && (!f.ExecutionMode.RequiresNextToContinue || step != currentContext)))))
                {
                    break;
                }
            }
        }

        public override ExecutionFlow CreateExecutionFlow(bool isPrimary)
        {
            var flow = base.CreateExecutionFlow(isPrimary);

            if (isPrimary)
            {
                var stack = this.GetSingleton<StateManager>().GetStack(m_statesStacks.Count);
                stack.Register(m_currentProcedure.Graph.ReferencesTable.GetGameObjects());
                stack.Register(this.GetSingleton<StateManager>().StatefulSceneObjects);
                stack.Register(new GameObject[] { GlobalValues.Current.gameObject });

                flow.ContextChanged -= Flow_ContextChanged;
                flow.ContextChanged += Flow_ContextChanged;

                m_statesStacks[flow] = new FlowStack(flow, stack);
            }

            return flow;
        }

        protected override void FlowEnded(ExecutionFlow flow)
        {
            base.FlowEnded(flow);
            if(flow.IsPrimaryFlow && m_runningExecutionFlows.Count(f => f.IsPrimaryFlow) < 1)
            {
                RaiseProcedureFinished(m_currentProcedure);
            }
        }

        private void Flow_ContextChanged(ExecutionFlow flow, IFlowContext newContext)
        {
            if(newContext is IProcedureStep step )
            {
                CanMoveNext = !step.IsMandatory;

                //var stack = m_statesStacks[flow];
                m_statesStacks.TryGetValue(flow, out FlowStack stack);
                var prevStep = PreviousStep;
                if (m_lastStep?.StepGUID != step.StepGUID)
                {
                    if (m_lastStep != null)
                    {
                        RaiseStepFinished(m_lastStep);
                    }

                    RaiseStepStarted(step);
                    if(prevStep?.StepGUID == step.StepGUID && m_movingBack)
                    {
                        // We went back
                        m_currentStepIndex--;
                        //RaiseStepStarted(step);
                    }
                    //else if (m_redoingNext)
                    //{
                    //    // We are redoing next step

                    //}
                    else
                    if (stack?.TryPush() == true)
                    {
                        // We went forward
                        if (m_redoingNext)
                        {
                        }
                        m_currentStepIndex++;
                        CanRedo = m_stacks.Count > m_currentStepIndex + 1 && (m_redoingNext || CurrentStep == step);
                        if (m_currentStepIndex < m_stacks.Count)
                        {
                            m_stacks[m_currentStepIndex].Reset(stack, step);
                        }
                        else
                        {
                            while (m_stacks.Count <= m_currentStepIndex)
                            {
                                m_stacks.Add(new StepStack(stack));
                            }
                        }
                        
                    }

                    m_lastStep = step;
                }
                else if(m_lastStep == step && !m_redoingCurrent && !m_externalControl)
                {
                    // Here we reexecute the same step
                    RaiseStepStarted(step);
                    m_redoingCurrent = true;
                    stack?.ExecuteCurrent();
                }
                m_redoingCurrent = false;
            }
        }


        #region [  EVENTS PART  ]

        protected void RaiseStepStarted(IProcedureStep step)
        {
            StepStarted?.Invoke(step);
            if(step is ProcedureObject poStep)
            {
                m_events.onStepStarted.Invoke(poStep);
            }
        }

        protected void RaiseStepFinished(IProcedureStep step)
        {
            StepFinished?.Invoke(step);
            if (step is ProcedureObject poStep)
            {
                m_events.onStepFinished?.Invoke(poStep);
            }
        }

        protected void RaiseProcedureStarted(Procedure procedure, ExecutionMode mode)
        {
            RunningProcedure = procedure;
            ProcedureStarted?.Invoke(this, procedure, mode);
            m_events.onProcedureStarted?.Invoke(procedure);
        }

        protected void RaiseProcedureFinished(Procedure procedure)
        {
            if(RunningProcedure == procedure) { RunningProcedure = null; }
            ProcedureFinished?.Invoke(this, procedure);
            m_events.onProcedureFinished?.Invoke(procedure);
        }

        protected void RaiseProcedureStopped(Procedure procedure)
        {
            if (RunningProcedure == procedure) { RunningProcedure = null; }
            ProcedureStopped?.Invoke(this, procedure);
            m_events.onProcedureStopped?.Invoke(procedure);
        }

        [Serializable]
        private struct Events
        {
            public UnityEventStep onStepStarted;
            public UnityEventStep onStepFinished;

            public UnityEventProcedure onProcedureStarted;
            public UnityEventProcedure onProcedureFinished;
            public UnityEventProcedure onProcedureStopped;
        }

        #endregion

        private class StepStack
        {
            private FlowStack m_stack;

            public IProcedureStep step { get; private set; }
            public FlowStack stack
            {
                get => m_stack;
                set
                {
                    if(m_stack != value)
                    {
                        m_stack = value;
                        step = m_stack.CurrentStep;
                    }
                }
            }

            public void Reset(FlowStack stack, IProcedureStep step)
            {
                m_stack = stack;
                this.step = step;
            }

            public StepStack(FlowStack stack)
            {
                step = stack.CurrentStep;
                this.stack = stack;
            }
        }

        private class FlowStack
        {
            public readonly ExecutionFlow flow;
            public readonly IStatesStack statesStack;
            public readonly List<StackItem> items;

            private int m_currentIndex;
            private int m_lastPoppedId;

            public IProcedureStep CurrentStep => 0 <= m_currentIndex && m_currentIndex < items.Count ? items[m_currentIndex].step : null;

            public FlowStack(ExecutionFlow flow, IStatesStack stack)
            {
                this.flow = flow;
                statesStack = stack;
                items = new List<StackItem>();

                m_currentIndex = -1;
                m_lastPoppedId = -2;

            }

            private StackItem GetItem(int index)
            {
                while(items.Count <= index)
                {
                    items.Add(new StackItem(items.Count, flow));
                }
                return items[index];
            }

            private void SetItem(int index, StackItem item)
            {
                if(index < items.Count)
                {
                    items[index] = item;
                    return;
                }
                while(items.Count < index)
                {
                    items.Add(new StackItem(items.Count, flow));
                }
                items.Add(item);
            }

            public bool TryPush()
            {
                if(flow.CurrentContext is IProcedureStep step)
                {
                    if (m_lastPoppedId != m_currentIndex++) {
                        statesStack.Snapshot(m_currentIndex, true);
                    }
                    SetItem(m_currentIndex, new StackItem(m_currentIndex, flow));
                    m_lastPoppedId = -2;
                    
                    return true;
                }
                return false;
            }

            public bool TryPop()
            {
                return TryPop(m_currentIndex - 1);
            }

            private bool TryPop(int id, int offset = 0)
            {
                id = id < 0 ? 0 : id;
                var item = 0 <= id && id < items.Count ? items[id] : items.Count > 0 ? items[items.Count - 1] : null;
                if (item == null) { return false; }

                flow.StopCurrentContext();
                statesStack.Restore(item.id);
                m_lastPoppedId = item.id;
                m_currentIndex = item.id + offset;
                Execute(item);
                return true;
            }

            public void ExecuteCurrent()
            {
                if(0 <= m_currentIndex && m_currentIndex < items.Count)
                {
                    statesStack.Restore(items[m_currentIndex].id);
                    Execute(items[m_currentIndex]);
                }
            }

            private void Execute(StackItem item)
            {
                if (flow.IsRunning)
                {
                    flow.ExecuteExclusive(item.context);
                }
                else if (flow.ExecutionEngine)
                {
                    flow.ExecutionEngine.StartExecutionFlow(flow);
                    flow.ExecuteExclusive(item.context);
                }
            }

            public bool TryRestoreNext()
            {
                return TryPop(m_currentIndex + 1, -1);
            }
        }

        private class StackItem
        {
            public readonly int id;
            public readonly IProcedureStep step;
            public readonly IFlowContext context;

            public StackItem(int id, ExecutionFlow flow)
            {
                this.id = id;
                context = flow.CurrentContext;
                step = context as IProcedureStep;
            }
        }

        private void OnDestroy()
        {
            if( s_instance == this)
            {
                s_instance = null;
            }

        }

        private void OnDisable()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
            Weavr.UnregisterSingleton(this);
        }
    }
}
