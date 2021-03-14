using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Utility;
using UnityEngine;

namespace TXT.WEAVR.Animation {

    public static class AnimationFactory {
        private static AnimationPool<LinearAnimation> s_linearAnimations = new AnimationPool<LinearAnimation>();
        private static AnimationPool<DeltaLinearAnimation> s_deltaAnimations = new AnimationPool<DeltaLinearAnimation>();
        private static AnimationPool<ScaleAnimation> s_scaleAnimations = new AnimationPool<ScaleAnimation>();
        private static AnimationPool<PingPongAnimation> s_alternateAnimations = new AnimationPool<PingPongAnimation>();
        private static AnimationPool<SpiralAnimation> s_spiralAnimations = new AnimationPool<SpiralAnimation>();
        private static Dictionary<Type, IAnimationPool> s_pools;

        static AnimationFactory() {
            s_pools = new Dictionary<Type, IAnimationPool>();
            s_pools[typeof(LinearAnimation)] = s_linearAnimations;
            s_pools[typeof(DeltaLinearAnimation)] = s_deltaAnimations;
            s_pools[typeof(ScaleAnimation)] = s_scaleAnimations;
            s_pools[typeof(PingPongAnimation)] = s_alternateAnimations;
            s_pools[typeof(SpiralAnimation)] = s_spiralAnimations;
        }

        public static void RegisterBaseAnimationType<T>() where T : BaseAnimation {
            Type type = typeof(T);
            if (!s_pools.ContainsKey(type)) {
                s_pools[type] = new AnimationPool<T>();
            }
        }

        public static void RegisterAnimationType<T>() where T : IAnimation {
            Type type = typeof(T);
            if (type.IsSubclassOf(typeof(BaseAnimation))) {
                s_pools[type] = new TypedAnimationPool(type);
            }
        }

        public static void RegisterAnimationType(Type animationType) {
            if (s_pools.ContainsKey(animationType)) { return; }
            if (animationType.IsSubclassOf(typeof(BaseAnimation))) {
                s_pools[animationType] = new TypedAnimationPool(animationType);
            }
            else if (animationType.GetInterface(typeof(IPooledAnimation).Name) != null && animationType.GetConstructor(Type.EmptyTypes) != null) {
                s_pools[animationType] = new GenericAnimationPool(() => Activator.CreateInstance(animationType) as IPooledAnimation);
            }
        }

        public static void RegisterAnimationType(Type animationType, Func<IPooledAnimation> builder) {
            if (s_pools.ContainsKey(animationType)) { return; }
            s_pools[animationType] = new GenericAnimationPool(builder);
        }

        public static void RegisterAnimationType(Type animationType, Func<IAnimationPool, IAnimation> builder) {
            if (s_pools.ContainsKey(animationType)) { return; }
            s_pools[animationType] = new GenericAnimationPool(builder);
        }

        public static IAnimation GetAnimation(Type animationType) {
            IAnimationPool pool = null;
            if (s_pools.TryGetValue(animationType, out pool)) {
                return pool.Get();
            }
            return null;
        }

        public static T GetAnimation<T>() where T : IAnimation {
            IAnimationPool pool = null;
            if (s_pools.TryGetValue(typeof(T), out pool)) {
                return (T)pool.Get();
            }
            return default(T);
        }

        public static T GetAnimation<T>(Func<T> fallbackCreationFunc) where T : IAnimation {
            IAnimationPool pool = null;
            if (s_pools.TryGetValue(typeof(T), out pool)) {
                return (T)pool.Get();
            }
            return fallbackCreationFunc();
        }

        /// <summary>
        /// Translates and rotates the specified object to match the destination
        /// </summary>
        /// <param name="destination">The destination to reach</param>
        /// <param name="offset">The offset of the gameobject to take into account</param>
        /// <param name="linearVelocity">The translation velocity. Setting 0 will instantly translate to destination</param>
        /// <param name="rotationVelocity">The rotation velocity. Setting 0 will instatntly rotate the object</param>
        /// <returns>The linear animation for the movement</returns>
        public static LinearAnimation MoveAndRotate(Transform destination, Transform offset, float linearVelocity, float rotationVelocity) {
            LinearAnimation animation = s_linearAnimations.GetAnimation();
            animation.IsRotationEnabled = true;
            animation.LinearSpeed = linearVelocity;
            animation.AngularSpeed = rotationVelocity;
            animation.UseAngularSpeed = rotationVelocity >= 0;
            animation.SetDestination(destination, offset);
            return animation;
        }

        /// <summary>
        /// Translates and rotates the specified object to match the destination
        /// </summary>
        /// <param name="destination">The destination to reach</param>
        /// <param name="offset">The offset of the gameobject to take into account</param>
        /// <param name="velocity">The translation and rotation velocity. Setting 0 will instantly translate and rotate to destination</param>
        /// <returns>The linear animation for the movement</returns>
        public static LinearAnimation MoveAndRotate(Transform destination, Transform offset, float velocity) {
            return MoveAndRotate(destination, offset, velocity, -1);
        }

        /// <summary>
        /// Translates and rotates the specified object to match the destination
        /// </summary>
        /// <param name="destination">The destination to reach</param>
        /// <param name="velocity">The translation and rotation velocity. Setting 0 will instantly translate and rotate to destination</param>
        /// <returns>The linear animation for the movement</returns>
        public static LinearAnimation MoveAndRotate(Transform destination, float velocity) {
            return MoveAndRotate(destination, null, velocity, -1);
        }

        /// <summary>
        /// Translates the specified object to match the destination
        /// </summary>
        /// <param name="destination">The destination to reach</param>
        /// <param name="offset">The offset of the gameobject to take into account</param>
        /// <param name="speed">The translation velocity. Setting 0 will instantly translate to destination</param>
        /// <returns>The linear animation for the movement</returns>
        public static LinearAnimation Move(Transform destination, Transform offset, float speed) {
            LinearAnimation animation = s_linearAnimations.GetAnimation();
            animation.IsRotationEnabled = false;
            animation.LinearSpeed = speed;
            animation.SetDestination(destination, offset);
            return animation;
        }

        /// <summary>
        /// Translates the specified object to match the destination
        /// </summary>
        /// <param name="destination">The destination to reach</param>
        /// <param name="speed">The translation velocity. Setting 0 will instantly translate to destination</param>
        /// <returns>The linear animation for the movement</returns>
        public static LinearAnimation Move(Transform destination, float speed) {
            return Move(destination, null, speed);
        }

        public static IAnimationHandler GetHandler(IAnimationData data) {
            return null;
        }
    }
}
