using System;
using TXT.WEAVR.AR;
using TXT.WEAVR.Core;
using TXT.WEAVR.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.Player.Views
{

    public class ARControlsView : MonoBehaviour, IARControlsView
    {
        [Tooltip("Parent for ar enable buttons. Needed to enable - disable AR functionality")]
        public GameObject arEnableParent;
        [Tooltip("Parent for move objects buttons. Needed to enable - disable functionality")]
        public GameObject moveParent;
        [Tooltip("The default ar mode when starting ar tracking")]
        public ARTargetMode defaultMode = ARTargetMode.Marker;

        [Header("AR Enable Items")]
        [Tooltip("Panel for the modes selection")]
        [Draggable]
        public GameObject modesSelectionPanel;
        [Tooltip("Toggle to enable AR view")]
        [Draggable]
        public ToggleButtons enableArToggle;
        [Tooltip("Toggle to use marker tracking")]
        [Draggable]
        public Toggle useMarkerToggle;
        [Tooltip("Toggle to use surface tracking")]
        [Draggable]
        public Toggle useSurfaceToggle;
        [Tooltip("Marker frame Panel")]
        [Draggable]
        public GameObject markerImage; //TODO: to activate/deactivate when using marker 


        [Header("AR Movement Items")]
        [Tooltip("Button to enable AR movement")]
        [Draggable]
        public ToggleButtons enableMoveToggle;
        [Tooltip("Toggle to use market tracking")]
        [Draggable]
        public GameObject advPlacementPanel; //TODO: to activate/deactivate with MoveButtons ONLY if AR is active (or separate AR Lock buttons)
        [Tooltip("Toggle to use surface tracking")]
        [Draggable]
        public Toggle advScaleToggle;
        [Tooltip("Toggle to use surface tracking")]
        [Draggable]
        public Toggle advRotationToggle;
        [Tooltip("Toggle to use surface tracking")]
        [Draggable]
        public Toggle advPositionToggle;


        [Header("AR Advanced Translation")]
        [Tooltip("The panel which holds advanced translation manipulation")]
        [Draggable]
        public GameObject advPositionPanel;
        [Tooltip("Slider for X translation")]
        [Draggable]
        public SpringSlider relativePosition_X;
        [Tooltip("Slider for Y translation")]
        [Draggable]
        public SpringSlider relativePosition_Y;
        [Tooltip("Slider for Z translation")]
        [Draggable]
        public SpringSlider relativePosition_Z;
        [Tooltip("Button to save position")]
        [Draggable]
        public Button savePositionButton;
        [Tooltip("Button to reset position")]
        [Draggable]
        public Button resetPositionButton;


        [Header("AR Advanced Rotation")]
        [Tooltip("The panel which holds advanced rotation manipulation")]
        [Draggable]
        public GameObject advRotationPanel;
        [Tooltip("Slider for X rotation")]
        [Draggable]
        public SpringSlider relativeRotation_X;
        [Tooltip("Slider for Y rotation")]
        [Draggable]
        public SpringSlider relativeRotation_Y;
        [Tooltip("Slider for Z rotation")]
        [Draggable]
        public SpringSlider relativeRotation_Z;
        [Tooltip("Button to save rotation")]
        [Draggable]
        public Button saveRotationButton;
        [Tooltip("Button to reset rotation")]
        [Draggable]
        public Button resetRotationButton;


        [Header("AR Scale Items")]
        [Tooltip("The panel which holds advanced scale manipulation")]
        [Draggable]
        public GameObject advScalePanel;
        [Tooltip("Button to save scale")]
        [Draggable]
        public RatioCounter scaleCounter;
        [Tooltip("Button to save scale")]
        [Draggable]
        public Button saveScaleButton;
        [Tooltip("Button to reset scale")]
        [Draggable]
        public Button resetScaleButton;

        private bool m_arEnabled;
        private bool m_advMovementEnabled;
        private ARTargetMode m_currentMode;

        public bool IsVisible {
            get => arEnableParent && arEnableParent.activeInHierarchy;
            set {
                if (value) { Show(); }
                else { Hide(); }
            }
        }

        public float Scale { get => scaleCounter.Scale; set => scaleCounter.Scale = value; }

        public bool IsArEnabled {
            get => m_arEnabled;
            set => EnableAR(value);
        }

        public ARTargetMode CurrentMode {
            get => m_currentMode;
            set {
                if (m_currentMode != value)
                {
                    useMarkerToggle.isOn = value == ARTargetMode.Marker;
                    useSurfaceToggle.isOn = value == ARTargetMode.Surface;
                }
            }
        }

        public event UnityAction<float> OnRelativeRotationX {
            add => relativeRotation_X.onValueChanged.AddListener(value);
            remove => relativeRotation_X.onValueChanged.RemoveListener(value);
        }

        public event UnityAction<float> OnRelativeRotationY {
            add => relativeRotation_Y.onValueChanged.AddListener(value);
            remove => relativeRotation_Y.onValueChanged.RemoveListener(value);
        }

        public event UnityAction<float> OnRelativeRotationZ {
            add => relativeRotation_Z.onValueChanged.AddListener(value);
            remove => relativeRotation_Z.onValueChanged.RemoveListener(value);
        }

        public event UnityAction OnARSaveRotation {
            add => saveRotationButton.onClick.AddListener(value);
            remove => saveRotationButton.onClick.RemoveListener(value);
        }

        public event UnityAction OnARResetRotation {
            add => resetRotationButton.onClick.AddListener(value);
            remove => resetRotationButton.onClick.RemoveListener(value);
        }

        public event UnityAction<float> OnRelativePositionX {
            add => relativePosition_X.onValueChanged.AddListener(value);
            remove => relativePosition_X.onValueChanged.RemoveListener(value);
        }

        public event UnityAction<float> OnRelativePositionY {
            add => relativePosition_Y.onValueChanged.AddListener(value);
            remove => relativePosition_Y.onValueChanged.RemoveListener(value);
        }

        public event UnityAction<float> OnRelativePositionZ {
            add => relativePosition_Z.onValueChanged.AddListener(value);
            remove => relativePosition_Z.onValueChanged.RemoveListener(value);
        }

        public event UnityAction OnARSavePosition {
            add => savePositionButton.onClick.AddListener(value);
            remove => savePositionButton.onClick.RemoveListener(value);
        }

        public event UnityAction OnARResetPosition {
            add => resetPositionButton.onClick.AddListener(value);
            remove => resetPositionButton.onClick.RemoveListener(value);
        }

        public event ViewDelegate OnHide;
        public event ViewDelegate OnShow;
        public event ViewDelegate OnBack;

        public event OnValueChanged<bool> OnAREnabledChanged;
        public event OnValueChanged<bool> OnPlacementUnlockChanged;
        public event OnValueChanged<ARTargetMode> OnModeChanged;

        public event UnityAction OnSaveScale {
            add => saveScaleButton.onClick.AddListener(value);
            remove => saveScaleButton.onClick.RemoveListener(value);
        }

        public event UnityAction OnResetScale {
            add => resetScaleButton.onClick.AddListener(value);
            remove => resetScaleButton.onClick.RemoveListener(value);
        }

        public event Action<float> OnScaleChange {
            add => scaleCounter.onScaleChange += value;
            remove => scaleCounter.onScaleChange -= value;
        }

        private void Awake()
        {
            CurrentMode = defaultMode;

            enableArToggle.ValueChanged -= EnableAR;
            enableArToggle.ValueChanged += EnableAR;

            useMarkerToggle.onValueChanged.AddListener(EnableMarkerMode);
            useSurfaceToggle.onValueChanged.AddListener(EnableSurfaceMode);

            enableMoveToggle.ValueChanged -= EnableAdvancedPlacement;
            enableMoveToggle.ValueChanged += EnableAdvancedPlacement;

            advPositionToggle.onValueChanged.AddListener(value => EnableAdvancedPanel(value ? advPositionPanel : null));
            advRotationToggle.onValueChanged.AddListener(value => EnableAdvancedPanel(value ? advRotationPanel : null));
            advScaleToggle.onValueChanged.AddListener(value => EnableAdvancedPanel(value ? advScalePanel : null));
        }

        public void Hide()
        {
            if (arEnableParent)
            {
                arEnableParent.SetActive(false);
                enableArToggle.gameObject.SetActive(false);
            }
            if (moveParent)
            {
                moveParent.SetActive(false);
            }
            OnHide?.Invoke(this);
        }

        public void Show()
        {
            if (arEnableParent)
            {
                arEnableParent.SetActive(true);
                enableArToggle.gameObject.SetActive(true);
            }
            if (moveParent)
            {
                moveParent.SetActive(true);
            }
            EnableAdvancedPlacement(false);
            EnableModeSelection(false);

            enableMoveToggle.gameObject.SetActive(false);
            OnShow?.Invoke(this);
        }

        private void EnableAdvancedPanel(GameObject panel)
        {
            if (advPositionPanel) { advPositionPanel.SetActive(advPositionPanel == panel); }
            if (advRotationPanel) { advRotationPanel.SetActive(advRotationPanel == panel); }
            if (advScalePanel) { advScalePanel.SetActive(advScalePanel == panel); }
        }

        private void EnableCorrectMovementPanel()
        {
            EnableAdvancedPanel(advPositionToggle.isOn ? advPositionPanel :
                                    advScaleToggle.isOn ? advScalePanel :
                                    advRotationToggle.isOn ? advRotationPanel :
                                    null);
        }

        private void EnableSurfaceMode(bool enable)
        {
            if (enable)
            {
                m_currentMode = ARTargetMode.Surface;
                OnModeChanged?.Invoke(ARTargetMode.Surface);
                markerImage.gameObject.SetActive(false);
            }
        }

        private void EnableMarkerMode(bool enable)
        {
            markerImage.gameObject.SetActive(enable);
            if (enable)
            {
                m_currentMode = ARTargetMode.Marker;
                OnModeChanged?.Invoke(ARTargetMode.Marker);
            }
        }

        public void TargetDetectionChanged(bool targetAquired)
        {
            if (markerImage)
            {
                markerImage.gameObject.SetActive(m_currentMode == ARTargetMode.Marker && !targetAquired);
            }
            if (targetAquired)
            {
                EnableModeSelection(false);
            }
        }

        public void EnableModeSelection(bool enable)
        {
            if (modesSelectionPanel)
            {
                modesSelectionPanel.SetActive(enable);
            }
        }

        public void EnableAdvancedPlacement(bool enable)
        {
            if (advPlacementPanel)
            {
                advPlacementPanel.SetActive(enable);
            }
            if (enable)
            {
                EnableCorrectMovementPanel();
            }
            OnPlacementUnlockChanged?.Invoke(enable);
        }

        public void EnableAR(bool enable)
        {
            if (m_arEnabled == enable) { return; }
            m_arEnabled = enable;

            EnableModeSelection(enable);

            enableMoveToggle.Value = false;
            enableMoveToggle.Interactable = enable;

            if (enable)
            {
                // By default start with last selected mode
                useMarkerToggle.isOn = m_currentMode == ARTargetMode.Marker;
                useMarkerToggle.isOn = m_currentMode == ARTargetMode.Surface;

                markerImage.gameObject.SetActive(m_currentMode == ARTargetMode.Marker);
            }
            else
            {
                EnableMarkerMode(false);
                EnableSurfaceMode(false);
                EnableAdvancedPlacement(false);
            }

            OnAREnabledChanged?.Invoke(enable);
        }

        public void SetARInteractivity(bool buttonsActive)
        {
            if (!buttonsActive)
            {
                EnableMarkerMode(false);
                EnableSurfaceMode(false);
                EnableAdvancedPlacement(false);
            }

            enableArToggle.Interactable = buttonsActive;
        }
    }
}
