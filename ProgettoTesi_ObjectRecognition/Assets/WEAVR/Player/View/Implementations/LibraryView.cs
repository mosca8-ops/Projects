using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using TXT.WEAVR.Common;

namespace TXT.WEAVR.Player.Views
{


    public class LibraryView : BaseView, ILibraryView
    {

        private const string k_ProceduresViewTypeKey = "LibraryView:ProceduresViewType";
        // STATIC PART??

        [Header("Groups")]
        [Tooltip("Button to open Group panel")] 
        [Draggable]
        public Button openGroupPanelButton;  // TODO: activate/deactivate wheter the group panel is open or not (OPS only)
        [Tooltip("Button to close Group panel")]
        [Draggable]
        public Button closeGroupPanelButton;  // TODO: (OPS only)
        [Tooltip("GameObject for Procedure Groups List")]
        [Draggable]
        public GameObject libraryGroupParent; // TODO: Will Activate/Deactivate (OPS only)
        [Tooltip("Canvas Panel for Procedure Groups Buttons")]
        [Draggable]
        public GameObject libraryGroupPanel; // TODO: Will be populated
        [Tooltip("Example of the procedure object to create the list")]
        [Draggable]
        [Type(typeof(IGroupItem))]
        public MonoBehaviour libraryGroupListExample; // TODO: Will populate Panel
        [Tooltip("List Selected Group Text - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI groupSelectedText;

        [Header("Cards")]
        [Tooltip("Canvas Parent for the Procedures List with card view")]
        [Draggable]
        public GameObject listCardParent; // TODO: Will Activate/Deactivate
        [Tooltip("Canvas Panel for the Procedures List with card view")]
        [Draggable]
        public GameObject listCardPanel; // TODO: Will be populated
        [Tooltip("Example of the procedure object preview with card view to create the list")]
        [Draggable]
        [Type(typeof(IProcedurePreview))]
        public MonoBehaviour listCardExample; // TODO: Will populate Panel
        [Tooltip("Scrollbar for the Procedures List with card view")]
        [Draggable]
        public Scrollbar listCardScrollbar;  // TODO: Control movement by Arrows(VR only)
        [Tooltip("Button up arrow of scrollbar for the Procedures List with card view")]
        [Draggable]
        public Button upArrowCardScrollbar; // TODO: Control Scrollbar movement (VR only)
        [Tooltip("Button down arrow of scrollbar for the Procedures List with card view")]
        [Draggable]
        public Button downArrowCardScrollbar;  // TODO: Control Scrollbar movement (VR only)

        [Header("Lines")]
        [Tooltip("Canvas Parent for the Procedures List with line view")]
        [Draggable]
        public GameObject listLineParent;  // TODO: Will Activate/Deactivate
        [Tooltip("Canvas Panel for the Procedures List with line view")]
        [Draggable]
        public GameObject listLinePanel; // TODO: Will be populated
        [Tooltip("Example of the procedure object preview with line view to create the list")]
        [Draggable]
        [Type(typeof(IProcedurePreview))]
        public MonoBehaviour listLineExample; // TODO: Will populate Panel
        [Tooltip("Scrollbar for the Procedures List with line view")]
        [Draggable]
        public Scrollbar listLineScrollbar;  // TODO: Control movement by Arrows(VR only)
        [Tooltip("Button up arrow of scrollbar for the Procedures List with line view")]
        [Draggable]
        public Button upArrowLineScrollbar;  // TODO: Control Scrollbar movement (VR only)
        [Tooltip("Button down arrow of scrollbar for the Procedures List with line view")]
        [Draggable]
        public Button downArrowLineScrollbar;  // TODO: Control Scrollbar movement (VR only)

        [Header("Other")]
        [Tooltip("Search Button")]
        [Draggable]
        public Button librarySearchButton;
        [Tooltip("Search Input Field")]
        [Draggable]
        public TMP_InputField librarySearchInput; // TODO: Will Activate/Deactivate 
        [Tooltip("Card View Button")]
        [Draggable]
        public Button cardViewButton;
        [Tooltip("Line View Button")]
        [Draggable]
        public Button lineViewButton;
        [Tooltip("Sort By Button")]
        [Draggable]
        public TMP_Dropdown librarySortBy;
        [Tooltip("Invert Sort By Order Button")]
        [Draggable]
        public Button invertSortButton;
        [Tooltip("Refresh Button")]
        [Draggable]
        public Button refreshButton;
        [Tooltip("Text to show when no procedures are available - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI noProceduresText;


        private IGroupViewModel m_selectedGroup;
        public IGroupViewModel SelectedGroup 
        { 
            get => m_selectedGroup;
            set
            {
                if(m_selectedGroup != value)
                {
                    m_selectedGroup = value;
                    ClearPreviews();
                    if(m_selectedGroup == null)
                    {
                        m_allProceduresGroup.item.IsSelected = true;
                        BuildPreviews(m_allProceduresGroup.procedures);
                    }
                    else
                    {
                        var group = GetGroup(m_selectedGroup.Id);
                        group.item.IsSelected = true;
                        BuildPreviews(group.procedures);
                    }
                    SelectedGroupChanged?.Invoke(m_selectedGroup);

                    UpdateNoProceduresMessage(m_selectedGroup != null ? "No procedures for this group" : "No procedures available");
                }
            }
        }

        public event Action<string> OnProcedureSearchUpdated;
        public event Action<IGroupViewModel> SelectedGroupChanged;
        public event Action<IProcedureViewModel> ProcedureSelected;
        public event Action OnRefresh;

        private List<IProcedureViewModel> m_procedures = new List<IProcedureViewModel>();
        private Dictionary<IProcedurePreview, IProcedureViewModel> m_previews = new Dictionary<IProcedurePreview, IProcedureViewModel>();
        private Dictionary<IProcedureViewModel, IProcedurePreview> m_viewModels = new Dictionary<IProcedureViewModel, IProcedurePreview>();
        private List<Group> m_groups = new List<Group>();
        private Group m_allProceduresGroup;
        private int m_lastReorderIndex;
        private bool m_ascendingSortBy;

        public GameObject CurrentListPanel { get; private set; }
        public IPool<IProcedurePreview> CurrentPool { get; private set; }

        private IPool<IProcedurePreview> PreviewCardsPool { get; set; }
        private IPool<IProcedurePreview> PreviewLinesPool { get; set; }
        private IPool<IGroupItem> GroupItemsPool { get; set; }

        public string SearchValue
        {
            get => librarySearchInput ? librarySearchInput.text : null;
            set
            {
                if (librarySearchInput)
                {
                    librarySearchInput.text = value;
                    NewSearchReady(value);
                }
            }
        }

        public IEnumerable<IProcedureViewModel> CurrentProcedures
            => SelectedGroup != null ? GetGroup(SelectedGroup.Id).procedures : m_procedures;

        public IEnumerable<IProcedureViewModel> CurrentlyVisibleProcedures { get; private set; }

        public IEnumerable<IGroupViewModel> Groups 
        { 
            get => m_groups.Select(g => g.group);
            set
            {
                // Get Selected Group
                var selected = SelectedGroup;

                // Clear existing items
                foreach (var g in m_groups)
                {
                    GroupItemsPool.Reclaim(g.item);
                }

                m_groups.Clear();

                int index = 1; // Needed to reorder in parent, the All Procedures is not part of the groups list
                               // but resided in transform parent as the first child
                var myGroup = value.FirstOrDefault(g => g.Id == Guid.Empty);
                if(myGroup != null)
                {
                    var group = CreateGroup(myGroup);
                    if(group.item is Component cItem)
                    {
                        cItem.transform.SetSiblingIndex(index++);
                    }
                    m_groups.Add(group);
                }

                foreach(var g in value.OrderBy(g => g.Name))
                {
                    if (g != myGroup)
                    {
                        var group = CreateGroup(g);
                        if (group.item is Component cItem)
                        {
                            cItem.transform.SetSiblingIndex(index++);
                        }
                        m_groups.Add(group);
                    }
                }

                SelectedGroup = value.FirstOrDefault(g => g.Id == selected?.Id);
            }
        }

        private Group CreateGroup(IGroupViewModel g)
        {
            var item = GroupItemsPool.Get();
            item.Id = g.Id;
            item.Label = g.Name;
            item.Description = g.Description;
            item.IsNew = false;

            item.OnSelected -= Group_OnSelected;
            item.OnSelected += Group_OnSelected;

            return new Group()
            {
                id = g.Id,
                group = g,
                procedures = new List<IProcedureViewModel>(),
                item = item,
            };
        }

        private void Group_OnSelected(ISelectItem item)
        {
            if(item == null)
            {
                // Select all procedures
                SelectedGroup = null;
            }
            else
            {
                SelectedGroup = GetGroup(item.Id).group;
            }
        }

        private Group GetGroup(in Guid id)
        {
            foreach (var group in m_groups)
            {
                if (group.id == id)
                {
                    return group;
                }
            }
            return default;
        }

        private void Awake()
        {
            // Setup input fields
            librarySearchInput.onValueChanged.AddListener(NewSearchReady);

            // Setup buttons
            cardViewButton.onClick.AddListener(SwitchToCardsView);
            lineViewButton.onClick.AddListener(SwitchToLinesView);


            // Setup other
            PreviewCardsPool = new Pool<IProcedurePreview>()
            {
                sample = listCardExample as IProcedurePreview,
                ResetObject = ClearProcedurePreview,
                container = listCardPanel,
            };

            PreviewLinesPool = new Pool<IProcedurePreview>()
            {
                sample = listLineExample as IProcedurePreview,
                ResetObject = ClearProcedurePreview,
                container = listLinePanel,
            };

            GroupItemsPool = new Pool<IGroupItem>()
            {
                sample = libraryGroupListExample as IGroupItem,
                ResetObject = ClearGroupItem,
                container = libraryGroupPanel,
            };

            m_allProceduresGroup = new Group()
            {
                id = Guid.NewGuid(),
                group = null,
                item = GroupItemsPool.Get(),
            };
            m_allProceduresGroup.item.Label = Translate("All Procedures");
            m_allProceduresGroup.item.Description = Translate("All procedures");
            m_allProceduresGroup.item.IsSelected = true;
            m_allProceduresGroup.item.IsNew = false;
            m_allProceduresGroup.item.Id = m_allProceduresGroup.id;
            m_allProceduresGroup.item.OnSelected += Group_OnSelected;

            if (refreshButton)
            {
                refreshButton.onClick.AddListener(Refresh);
            }

            switch(PlayerPrefs.GetString(k_ProceduresViewTypeKey, "Cards"))
            {
                case "Cards":
                    EnableCardsControls();
                    break;
                case "Lines":
                default:
                    EnableLinesControls();
                    break;
            }

            // Setup sorting
            m_lastReorderIndex = 2; // Default is by date
            m_ascendingSortBy = true;
            librarySortBy.ClearOptions();
            librarySortBy.AddOptions(new List<TMP_Dropdown.OptionData>()
            {
                new TMP_Dropdown.OptionData(Translate("Name")),
                new TMP_Dropdown.OptionData(Translate("Group")),
                new TMP_Dropdown.OptionData(Translate("Recently Assigned")),
            });
            librarySortBy.value = m_lastReorderIndex;
            librarySortBy.onValueChanged.AddListener(SortBy_ValueChanged);
            if (invertSortButton)
            {
                invertSortButton.onClick.AddListener(ChangeSortOrder_Clicked);
            }

            UpdateNoProceduresMessage("No procedures available");
        }

        private void ChangeSortOrder_Clicked()
        {
            ClearPreviews();
            m_ascendingSortBy = !m_ascendingSortBy;
            BuildPreviews(CurrentProcedures);
        }

        private void SortBy_ValueChanged(int orderIndex)
        {
            ClearPreviews();
            m_lastReorderIndex = orderIndex;
            BuildPreviews(CurrentProcedures);
        }

        private void EnableLinesControls()
        {
            if (CurrentListPanel) { CurrentListPanel.SetActive(false); }
            CurrentListPanel = listLineParent;
            CurrentPool = PreviewLinesPool;
            lineViewButton.gameObject.SetActive(false);
            cardViewButton.gameObject.SetActive(true);
            CurrentListPanel.SetActive(true);
            PlayerPrefs.SetString(k_ProceduresViewTypeKey, "Lines");
        }

        private void EnableCardsControls()
        {
            if (CurrentListPanel) { CurrentListPanel.SetActive(false); }
            CurrentListPanel = listCardParent;
            CurrentPool = PreviewCardsPool;
            cardViewButton.gameObject.SetActive(false);
            lineViewButton.gameObject.SetActive(true);
            CurrentListPanel.SetActive(true);
            PlayerPrefs.SetString(k_ProceduresViewTypeKey, "Cards");
        }

        private IEnumerable<IProcedureViewModel> ReorderPreviews(IEnumerable<IProcedureViewModel> procedures, int reorderIndex)
        {
            m_lastReorderIndex = reorderIndex;
            switch (reorderIndex)
            {
                case 0: // By Name
                    return ReorderByName(procedures, m_ascendingSortBy);
                case 1: // By Group
                    return ReorderByGroups(procedures, m_ascendingSortBy);
                case 2: // By Recently Assigned Date
                    return ReorderByDate(procedures, !m_ascendingSortBy);
            }
            return procedures;
        }

        private void Refresh()
        {
            OnRefresh?.Invoke();
        }

        private void ClearProcedurePreview(IProcedurePreview p)
        {
            p.Clear();
            p.OnAction -= Preview_OnAction;
            p.OnSelected -= Preview_OnSelected;
        }

        private void ClearGroupItem(IGroupItem g)
        {
            g.Clear();
            //g.OnSelected -= Group_OnSelected;
        }

        public void AddProcedures(IEnumerable<IProcedureViewModel> procedures)
        {
            m_procedures.AddRange(procedures);
            m_allProceduresGroup.procedures = m_procedures;
            foreach (var procedure in procedures)
            {
                foreach (var groupViewModel in procedure.AssignedGroups)
                {
                    var group = GetGroup(groupViewModel.Id);
                    if (!group.procedures.Contains(procedure))
                    {
                        group.procedures.Add(procedure);
                    }
                }
            }
            ClearPreviews();
            BuildPreviews(CurrentProcedures);
            UpdateNoProceduresMessage("No procedures available");
        }

        public void ClearProcedures()
        {
            ClearPreviews();
            m_procedures.Clear();
        }

        private void ClearPreviews()
        {
            if (CurrentPool != null)
            {
                foreach (var pair in m_previews)
                {
                    var preview = pair.Key;
                    preview.OnAction -= Preview_OnAction;
                    preview.OnSelected -= Preview_OnSelected;
                    CurrentPool.Reclaim(preview);

                    pair.Value.StatusChanged -= ViewModel_StatusChanged;
                }
            }
            m_previews.Clear();

            foreach(var viewModel in m_viewModels.Keys)
            {
                viewModel.StatusChanged -= ViewModel_StatusChanged;
            }
            m_viewModels.Clear();
        }

        private void BuildPreviews(IEnumerable<IProcedureViewModel> procedures)
        {
            procedures = ReorderPreviews(procedures, m_lastReorderIndex);
            int index = 0;
            foreach (var viewModel in procedures)
            {
                IProcedurePreview preview = CreatePreview(viewModel);

                if (preview is Component cPreview)
                {
                    cPreview.transform.SetSiblingIndex(index);
                }
                index++;
                m_previews[preview] = viewModel;
                m_viewModels[viewModel] = preview;
            }

            procedures = Filter(procedures, SearchValue);

            CurrentlyVisibleProcedures = procedures;
        }

        private void UpdateNoProceduresMessage(string message = "")
        {
            if (noProceduresText)
            {
                noProceduresText.gameObject.SetActive(CurrentlyVisibleProcedures == null || !CurrentlyVisibleProcedures.Any());
                if (!string.IsNullOrEmpty(message))
                {
                    noProceduresText.text = Translate(message);
                }
            }
        }

        private IProcedurePreview CreatePreview(IProcedureViewModel viewModel)
        {
            var preview = CurrentPool.Get();
            preview.Name = viewModel.Name;
            preview.Image = viewModel.Image;
            preview.AssignedGroupName = viewModel.AssignedGroups?.FirstOrDefault()?.Name;
            preview.Description = viewModel.Description;
            preview.Status = viewModel.Status;
            preview.Hide();

            preview.OnAction -= Preview_OnAction;
            preview.OnSelected -= Preview_OnSelected;
            viewModel.StatusChanged -= ViewModel_StatusChanged;
            viewModel.StatusChanged += ViewModel_StatusChanged;

            UpdatePreviewState(viewModel, preview);

            preview.OnSelected += Preview_OnSelected;
            return preview;
        }

        private void ViewModel_StatusChanged(IProcedureViewModel viewModel, ProcedureFlags newStatus)
        {
            if (!m_viewModels.TryGetValue(viewModel, out IProcedurePreview preview))
            {
                return;
            }
            UpdatePreviewState(viewModel, preview);
        }

        private void UpdatePreviewState(IProcedureViewModel viewModel, IProcedurePreview preview)
        {
            preview.StopLoading();
            preview.Status = viewModel.Status;
            preview.OnAction -= Preview_OnAction;
            if (viewModel.Status.HasFlag(ProcedureFlags.Syncing))
            {
                preview.StartLoading(Translate("Syncing..."), viewModel.GetSyncProgress);
            }
            else if (!viewModel.Status.HasFlag(ProcedureFlags.Ready))
            {
                preview.OnAction += Preview_OnAction;
            }
        }

        private void Preview_OnSelected(IProcedurePreview preview)
        {
            if (m_previews.TryGetValue(preview, out IProcedureViewModel viewModel))
            {
                ProcedureSelected?.Invoke(viewModel);
            }
        }

        private async void Preview_OnAction(IProcedurePreview preview)
        {
            if (m_previews.TryGetValue(preview, out IProcedureViewModel viewModel))
            {
                if (!viewModel.Status.HasFlag(ProcedureFlags.Ready))
                {
                    // Need to sync it
                    preview.OnAction -= Preview_OnAction;
                    //preview.StartLoading(Translate("Syncing..."), viewModel.GetSyncProgress);
                    try
                    {
                        await viewModel.Sync();
                    }
                    catch(Exception ex)
                    {
                        WeavrDebug.LogException(viewModel, ex);
                        preview.OnAction += Preview_OnAction;
                    }
                    finally
                    {
                        preview.StopLoading();
                    }
                }
            }
        }

        private void SwitchToLinesView()
        {
            ClearPreviews();
            EnableLinesControls();
            BuildPreviews(CurrentProcedures);
        }


        private void SwitchToCardsView()
        {
            ClearPreviews();
            EnableCardsControls();
            BuildPreviews(CurrentProcedures);
        }

        public IEnumerable<IProcedureViewModel> ReorderByName(IEnumerable<IProcedureViewModel> procedures, bool ascending)
        {
            return ascending ? procedures.OrderBy(p => p.Name)
                                    : procedures.OrderByDescending(p => p.Name);
        }

        public IEnumerable<IProcedureViewModel> ReorderByDate(IEnumerable<IProcedureViewModel> procedures, bool ascending)
        {
            return ascending ? procedures.OrderBy(p => p.AssignedDate)
                                    : procedures.OrderByDescending(p => p.AssignedDate);
        }

        public IEnumerable<IProcedureViewModel> ReorderByGroups(IEnumerable<IProcedureViewModel> proceduresList, bool ascending)
        {
            List<IProcedureViewModel> procedures = new List<IProcedureViewModel>();
            var orderedGroups = ascending ? m_groups.OrderBy(g => g.group?.Name) : m_groups.OrderByDescending(g => g.group?.Name);
            foreach (var group in orderedGroups)
            {
                foreach(var groupProcedure in group.procedures)
                {
                    if (!procedures.Contains(groupProcedure) && proceduresList.Any(p => p.Id == groupProcedure.Id))
                    {
                        procedures.Add(groupProcedure);
                    }
                }
            }
            return procedures;
        }

        private void NewSearchReady(string searchString)
        {
            searchString = searchString?.ToLower();
            if (OnProcedureSearchUpdated != null)
            {
                OnProcedureSearchUpdated(searchString);
            }
            else
            {
                // Perform the standard filter search
                ClearPreviews();
                BuildPreviews(CurrentProcedures);
                UpdateNoProceduresMessage(string.IsNullOrEmpty(searchString) ? "No procedures available" : $"No procedures found for '{searchString}'");
            }
        }

        private IEnumerable<IProcedureViewModel> Filter(IEnumerable<IProcedureViewModel> procedures, string searchString)
        {

            if (string.IsNullOrEmpty(searchString))
            {
                // Show all of them
                foreach(var viewModel in procedures)
                {
                    if(m_viewModels.TryGetValue(viewModel, out IProcedurePreview preview))
                    {
                        preview.IsVisible = true;
                    }
                }
                return procedures;
            }
            else
            {
                List<IProcedureViewModel> viewModels = new List<IProcedureViewModel>();
                foreach(var viewModel in procedures)
                {
                    if(m_viewModels.TryGetValue(viewModel, out IProcedurePreview preview))
                    {
                        preview.IsVisible = IsValid(viewModel, searchString);
                        if (preview.IsVisible)
                        {
                            viewModels.Add(viewModel);
                        }
                        else
                        {
                            viewModels.Remove(viewModel);
                        }
                    }
                }
                return viewModels;
            }
        }

        private static bool IsValid(IProcedureViewModel viewModel, string searchString)
        {
            return viewModel.Name.ToLower().Contains(searchString) || viewModel.Name.Replace(" ", "").ToLower().Contains(searchString);
        }

        public void RefreshViews()
        {
            ClearPreviews();
            BuildPreviews(CurrentProcedures);
            UpdateNoProceduresMessage("No procedures available");
        }

        private struct Group
        {
            public Guid id;
            public IGroupViewModel group;
            public IGroupItem item;
            public List<IProcedureViewModel> procedures;
        }
    }
}
