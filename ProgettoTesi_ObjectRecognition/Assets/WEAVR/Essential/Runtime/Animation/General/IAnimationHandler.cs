using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Animation
{
    public delegate void OnAnimationEnded(GameObject gameObject, IAnimationData data);
    public delegate void OnAnimationEnded2(GameObject gameObject, IAnimation animation);

    public enum AnimationState { NotStarted, Playing, Paused, Stopped, Finished }

    public interface IAnimationData {
        OnAnimationEnded AnimationEndCallback { get; set; }
        bool ParseData(params object[] data);
    }

    public interface IAutoAnimationData : IAnimationData
    {
        void ApplyData(IAnimationHandler handler);
    }

    public interface IAnimation
    {
        int Id { get; set; }
        AnimationState CurrentState { get; set; }
        GameObject GameObject { get; set; }
        OnAnimationEnded2 AnimationEndCallback { get; set; }
        bool DeserializeData(object[] data);
        object[] SerializeData();
        void OnStart();
        void Animate(float dt);
        void OnDiscard();
    }

    public interface IAnimationPool
    {
        IAnimation Get();
        void Reclaim(IAnimation animation);
    }

    public interface IPooledAnimation : IAnimation
    {
        void SetPool(IAnimationPool pool);
    }

    public interface IAnimationHandler
    {
        System.Type[] HandledTypes { get; }
        AnimationState CurrentState { get; set; }
        GameObject GameObject { get; set; }
        IAnimationData CurrentData { get; set; }
        void Animate(float dt);
    }

}