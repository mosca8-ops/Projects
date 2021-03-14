using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

namespace TXT.WEAVR.Procedure
{
    public enum ExecutionState
    {
        NotStarted = 0,
        Ready = 1 << 0,
        Started = 1 << 1,
        Running = 1 << 2,
        Finished = 1 << 3,
        Faulted = 1 << 4,
        Paused = 1 << 5,
        ForceStopped = 1 << 6,
        Skipped = 1 << 7,
        Breakpoint = 1 << 8,
    }

    public enum ContextState
    {
        Standby         = 0,
        Ready           = 1 << 0,
        Running         = 1 << 1,
        Finished        = 1 << 2,
        Faulted         = 1 << 3,
        ForceStopped    = 1 << 4,
    }

    public delegate void OnExecutionStateChanged(IFlowElement element, ExecutionState newState);
    public delegate void OnExecutionContextChanged(ExecutionFlow flow, IFlowContext newContext);
    public delegate void OnExecutionContextFinished(ExecutionFlow flow, IFlowContext context);
    public delegate void OnExecutionTick(ExecutionFlow flow, float dt);

    public class ExecutionFlow : ProcedureObject
    {
        private delegate bool FetchElement(out IFlowElement element);

        public const int k_MaxAsyncThreads = 8;

        private const ExecutionState k_ValidTickState = (ExecutionState.Started 
                                                         | ExecutionState.Ready 
                                                         | ExecutionState.Running 
                                                         | ExecutionState.Paused 
                                                         | ExecutionState.Breakpoint);
        private const ExecutionState k_ClearState = ExecutionState.NotStarted | ExecutionState.Breakpoint;
        private const ExecutionState k_ReadyFlag = ExecutionState.NotStarted | ExecutionState.Ready;
        private const ExecutionState k_CanStart = ~(ExecutionState.Started | ExecutionState.Ready | ExecutionState.Running);

        private List<Coroutine> m_localCoroutines;
        private List<bool> m_finishedCoroutines;
        private int m_minCoroutineExecIndex = 0;
        private List<KeyValuePair<IEnumerator, int>> m_coroutinesQueue;
        private ExecutionFlowsEngine m_engine;

        public ExecutionFlowsEngine ExecutionEngine
        {
            get { return m_engine; }
            set
            {
                if (m_engine != value)
                {
                    if (m_engine)
                    {
                        StopAllCoroutines();
                    }
                    m_engine = value;
                    PropertyChanged(nameof(ExecutionEngine));
                }
            }
        }

        private ExecutionMode m_executionMode;

        private IFlowContext m_currentContext;
        private List<KeyValuePair<IFlowContext, Action>> m_contextsQueue;

        private List<IFlowElement> m_currentBlock;
        private IEnumerator<IFlowElement> m_currentBlockCursor;

        private ExecutionMode m_replayExecutionMode;
        private List<IFlowElement> m_replayElements;
        private List<IFlowElement> m_replayAsyncElements;
        private IEnumerator<IFlowElement> m_replayCursor;
        private IFlowElement m_currentReplayElement;

        private List<ExecutionThread> m_asyncThreads;
        private List<IFlowElement> m_fullyAsyncElements;
        private List<IFlowContextClosedElement> m_closingElements;
        private IFlowElement m_currentElement;

        private List<IFlowElement> m_breakpoints;

        private bool m_paused;
        private bool m_autoAdvance;
        private bool m_inDebug;

        private bool m_inOrderUndoElements;
        private bool m_queueIsLocked;

        private int m_elementsToMove;

        private int? m_idInEngine = null;

        public int Id
        {
            get => m_idInEngine ?? 0;
            set
            {
                if (!m_idInEngine.HasValue)
                {
                    m_idInEngine = value;
                }
            }
        }

        public bool IsPrimaryFlow { get; set; }

        public ExecutionMode ExecutionMode
        {
            get { return m_executionMode; }
            set
            {
                if (m_executionMode != value)
                {
                    SetExecutionMode(value, true);
                }
            }
        }

        public void SetExecutionMode(ExecutionMode mode, bool restartCurrentContext)
        {
            m_executionMode = mode;
            PropertyChanged(nameof(ExecutionMode));
            if (restartCurrentContext && CurrentContext != null)
            {
                CloseCurrentContext();
                StartContext(CurrentContext);
            }
        }

        public IFlowContext CurrentContext
        {
            get { return m_currentContext; }
            private set
            {
                if (m_currentContext != value)
                {
                    if (m_currentContext != null)
                    {
                        CloseCurrentContext();
                    }
                    m_currentContext = value;
                    ContextChanged?.Invoke(this, value);
                    if (value != null)
                    {
                        StartContext(value);
                    }
                }
            }
        }

        public IFlowContext NextContext => m_contextsQueue.Count == 0 ? null : CurrentContext == null ? m_contextsQueue[0].Key : m_contextsQueue.Count > 1 ? m_contextsQueue[1].Key : null;

        public bool Paused
        {
            get { return m_paused; }
            set
            {
                if(m_paused != value)
                {
                    if (value) { PauseInternal(); }
                    else { ResumeInternal(); }

                    m_paused = value;
                    PropertyChanged(nameof(Paused));
                }
            }
        }

        public bool AutoAdvance
        {
            get { return m_autoAdvance; }
            set
            {
                if(m_autoAdvance != value)
                {
                    m_autoAdvance = value;
                    PropertyChanged(nameof(AutoAdvance));
                    if(m_autoAdvance && CurrentContext == null && m_contextsQueue.Count > 0)
                    {
                        CurrentContext = m_contextsQueue[0].Key;
                    }
                    if (m_autoAdvance)
                    {

                    }
                }
            }
        }

        public bool IsDebugging
        {
            get { return m_inDebug; }
            set
            {
                if(m_inDebug != value)
                {
                    m_inDebug = value;
                    Paused = value;
                    PropertyChanged(nameof(IsDebugging));
                }
            }
        }

        public bool IsRunning => ExecutionEngine && ExecutionEngine.IsFlowRunning(this);

        public bool UndoInOrderElements
        {
            get => m_inOrderUndoElements;
            set
            {
                if(m_inOrderUndoElements != value)
                {
                    m_inOrderUndoElements = value;
                }
            }
        }

        public bool ExecutionFinished => AutoAdvance && m_contextsQueue.Count == 0 && (CurrentContext == null || m_currentBlockCursor.Current == null);

        public IReadOnlyList<ExecutionThread> AsyncThreads => m_asyncThreads;
        public IReadOnlyList<IFlowElement> Breakpoints => m_breakpoints;
        public IReadOnlyList<IFlowElement> FullyAsyncElements => m_fullyAsyncElements;


        public event OnExecutionContextChanged ContextChanged;
        public event OnExecutionContextChanged ContextAdded;
        public event OnExecutionContextChanged ContextRemoved;
        public event OnExecutionContextFinished ContextFinished;
        public event OnExecutionTick OnTick;

        #region [  COROUTINE HANDLING  ]

        public Coroutine StartCoroutine(IEnumerator coroutine)
        {
            if (m_engine != null)
            {
                m_finishedCoroutines.Add(false);
                var routine = m_engine.StartCoroutine(StartLocalCoroutine(m_localCoroutines.Count, coroutine));
                m_localCoroutines.Add(routine);
                return routine;
            }
            return null;
        }

        public void StopCoroutine(Coroutine coroutine)
        {
            if (m_localCoroutines != null && m_engine != null && m_localCoroutines.Remove(coroutine))
            {
                m_engine.StopCoroutine(coroutine);
            }
        }

        public void StopAllCoroutines()
        {
            if (m_engine != null && m_localCoroutines != null)
            {
                while (m_localCoroutines.Count > 0)
                {
                    m_engine.StopCoroutine(m_localCoroutines[0]);
                    m_localCoroutines.RemoveAt(0);
                }
                m_finishedCoroutines.Clear();
                m_minCoroutineExecIndex = 0;
            }
        }

        private IEnumerator StartLocalCoroutine(int index, IEnumerator coroutine)
        {
            yield return coroutine;
            m_finishedCoroutines[index] = true;
            m_minCoroutineExecIndex = index + 1;
            for (int i = 0; i < index; i++)
            {
                if (!m_finishedCoroutines[i])
                {
                    m_minCoroutineExecIndex = i;
                    break;
                }
            }
        }

        #endregion

        #region [  GENERAL  ]

        protected override void OnEnable()
        {
            if (m_asyncThreads == null)
            {
                m_asyncThreads = new List<ExecutionThread>();
            }
            if (m_fullyAsyncElements == null)
            {
                m_fullyAsyncElements = new List<IFlowElement>();
            }
            if (m_closingElements == null)
            {
                m_closingElements = new List<IFlowContextClosedElement>();
            }
            if(m_contextsQueue == null)
            {
                m_contextsQueue = new List<KeyValuePair<IFlowContext, Action>>();
            }
            if(m_breakpoints == null)
            {
                m_breakpoints = new List<IFlowElement>();
            }
            if(m_coroutinesQueue == null)
            {
                m_coroutinesQueue = new List<KeyValuePair<IEnumerator, int>>();
            }
            if (m_localCoroutines == null)
            {
                m_localCoroutines = new List<Coroutine>();
            }
            if(m_finishedCoroutines == null)
            {
                m_finishedCoroutines = new List<bool>();
            }
            if(m_replayElements == null)
            {
                m_replayElements = new List<IFlowElement>();
            }
            if(m_replayAsyncElements == null)
            {
                m_replayAsyncElements = new List<IFlowElement>();
            }
            m_autoAdvance = true;
        }

        public ExecutionThread GetThread(int index)
        {
            index = index < k_MaxAsyncThreads ? index : k_MaxAsyncThreads;
            if (m_asyncThreads.Count < index)
            {
                for (int i = m_asyncThreads.Count; i < index; i++)
                {
                    m_asyncThreads.Add(new ExecutionThread(this, i));
                }
            }
            return m_asyncThreads[index - 1];
        }

        public void ChangeExecutionMode(ExecutionMode mode, bool notify)
        {
            if (notify) { ExecutionMode = mode; }
            else { m_executionMode = mode; }
        }

        #endregion

        #region [  COROUTINE QUEUE RELATED  ]

        public void EnqueueCoroutine(IEnumerator coroutine, bool runAfterPreviousSyncCoroutines = false)
        {
            m_coroutinesQueue.Add(new KeyValuePair<IEnumerator, int>(coroutine, runAfterPreviousSyncCoroutines ? m_localCoroutines.Count : -1));
            if(m_coroutinesQueue.Count == 1)
            {
                m_engine?.StartCoroutine(StartCoroutinesQueue());
            }
        }

        private IEnumerator StartCoroutinesQueue()
        {
            while(m_coroutinesQueue.Count > 0)
            {
                while(m_coroutinesQueue[0].Value > m_minCoroutineExecIndex)
                {
                    yield return null;
                }
                yield return m_coroutinesQueue[0].Key;
                m_coroutinesQueue.RemoveAt(0);
            }
        }

        #endregion

        #region [  CONTEXT RELATED  ]

        public IFlowElement TryGetCurrentElement()
        {
            return m_currentElement;
        }

        public void RestartCurrentContext()
        {
            CloseCurrentContext();
            StartContext(CurrentContext);
        }

        public void EnqueueContext(IFlowContext context, Action onFinishedCallback = null)
        {
            if (m_queueIsLocked || ExecutionEngine.IsLocked) { return; }

            Assert.IsNotNull(context, "Cannot Enqueue null ExecutionContext");
            context.CurrentState = ContextState.Ready;
            m_contextsQueue.Add(new KeyValuePair<IFlowContext, Action>(context, onFinishedCallback));
            ContextAdded?.Invoke(this, context);
            if(CurrentContext == null && AutoAdvance)
            {
                CurrentContext = context;
            }
        }

        public void SetStartContext(IFlowContext context, Action onFinishedCallback = null)
        {
            if (m_queueIsLocked || ExecutionEngine.IsLocked) { return; }

            context.CurrentState = ContextState.Ready;
            m_contextsQueue.Add(new KeyValuePair<IFlowContext, Action>(context, onFinishedCallback));
            ContextAdded?.Invoke(this, context);
        }

        public void StartExecution()
        {
            if(CurrentContext == null && AutoAdvance && m_contextsQueue.Count > 0)
            {
                CurrentContext = m_contextsQueue[0].Key;
            }
        }

        public bool IsInQueue(IFlowContext context)
        {
            return CurrentContext == context || m_contextsQueue.Any(p => p.Key == context);
        }

        public bool IsNextInQueue(IFlowContext context) => NextContext == context;

        public void Remove(IFlowContext context)
        {
            Assert.IsNotNull(context, "Cannot remove ExecutionContext which is null");
            if(context == CurrentContext)
            {
                CloseCurrentContext();
                CurrentContext.CurrentState = (CurrentContext.CurrentState & ~ContextState.Running) | ContextState.ForceStopped;
            }
            for (int i = 0; i < m_contextsQueue.Count; i++)
            {
                if(m_contextsQueue[i].Key == context)
                {
                    m_contextsQueue.RemoveAt(i);
                    ContextRemoved?.Invoke(this, context);
                    break;
                }
            }
        }

        public void ExecuteExclusive(IFlowContext context, Action onFinishedCallback = null)
        {
            Clear();
            //if (context == CurrentContext)
            //{
            //    ContextChanged?.Invoke(this, context);
            //}
            EnqueueContext(context, onFinishedCallback);
        }

        public void ExecuteNetworkPartOnly(bool stopCurrentContext, params IFlowContext[] contexts)
        {
            if(stopCurrentContext && CurrentContext != null)
            {
                EndCurrentContext();
            }

            List<IFlowElement> flowElements = null;
            List<IFlowContextClosedElement> exitElements = new List<IFlowContextClosedElement>();

            foreach(var context in contexts)
            {
                bool wasRunning = context.CurrentState.HasFlag(ContextState.Running);
                context.CurrentState = ContextState.Running;
                if (context is IFlowProvider flowProvider && (flowElements = flowProvider.GetFlowElements()) != null)
                {
                    //m_currentBlock = flowProvider.GetFlowElements();
                    if (!wasRunning)
                    {
                        foreach (var elem in flowElements)
                        {
                            elem.CurrentState &= k_ClearState;
                            if (elem is IProgressElement progressElement)
                            {
                                progressElement.ResetProgress();
                            }
                        }
                    }

                    context.OnExecutionStarted(this);

                    foreach(var elem in flowElements)
                    {
                        if(elem.CanExecute(ExecutionMode))
                        {
                            if (elem is INetworkProcedureObject networkElem && networkElem.IsGlobal)
                            {
                                if (elem is IFlowContextClosedElement closingElem && closingElem.RevertOnExit)
                                {
                                    if (UndoInOrderElements) { exitElements.Insert(0, closingElem); }
                                    else { exitElements.Add(closingElem); }
                                }
                                StartElement(elem, this, ExecutionMode);
                                FastForwardElement(elem);
                            }
                            else
                            {
                                elem.CurrentState = ExecutionState.Finished;
                                if(elem is IActiveProgressElement progressElem)
                                {
                                    progressElem.Progress = 1;
                                }
                            }
                        }
                    }

                    foreach(var exitElem in exitElements)
                    {
                        exitElem.OnContextExit(this);
                    }

                    FinalizeContext(context, true);
                }
                else
                {
                    context.OnExecutionStarted(this);
                    FinalizeContext(context, true);
                    //currentContext.OnExecutionEnded(this);
                }

                exitElements.Clear();
            }
        }

        public void ExecuteInLine(IFlowContext context, Action onFinishedCallback = null)
        {
            if (context == CurrentContext)
            {
                return;
            }
            if (CurrentContext != null)
            {
                //var currentContext = CurrentContext;
                //FastForward(f => currentContext != CurrentContext || CurrentContext.CurrentState.HasFlag(ContextState.Finished), raiseEvents: false);
                //CloseCurrentContext();
                //CurrentContext.CurrentState = (CurrentContext.CurrentState & ~ContextState.Running) | ContextState.ForceStopped;
                //m_currentContext = null;
                try
                {
                    m_queueIsLocked = true;
                    FastForwardCurrentContext();
                }
                catch(Exception e)
                {
                    WeavrDebug.LogException(this, e);
                }
                finally
                {
                    m_queueIsLocked = false;
                }
            }
            if (m_contextsQueue.Any(c => c.Key == context))
            {
                try
                {
                    m_queueIsLocked = true;
                    FastForward(f => CurrentContext == context, false);
                }
                catch (Exception e)
                {
                    WeavrDebug.LogException(this, e);
                }
                finally
                {
                    m_queueIsLocked = false;
                }
            }
            else
            {
                while (m_contextsQueue.Count > 0 && m_contextsQueue[0].Key != context)
                {
                    m_contextsQueue.RemoveAt(0);
                }
                if (m_contextsQueue.Count == 0)
                {
                    EnqueueContext(context, onFinishedCallback);
                }
                else
                {
                    CurrentContext = m_contextsQueue[0].Key;
                }
            }
        }

        public bool FastForward(Func<ExecutionFlow, bool> stopCondition, bool raiseEvents = true)
        {
            if(CurrentContext == null) { return false; }
            if(stopCondition == null)
            {
                var currentContext = CurrentContext;
                stopCondition = f => currentContext != CurrentContext;
            }
            
            // Fast forward current context
            if(m_currentElement != null)
            {
                FastForwardElement(m_currentElement);
                if (stopCondition(this)) { return true; }
            }

            List<IFlowAsyncElement> tempAsyncElements = new List<IFlowAsyncElement>();

            while (CurrentContext != null)
            {
                if (m_currentContext is IFlowProvider)
                {
                    while (FastForwardFetchElement(out m_currentElement))
                    {
                        StartElement(m_currentElement, this, ExecutionMode);
                        
                        if (stopCondition(this)) 
                        { 
                            // This is mid context stop, so need to redistribute the async elements
                            foreach(var elem in tempAsyncElements)
                            {
                                AssignToThread(elem, elem.AsyncThread);
                            }
                            // In case current element is async, then fetch the next one
                            if (m_currentElement is IFlowAsyncElement currentAsyncElem && currentAsyncElem.AsyncThread != 0)
                            {
                                if(FetchNextElement(out m_currentElement))
                                {
                                    StartElement(m_currentElement, this, ExecutionMode);
                                }
                            }
                            return true;
                        }

                        if (m_currentElement is IFlowAsyncElement asyncElem && asyncElem.AsyncThread != 0)
                        {
                            tempAsyncElements.Add(asyncElem);
                        }
                        else
                        {
                            FastForwardElement(m_currentElement);
                        }
                    }
                }

                // Context is about to end, so need to fast-forward the async elements
                foreach(var elem in tempAsyncElements)
                {
                    FastForwardElement(elem);
                }
                tempAsyncElements.Clear();

                // Clear current async elements
                foreach (var thread in m_asyncThreads)
                {
                    if (!thread.FastForward(this, stopCondition))
                    {
                        thread.Clear();
                        return false;
                    }
                    thread.Clear();
                }

                foreach (var elem in m_fullyAsyncElements)
                {
                    FastForwardElement(elem);
                }
                m_fullyAsyncElements.Clear();

                ExecuteClosingElements();
                FinalizeCurrentContext(raiseEvents);
                m_currentContext = null;

                m_currentContext = m_contextsQueue.Count > 0 ? m_contextsQueue[0].Key : null;
                if (raiseEvents)
                {
                    ContextChanged?.Invoke(this, m_currentContext);
                }

                if(m_currentContext != null)
                {

                    bool wasRunning = m_currentContext.CurrentState.HasFlag(ContextState.Running);
                    m_currentContext.CurrentState = ContextState.Running;

                    if (m_currentContext is IFlowProvider flowProvider && (m_currentBlock = flowProvider.GetFlowElements()) != null)
                    {
                        //m_currentBlock = flowProvider.GetFlowElements();
                        if (!wasRunning)
                        {
                            foreach (var elem in m_currentBlock)
                            {
                                elem.CurrentState &= k_ClearState;
                                if (elem is IProgressElement progressElement)
                                {
                                    progressElement.ResetProgress();
                                }
                            }
                        }
                        m_currentBlockCursor = m_currentBlock.GetEnumerator();
                    }

                    m_currentContext.OnExecutionStarted(this);

                    if (stopCondition(this)) { return true; }
                }
            }

            return false;
        }

        private void FastForwardCurrentContext(bool raiseEvents = false)
        {
            FastForward(null, raiseEvents: raiseEvents);
        }

        public void Clear()
        {
            if(CurrentContext != null)
            {
                CloseCurrentContext();
                CurrentContext.CurrentState = (CurrentContext.CurrentState & ~ContextState.Running) | ContextState.ForceStopped;
                m_currentContext = null;
            }
            m_contextsQueue.Clear();
        }

        private void StartContext(IFlowContext currentContext)
        {
            //Debug.Log($"Starting Current Context: {(CurrentContext as GraphObject)?.Title}");
            bool wasRunning = currentContext.CurrentState.HasFlag(ContextState.Running);
            currentContext.CurrentState = ContextState.Running;
            if (currentContext is IFlowProvider flowProvider && (m_currentBlock = flowProvider.GetFlowElements()) != null)
            {
                //m_currentBlock = flowProvider.GetFlowElements();
                if (!wasRunning)
                {
                    foreach (var elem in m_currentBlock)
                    {
                        elem.CurrentState &= k_ClearState;
                        if(elem is IProgressElement progressElement)
                        {
                            progressElement.ResetProgress();
                        }
                    }
                }
                m_currentBlockCursor = m_currentBlock.GetEnumerator();

                currentContext.OnExecutionStarted(this);
                if (FetchNextElement(out m_currentElement))
                {
                    StartElement(m_currentElement, this, ExecutionMode);
                }
                else
                {
                    EndCurrentContext();
                }
            }
            else
            {
                currentContext.OnExecutionStarted(this);
                EndCurrentContext();
                //currentContext.OnExecutionEnded(this);
            }
        }

        private void CloseCurrentContext(bool executeClosingElements = true)
        {
            //Debug.Log($"Closing Current Context: {(CurrentContext as GraphObject)?.Title}");
            if (executeClosingElements)
            {
                ExecuteClosingElements();
            }

            foreach (var thread in m_asyncThreads)
            {
                thread.ForceStop();
                thread.Clear();
            }

            foreach (var elem in m_fullyAsyncElements)
            {
                StopElement(elem);
                // Remove from remaining closing elements
                if (elem is IFlowContextClosedElement closedElem)
                {
                    m_closingElements.Remove(closedElem);
                }
            }
            m_fullyAsyncElements.Clear();

            // Stop and remove remaining elements
            foreach(var closingElem in m_closingElements)
            {
                StopElement(closingElem);
            }
            m_closingElements.Clear();

            if(m_currentElement != null && m_currentElement.CurrentState.HasFlag(ExecutionState.Running))
            {
                StopElement(m_currentElement);
            }

            m_replayElements.Clear();
            foreach(var elem in m_replayAsyncElements)
            {
                StopElement(elem);
            }
            m_replayAsyncElements.Clear();
            m_replayCursor = null;
            m_currentReplayElement = null;

            Paused = false;
        }

        private void ExecuteClosingElements()
        {
            if (m_inOrderUndoElements)
            {
                foreach (var elem in m_closingElements)
                {
                    ExecuteOnContextExit(elem, this);
                }
            }
            else
            {
                for (int i = m_closingElements.Count - 1; i >= 0; i--)
                {
                    ExecuteOnContextExit(m_closingElements[i], this);
                }
            }
            m_closingElements.Clear();
        }
        
        public void EndCurrentContext(bool raiseEvents = true)
        {
            if (CurrentContext != null)
            {
                CloseCurrentContext();
                FinalizeCurrentContext(raiseEvents);
            }
            if (AutoAdvance)
            {
                m_currentContext = null;
                CurrentContext = m_contextsQueue.Count > 0 ? m_contextsQueue[0].Key : null;
            }
        }

        public void StopCurrentContext()
        {
            if (CurrentContext != null)
            {
                CloseCurrentContext(executeClosingElements: false);
                m_currentContext = null;
            }
        }

        private void FinalizeCurrentContext(bool raiseEvents)
        {
            FinalizeContext(CurrentContext, raiseEvents);
            m_contextsQueue[0].Value?.Invoke();
            m_contextsQueue.RemoveAt(0);
            m_currentContext = null;
        }

        private void FinalizeContext(IFlowContext context, bool raiseEvents)
        {
            context.CurrentState = (context.CurrentState & ~ContextState.Running) | ContextState.Finished;
            if (raiseEvents)
            {
                context.OnExecutionEnded(this);
                ContextFinished?.Invoke(this, context);
            }
        }

        #endregion

        #region [  DEBUG RELATED  ]

        public void ClearBreakpoints()
        {
            foreach(var elem in m_breakpoints)
            {
                elem.CurrentState &= ~ExecutionState.Breakpoint;
            }
            m_breakpoints.Clear();
        }

        public void MoveToNextBreakpoint()
        {
            if (CurrentContext == null)
            {
                if (m_contextsQueue.Count == 0)
                {
                    return;
                }
                CurrentContext = m_contextsQueue[0].Key;
            }

            m_elementsToMove = int.MaxValue;
            IsDebugging = true;
            Resume();
        }

        public void MoveOneElementForward()
        {
            if(CurrentContext == null)
            {
                if(m_contextsQueue.Count == 0)
                {
                    return;
                }
                CurrentContext = m_contextsQueue[0].Key;
            }

            IsDebugging = true;
            if (m_elementsToMove <= 0)
            {
                m_elementsToMove = 1;
                Resume();
            }
            else
            {
                m_elementsToMove++;
            }
        }

        #endregion

        #region [  CONTROL RELATED  ]

        public void Pause()
        {
            Paused = true;
        }

        public void Resume()
        {
            Paused = false;
        }

        private void PauseInternal()
        {
            foreach(var thread in m_asyncThreads)
            {
                thread.Pause();
            }

            foreach(var elem in m_fullyAsyncElements)
            {
                PauseElement(elem);
            }

            if (m_currentElement != null)
            {
                PauseElement(m_currentElement);
            }
        }

        private void ResumeInternal()
        {
            if(CurrentContext == null && AutoAdvance && m_contextsQueue.Count > 0)
            {
                CurrentContext = m_contextsQueue[0].Key;
            }
            foreach (var thread in m_asyncThreads)
            {
                thread.Resume();
            }

            foreach (var elem in m_fullyAsyncElements)
            {
                ResumeElement(elem);
            }

            if (m_currentElement != null)
            {
                ResumeElement(m_currentElement);
            }
        }

        public void ForceStop()
        {
            foreach (var thread in m_asyncThreads)
            {
                thread.ForceStop();
            }

            foreach (var elem in m_fullyAsyncElements)
            {
                StopElement(elem);
            }
        }

        public void StopAllAsync()
        {
            foreach (var elem in m_fullyAsyncElements)
            {
                StopElement(elem);
            }
            m_fullyAsyncElements.Clear();
        }

        public void ReplayInMode(ExecutionMode mode, bool compatibleOnly)
        {
            // Search flow elements till the current one and reexecute those
            // in a different but similar manner as the main ones
            // OnStart, Execute, OnEnd
            // Get the previously executed action up till the current one

            m_replayElements.Clear();

            foreach (var elem in m_replayAsyncElements)
            {
                StopElement(elem);
            }
            m_replayAsyncElements.Clear();

            foreach (var elem in m_currentBlock)
            {
                if (elem == m_currentElement)
                {
                    break;
                }
                if(!compatibleOnly || elem is IReplayModeElement)
                {
                    StopElement(elem);
                    m_replayElements.Add(elem);
                }
            }

            m_currentReplayElement = null;
            m_replayCursor = m_replayElements.GetEnumerator();
            m_replayExecutionMode = mode;
        }

        #endregion

        #region [  UPDATE RELATED  ]

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick(float dt)
        {
            if (Application.isEditor)
            {
                TickEditor(dt);
            }
            else
            {
                TickRuntime(dt);   
            }
        }

        private void TickEditor(float dt)
        {
            if (Paused) { return; }

            if (m_currentElement != null && CanTick(m_currentElement) && ExecuteElement(m_currentElement, dt))
            {
                if (m_currentElement.CurrentState.HasFlag(ExecutionState.Faulted))
                {
                    CurrentContext.CurrentState |= ContextState.Faulted;
                }
                m_currentElement = null;
            }
            while (m_currentElement == null && FetchNextElement(out IFlowElement element))
            {
                StartElement(element, this, ExecutionMode);
                if (Paused)
                {
                    m_currentElement = element;
                    return;
                }
                if (CanTick(element) && !ExecuteElement(element, dt))
                {
                    m_currentElement = element;
                }
                else if (element.CurrentState.HasFlag(ExecutionState.Faulted))
                {
                    CurrentContext.CurrentState |= ContextState.Faulted;
                }
            }

            if (m_currentElement == null)
            {
                EndCurrentContext();
                return;
            }

            // Tick fully async elements
            for (int i = 0; i < m_fullyAsyncElements.Count; i++)
            {
                var element = m_fullyAsyncElements[i];
                if (!CanTick(element) || ExecuteElement(element, dt))
                {
                    if (element.CurrentState.HasFlag(ExecutionState.Faulted))
                    {
                        CurrentContext.CurrentState |= ContextState.Faulted;
                    }
                    m_fullyAsyncElements.RemoveAt(i--);
                }
            }

            // Tick threads
            foreach (var thread in m_asyncThreads)
            {
                thread.TickEditor(dt);
                if (Paused)
                {
                    OnTick?.Invoke(this, dt);
                    return;
                }
            }

            if(m_replayElements.Count > 0)
            {
                if (m_currentReplayElement != null && CanTick(m_currentReplayElement) && ExecuteElement(m_currentReplayElement, dt))
                {
                    m_currentReplayElement = null;
                }
                while (m_currentReplayElement == null && FetchNextReplayElement(out IFlowElement element))
                {
                    StartElement(element, this, m_replayExecutionMode);
                    if (Paused)
                    {
                        m_currentReplayElement = element;
                        return;
                    }
                    if (CanTick(element) && !ExecuteElement(element, dt))
                    {
                        m_currentReplayElement = element;
                    }
                }

                if (m_currentReplayElement == null)
                {
                    m_replayElements.Clear();
                }
            }
            
            if (m_replayAsyncElements.Count > 0)
            {
                TickAsyncElements(dt, m_replayAsyncElements);
            }

            OnTick?.Invoke(this, dt);
        }
        
        private void TickAsyncElements(float dt, List<IFlowElement> asyncList)
        {
            for (int i = 0; i < asyncList.Count; i++)
            {
                var element = asyncList[i];
                if (!CanTick(element) || ExecuteElement(element, dt))
                {
                    asyncList.RemoveAt(i--);
                }
            }
        }

        private void TickRuntime(float dt)
        {
            if (m_currentElement != null && CanTick(m_currentElement) && ExecuteElement(m_currentElement, dt))
            {
                m_currentElement = null;
            }
            while (m_currentElement == null && FetchNextElement(out IFlowElement element))
            {
                StartElement(element, this, ExecutionMode);
                if (CanTick(element) && !ExecuteElement(element, dt))
                {
                    m_currentElement = element;
                }
            }

            if (m_currentElement == null)
            {
                EndCurrentContext();
                return;
            }

            // Tick fully async elements
            TickAsyncElements(dt, m_fullyAsyncElements);

            // Tick threads
            foreach (var thread in m_asyncThreads)
            {
                thread.TickRuntime(dt);
            }

            if (m_replayElements.Count > 0)
            {
                if (m_currentReplayElement != null && CanTick(m_currentReplayElement) && ExecuteElement(m_currentReplayElement, dt))
                {
                    m_currentReplayElement = null;
                }
                while (m_currentReplayElement == null && FetchNextReplayElement(out IFlowElement element))
                {
                    StartElement(element, this, ExecutionMode);
                    if (CanTick(element) && !ExecuteElement(element, dt))
                    {
                        m_currentReplayElement = element;
                    }
                }

                if (m_currentReplayElement == null)
                {
                    m_replayElements.Clear();
                }

            }

            if (m_replayAsyncElements.Count > 0)
            {
                TickAsyncElements(dt, m_replayAsyncElements);
            }

            OnTick?.Invoke(this, dt);
        }

        private bool FetchNextElement(out IFlowElement element)
        {
            while (m_currentBlockCursor != null && m_currentBlockCursor.MoveNext())
            {
                if (m_currentBlockCursor.Current.CanExecute(ExecutionMode))
                {
                    element = m_currentBlockCursor.Current;
                    if (element is IFlowContextClosedElement closingElement && closingElement.RevertOnExit)
                    {
                        m_closingElements.Add(closingElement);
                    }
                    if (element is IFlowAsyncElement asyncElem && AssignToThread(element, asyncElem.AsyncThread))
                    {
                        continue;
                    }

                    element.CurrentState = ExecutionState.Ready;
                    return true;
                }
                else
                {
                    m_currentBlockCursor.Current.CurrentState = ExecutionState.Skipped;
                }
            }
            element = null;
            return false;
        }

        private bool FetchNextReplayElement(out IFlowElement element)
        {
            while (m_replayCursor.MoveNext())
            {
                if (m_replayCursor.Current.CanExecute(m_replayExecutionMode))
                {
                    element = m_replayCursor.Current;
                    if (element is IFlowContextClosedElement closingElement 
                        && closingElement.RevertOnExit 
                        && !m_closingElements.Contains(closingElement))
                    {
                        m_closingElements.Add(closingElement);
                    }
                    if (element is IFlowAsyncElement asyncElem && asyncElem.AsyncThread < 0)
                    {
                        if ((element.CurrentState & k_CanStart) != 0)
                        {
                            element.CurrentState = ExecutionState.Ready;
                            StartElement(element, this, m_replayExecutionMode);
                        }
                        if (CanTick(element))
                        {
                            m_replayAsyncElements.Add(element);
                        }
                        continue;
                    }

                    element.CurrentState = ExecutionState.Ready;
                    return true;
                }
                else
                {
                    m_replayCursor.Current.CurrentState = ExecutionState.Skipped;
                }
            }
            element = null;
            return false;
        }

        private bool FastForwardFetchElement(out IFlowElement element)
        {
            while (m_currentBlockCursor.MoveNext())
            {
                if (m_currentBlockCursor.Current.CanExecute(ExecutionMode))
                {
                    element = m_currentBlockCursor.Current;
                    if (element is IFlowContextClosedElement closingElement && closingElement.RevertOnExit)
                    {
                        m_closingElements.Add(closingElement);
                    }
                    element.CurrentState = ExecutionState.Ready;
                    return true;
                }
                else
                {
                    m_currentBlockCursor.Current.CurrentState = ExecutionState.Skipped;
                }
            }
            element = null;
            return false;
        }

        private bool FetchingHasEnded()
        {
            return m_currentBlockCursor.Current == null;
        }

        private bool AssignToThread(IFlowElement element, int index)
        {
            if(index < 0)
            {
                if (element.CurrentState == ExecutionState.NotStarted)
                {
                    element.CurrentState = ExecutionState.Ready;
                    StartElement(element, this, ExecutionMode);
                }
                if (CanTick(element))
                {
                    m_fullyAsyncElements.Add(element);
                }
                return true;
            }
            else if(index > 0)
            {
                GetThread(index).Add(element);
                return true;
            }
            return false;
        }

        #endregion
        
        #region [  ELEMENT MANAGEMENT  ]

        private static bool CanTick(IFlowElement element)
        {
            return (element.CurrentState & k_ValidTickState) != 0;
        }

        private static string GetValidErrorMessage(Exception e)
        {
            if (string.IsNullOrEmpty(e.Message))
            {
                return e.InnerException != null ? GetValidErrorMessage(e.InnerException) : e.GetType().Name;
            }
            return e.Message;
        }

        internal void StartElement(IFlowElement element, ExecutionFlow flow, ExecutionMode mode)
        {
            if (ExecutionEngine.IsSafeMode)
            {
                try
                {
                    element.OnStart(flow, mode);
                    if (flow.IsDebugging && --flow.m_elementsToMove <= 0)
                    {
                        flow.Pause();
                        flow.m_elementsToMove = 0;
                    }
                    if (element.CurrentState.HasFlag(ExecutionState.Breakpoint) && Application.isEditor)
                    {
                        flow.m_breakpoints.Add(element);
                        if ((element.CurrentState & k_ReadyFlag) != 0)
                        {
                            element.CurrentState = ExecutionState.Started;
                        }
                        if (flow.IsDebugging)
                        {
                            flow.Pause();
                            flow.m_elementsToMove = 0;
                        }
                    }
                    else if ((element.CurrentState & k_ReadyFlag) != 0)
                    {
                        element.CurrentState = ExecutionState.Started;
                    }
                }
                catch (Exception e)
                {
                    element.Exception = e;
                    element.ErrorMessage = $"[OnStart]: {GetValidErrorMessage(e)}";
                    element.CurrentState |= ExecutionState.Faulted;
                }
            }
            else
            {
                element.OnStart(flow, mode);
                if ((element.CurrentState & k_ReadyFlag) != 0)
                {
                    element.CurrentState = ExecutionState.Started;
                }
            }
        }

        internal bool ExecuteElement(IFlowElement element, float dt)
        {
            if (ExecutionEngine.IsSafeMode)
            {
                try
                {
                    element.CurrentState = ExecutionState.Running;
                    if (element.Execute(dt))
                    {
                        element.CurrentState = ExecutionState.Finished;
                        if (element.CurrentState.HasFlag(ExecutionState.Finished) && element is IExecutionCallbackElement)
                        {
                            ((IExecutionCallbackElement)element).OnExecutionFinished?.Invoke();
                        }
                        return true;
                    }
                }
                catch (Exception e)
                {
                    element.Exception = e;
                    element.ErrorMessage = $"[OnExecute]: {GetValidErrorMessage(e)}";
                    element.CurrentState |= ExecutionState.Faulted;
                    return true;
                }
            }
            else
            {
                element.CurrentState = ExecutionState.Running;
                if (element.Execute(dt))
                {
                    element.CurrentState = ExecutionState.Finished;
                    if (element.CurrentState.HasFlag(ExecutionState.Finished) && element is IExecutionCallbackElement)
                    {
                        ((IExecutionCallbackElement)element).OnExecutionFinished?.Invoke();
                    }
                    return true;
                }
            }
            return false;
        }

        private void ExecuteOnContextExit(IFlowContextClosedElement elem, ExecutionFlow flow)
        {
            if (ExecutionEngine.IsSafeMode)
            {
                try
                {
                    elem.OnContextExit(flow);
                }
                catch(Exception e)
                {
                    elem.Exception = e;
                    elem.ErrorMessage = $"[OnContextExit]: {GetValidErrorMessage(e)}";
                    elem.CurrentState |= ExecutionState.Faulted;
                }
            }
            else
            {
                elem.OnContextExit(flow);
            }
        }


        internal void StopElement(IFlowElement element)
        {
            if (ExecutionEngine.IsSafeMode)
            {
                try
                {
                    element.OnStop();
                    element.CurrentState = ExecutionState.ForceStopped;
                }
                catch (Exception e)
                {
                    element.Exception = e;
                    element.ErrorMessage = $"[OnStop]: {GetValidErrorMessage(e)}";
                    element.CurrentState |= ExecutionState.Faulted;
                }
            }
            else
            {
                element.OnStop();
                element.CurrentState = ExecutionState.ForceStopped;
            }
        }

        internal void FastForwardElement(IFlowElement element)
        {
            if (ExecutionEngine.IsSafeMode)
            {
                try
                {
                    element.FastForward();
                    element.CurrentState = ExecutionState.Finished;
                }
                catch (Exception e)
                {
                    element.Exception = e;
                    element.ErrorMessage = $"[OnFastForward]: {GetValidErrorMessage(e)}";
                    element.CurrentState |= ExecutionState.Faulted;
                }
            }
            else
            {
                element.FastForward();
                element.CurrentState = ExecutionState.Finished;
            }
        }

        internal void PauseElement(IFlowElement element)
        {
            if (ExecutionEngine.IsSafeMode)
            {
                try
                {
                    element.CurrentState |= ExecutionState.Paused;
                    element.OnPause();
                }
                catch (Exception e)
                {
                    element.Exception = e;
                    element.ErrorMessage = $"[OnPause]: {GetValidErrorMessage(e)}";
                    element.CurrentState |= ExecutionState.Faulted;
                }
            }
            else
            {
                element.CurrentState |= ExecutionState.Paused;
                element.OnPause();
            }
        }

        internal void ResumeElement(IFlowElement element)
        {
            if (ExecutionEngine.IsSafeMode)
            {
                try
                {
                    element.OnResume();
                    element.CurrentState &= ~ExecutionState.Paused;
                }
                catch (Exception e)
                {
                    element.Exception = e;
                    element.ErrorMessage = $"[OnResume]: {GetValidErrorMessage(e)}";
                    element.CurrentState |= ExecutionState.Faulted;
                }
            }
            else
            {
                element.OnResume();
                element.CurrentState &= ~ExecutionState.Paused;
            }
        }

        #endregion

        #region [  ELEMENTS QUEUE  ]

        /// <summary>
        /// TODO: To be replaced all over the execution flow
        /// </summary>
        private class ElementsQueue
        {
            private ExecutionFlow m_flow;
            public ExecutionMode executionMode;
            public IEnumerable<IFlowElement> elements;
            public List<IFlowElement> asyncElements;
            public IFlowElement current;
            public IEnumerator<IFlowElement> cursor;

            public FetchElement Fetch;
            public Action<IFlowElement> OnElementFault;


            public ElementsQueue(ExecutionFlow flow, IEnumerable<IFlowElement> elementsList)
            {
                m_flow = flow;
                asyncElements = new List<IFlowElement>();
            }

            private bool ExecuteAndFetch(float dt)
            {
                if (current != null && CanTick(current) && m_flow.ExecuteElement(current, dt))
                {
                    if (current.CurrentState.HasFlag(ExecutionState.Faulted))
                    {
                        OnElementFault?.Invoke(current);
                    }
                    current = null;
                }
                while (current == null && Fetch(out IFlowElement element))
                {
                    m_flow.StartElement(element, m_flow, executionMode);
                    if (m_flow.Paused)
                    {
                        current = element;
                        return true;
                    }
                    if (CanTick(element) && !m_flow.ExecuteElement(element, dt))
                    {
                        current = element;
                    }
                    else if (element.CurrentState.HasFlag(ExecutionState.Faulted))
                    {
                        OnElementFault?.Invoke(element);
                    }
                }
                return current != null;
            }
        }

        #endregion

        #region [  EXECUTION THREAD  ]

        // For Async execution
        public class ExecutionThread
        {
            private List<IFlowElement> m_elements;
            private List<IFlowElement> m_processedElements;

            public ExecutionFlow Flow { get; private set; }
            public int Id { get; private set; }
            public IReadOnlyList<IFlowElement> ElementsToProcess => m_elements;
            public IReadOnlyList<IFlowElement> ProcessedElements => m_processedElements;
            public bool Finished { get; private set; }
            public bool Paused { get; private set; }
            public IFlowElement CurrentElement => m_elements.Count > 0 ? m_elements[0] : null;

            public ExecutionThread(ExecutionFlow flow, int id)
            {
                Flow = flow;
                Id = id;
                m_elements = new List<IFlowElement>();
                m_processedElements = new List<IFlowElement>();
            }

            public void Add(IFlowElement element)
            {
                m_elements.Add(element);
                element.CurrentState = ExecutionState.Ready;
                Finished = false;
            }

            public void Clear()
            {
                m_elements.Clear();
                m_processedElements.Clear();
                Finished = true;
            }

            internal void TickEditor(float dt)
            {
                while (m_elements.Count > 0)
                {
                    var element = m_elements[0];
                    if ((element.CurrentState & k_ReadyFlag) != 0)
                    {
                        Flow.StartElement(element, Flow, Flow.ExecutionMode);
                        if (Flow.Paused)
                        {
                            return;
                        }
                    }
                    if (CanTick(element) && !Flow.ExecuteElement(element, dt))
                    {
                        break;
                    }
                    else
                    {
                        if (element.CurrentState.HasFlag(ExecutionState.Faulted))
                        {
                            Flow.CurrentContext.CurrentState |= ContextState.Faulted;
                        }
                        m_elements.RemoveAt(0);
                        m_processedElements.Add(element);
                    }
                }
                Finished = m_elements.Count == 0;
            }

            internal void TickRuntime(float dt)
            {
                while (m_elements.Count > 0)
                {
                    var element = m_elements[0];
                    if ((element.CurrentState & k_ReadyFlag) != 0)
                    {
                        Flow.StartElement(element, Flow, Flow.ExecutionMode);
                    }
                    if (CanTick(element) && !Flow.ExecuteElement(element, dt))
                    {
                        break;
                    }
                    else
                    {
                        m_elements.RemoveAt(0);
                        m_processedElements.Add(element);
                    }
                }
                Finished = m_elements.Count == 0;
            }

            public void Pause()
            {
                if(m_elements.Count > 0)
                {
                    Flow.PauseElement(m_elements[0]);
                }
                Paused = true;
            }

            public void Resume()
            {
                if(m_elements.Count > 0)
                {
                    Flow.ResumeElement(m_elements[0]);
                }
                Paused = false;
            }

            public void ForceStop()
            {
                for (int i = 0; i < m_elements.Count; i++)
                {
                    Flow.StopElement(m_elements[i]);
                }
                m_elements.Clear();
            }

            public bool FastForward(ExecutionFlow flow, Func<ExecutionFlow, bool> stopCondition)
            {
                for (int i = 0; i < m_elements.Count; i++)
                {
                    Flow.StopElement(m_elements[i]);
                    if (stopCondition(flow))
                    {
                        return false;
                    }
                }
                m_elements.Clear();
                return true;
            }
        }

        #endregion

    }
    
    public interface IFlowContext
    {
        bool CanBeShared { get; }
        ContextState CurrentState { get; set; }
        void OnExecutionStarted(ExecutionFlow flow);
        void OnExecutionEnded(ExecutionFlow flow);
    }

    public interface IFlowProvider : IFlowContext
    {
        List<IFlowElement> GetFlowElements();
    }

    public interface IFlowElement
    {
        ExecutionState CurrentState { get; set; }
        string ErrorMessage { get; set; }
        Exception Exception { get; set; }

        event OnExecutionStateChanged StateChanged;
        bool CanExecute(ExecutionMode executionMode);
        void OnStart(ExecutionFlow flow, ExecutionMode executionMode);
        bool Execute(float dt);
        void OnPause();
        void OnResume();
        void OnStop();
        void FastForward();
    }

    public interface IExecutionCallbackElement
    {
        Action OnExecutionFinished { get; set; }
    }

    public interface IFlowAsyncElement : IFlowElement
    {
        int AsyncThread { get; set; }
    }

    public interface IFlowContextClosedElement : IFlowElement
    {
        bool RevertOnExit { get; set; }
        void OnContextExit(ExecutionFlow flow);
    }

    public interface IReplayModeElement
    {

    }

    public interface IExecutionBarrier
    {

    }
}
