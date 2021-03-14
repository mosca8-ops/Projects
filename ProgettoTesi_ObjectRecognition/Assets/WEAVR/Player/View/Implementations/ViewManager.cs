using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.UI;
using UnityEngine;

namespace TXT.WEAVR.Player.Views
{

    public class ViewManager : MonoBehaviour, IViewManager
    {
        [SerializeField]
        [Draggable]
        [Type(typeof(IView))]
        private Component[] m_allViews;
        [SerializeField]
        [Type(typeof(IPopup))]
        [Draggable]
        private Component[] m_allPopups;

        public IReadOnlyList<IView> AllViews => m_allViews.Cast<IView>().ToList();
        public IReadOnlyList<IPopup> AllPopups => m_allPopups.Cast<IPopup>().ToList();

        public List<IView> ViewHistory { get; private set; } = new List<IView>(255);

        public IView LastShownView => ViewHistory.Count > 0 ? ViewHistory[ViewHistory.Count - 1] : default;

        private void Reset()
        {
            m_allViews = GetComponentsInChildren<BaseView>(true);
            m_allPopups = GetComponentsInChildren<IPopup>(true).Cast<Component>().ToArray();
        }

        // Start is called before the first frame update
        void Awake()
        {
            foreach(var view in m_allViews)
            {
                if (view is IView iview)
                {
                    iview.OnShow -= View_OnShow;
                    iview.OnShow += View_OnShow;
                }
            }
        }

        private void OnDestroy()
        {
            foreach(var view in m_allViews)
            {
                if(view is IView iview && iview != null)
                {
                    iview.OnShow -= View_OnShow;
                }
            }
        }

        private void View_OnShow(IView view)
        {
            ViewHistory.Add(view);
        }

        public T GetView<T>(Predicate<T> predicate = null) where T : IView
        {
            predicate = predicate ?? DefaultPredicate;
            foreach(var view in m_allViews)
            {
                if(view is T tview && predicate(tview))
                {
                    return tview;
                }
            }
            return default;
        }

        public T GetPopup<T>(Predicate<T> predicate = null) where T : IPopup
        {
            predicate = predicate ?? DefaultPredicate;
            foreach (var view in m_allPopups)
            {
                if (view is T tview && predicate(tview))
                {
                    return tview;
                }
            }
            return default;
        }

        private bool DefaultPredicate<T>(T target) => true;

        public void Hide(IView view)
        {
            view.Show();
        }

        public void Show(IView view)
        {
            view.Hide();
        }

        public void ShowPopup(IView view)
        {
            view.Show();
        }

        public void StartLoading(object owner, string caption, Func<float> progressUpdateFunctor)
        {
            if(owner is ILoadingView view)
            {
                view.StartLoading(caption, progressUpdateFunctor);
            }
            else if(owner is GameObject go)
            {
                var loader = go.GetComponentInChildren<IProgressLoader>(true);
                loader?.Show(caption, progressUpdateFunctor);
            }
            else if(owner is Component c)
            {
                var loader = c.GetComponentInChildren<IProgressLoader>(true);
                loader?.Show(caption, progressUpdateFunctor);
            }
        }

        public void StartLoading(object owner, string caption = null)
        {
            if(owner is ILoadingView view)
            {
                view.StartLoading(caption);
            }
            else if (owner is GameObject go)
            {
                var loader = go.GetComponentInChildren<IProgressLoader>(true);
                loader?.Show(caption);
            }
            else if (owner is Component c)
            {
                var loader = c.GetComponentInChildren<IProgressLoader>(true);
                loader?.Show(caption);
            }
        }

        public void StopLoading(object owner)
        {
            if(owner is ILoadingView view)
            {
                view.StopLoading();
            }
            else if (owner is GameObject go)
            {
                var loader = go.GetComponentInChildren<IProgressLoader>(true);
                loader?.Hide();
            }
            else if (owner is Component c)
            {
                var loader = c.GetComponentInChildren<IProgressLoader>(true);
                loader?.Hide();
            }
        }

        public (IView view, int historyIndex) GetViewFromHistory(int indexFromLastOne = 0)
        {
            int index = ViewHistory.Count - indexFromLastOne - 1;
            return index < 0 ? (default, -1) : (ViewHistory[index], index);
        }

        public void BacktrackHistoryToIndex(int index, bool hideLaterViews)
        {
            while(ViewHistory.Count > index)
            {
                if (hideLaterViews)
                {
                    ViewHistory[index].Hide();
                }
                ViewHistory.RemoveAt(index);
            }
        }
    }
}
