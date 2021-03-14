using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using TXT.WEAVR.Common;
using TXT.WEAVR.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Player.Views
{
    public class ProcedureRunView : BaseView, IProcedureRunView
    {
        [Header("General Components")]
        [Tooltip("Procedure Title - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureTitle;
        [Tooltip("Button to exit this view")]
        [Draggable]
        public LabeledButton exitButton;
        [Tooltip("The image with fill enabled to use as progress bar")]
        [Draggable]
        public Image procedureProgress;
        [Tooltip("Button for restarting procedure")]
        [Draggable]
        public Button restartProcedureButton;

        [Header("Step Related Components")]
        [Tooltip("Step Title - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI stepTitle;
        [Tooltip("Step Number - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI stepNumber;
        [Tooltip("Step Description - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI stepDecription;

        [Header("Buttons Related")]
        [Tooltip("The parent for prev and next buttons")]
        [Draggable]
        public GameObject navigationButtonsParent;
        [Tooltip("Button for next Step")]
        [Draggable]
        public Button nextButton;
        [Tooltip("Button for next Step")]
        [Draggable]
        public Button prevButton;
        [SerializeField]
        private StandardButtons m_standardButtons;
        [Tooltip("Parent to hide more or less buttons")]
        [Draggable]
        public GameObject moreButtonsButton;
        [Tooltip("Where to store the additional buttons")]
        [Draggable]
        public GameObject additionalButtonsParent;
        [Tooltip("The sample to be used when adding new buttons")]
        [Draggable]
        [Type(typeof(IClickItem))]
        public Component additionalButtonSample;

        [Header("Map (aka Side Panel) Related")]
        [Tooltip("The parent for prev and next buttons")]
        [Draggable]
        public GameObject mapParent;
        [Tooltip("The sample to be used when adding a new map (aka Side Panel) selectable item")]
        [Draggable]
        [Type(typeof(IViewItem))]
        public Component mapSelectableItemSample;
        [Tooltip("The sample to be used when adding a new map (aka Side Panel) clicakble item")]
        [Draggable]
        [Type(typeof(IViewItem))]
        public Component mapClickableItemSample;

        private List<IResetState> m_elementsStates = new List<IResetState>();
        private List<IClickItem> m_additionalButtonsList = new List<IClickItem>();
        private List<(IClickItem item, Transform parent)> m_specialButtonsList = new List<(IClickItem item, Transform parent)>();
        private List<IViewItem> m_mapItemsList = new List<IViewItem>();
        private IPool<IClickItem> m_additionalButtonsPool;
        private IPool<ISelectItem> m_mapSelectableItemsPool;
        private IPool<IClickItem> m_mapClickableItemsPool;

        public IPool<IClickItem> AdditionalButtonsPool {
            get {
                if (m_additionalButtonsPool == null)
                {
                    m_additionalButtonsPool = new Pool<IClickItem>()
                    {
                        container = additionalButtonsParent,
                        sample = additionalButtonSample as IClickItem,
                        ResetObject = ResetAdditionalButton
                    };
                }
                return m_additionalButtonsPool;
            }
        }

        public IPool<IClickItem> MapButtonsPool {
            get {
                if (m_mapClickableItemsPool == null)
                {
                    m_mapClickableItemsPool = new Pool<IClickItem>()
                    {
                        container = mapParent,
                        sample = mapClickableItemSample as IClickItem,
                        ResetObject = ResetMapItem
                    };
                }
                return m_mapClickableItemsPool;
            }
        }

        public IPool<ISelectItem> MapSelectsPool {
            get {
                if (m_mapSelectableItemsPool == null)
                {
                    m_mapSelectableItemsPool = new Pool<ISelectItem>()
                    {
                        container = mapParent,
                        sample = mapSelectableItemSample as ISelectItem,
                        ResetObject = ResetMapItem
                    };
                }
                return m_mapSelectableItemsPool;
            }
        }

        public string ProcedureTitle { get => GetText(procedureTitle); set => SetText(procedureTitle, value); }
        public string StepTitle { get => GetText(stepTitle); set => SetText(stepTitle, value); }
        public string StepNumber { get => GetText(stepNumber); set => SetText(stepNumber, value); }
        public string StepDescription { get => GetText(stepDecription); set => SetText(stepDecription, value); }
        public bool EnableNavigationButtons {
            get => navigationButtonsParent ? navigationButtonsParent.activeInHierarchy
                : nextButton.gameObject.activeInHierarchy || prevButton.gameObject.activeInHierarchy;
            set {
                if (navigationButtonsParent)
                {
                    navigationButtonsParent.SetActive(value);
                }
                else
                {
                    nextButton.gameObject.SetActive(value);
                    prevButton.gameObject.SetActive(value);
                }
            }
        }
        public string ExitButtonLabel { get => exitButton.Label; set => exitButton.Label = value; }
        public float ProcedureProgress {
            get => procedureProgress ? procedureProgress.fillAmount : 0;
            set { if (procedureProgress) { procedureProgress.fillAmount = value; } }
        }

        public bool ShowAllButtons {
            get => moreButtonsButton && moreButtonsButton.activeInHierarchy;
            set {
                if (moreButtonsButton)
                {
                    moreButtonsButton.SetActive(!value);
                }
                if (additionalButtonsParent)
                {
                    additionalButtonsParent.SetActive(value);
                }
            }
        }

        public event ViewDelegate<IProcedureRunView> OnNext;
        public event ViewDelegate<IProcedureRunView> OnPrev;
        public event ViewDelegate<IProcedureRunView> OnExit;
        public event ViewDelegate<IProcedureRunView> OnRestart;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetText(TextMeshProUGUI textComponent) => textComponent ? textComponent.text : null;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetText(TextMeshProUGUI textComponent, string value)
        {
            if (textComponent) { textComponent.text = value; }
        }

        private void Awake()
        {
            m_elementsStates.AddRange(additionalButtonsParent.GetComponentsInChildren<IResetState>(true));
            foreach(var selectable in additionalButtonsParent.GetComponentsInChildren<Selectable>(true))
            {
                if(!m_elementsStates.Any(s => s is SelectableState ss && ss.selectable == selectable))
                {
                    m_elementsStates.Insert(0, new SelectableState()
                    {
                        selectable = selectable,
                        active = selectable.gameObject.activeSelf,
                        interactable = selectable.interactable,
                        isOn = selectable is Toggle toggle ? toggle.isOn : (bool?)null
                    });
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            nextButton.onClick.AddListener(NextClicked);
            prevButton.onClick.AddListener(PrevClicked);
            exitButton.onClick.AddListener(ExitClicked);

            restartProcedureButton.onClick.AddListener(RestartProcedureClicked);
        }

        public override void Show()
        {
            base.Show();
            if (moreButtonsButton)
            {
                moreButtonsButton.SetActive(GetChildren(additionalButtonsParent.transform).Any(c => c && c.gameObject.activeSelf));
            }
        }

        private void PrevClicked() => OnPrev?.Invoke(this);
        private void NextClicked() => OnNext?.Invoke(this);

        private IEnumerable<Transform> GetChildren(Transform t)
        {
            Transform[] children = new Transform[t.childCount];
            for (int i = 0; i < t.childCount; i++)
            {
                children[i] = t.GetChild(i);
            }
            return children;
        }

        private void ExitClicked()
        {
            PopupManager.Show(PopupManager.MessageType.Question, Translate("Warning"), Translate("Do you want to return to library?"), () => OnExit?.Invoke(this), null);
        }

        private void RestartProcedureClicked()
        {
            PopupManager.Show(PopupManager.MessageType.Question, Translate("Warning"), Translate("Do you want to restart the procedure?"), () => OnRestart?.Invoke(this), null);
        }

        private void ResetAdditionalButton(IClickItem obj)
        {
            obj.Clear();
        }

        private void ResetMapItem(ISelectItem obj)
        {
            obj.Clear();
        }

        private void ResetMapItem(IClickItem obj)
        {
            obj.Clear();
        }

        public void AddButton(IButtonViewModel button)
        {
            var item = AdditionalButtonsPool.Get();
            item.Id = button.Id;
            item.Image = button.Image;
            item.Label = button.Name;
            item.Color = button.Color;
            item.OnClick -= button.Click;
            item.OnClick += button.Click;
            button.Changed -= Button_Changed;
            button.Changed += Button_Changed;
            if (item is Component c)
            {
                c.gameObject.SetActive(true);
            }
            m_additionalButtonsList.Add(item);
        }

        private void Button_Changed(IViewModel viewModel)
        {
            var button = GetButton(m_additionalButtonsList, viewModel as IButtonViewModel);
            if (button != null && viewModel is IItemViewModel ivm)
            {
                button.Label = ivm.Name;
                button.Image = ivm.Image;
                button.Color = ivm.Color;
            }
        }

        private IClickItem GetButton<T>(IEnumerable<T> list, IItemViewModel viewModel) where T : IViewItem
        {
            foreach (var item in list)
            {
                if (item is IClickItem c && c.Id == viewModel.Id)
                {
                    return c;
                }
            }
            return null;
        }

        private ISelectItem GetSelectable<T>(IEnumerable<T> list, IItemViewModel viewModel) where T : IViewItem
        {
            foreach (var item in list)
            {
                if (item is ISelectItem c && c.Id == viewModel.Id)
                {
                    return c;
                }
            }
            return null;
        }

        public void AddMapItem(IItemViewModel item)
        {
            IViewItem view = null;
            switch (item)
            {
                case IButtonViewModel v:
                    view = MapButtonsPool.Get();
                    (view as IClickItem).OnClick -= v.Click;
                    (view as IClickItem).OnClick += v.Click;
                    break;
                case ISelectableViewModel v:
                    view = MapSelectsPool.Get();
                    (view as ISelectItem).OnSelected -= i => v.Select();
                    (view as ISelectItem).OnSelected += i => v.Select();
                    break;
            }
            view.Id = item.Id;
            view.Image = item.Image;
            view.Label = item.Name;
            view.Color = item.Color;
            if (view is Component c)
            {
                c.gameObject.SetActive(true);
            }
            m_mapItemsList.Add(view);
        }

        public void ClearButtons()
        {
            foreach (var item in m_additionalButtonsList)
            {
                AdditionalButtonsPool.Reclaim(item);
            }
            foreach (var (item, itemParent) in m_specialButtonsList)
            {
                if (item is Component c)
                {
                    c.gameObject.SetActive(false);
                    c.transform.SetParent(itemParent, false);
                }
            }
            m_additionalButtonsList.Clear();
        }

        public void ClearMapItems()
        {
            foreach (var item in m_mapItemsList)
            {
                if (item is IClickItem c)
                {
                    MapButtonsPool.Reclaim(c);
                }
                else if (item is ISelectItem s)
                {
                    MapSelectsPool.Reclaim(s);
                }
            }
            m_mapItemsList.Clear();
        }

        public void RemoveButton(IButtonViewModel button)
        {
            for (int i = 0; i < m_additionalButtonsList.Count; i++)
            {
                var item = m_additionalButtonsList[i];
                if (item.Id == button.Id)
                {
                    m_additionalButtonsList.RemoveAt(i);
                    AdditionalButtonsPool.Reclaim(item);
                    return;
                }
            }
        }

        public void RemoveMapItem(IItemViewModel itemViewModel)
        {
            for (int i = 0; i < m_mapItemsList.Count; i++)
            {
                var item = m_mapItemsList[i];
                if (item.Id == itemViewModel.Id)
                {
                    m_mapItemsList.RemoveAt(i);
                    if (item is IClickItem c)
                    {
                        MapButtonsPool.Reclaim(c);
                    }
                    else if (item is ISelectItem s)
                    {
                        MapSelectsPool.Reclaim(s);
                    }
                    return;
                }
            }
        }

        public void StartProgress(int id, string label, Func<float> progressFunctor)
        {
            // TODO: Implement simple various procedure progresses
        }

        public void StopProgress(int id)
        {
            // TODO: Implement simple various procedure progresses
        }

        public void AddSpecialButton(IClickItem button)
        {
            if (m_specialButtonsList.Any(b => b.item == button)) { return; }

            Transform itemParent = transform;
            if (button is Component c)
            {
                itemParent = c.transform.parent;
                if (itemParent != additionalButtonsParent.transform)
                {
                    c.transform.SetParent(additionalButtonsParent.transform, false);
                }
                c.gameObject.SetActive(true);
            }
            m_specialButtonsList.Add((button, itemParent));
        }

        public void RemoveSpecialButton(IClickItem button)
        {
            foreach (var (item, itemParent) in m_specialButtonsList)
            {
                if (item == button && item is Component c)
                {
                    c.gameObject.SetActive(false);
                    c.transform.SetParent(itemParent, false);
                }
            }
        }
        
        public void ResetAllButtons()
        {
            foreach(var selState in m_elementsStates)
            {
                selState?.ResetState();
            }
        }

        public IStandardButtonsSet GetStandardButtons() => m_standardButtons;

        public void SetNextEnabled(bool enable)
        {
            if (nextButton) { nextButton.interactable = enable; }
        }

        public void SetPrevEnabled(bool enable)
        {
            if (prevButton) { prevButton.interactable = enable; }
        }

        private struct SelectableState : IResetState
        {
            public Selectable selectable;
            public bool active;
            public bool interactable;
            public bool? isOn;

            public void ResetState()
            {
                if (selectable)
                {
                    selectable.gameObject.SetActive(active);
                    selectable.interactable = interactable;
                    if (isOn.HasValue && selectable is Toggle toggle)
                    {
                        toggle.isOn = isOn.Value;
                    }
                }
            }
        }

        [Serializable]
        private struct StandardButtons : IStandardButtonsSet
        {
            [Type(typeof(IClickItem))]
            public Component resetCameraOrbit;
            [Type(typeof(ISwitchItem))]
            public Component lockCameraOrbit;

            public IClickItem ResetCameraOrbit => resetCameraOrbit as IClickItem;

            public ISwitchItem LockCameraOrbit => lockCameraOrbit as ISwitchItem;
        }

    }
}
