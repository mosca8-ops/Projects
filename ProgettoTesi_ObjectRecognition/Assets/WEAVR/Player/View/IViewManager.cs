using System;
using System.Collections;
using System.Collections.Generic;

namespace TXT.WEAVR.Player.Views
{

    public interface IViewManager
    {
        IReadOnlyList<IView> AllViews { get; }
        IReadOnlyList<IPopup> AllPopups { get; }
        IView LastShownView { get; }

        void Show(IView view);
        void Hide(IView view);
        void ShowPopup(IView view);

        void StartLoading(object owner, string caption, Func<float> progressUpdateFunctor);
        void StartLoading(object owner, string caption = null);
        void StopLoading(object owner);

        T GetView<T>(Predicate<T> predicate = null) where T : IView;
        T GetPopup<T>(Predicate<T> predicate = null) where T : IPopup;

        (IView view, int historyIndex) GetViewFromHistory(int indexFromLastOne = 0);
        void BacktrackHistoryToIndex(int index, bool hideLaterViews);
    }
}
