using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using TXT.WEAVR.Localization;
using System;
using System.Linq;
using TXT.WEAVR.UI;
using TXT.WEAVR.Common;

namespace TXT.WEAVR.Player.Views
{
    public class ProcedureView : BaseView, IProcedureView
    {
        [Space]
        [Header("Procedure Page Items")]
        [Tooltip("Procedure Image preview")]
        [Draggable]
        public Image procedureImage;
        [Tooltip("Button controls video preview")]
        [Draggable]
        public Button procedurePlayVideoButton; // To decide if used
        [Tooltip("GameObject controls video preview raycast")]
        [Draggable]
        public GameObject procedurePlayVideoPanel; // To decide if used
        [Tooltip("Procedure Language list button - TMP dropdown")]
        [Draggable]
        public TMP_Dropdown procedureLanguageDropdown;
        [Tooltip("Procedure download loader")]
        [Draggable]
        [Type(typeof(IProgressLoader))]
        public Component syncLoader;

        [Header("Labels")]
        [Tooltip("Procedure Name - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureName;
        [Tooltip("Procedure Assigned Group - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI assignedGroupName;
        [Tooltip("Procedure Overview - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureOverview;
        [Tooltip("Procedure LAST UPDATE - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureLastUpdate;

        [Header("Buttons")]
        [Tooltip("Start Button")]
        [Draggable]
        public Button procedureStartButton;
        [Tooltip("Update Button")]
        [Draggable]
        public Button procedureUpdateButton;
        [Tooltip("Remove Button")]
        [Draggable]
        public Button procedureRemoveButton;
        [Tooltip("Remove Button")]
        [Draggable]
        public Button procedureDownloadButton;
        [Tooltip("Back Button")]
        [Draggable]
        public Button procedureBackButton;

        [Header("Stats")]
        [Tooltip("Procedure estimated time - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureEstTime;
        [Tooltip("Procedure number of steps - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureStepNumber;
        [Tooltip("ProcedureCompletedTimes - TMP text UI")]
        [Draggable]
        public TextMeshProUGUI procedureCompletedTimes;


        [Header("Status")]
        public GameObject statusInProgress; // To decide if used (data from server?)
        public GameObject statusDone; // To decide if used (data from server?)
        public GameObject statusNew; // To decide if used (data from server?)
        public GameObject statusUpdateAvailable; // To decide if used (data from server?)

        private List<Language> m_languages;
        private Sprite m_defaultProcedureSprite;

        public Texture2D ProcedureImage { get => procedureImage.sprite.texture; set => procedureImage.sprite = value ? SpriteCache.Instance.Get(value) : m_defaultProcedureSprite; }

        public string ProcedureName { get => procedureName.text; set => procedureName.text = value; }
        public string AssignedGroupName { get => assignedGroupName.text; set => assignedGroupName.text = value; }
        public string ProcedureOverview { get => procedureOverview.text; set => procedureOverview.text = value; }
        public string ProcedureLastUpdate { get => procedureLastUpdate.text; set => procedureLastUpdate.text = value; }
        public string ProcedureEstTime { get => procedureEstTime.text; set => procedureEstTime.text = value; }
        public int ProcedureStepNumber { get => int.Parse(procedureStepNumber.text); set => procedureStepNumber.text = value.ToString(); }
        public int ProcedureCompletedTimes { get => int.Parse(procedureCompletedTimes.text); set => procedureCompletedTimes.text = value.ToString(); }

        public Language Language { 
            get => m_languages[procedureLanguageDropdown.value]; 
            set => procedureLanguageDropdown.value = m_languages.IndexOf(value); 
        }

        public event Action OnRemove;
        public event Action OnStart;
        public event Action OnUpdate;
        public event Action OnDownload;

        private void Awake()
        {
            if (procedureImage)
            {
                m_defaultProcedureSprite = procedureImage.sprite;
            }
            procedureStartButton.onClick.AddListener(ProcedureStart);
            procedureUpdateButton.onClick.AddListener(ProcedureUpdate);
            procedureRemoveButton.onClick.AddListener(ProcedureRemove);
            procedureDownloadButton.onClick.AddListener(ProcedureDownload);
            procedureBackButton.onClick.AddListener(Back);
        }

        private void ProcedureDownload() => OnDownload?.Invoke();
        private void ProcedureRemove() => OnRemove?.Invoke();
        private void ProcedureUpdate() => OnUpdate?.Invoke();
        private void ProcedureStart() => OnStart?.Invoke();

        public void SetAvailableLanguages(IEnumerable<Language> languages)
        {
            m_languages = new List<Language>(languages);
            procedureLanguageDropdown.ClearOptions();
            procedureLanguageDropdown.AddOptions(languages.Select(l => new TMP_Dropdown.OptionData(l.TwoLettersISOName)).ToList());
        }

        private void StopSyncLoader()
        {
            if(syncLoader is IProgressLoader loader)
            {
                loader.Hide();
            }
        }

        private void StartSyncLoader(string label, Func<float> progressGetter)
        {
            if (syncLoader is IProgressLoader loader)
            {
                loader.Show(label, progressGetter);
            }
        }

        public void SetStatus(ProcedureFlags status, Func<float> progressForLoader)
        {
            StopSyncLoader();
            procedureStartButton.gameObject.SetActive(status.HasFlag(ProcedureFlags.Ready) && !status.HasFlag(ProcedureFlags.Syncing));
            procedureRemoveButton.gameObject.SetActive(status.HasFlag(ProcedureFlags.Ready | ProcedureFlags.CanBeRemoved) && !status.HasFlag(ProcedureFlags.Syncing));
            procedureUpdateButton.gameObject.SetActive(status.HasFlag(ProcedureFlags.Ready) && !status.HasFlag(ProcedureFlags.Sync) && !status.HasFlag(ProcedureFlags.Syncing));
            procedureDownloadButton.gameObject.SetActive(!status.HasFlag(ProcedureFlags.Ready) && !status.HasFlag(ProcedureFlags.Syncing));

            if (statusDone) { statusDone.SetActive(status.HasFlag(ProcedureFlags.Sync) && !status.HasFlag(ProcedureFlags.New) && !status.HasFlag(ProcedureFlags.Syncing)); }
            if (statusNew) { statusNew.SetActive(status.HasFlag(ProcedureFlags.New | ProcedureFlags.Ready) && status.HasFlag(ProcedureFlags.Sync) && !status.HasFlag(ProcedureFlags.Syncing)); }
            if (statusInProgress) { statusInProgress.SetActive(status.HasFlag(ProcedureFlags.Syncing)); }
            if (statusUpdateAvailable) { statusUpdateAvailable.SetActive(status.HasFlag(ProcedureFlags.Ready) && !status.HasFlag(ProcedureFlags.Sync) && !status.HasFlag(ProcedureFlags.Syncing)); }

            if (status.HasFlag(ProcedureFlags.Syncing))
            {
                StartSyncLoader(Translate("Syncing..."), progressForLoader);
            }
        }
    }
}
