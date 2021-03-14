using System;

namespace TXT.WEAVR.Player.Views
{
    public delegate void ViewDelegate(IView view);
    public delegate void OnUserActionDelegate(IView view, string action, object parameters);
    public delegate void ViewDelegate<T>(T view) where T : IView;

    public interface IView
    {
        void Show();
        void Hide();
        bool IsVisible { get; set; }

        event ViewDelegate OnShow;
        event ViewDelegate OnHide;
        event ViewDelegate OnBack;
    }

    public interface ILoadingView
    {
        void StartLoading(string title, Func<float> progressCallback);
        void StartLoading(string title = "");
        void StopLoading();
    }

    public static partial class ViewExtensions
    {
        public static void StartLoading(this IView view, string title = "")
        {
            if (view is ILoadingView loadingView) { loadingView.StartLoading(title); }
        }

        public static void StartLoading(this IView view, string title, Func<float> progressCallback)
        {
            if (view is ILoadingView loadingView) { loadingView.StartLoading(title, progressCallback); }
        }

        public static IProgress StartLoadingWithProgress(this IView view, string title = "")
        {
            var progress = new Progress();
            if (view is ILoadingView loadingView) { 
                loadingView.StartLoading(title, progress.GetProgress); 
            }
            return progress;
        }

        public static void StopLoading(this IView view)
        {
            if(view is ILoadingView loadingView) { loadingView.StopLoading(); }
        }

        private class Progress : IProgress
        {
            public float progress;
            public float GetProgress() => progress;
            public void SetProgress(float value) => progress = value;
        }
    }
}
