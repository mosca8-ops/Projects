
using System;

namespace TXT.WEAVR.UI
{
    public interface ILoader
    {
        string Text { get; }
        void Show(string caption);
        void Hide();
    }

    public interface IProgressLoader : ILoader
    {
        float Progress { get; }
        void Show(string caption, Func<float> progressUpdate);
    }
}