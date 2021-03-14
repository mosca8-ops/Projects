namespace TXT.WEAVR.Interaction
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    [AddComponentMenu("WEAVR/UI/Context Menu 3D")]
    public class ContextMenu3D : AbstractMenu3D
    {
        #region [  STATIC PART  ]
        private static ContextMenu3D _instance;
        /// <summary>
        /// Gets the instance of the context menu
        /// </summary>
        public static ContextMenu3D Instance {
            get {
                if(_instance == null) {
                    _instance = FindObjectOfType<ContextMenu3D>();
                    if(_instance == null) {
                        foreach(var rootObject in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) {
                            _instance = rootObject.GetComponentInChildren<ContextMenu3D>(true);
                            if(_instance != null) {
                                break;
                            }
                        }
                    }
                    if (_instance != null) {
                        _instance.Start();
                    }
                    else {
                        Debug.LogErrorFormat("[{0}]: Unable to find gameobject with such component", typeof(ContextMenu3D).Name);
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Begins a new menu
        /// </summary>
        /// <returns>The newly started menu</returns>
        public static ContextMenu3D Begin() {
            return Instance.BeginMenu();
        }

        #endregion
        [Header("Menu")]
        [SerializeField]
        [Draggable]
        private Transform _buttonsPanel;
        [SerializeField]
        [Draggable]
        private Button _buttonSample;

        private List<GameObject> _buttons;

        protected override void Clear() {
            base.Clear();
            foreach (var button in _buttons) {
                Destroy(button);
            }
            _buttons.Clear();
        }

        public ContextMenu3D BeginMenu() {
            Hide();
            return this;
        }

        public ContextMenu3D AddMenuItem(string name, UnityAction onClickCallback) {
            var newButton = Instantiate(_buttonSample);
            newButton.onClick.AddListener(onClickCallback);
            newButton.onClick.AddListener(Hide);
            var textElement = newButton.GetComponentInChildren<UI.ITextComponent>(true);
            if (textElement != null)
            {
                textElement.Text = name;
            }
            else
            {
                var textComponent = newButton.GetComponentInChildren<Text>(true);
                if (textComponent)
                {
                    textComponent.text = name;
                }
            }
            newButton.transform.SetParent(_buttonsPanel, false);
            newButton.gameObject.SetActive(true);
            _buttons.Add(newButton.gameObject);
            return this;
        }

        // Use this for initialization
        protected override void Start() {
            base.Start();
            if(_instance == null) {
                _instance = this;
            }
            else if(_instance != this) {
                Debug.LogErrorFormat("[{0}]: More than one instance detected, deleting object '{1}'", typeof(ContextMenu3D).Name, gameObject.name);
                Destroy(this);
                return;
            }
            _buttons = new List<GameObject>();
        }
    }
}