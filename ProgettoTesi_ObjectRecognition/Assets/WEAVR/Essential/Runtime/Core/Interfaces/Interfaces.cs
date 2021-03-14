using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR
{
    public interface IResetState
    {
        void ResetState();
    }

    public interface IExecuteDisabled
    {
        void InitDisabled();
    }

    public interface IActiveProgressElement : IProgressElement
    {
        new float Progress { get; set; }
    }

    public interface IProgressElement
    {
        float Progress { get; }
        void ResetProgress();
    }

    public interface IProgress
    {
        float GetProgress();
        void SetProgress(float progress);
    }

    public class ProgressElements
    {
        public static float Min(IProgressElement a, IProgressElement b)
        {
            return a.Progress > b.Progress ? b.Progress : a.Progress;
        }

        public static float Min(IProgressElement a, IProgressElement b, IProgressElement c)
        {
            if (a.Progress < b.Progress)
            {
                return a.Progress < c.Progress ? a.Progress : c.Progress;
            }
            return b.Progress < c.Progress ? b.Progress : c.Progress;
        }

        public static float Min(IProgressElement a, IProgressElement b, IProgressElement c, IProgressElement d)
        {
            if (a.Progress < b.Progress)
            {
                if (a.Progress < c.Progress)
                {
                    return a.Progress < d.Progress ? a.Progress : d.Progress;
                }
                else
                {
                    return c.Progress < d.Progress ? c.Progress : d.Progress;
                }
            }
            else if (b.Progress < c.Progress)
            {
                return b.Progress < d.Progress ? b.Progress : d.Progress;
            }
            return c.Progress < d.Progress ? c.Progress : d.Progress;
        }

        public static float Min(params IProgressElement[] values)
        {
            if (values == null || values.Length == 0) { return 0; }
            float progress = values[0].Progress;
            for (int i = 1; i < values.Length; i++)
            {
                if (progress > values[i].Progress)
                {
                    progress = values[i].Progress;
                }
            }
            return progress;
        }
    }

    public delegate void DataChanged<T>(object source, T newData);

    public interface IDataProvider
    {

        bool CanProvide<T>();
        T Provide<T>();
    }

    public interface IDataHandler
    {
        bool CanHandle<T>();
        void Handle<T>(T data);
    }

    public interface IClonedCallback
    {
        void OnCloned(object source);
    }

    public interface IReferenceTable
    {
        Dictionary<PropertyName, SceneItem> IDs { get; }
    }

    public interface IWeavrSettingsClient
    {
        string SettingsSection { get; }
        IEnumerable<ISettingElement> Settings { get; }
    }

    public interface IWeavrSettingsListener
    {
        void OnSettingChanged(string settingKey);
    }

    public static class VisualMarkerDelegates
    {
        public delegate void OnReleaseMarker(IVisualMarker marker);
    }

    public interface IVisualMarker
    {
        bool AutoDisableOnRelease { get; }
        bool LookAtInspector { get; set; }
        Color Color { get; set; }
        string Text { get; set; }
        void SetTarget(GameObject target, Pose localPose);
        void Release();
        void CopyValuesFrom(IVisualMarker otherMarker);
        event VisualMarkerDelegates.OnReleaseMarker Released;
    }
}