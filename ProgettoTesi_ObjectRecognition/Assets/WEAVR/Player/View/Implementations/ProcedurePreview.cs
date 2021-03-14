
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace TXT.WEAVR.Player.Views
{
    public class ProcedurePreview : BaseView, IProcedurePreview
    {
        [Header("Components")]
        [Draggable]
        public Image procedureImage;
        [Draggable]
        public Sprite defaultImage;
        [Draggable]
        public TextMeshProUGUI procedureName;
        [Draggable]
        public TextMeshProUGUI assignedGroupName;
        [Tooltip("Button to trigger the procedure selection")]
        [Draggable]
        public Button selectProcedureButton;
        [Tooltip("Button to activate a predefined action")]
        [Draggable]
        public Button innerActionButton;

        [Header("Status")]
        public GameObject statusDone;
        public GameObject statusNew;
        public GameObject statusToUpgrade;

        private ProcedureFlags m_status = ProcedureFlags.Undefined;

        public string Name { get => procedureName.text; set => procedureName.text = value; }
        public string Description { get; set; }
        public string AssignedGroupName
        {
            get => assignedGroupName ? assignedGroupName.text : null;
            set { if (assignedGroupName) assignedGroupName.text = value; }
        }

        public Texture2D Image
        {
            get => procedureImage && procedureImage.sprite ? procedureImage.sprite.texture : null;
            set
            {
                if (procedureImage && procedureImage.sprite && procedureImage.sprite.texture != value)
                {
                    if (value)
                    {
                        procedureImage.sprite = SpriteCache.Instance.Get(value);
                    }
                    else
                    {
                        procedureImage.sprite = defaultImage;
                    }
                }
            }
        }

        public ProcedureFlags Status { 
            get => m_status;
            set
            {
                if(m_status != value)
                {
                    m_status = value;
                    UpdateStatusObjects(m_status);
                }
            } 
        }

        private void UpdateStatusObjects(ProcedureFlags status)
        {
            if (statusNew) { statusNew.SetActive(status.HasFlag(ProcedureFlags.New | ProcedureFlags.Ready) 
                                              && !status.HasFlag(ProcedureFlags.Syncing)); }
            if (statusDone) { statusDone.SetActive(status.HasFlag(ProcedureFlags.Ready) 
                                                && !status.HasFlag(ProcedureFlags.New)
                                                && status.HasFlag(ProcedureFlags.Sync) 
                                                && !status.HasFlag(ProcedureFlags.Syncing)); }
            if (statusToUpgrade) { statusToUpgrade.SetActive(status.HasFlag(ProcedureFlags.Ready)
                                                && !status.HasFlag(ProcedureFlags.New)
                                                && !status.HasFlag(ProcedureFlags.Sync)
                                                && !status.HasFlag(ProcedureFlags.Syncing)); }
        }

        private ViewDelegate<IProcedurePreview> m_onSelected;
        public event ViewDelegate<IProcedurePreview> OnSelected
        {
            add
            {
                m_onSelected += value;
                if (selectProcedureButton) { selectProcedureButton.interactable = true; }
            }
            remove
            {
                m_onSelected -= value;
                if(selectProcedureButton) { selectProcedureButton.interactable = m_onSelected != null; }
            }
        }

        private ViewDelegate<IProcedurePreview> m_onAction;
        public event ViewDelegate<IProcedurePreview> OnAction
        {
            add
            {
                m_onAction += value;
                if (innerActionButton) 
                {
                    innerActionButton.gameObject.SetActive(true);
                    innerActionButton.interactable = true; 
                }
            }
            remove
            {
                m_onAction -= value;
                if (innerActionButton) 
                { 
                    innerActionButton.interactable = m_onSelected != null;
                    innerActionButton.gameObject.SetActive(m_onAction?.GetInvocationList()?.Length > 0);
                }
            }
        }

        protected override void Start()
        {
            base.Start();
            if (selectProcedureButton)
            {
                selectProcedureButton.onClick.AddListener(OnSelectedClicked);
            }
            if (innerActionButton)
            {
                innerActionButton.onClick.AddListener(OnInnerActionClicked);
            }
        }

        protected virtual void OnEnable()
        {
            //if (innerActionButton) { innerActionButton.gameObject.SetActive(true); }
        }

        protected virtual void OnSelectedClicked() => m_onSelected?.Invoke(this);
        protected virtual void OnInnerActionClicked() => m_onAction?.Invoke(this);

        public void Clear()
        {
            m_onAction = null;
            m_onSelected = null;
            if(selectProcedureButton) { selectProcedureButton.interactable = false; }
            if(innerActionButton) { innerActionButton.interactable = false; }
            UpdateStatusObjects(m_status);
            Image = null;
            Name = null;
            Description = null;
            AssignedGroupName = null;
        }

        public override void StartLoading(string title = "")
        {
            base.StartLoading(title);
            if (innerActionButton) innerActionButton.gameObject.SetActive(false);
        }

        public override void StartLoading(string title, Func<float> progressCallback)
        {
            base.StartLoading(title, progressCallback);
            if (innerActionButton) innerActionButton.gameObject.SetActive(false);
        }

        public override void StopLoading()
        {
            base.StopLoading();
            if (innerActionButton) { innerActionButton.gameObject.SetActive(m_onAction?.GetInvocationList()?.Length > 0); }
        }
    }
}