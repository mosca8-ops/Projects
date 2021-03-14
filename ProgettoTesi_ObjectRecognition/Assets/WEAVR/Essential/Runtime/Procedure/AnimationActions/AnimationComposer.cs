using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TXT.WEAVR.Common;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{

    public class AnimationComposer : BaseReversibleProgressAction, ITargetingObject, ICreatedCloneCallback, ISerializedNetworkProcedureObject
    {
        public const int k_MaxLoopCount = 7;
        public const int k_MinTrackId = 0;
        public const int k_MaxTrackId = 3;

        [SerializeField]
        private List<BaseAnimationBlock> m_blocks;

        [SerializeField]
        [ShowIf(nameof(CanLoop))]
        private bool m_loop = false;
        [SerializeField]
        [ShowIf(nameof(IsLooping))]
        [Range(1, k_MaxLoopCount)]
        private int m_loopCount = 1;
        [SerializeField]
        [Tooltip("Whether to alternate the animation or not")]
        [ShowIf(nameof(IsLooping))]
        private bool m_alternate = true;
        [SerializeField]
        [Tooltip("The speed of the whole animation")]
        private float m_speed = 1;

        #region [  ISerializedNetworkProcedureObject IMPLEMENTATION  ]

        [SerializeField]
        private bool m_isGlobal = true;
        public string IsGlobalFieldName => nameof(m_isGlobal);
        public bool IsGlobal => m_isGlobal;

        #endregion

        private float m_sweepTime = -1;
        
        public List<BaseAnimationBlock> AnimationBlocks => m_blocks;

        public UnityObject Target {
            get
            {
                if (m_blocks.Count > 0)
                {
                    for (int i = 0; i < m_blocks.Count; i++)
                    {
                        if(m_blocks[i] is ITargetingObject tObj)
                        {
                            return tObj.Target;
                        }
                    }
                }
                return null;
            }
            set
            {
                foreach(var block in m_blocks)
                {
                    if(block is ITargetingObject tObj)
                    {
                        tObj.Target = value;
                    }
                }
            }
        }

        public string TargetFieldName => nameof(Target);

        public bool Loop
        {
            get => m_loop;
            set
            {
                if(m_loop != (value && AsyncThread != 0))
                {
                    BeginChange();
                    m_loop = (value && AsyncThread != 0);
                    PropertyChanged(nameof(Loop));
                }
            }
        }

        public int LoopCount
        {
            get => m_loopCount;
            set
            {
                if(m_loopCount != value)
                {
                    BeginChange();
                    m_loopCount = Mathf.Clamp(value, 1, k_MaxLoopCount);
                    PropertyChanged(nameof(LoopCount));
                }
            }
        }

        public bool IsAlternating
        {
            get => m_loop && m_alternate;
            set
            {
                if(m_alternate != value)
                {
                    BeginChange();
                    m_alternate = value;
                    PropertyChanged(nameof(IsAlternating));
                }
            }
        }

        public float Speed
        {
            get => m_speed;
            set
            {
                if(m_speed != value)
                {
                    BeginChange();
                    m_speed = value;
                    PropertyChanged(nameof(Speed));
                }
            }
        }

        [NonSerialized]
        private float m_totalDuration;
        [NonSerialized]
        private float m_time;
        [NonSerialized]
        private float m_currentSpeed;
        [NonSerialized]
        private float m_currentLoopCount;
        
        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            foreach(var block in m_blocks)
            {
                block.Prepare();
                m_totalDuration = Mathf.Max(block.EndTime, m_totalDuration);
            }
            m_currentSpeed = m_speed == 0 ? 0 : m_speed;
            m_time = m_currentSpeed > 0 ? 0 : m_totalDuration;
            m_currentLoopCount = m_loopCount == k_MaxLoopCount ? int.MaxValue : Mathf.Max(m_loopCount, 1);
            m_sweepTime = -1;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_blocks == null)
            {
                m_blocks = new List<BaseAnimationBlock>();
            }
            else
            {
                for (int i = 0; i < m_blocks.Count; i++)
                {
                    if (m_blocks[i])
                    {
                        m_blocks[i].Composer = this;
                        //m_blocks[i].OnModified += AnimationComposer_OnModified;
                    }
                    else
                    {
                        m_blocks.RemoveAt(i--);
                    }
                }
            }
        }

        private void AnimationComposer_OnModified(ProcedureObject obj)
        {
            Modified();
        }

        public override void CollectProcedureObjects(List<ProcedureObject> list)
        {
            base.CollectProcedureObjects(list);
            foreach (var block in m_blocks)
            {
                block.CollectProcedureObjects(list);
            }
        }

        private bool CanLoop()
        {
            return AsyncThread != 0;
        }

        private bool IsLooping()
        {
            return AsyncThread != 0 && m_loop;
        }

        public override void OnValidate()
        {
            base.OnValidate();
            m_loop &= AsyncThread != 0;
            if (m_loop)
            {
                m_loopCount = Mathf.Max(1, m_loopCount);
            }
        }

        public override bool Execute(float dt)
        {
            AnimateBlocks(m_time);

            if (m_currentSpeed > 0)
            {
                if (m_time >= m_totalDuration)
                {
                    if (m_loop && m_currentLoopCount > 0)
                    {
                        if (m_alternate)
                        {
                            m_currentSpeed = -m_currentSpeed;
                        }
                        else
                        {
                            m_time = 0;
                            m_currentLoopCount--;
                        }
                        return false;
                    }
                    return true;
                }
                m_time = Mathf.Min(m_time + dt * m_currentSpeed, m_totalDuration);
                Progress = m_totalDuration > 0 ? m_time / m_totalDuration : 1;
            }
            else
            {
                if (m_time <= 0)
                {
                    if (m_loop && m_currentLoopCount-- > 0)
                    {
                        if (m_alternate)
                        {
                            m_currentSpeed = -m_currentSpeed;
                        }
                        else
                        {
                            m_time = m_totalDuration;
                        }
                        return false;
                    }
                    return true;
                }
                m_time = Mathf.Max(m_time + dt * m_currentSpeed, 0);
                Progress = 1 - (m_totalDuration > 0 ? m_time / m_totalDuration : 1);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AnimateBlocks(float time)
        {
            if (Application.isEditor)
            {
                AnimateEditor(time);
            }
            else
            {
                AnimateRuntime(time);
            }
        }

        private void AnimateEditor(float time)
        {
            if (time > m_sweepTime)
            {
                for (int i = 0; i < m_blocks.Count; i++)
                {
                    float startTime = m_blocks[i].StartTime;
                    if (time < startTime)
                    {
                        break;
                    }
                    else if (m_sweepTime > startTime)
                    {
                        continue;
                    }
                    for (int j = 0; j < i; j++)
                    {
                        if (m_blocks[j].StartTime <= startTime)
                        {
                            try
                            {
                                m_blocks[j].Animate(startTime);
                            }
                            catch(Exception e)
                            {
                                m_blocks[j].ErrorMessage = $"[Animation {j}].Animate: {e.Message}";
                                ErrorMessage += m_blocks[j].ErrorMessage + "\n";
                            }
                        }
                    }
                    try
                    {
                        m_blocks[i].OnStart();
                    }
                    catch (Exception e)
                    {
                        m_blocks[i].ErrorMessage = $"[Animation {i}].OnStart: {e.Message}";
                        ErrorMessage += m_blocks[i].ErrorMessage + "\n";
                    }
                }
            }
            m_sweepTime = Mathf.Max(time, m_sweepTime);
            if (m_currentSpeed > 0)
            {
                for (int i = 0; i < m_blocks.Count; i++)
                {
                    try
                    {
                        m_blocks[i].Animate(time);
                    }
                    catch (Exception e)
                    {
                        m_blocks[i].ErrorMessage = $"[Animation {i}].Animate: {e.Message}";
                        ErrorMessage += m_blocks[i].ErrorMessage + "\n";
                    }
                }
            }
            else
            {
                for (int i = m_blocks.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        m_blocks[i].Animate(time);
                    }
                    catch (Exception e)
                    {
                        m_blocks[i].ErrorMessage = $"[Animation {i}].Animate: {e.Message}";
                        ErrorMessage += m_blocks[i].ErrorMessage + "\n";
                    }
                }
            }
        }

        private void AnimateRuntime(float time)
        {
            if (time > m_sweepTime)
            {
                for (int i = 0; i < m_blocks.Count; i++)
                {
                    float startTime = m_blocks[i].StartTime;
                    if (time < startTime)
                    {
                        break;
                    }
                    else if (m_sweepTime > startTime)
                    {
                        continue;
                    }
                    for (int j = 0; j < i; j++)
                    {
                        if (m_blocks[j].StartTime <= startTime)
                        {
                            m_blocks[j].Animate(startTime);
                        }
                    }
                    m_blocks[i].OnStart();
                }
            }
            m_sweepTime = Mathf.Max(time, m_sweepTime);
            if (m_currentSpeed > 0)
            {
                for (int i = 0; i < m_blocks.Count; i++)
                {
                    m_blocks[i].Animate(time);
                }
            }
            else
            {
                for (int i = m_blocks.Count - 1; i >= 0; i--)
                {
                    m_blocks[i].Animate(time);
                }
            }
        }

        public override void FastForward()
        {
            base.FastForward();
            AnimateBlocks(m_totalDuration);
            m_time = 0;
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (RevertOnExit)
            {
                // This is a small workaround for 
                // the state manager to register the correct state
                AnimateBlocks(0);

                // Execute the backwards animation
                if (AsyncThread == 0)
                {
                    flow.EnqueueCoroutine(AnimateBackwards(), true);
                }
                else
                {
                    flow.StartCoroutine(AnimateBackwards());
                }
            }
        }

        public override void OnStop()
        {
            base.OnStop();
            AnimateBlocks(RevertOnExit ? 0 : m_totalDuration);
            m_time = 0;
        }

        private IEnumerator AnimateBackwards()
        {
            // TODO: Uncoment to fix the backwards step
            //yield return new WaitForSeconds(0.00001f);
            float speed = -m_speed;
            //float time = speed > 0 ? 0 : m_totalDuration;
            float time = m_time;
            if (speed > 0)
            {
                AnimateBlocks(time);
                while (time < m_totalDuration)
                {
                    time = Mathf.Min(time + Time.deltaTime * speed, m_totalDuration);
                    yield return null;
                    AnimateBlocks(time);
                }
            }
            else
            {
                AnimateBlocks(time);
                while (time > 0)
                {
                    time = Mathf.Max(time + Time.deltaTime * speed, 0);
                    yield return null;
                    AnimateBlocks(time);
                }
            }
        }

        public override string GetDescription()
        {
            return $"{m_blocks?.Count ?? 0} Animations";
        }

        public override ProcedureObject Clone()
        {
            if(base.Clone() is AnimationComposer other)
            {
                other.m_blocks = new List<BaseAnimationBlock>();
                foreach(var block in m_blocks)
                {
                    var clone = block.Clone();
                    clone.name = block.name;

                    if (clone is BaseAnimationBlock b)
                    {
                        b.Variant = block.Variant;
                        other.m_blocks.Add(b);
                    }
                }
                return other;
            }
            return null;
        }

        public void OnCreatedByCloning()
        {
            m_blocks.Clear();
        }
    }
}
