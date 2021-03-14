using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    public class AnimationTable
    {
        private AnimationEngine m_engine;
        private GameObject m_gameObject;
        private AnimationQueue m_mainQueue;
        private List<AnimationQueue> m_queues;
        private Dictionary<int, IAnimation> m_registry;

        public bool IsPaused { get; set; }

        public AnimationTable(AnimationEngine engine, GameObject gameObject)
        {
            m_engine = engine;
            m_gameObject = gameObject;
            m_queues = new List<AnimationQueue>();
            m_mainQueue = new AnimationQueue();
            m_queues.Add(m_mainQueue);
            m_registry = new Dictionary<int, IAnimation>();
        }

        public void Animate(float dt)
        {
            if (IsPaused) return;

            for (int i = 0; i < m_queues.Count; i++)
            {
                var queue = m_queues[i];
                if (queue.Count == 0) continue;
                var animation = queue[0];
                if (animation.CurrentState == AnimationState.Playing)
                {
                    animation.Animate(dt);
                }
                if (animation.CurrentState == AnimationState.Finished)
                {
                    MoveNext(queue);
                    m_engine.RemoveFromRegistry(animation.Id);
                    if (animation.AnimationEndCallback != null)
                    {
                        animation.AnimationEndCallback(m_gameObject, animation);
                        animation.AnimationEndCallback = null;
                    }
                    animation.OnDiscard();
                }
            }
        }

        public void AppendSequential(params IAnimation[] animations)
        {
            foreach (var anim in animations)
            {
                m_registry[anim.Id] = anim;
                m_mainQueue.Add(anim);
            }
            StartAnimation(m_mainQueue[0]);
        }


        public void PrependSequential(params IAnimation[] animations)
        {
            for (int i = 0; i < animations.Length; i++)
            {
                var anim = animations[i];
                m_registry[anim.Id] = anim;
                m_mainQueue.Insert(i, anim);
            }
            StartAnimation(m_mainQueue[0]);
        }

        public void AppendNewAsync(params IAnimation[] animations)
        {
            for (int i = 0; i < animations.Length; i++)
            {
                var queue = new AnimationQueue();
                var anim = animations[i];
                m_registry[anim.Id] = anim;
                queue.Add(anim);
                StartAnimation(queue[0]);
                m_queues.Add(queue);
            }
        }

        public void AppendToExistingAsync(params IAnimation[] animations)
        {
            CreateAdditionalQueues(animations);
            for (int i = 0; i < animations.Length; i++)
            {
                var anim = animations[i];
                var queue = m_queues[i + 1];
                m_registry[anim.Id] = anim;
                queue.Add(anim);
                StartAnimation(queue[0]);
            }
        }

        public void PrependAsync(params IAnimation[] animations)
        {
            CreateAdditionalQueues(animations);
            for (int i = 0; i < animations.Length; i++)
            {
                var anim = animations[i];
                var queue = m_queues[i + 1];
                m_registry[anim.Id] = anim;
                queue.Insert(0, anim);
                StartAnimation(queue[0]);
            }
        }

        public void ForceStopAll()
        {
            foreach (var queue in m_queues)
            {
                foreach (var anim in queue)
                {
                    anim.CurrentState = AnimationState.Stopped;
                    anim.AnimationEndCallback = null;
                    anim.OnDiscard();
                }
                queue.Clear();
            }
        }

        public void Clear(bool deepClear = false)
        {
            foreach (var queue in m_queues)
            {
                foreach (var anim in queue)
                {
                    anim.AnimationEndCallback = null;
                    anim.OnDiscard();
                }
                queue.Clear();
            }
            if (deepClear)
            {
                DeleteEmptyQueues();
            }
        }

        public void ForceStopCurrent(bool notifyObservers)
        {
            ForceStopFirst(m_mainQueue, notifyObservers);
        }

        public void ForceStopCurrentIncludingAsync(bool notifyObservers)
        {
            foreach (var queue in m_queues)
            {
                ForceStopFirst(queue, notifyObservers);
            }
        }

        public void Remove(int animationId)
        {
            IAnimation animation = null;
            if (m_registry.TryGetValue(animationId, out animation))
            {
                Remove(animation);
            }
        }

        public void Remove(IAnimation animation)
        {
            if (m_registry.Remove(animation.Id))
            {
                for (int i = 0; i < m_queues.Count; i++)
                {
                    if (m_queues[i].Remove(animation))
                    {
                        animation.AnimationEndCallback = null;
                        animation.OnDiscard();
                        return;
                    }
                }
            }
        }

        public void DeleteEmptyQueues()
        {
            for (int i = 1; i < m_queues.Count; i++)
            {
                if (m_queues[i].Count == 0)
                {
                    m_queues.RemoveAt(i--);
                }
            }
        }

        private void CreateAdditionalQueues(IAnimation[] animations)
        {
            int queuesToAdd = animations.Length - m_queues.Count + 1;
            for (int i = 0; i < queuesToAdd; i++)
            {
                m_queues.Add(new AnimationQueue());
            }
        }

        private void StartAnimation(IAnimation animation)
        {
            if (animation.CurrentState == AnimationState.NotStarted)
            {
                animation.CurrentState = AnimationState.Playing;
                animation.OnStart();
            }
        }

        private bool MoveNext(AnimationQueue queue)
        {
            if (queue.Count > 0)
            {
                queue.RemoveAt(0);
                if (queue.Count > 0)
                {
                    StartAnimation(queue[0]);
                }
                return true;
            }
            return false;
        }

        private void ForceStopFirst(AnimationQueue queue, bool notifyObservers)
        {
            if (queue.Count > 0)
            {
                var animation = queue[0];
                MoveNext(queue);
                m_engine.RemoveFromRegistry(animation.Id);
                if (notifyObservers && animation.AnimationEndCallback != null)
                {
                    animation.AnimationEndCallback(m_gameObject, animation);
                }
                animation.AnimationEndCallback = null;
                animation.OnDiscard();
            }
        }

        public IEnumerator<IAnimation> ValidAnimations()
        {
            if (m_mainQueue.Count > 0)
            {
                yield return m_mainQueue[0];
            }
            for (int i = 1; i < m_queues.Count; i++)
            {
                yield return m_queues[i][0];
            }
        }

        public IEnumerator<IAnimation> GetEnumerator()
        {
            foreach (var queue in m_queues)
            {
                foreach (var anim in queue)
                {
                    yield return anim;
                }
            }
        }
    }
}

