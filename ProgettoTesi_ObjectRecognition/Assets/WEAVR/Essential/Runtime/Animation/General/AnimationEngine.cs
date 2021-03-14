using System.Collections.Generic;
using UnityEngine;


namespace TXT.WEAVR.Animation
{
    [DefaultExecutionOrder(28000)]
    [AddComponentMenu("WEAVR/Animation/Animation Engine")]
    public class AnimationEngine : MonoBehaviour, IWeavrSingleton
    {

        #region [  STATIC PART  ]
        private static AnimationEngine _main;
        /// <summary>
        /// Gets the first instantiated or found object mover
        /// </summary>
        public static AnimationEngine Main {
            get {
                if (!_main)
                {
                    _main = Weavr.GetInCurrentScene<AnimationEngine>();
                    if (!_main)
                    {
                        // Create a default one
                        var go = new GameObject("WEAVR Animation Engine");
                        _main = go.AddComponent<AnimationEngine>();
                        _main.Awake();
                    }
                }
                return _main;
            }
        }
        #endregion

        public enum UpdateType { Normal, Fixed, Late }

        [Tooltip("In which update call to update animations")]
        public UpdateType updateType = UpdateType.Fixed;

        private int m_idCounter;
        private List<GameObject> m_keysToRemove;
        private Dictionary<GameObject, AnimationTable> m_tables;
        private Dictionary<int, AnimationTable> m_tablesRegistry;
        private Dictionary<int, IAnimation> m_animationRegistry;

        private List<AnimationTable> m_tablesToAnimate;

        private void Awake()
        {
            if (_main == null)
            {
                _main = this;
            }
            m_keysToRemove = new List<GameObject>();
            m_tables = new Dictionary<GameObject, AnimationTable>();
            m_tablesRegistry = new Dictionary<int, AnimationTable>();
            m_animationRegistry = new Dictionary<int, IAnimation>();
            m_tablesToAnimate = new List<AnimationTable>();
        }

        #region [  ANIMATION CONTROL LOGIC  ]

        /// <summary>
        /// Stops the specified animation
        /// </summary>
        /// <param name="animationId">The id of the animation to stop</param>
        /// <param name="notifyObservers">[Optional] Whether to notify observers or not</param>
        public void StopAnimation(int animationId, bool notifyObservers = false)
        {
            if (m_animationRegistry.TryGetValue(animationId, out IAnimation animation))
            {
                animation.CurrentState = AnimationState.Stopped;
                if (notifyObservers && animation.AnimationEndCallback != null)
                {
                    animation.AnimationEndCallback(animation.GameObject, animation);
                }
                m_tablesRegistry[animationId].Remove(animation);
            }
        }

        /// <summary>
        /// Pauses the specified animation
        /// </summary>
        /// <param name="animationId">The id of the animation to pause</param>
        public void PauseAnimation(int animationId)
        {
            if (m_animationRegistry.TryGetValue(animationId, out IAnimation animation))
            {
                animation.CurrentState = AnimationState.Paused;
            }
        }

        /// <summary>
        /// Resumes the specified animation
        /// </summary>
        /// <param name="animationId">The id of the animation to resume</param>
        public void Resume(int animationId)
        {
            if (m_animationRegistry.TryGetValue(animationId, out IAnimation animation))
            {
                animation.CurrentState = AnimationState.Playing;
            }
        }

        /// <summary>
        /// Pause the animation queue on specified <see cref="GameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to pause the animation queue</param>
        public void PauseAnimation(GameObject gameObject)
        {
            if (m_tables.TryGetValue(gameObject, out AnimationTable table))
            {
                table.IsPaused = true;
            }
        }

        /// <summary>
        /// Resumes the animation queue on specified <see cref="GameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to resume the animation queue</param>
        public void ResumeAnimation(GameObject gameObject)
        {
            if (m_tables.TryGetValue(gameObject, out AnimationTable table))
            {
                table.IsPaused = false;
            }
        }

        /// <summary>
        /// Stops the current animation on specified <see cref="GameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to stop the current animation</param>
        /// <param name="notifyObservers">[Optional] Whether to notify observers or not</param>
        public void StopAnimation(GameObject gameObject, bool notifyObservers = false)
        {
            if (m_tables.TryGetValue(gameObject, out AnimationTable table))
            {
                table.ForceStopCurrent(notifyObservers);
            }
        }

        /// <summary>
        /// Stops the whole animation queue on specified <see cref="GameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to stop the animation queue</param>
        public void StopAllAnimations(GameObject gameObject)
        {
            if (m_tables.TryGetValue(gameObject, out AnimationTable table))
            {
                table.ForceStopAll();
            }
            m_animationRegistry.Clear();
            m_tablesRegistry.Clear();
        }

        /// <summary>
        /// Removes the specified animation from the animation registry
        /// </summary>
        /// <param name="animationId">The id of the animation to remove</param>
        public void RemoveFromRegistry(int animationId)
        {
            if (m_animationRegistry.Remove(animationId))
            {
                m_tablesRegistry.Remove(animationId);
            }
        }

        #endregion

        #region [  ANIMATION REGISTRATION LOGIC  ]

        /// <summary>
        /// Changes the current animation to the specified one of the <paramref name="gameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="animation">The <see cref="IAnimation"/>s to create animation from </param>
        /// <param name="onEndedCallback">The <see cref="OnAnimationEnded2"/> the callback to call when finished </param>
        /// <returns>The id of the newly created animation</returns>
        public int ChangeCurrentTo(GameObject gameObject, OnAnimationEnded2 onEndedCallback, IAnimation animation)
        {
            var table = GetTable(gameObject);
            table.ForceStopCurrent(false);
            animation.GameObject = gameObject;
            animation.AnimationEndCallback = onEndedCallback;
            AssignNewId(animation);
            table.AppendSequential(animation);
            return animation.Id;
        }

        /// <summary>
        /// Applies animation to the specified <paramref name="gameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="animation">The <see cref="IAnimation"/>s to be applied </param>
        /// <param name="onEndedCallback">The <see cref="OnAnimationEnded2"/> the callback to call when finished </param>
        /// <returns>The id of the newly created animation</returns>
        public int Animate(GameObject gameObject, IAnimation animation, OnAnimationEnded2 onEndedCallback)
        {
            return ChangeCurrentTo(gameObject, onEndedCallback, animation);
        }

        /// <summary>
        /// Adds to the animation queue of the <paramref name="gameObject"/> the specified <paramref name="data"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="onEndedCallback">The <see cref="OnAnimationEnded2"/> the callback to call when finished </param>
        /// <param name="animations">The <see cref="IAnimation"/>s to create animation from </param>
        /// <returns>The id of the newly created animation</returns>
        public int AppendAnimations(GameObject gameObject, OnAnimationEnded2 onEndedCallback, params IAnimation[] animations)
        {
            var table = GetTable(gameObject);
            UpdateData(gameObject, onEndedCallback, animations);
            table.AppendSequential(animations);
            return animations[0].Id;
        }


        /// <summary>
        /// Adds in front of the animation queue of the <paramref name="gameObject"/> the specified <paramref name="animations"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="onEndedCallback">The <see cref="OnAnimationEnded2"/> the callback to call when finished </param>
        /// <param name="animations">The <see cref="IAnimation"/>s in order </param>
        /// <returns>The id of the newly created animation. In case multiple animations are created, the first id is returned</returns>
        public int PrependAnimations(GameObject gameObject, OnAnimationEnded2 onEndedCallback, params IAnimation[] animations)
        {
            var table = GetTable(gameObject);
            UpdateData(gameObject, onEndedCallback, animations);
            table.PrependSequential(animations);
            return animations[0].Id;
        }

        /// <summary>
        /// Adds animations to be played in parallel to existing ones of the <paramref name="gameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="onEndedCallback">The <see cref="OnAnimationEnded2"/> the callback to call when finished </param>
        /// <param name="animations">The <see cref="IAnimation"/>s in order </param>
        /// <returns>The id of the newly created animation</returns>
        public int AddAsyncAnimations(GameObject gameObject, OnAnimationEnded2 onEndedCallback, params IAnimation[] animations)
        {
            var table = GetTable(gameObject);
            UpdateData(gameObject, onEndedCallback, animations);
            table.AppendNewAsync(animations);
            return animations[0].Id;
        }

        /// <summary>
        /// Appends animations to be played in parallel to existing ones of the <paramref name="gameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="onEndedCallback">The <see cref="OnAnimationEnded2"/> the callback to call when finished </param>
        /// <param name="animations">The <see cref="IAnimation"/>s in order </param>
        /// <returns>The id of the newly created animation</returns>
        public int AppendAsyncAnimations(GameObject gameObject, OnAnimationEnded2 onEndedCallback, params IAnimation[] animations)
        {
            var table = GetTable(gameObject);
            UpdateData(gameObject, onEndedCallback, animations);
            table.AppendToExistingAsync(animations);
            return animations[0].Id;
        }

        /// <summary>
        /// Prepends animations to be played in parallel to existing ones of the <paramref name="gameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="onEndedCallback">The <see cref="OnAnimationEnded2"/> the callback to call when finished </param>
        /// <param name="animations">The <see cref="IAnimation"/> in order </param>
        /// <returns>The id of the newly created animation</returns>
        public int PrependAsyncAnimations(GameObject gameObject, OnAnimationEnded2 onEndedCallback, params IAnimation[] animations)
        {
            var table = GetTable(gameObject);
            UpdateData(gameObject, onEndedCallback, animations);
            table.PrependAsync(animations);
            return animations[0].Id;
        }

        #endregion

        #region [  COMMODITY METHODS  ]

        private void AssignNewId(IAnimation animation)
        {
            m_animationRegistry[m_idCounter] = animation;
            m_tablesRegistry[m_idCounter] = m_tables[animation.GameObject];
            animation.Id = m_idCounter++;
        }

        private void UpdateData(GameObject gameObject, OnAnimationEnded2 onEndedCallback, IAnimation[] animations)
        {
            if (onEndedCallback != null)
            {
                for (int i = 0; i < animations.Length; i++)
                {
                    var animation = animations[i];
                    animation.GameObject = gameObject;
                    animation.AnimationEndCallback = onEndedCallback;
                    AssignNewId(animation);
                }
            }
            else
            {
                for (int i = 0; i < animations.Length; i++)
                {
                    var animation = animations[i];
                    animation.GameObject = gameObject;
                    AssignNewId(animation);
                }
            }
        }

        private AnimationTable GetTable(GameObject gameObject)
        {
            AnimationTable table = null;
            if (!m_tables.TryGetValue(gameObject, out table))
            {
                table = new AnimationTable(this, gameObject);
                m_tables[gameObject] = table;
            }
            return table;
        }

        private void ClearGameObjectTable(GameObject gameObject)
        {
            AnimationTable table = null;
            if (!m_tables.TryGetValue(gameObject, out table))
            {
                return;
            }
            foreach (var animation in table)
            {
                m_tablesRegistry.Remove(animation.Id);
                m_animationRegistry.Remove(animation.Id);
            }
            table.Clear();
        }

        #endregion

        #region [  UPDATE LOGIC  ]

        private void SpinAnimations(float dt)
        {
            m_tablesToAnimate.Clear();
            foreach (var keyPair in m_tables)
            {
                // Check if the game object was not destroyed
                if (!keyPair.Key)
                {
                    m_keysToRemove.Add(keyPair.Key);
                    continue;
                }

                m_tablesToAnimate.Add(keyPair.Value);
            }

            if (m_keysToRemove.Count > 0)
            {
                foreach (var key in m_keysToRemove)
                {
                    m_tables[key].Clear();
                    m_tables.Remove(key);
                }
                m_keysToRemove.Clear();
            }

            for (int i = 0; i < m_tablesToAnimate.Count; i++)
            {
                m_tablesToAnimate[i].Animate(dt);
            }
        }

        private void Update()
        {
            if (updateType == UpdateType.Normal)
            {
                SpinAnimations(Time.smoothDeltaTime);
            }
        }

        private void LateUpdate()
        {
            if (updateType == UpdateType.Normal)
            {
                SpinAnimations(Time.smoothDeltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (updateType == UpdateType.Fixed)
            {
                SpinAnimations(Time.fixedDeltaTime);
            }
        }

        #endregion
    }
}
