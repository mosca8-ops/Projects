using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    public static class AnimationExtensions
    {
        /// <summary>
        /// Applies animation to the specified <paramref name="gameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="animation">The <see cref="IAnimation"/>s to be applied </param>
        /// <param name="onEndedCallback">The <see cref="OnAnimationEnded2"/> the callback to call when finished </param>
        /// <returns>The id of the newly created animation</returns>
        public static int Animate(this GameObject gameObject, IAnimation animation, OnAnimationEnded2 onEndedCallback) {
            return AnimationEngine.Main.Animate(gameObject, animation, onEndedCallback);
        }

        /// <summary>
        /// Applies animation to the specified <paramref name="gameObject"/>
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to animate</param>
        /// <param name="animation">The <see cref="IAnimation"/>s to be applied </param>
        /// <returns>The id of the newly created animation</returns>
        public static int Animate(this GameObject gameObject, IAnimation animation) {
            return AnimationEngine.Main.Animate(gameObject, animation, null);
        }

        /// <summary>
        /// Applies animation to the specified <paramref name="behaviour"/>'s gameObject
        /// </summary>
        /// <param name="behaviour">The <see cref="Component"/>'s <see cref="GameObject"/> to animate</param>
        /// <param name="animation">The <see cref="IAnimation"/>s to be applied </param>
        /// <param name="onEndedCallback">The <see cref="OnAnimationEnded2"/> the callback to call when finished </param>
        /// <returns>The id of the newly created animation</returns>
        public static int Animate(this MonoBehaviour behaviour, IAnimation animation, OnAnimationEnded2 onEndedCallback) {
            return AnimationEngine.Main.Animate(behaviour.gameObject, animation, onEndedCallback);
        }

        /// <summary>
        /// Applies animation to the specified <paramref name="behaviour"/>'s gameObject
        /// </summary>
        /// <param name="behaviour">The <see cref="Component"/>'s <see cref="GameObject"/> to animate</param>
        /// <param name="animation">The <see cref="IAnimation"/>s to be applied </param>
        /// <returns>The id of the newly created animation</returns>
        public static int Animate(this MonoBehaviour behaviour, IAnimation animation) {
            return AnimationEngine.Main.Animate(behaviour.gameObject, animation, null);
        }

        /// <summary>
        /// Stops the specified animation
        /// </summary>
        /// <param name="gameObject">The gameobject to stop animation on</param>
        /// <param name="animationId">The id of the animation to stop</param>
        public static void StopAnimation(this GameObject gameObject, int animationId) {
            AnimationEngine.Main.StopAnimation(animationId);
        }

        /// <summary>
        /// Stops the current animation on gameObject
        /// </summary>
        /// <param name="gameObject">The gameobject to stop animation on</param>
        public static void StopAnimation(this GameObject gameObject) {
            AnimationEngine.Main.StopAnimation(gameObject);
        }

        /// <summary>
        /// Stops all animations on gameObject
        /// </summary>
        /// <param name="gameObject">The gameobject to stop animation on</param>
        public static void StopAllAnimations(this GameObject gameObject) {
            AnimationEngine.Main.StopAllAnimations(gameObject);
        }

        /// <summary>
        /// Stops the specified animation
        /// </summary>
        /// <param name="component">The <see cref="Component"/> to stop animation on</param>
        /// <param name="animationId">The id of the animation to stop</param>
        public static void StopAnimation(this Component component, int animationId) {
            AnimationEngine.Main.StopAnimation(animationId);
        }

        /// <summary>
        /// Stops the current animation on gameObject of <paramref name="component"/>
        /// </summary>
        /// <param name="component">The <see cref="Component"/> to stop animation on</param>
        public static void StopAnimation(this Component component) {
            AnimationEngine.Main.StopAnimation(component.gameObject);
        }

        /// <summary>
        /// Stops the current animation on gameObject of <paramref name="component"/>
        /// </summary>
        /// <param name="component">The <see cref="Component"/> to stop animation on</param>
        public static void StopAllAnimations(this Component component) {
            AnimationEngine.Main.StopAllAnimations(component.gameObject);
        }
    }
}
