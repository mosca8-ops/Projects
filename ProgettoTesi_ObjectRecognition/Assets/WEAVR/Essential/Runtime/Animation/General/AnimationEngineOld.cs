using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    [Obsolete("Use Animation Engine which is newer and more customizable")]
    [AddComponentMenu("")]
    public class AnimationEngineOld : MonoBehaviour
    {

        #region [  STATIC PART  ]
        private static AnimationEngineOld _main;
        /// <summary>
        /// Gets the first instantiated or found object mover
        /// </summary>
        public static AnimationEngineOld Main {
            get {
                if (_main == null) {
                    _main = FindObjectOfType<AnimationEngineOld>();
                    if (_main == null) {
                        // Create a default one
                        var go = new GameObject("WEAVR Animation Engine");
                        _main = go.AddComponent<AnimationEngineOld>();
                        _main.Awake();
                    }
                }
                return _main;
            }
        }
        #endregion

        public enum UpdateType { Normal, Fixed, Late}

        [Tooltip("In which update call to update animations")]
        public UpdateType updateType = UpdateType.Fixed;

        private int m_idCounter;
        private List<GameObject> m_keysToRemove;
        private Dictionary<GameObject, AnimationDataQueue> m_queues;
        private Dictionary<int, KeyValuePair<GameObject, IAnimationData>> m_dataRegistry;
        private Dictionary<IAnimationData, int> m_inverseDataRegistry;

        private void Awake() {
            if (_main == null) {
                _main = this;
            }
            m_keysToRemove = new List<GameObject>();
            m_queues = new Dictionary<GameObject, AnimationDataQueue>();
            m_dataRegistry = new Dictionary<int, KeyValuePair<GameObject, IAnimationData>>();
            m_inverseDataRegistry = new Dictionary<IAnimationData, int>();
        }

        #region [  ANIMATION CONTROL LOGIC  ]

        /// <summary>
        /// Moves the specified animation in front of other animations
        /// </summary>
        /// <param name="animationId">The id of the animation to move</param>
        public void PrioritizeAnimation(int animationId) {
            KeyValuePair<GameObject, IAnimationData> keyValuePair;
            if (m_dataRegistry.TryGetValue(animationId, out keyValuePair)) {
                m_queues[keyValuePair.Key].BringToFront(keyValuePair.Value);
            }
        }

        /// <summary>
        /// Stops the specified animation
        /// </summary>
        /// <param name="animationId">The id of the animation to stop</param>
        /// <param name="notifyObservers">[Optional] Whether to notify observers or not</param>
        public void StopAnimation(int animationId, bool notifyObservers = false) {
            KeyValuePair<GameObject, IAnimationData> keyValuePair;
            if(m_dataRegistry.TryGetValue(animationId, out keyValuePair)) {
                m_queues[keyValuePair.Key].Remove(keyValuePair.Value);
                if (notifyObservers && keyValuePair.Value.AnimationEndCallback != null) {
                    keyValuePair.Value.AnimationEndCallback(keyValuePair.Key, keyValuePair.Value);
                }
            }
        }

        /// <summary>
        /// Pauses the specified animation
        /// </summary>
        /// <param name="animationId">The id of the animation to pause</param>
        public void PauseAnimation(int animationId) {
            KeyValuePair<GameObject, IAnimationData> keyValuePair;
            if (m_dataRegistry.TryGetValue(animationId, out keyValuePair)) {
                m_queues[keyValuePair.Key].ChangeState(keyValuePair.Value, AnimationState.Paused);
            }
        }

        /// <summary>
        /// Resumes the specified animation
        /// </summary>
        /// <param name="animationId">The id of the animation to resume</param>
        public void Resume(int animationId) {
            KeyValuePair<GameObject, IAnimationData> keyValuePair;
            if (m_dataRegistry.TryGetValue(animationId, out keyValuePair)) {
                m_queues[keyValuePair.Key].ChangeState(keyValuePair.Value, AnimationState.Playing);
            }
        }

        /// <summary>
        /// Pause the animation queue on specified <see cref="GameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to pause the animation queue</param>
        public void PauseAnimation(GameObject gameObject) {
            AnimationDataQueue queue = null;
            if(m_queues.TryGetValue(gameObject, out queue)) {
                queue.ChangeCurrentState(AnimationState.Paused);
            }
        }

        /// <summary>
        /// Resumes the animation queue on specified <see cref="GameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to resume the animation queue</param>
        public void ResumeAnimation(GameObject gameObject) {
            AnimationDataQueue queue = null;
            if (m_queues.TryGetValue(gameObject, out queue)) {
                queue.ChangeCurrentState(AnimationState.Playing);
            }
        }

        /// <summary>
        /// Stops the current animation on specified <see cref="GameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to stop the current animation</param>
        public void StopAnimation(GameObject gameObject) {
            AnimationDataQueue queue = null;
            if (m_queues.TryGetValue(gameObject, out queue)) {
                queue.Pop();
            }
        }

        /// <summary>
        /// Stops the whole animation queue on specified <see cref="GameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to stop the animation queue</param>
        public void StopAllAnimations(GameObject gameObject) {
            AnimationDataQueue queue = null;
            if (m_queues.TryGetValue(gameObject, out queue)) {
                queue.Clear();
            }
        }

        #endregion

        #region [  ANIMATION REGISTRATION LOGIC  ]

        /// <summary>
        /// Adds to the animation queue of the <paramref name="gameObject"/> the specified <paramref name="data"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="data">The <see cref="IAnimationData"/> to create animation from </param>
        /// <param name="onEndedCallback">[Optional] The <see cref="OnAnimationEnded"/> the callback to call when finished </param>
        /// <returns>The id of the newly created animation</returns>
        public int AppendAnimation(GameObject gameObject, IAnimationData data, OnAnimationEnded onEndedCallback = null) {
            var queue = GetQueue(gameObject);
            data.AnimationEndCallback = onEndedCallback;
            queue.Append(data);
            return GetNewId(gameObject, data);
        }

        /// <summary>
        /// Adds in front of the animation queue of the <paramref name="gameObject"/> the specified <paramref name="data"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="data">The <see cref="IAnimationData"/> to create animation from </param>
        /// <param name="onEndedCallback">[Optional] The <see cref="OnAnimationEnded"/> the callback to call when finished </param>
        /// <returns>The id of the newly created animation</returns>
        public int PrependAnimation(GameObject gameObject, IAnimationData data, OnAnimationEnded onEndedCallback = null) {
            var queue = GetQueue(gameObject);
            data.AnimationEndCallback = onEndedCallback;
            queue.Prepend(data);
            return GetNewId(gameObject, data);
        }

        /// <summary>
        /// Clears the animation queue of the <paramref name="gameObject"/> and adds the specified <paramref name="data"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="data">The <see cref="IAnimationData"/> to create animation from </param>
        /// <param name="onEndedCallback">[Optional] The <see cref="OnAnimationEnded"/> the callback to call when finished </param>
        /// <returns>The id of the newly created animation</returns>
        public int OverrideAnimation(GameObject gameObject, IAnimationData data, OnAnimationEnded onEndedCallback = null) {
            var queue = GetQueue(gameObject);
            data.AnimationEndCallback = onEndedCallback;
            ClearGameObjectRegistry(gameObject);
            queue.MakeUnique(data);
            return GetNewId(gameObject, data);
        }

        #endregion

        #region [  COMMODITY METHODS  ]

        private int GetNewId(GameObject gameObject, IAnimationData data) {
            m_dataRegistry[m_idCounter] = new KeyValuePair<GameObject, IAnimationData>(gameObject, data);
            m_inverseDataRegistry[data] = m_idCounter;
            return m_idCounter++;
        }

        private AnimationDataQueue GetQueue(GameObject gameObject) {
            AnimationDataQueue queue = null;
            if (!m_queues.TryGetValue(gameObject, out queue)) {
                queue = new AnimationDataQueue(gameObject);
                m_queues[gameObject] = queue;
            }
            return queue;
        }

        private void ClearGameObjectRegistry(GameObject gameObject) {
            AnimationDataQueue queue = null;
            if (!m_queues.TryGetValue(gameObject, out queue)) {
                return;
            }
            foreach(var data in queue.GetData()) {
                m_dataRegistry.Remove(m_inverseDataRegistry[data]);
                m_inverseDataRegistry.Remove(data);
            }
        }

        #endregion

        #region [  UPDATE LOGIC  ]

        private void SpinAnimations(float dt) {
            foreach(var keyPair in m_queues) {
                // Check if the game object was not destroyed
                if(keyPair.Key == null) {
                    m_keysToRemove.Add(keyPair.Key);
                    continue;
                }

                // Get the handler
                var handler = keyPair.Value.Peek();

                // Check if no handler present
                if(handler == null) { continue; }

                if (handler.CurrentState == AnimationState.Playing) {
                    handler.Animate(dt);
                }

                if (handler.CurrentState == AnimationState.Finished) {
                    keyPair.Value.Pop();
                    m_dataRegistry.Remove(m_inverseDataRegistry[handler.CurrentData]);
                    m_inverseDataRegistry.Remove(handler.CurrentData);
                    if(handler.CurrentData.AnimationEndCallback != null) {
                        handler.CurrentData.AnimationEndCallback(keyPair.Key, handler.CurrentData);
                    }
                }
            }

            if(m_keysToRemove.Count > 0) {
                foreach(var key in m_keysToRemove) {
                    m_queues.Remove(key);
                }
                m_keysToRemove.Clear();
            }
        }

        private void Update() {
            if(updateType == UpdateType.Normal) {
                SpinAnimations(Time.smoothDeltaTime);
            }
        }

        private void LateUpdate() {
            if (updateType == UpdateType.Normal) {
                SpinAnimations(Time.deltaTime);
            }
        }

        private void FixedUpdate() {
            if (updateType == UpdateType.Fixed) {
                SpinAnimations(Time.fixedDeltaTime);
            }
        }

        #endregion
    }
}
