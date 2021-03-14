using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Animation
{

    public class AnimationDataQueue
    {
        private GameObject m_gameObject;
        private List<IAnimationData> m_queue;
        private Stack<IAnimationHandler> m_startedHandlers;
        private List<AnimationState> m_animationStates;
        private Dictionary<Type, IAnimationHandler> m_availableHandlers;
        private IAnimationHandler m_currentHandler;

        public GameObject GameObject {
            get {
                return m_gameObject;
            }
        }

        public int QueueSize {
            get {
                return m_queue.Count;
            }
        }

        public int Count {
            get {
                return m_currentHandler != null ? m_queue.Count + 1 : m_queue.Count;
            }
        }
        
        public AnimationDataQueue(GameObject gameObject) {
            m_gameObject = gameObject;
            m_queue = new List<IAnimationData>();
            m_animationStates = new List<AnimationState>();
            m_availableHandlers = new Dictionary<Type, IAnimationHandler>();

            // TODO: Implement started handlers logic [When prepending or moving in front]
            //m_startedHandlers = new Stack<IAnimationHandler>();
        }

        public IAnimationHandler Peek() {
            return m_currentHandler;
        }

        public IAnimationHandler Pop() {
            var handler = m_currentHandler;
            MoveNext();
            return handler;
        }


        private void MoveNext() {
            m_currentHandler = null;
            if (m_queue.Count == 0) { return; }
            PrepareHandler(m_queue[0], m_animationStates[0]);
            m_queue.RemoveAt(0);
            m_animationStates.RemoveAt(0);
        }

        private void PrepareHandler(IAnimationData data, AnimationState state) {
            if (!m_availableHandlers.TryGetValue(data.GetType(), out m_currentHandler)) {
                m_currentHandler = AnimationFactory.GetHandler(data);
                if (m_currentHandler == null) {
                    return;
                }
                m_currentHandler.GameObject = m_gameObject;
                m_availableHandlers[data.GetType()] = m_currentHandler;
            }
            m_currentHandler.CurrentData = data;
            m_currentHandler.CurrentState = state == AnimationState.NotStarted ? AnimationState.Playing : state;
        }

        public void Append(IAnimationData data) {
            m_queue.Add(data);
            m_animationStates.Add(AnimationState.NotStarted);
            if(m_queue.Count == 1) {
                PrepareHandler(data, AnimationState.NotStarted);
            }
        }

        public void Change(IAnimationData previous, IAnimationData newData) {
            for (int i = 0; i < m_queue.Count; i++) {
                if(m_queue[i] == previous) {
                    m_queue[i] = newData;
                    break;
                }
            }
        }

        public void Remove(IAnimationData data) {
            if (m_currentHandler != null && m_currentHandler.CurrentData == data) {
                Pop();
            }
            else {
                RemoveData(data);
            }
        }

        public void ChangeCurrentState(AnimationState state) {
            if(m_queue.Count > 0) {
                m_animationStates[0] = state;
                if(m_currentHandler != null) {
                    m_currentHandler.CurrentState = state;
                }
            }
        }

        public void ChangeState(IAnimationData data, AnimationState state) {
            int index = IndexOf(data);
            if(index >= 0) {
                m_animationStates[index] = state;
                if(index == 0 && m_currentHandler != null) {
                    m_currentHandler.CurrentState = state;
                }
            }
        }

        public bool BringToFront(IAnimationData data) {
            int index = IndexOf(data);
            if (index >= 0) {
                m_queue.Insert(0, data);
                m_queue.RemoveAt(index);
                var state = m_animationStates[index];
                PrepareHandler(data, state);
                m_animationStates.RemoveAt(index);
                m_animationStates.Insert(0, state);
                return true;
            }
            return false;
        }

        public void Prepend(IAnimationData data) {
            m_queue.Insert(0, data);
            m_animationStates.Insert(0, AnimationState.NotStarted);
            PrepareHandler(data, AnimationState.NotStarted);
        }

        public void Clear() {
            m_queue.Clear();
            m_animationStates.Clear();
            m_currentHandler = null;
        }

        public void MakeUnique(IAnimationData data) {
            Clear();
            Prepend(data);
        }

        public IEnumerable<IAnimationData> GetData() {
            return m_queue;
        }

        public IEnumerator<IAnimationData> GetEnumerator() {
            foreach(var data in m_queue) {
                yield return data;
            }
        }

        private void RemoveData(IAnimationData data) {
            for (int i = 0; i < m_queue.Count; i++) {
                if(m_queue[i] == data) {
                    m_queue.RemoveAt(i);
                    m_animationStates.RemoveAt(i);
                }
            }
        }

        private int IndexOf(IAnimationData data) {
            for (int i = 0; i < m_queue.Count; i++) {
                if (m_queue[i] == data) {
                    return i;
                }
            }
            return -1;
        }
    }
}
