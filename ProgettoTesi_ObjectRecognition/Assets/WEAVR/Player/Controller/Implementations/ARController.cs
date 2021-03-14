using System.Threading.Tasks;
using TXT.WEAVR.AR;
using TXT.WEAVR.Core;
using TXT.WEAVR.InteractionUI;
using TXT.WEAVR.Player.Model;
using TXT.WEAVR.Player.Views;
using UnityEngine;

namespace TXT.WEAVR.Player.Controller
{

    public class ARController : BaseController, IARController
    {
        public IProceduresModel Model { get; private set; }
        public IARControlsView View { get; private set; }
        public IARObjectPositioner Positioner { get; private set; }
        public IInteractablePanel InputPanel { get; private set; }
        public IGesturesView GesturesView { get; private set; }

        public Camera StandardCamera { get; private set; }
        public Camera ARCamera => Positioner.ARCamera;

        private StoreTransform _storedTransform = null;
        private float m_scale = 1;

        public Vector3 PositionOffset { get; private set; }
        public Vector3 RotationOffset { get; private set; }
        public float Scale { get => m_scale; set => m_scale = Mathf.Clamp(value, 0.05f, 20f); }
        public bool TargetSet { get; private set; }
        public bool MovementIsActive { get; private set; }
        public bool XRInitialized { get; private set; }

        private bool m_destroyPositioner;
        private bool m_gesturesViewVisibility;

        public event OnValueChanged<bool> OnAREnabled;

        public ARController(IDataProvider provider) : base(provider)
        {
            Model = provider.GetModel<IProceduresModel>();
            View = provider.GetView<IARControlsView>();
            GesturesView = provider.GetView<IGesturesView>();
            Scale = 1;
        }

        public async Task Start()
        {
            TargetSet = false;
            MovementIsActive = false;
            StandardCamera = null;
            if (!XRInitialized)
            {
                XRInitialized = await InitializeXRSystem();
            }
            View.Show();
            m_gesturesViewVisibility = GesturesView?.IsVisible ?? false;
            if (Model.RunningProcedure.Capabilities.usesAR)
            {
                m_destroyPositioner = false;
                Positioner = SceneTools.GetComponentInScene<IARObjectPositioner>();
                if (Positioner == null)
                {
                    var positionerGO = Resources.Load<GameObject>("AR/AR Object Positioner");
                    if (positionerGO && positionerGO.GetComponentInChildren<IARObjectPositioner>() != null)
                    {
                        positionerGO.transform.position = new Vector3(1000, 1000, 1000);
                        Positioner = Object.Instantiate(positionerGO).GetComponentInChildren<IARObjectPositioner>();
                        m_destroyPositioner = Positioner != null;
                    }
                }
                if (Positioner != null)
                {
                    Positioner.Active = true;
                    var isSupported = await Positioner.CheckIfARIsSupported();
                    Positioner.Active = false;
                    if (isSupported)
                    {
                        InputPanel = GesturesView?.GetPanel();
                        if (InputPanel != null)
                        {
                            InputPanel.ResetInput();
                        }
                        PositionOffset = Vector3.zero;
                        RotationOffset = Vector3.zero;
                        Scale = 1;

                        UnhookFromEvents();
                        HookToEvents();
                        return;
                    }
                    else
                    {
                        WeavrDebug.LogError(this, "This device does not support AR Functionality");
                        if (m_destroyPositioner && Positioner is Component c)
                        {
                            Object.Destroy(c.gameObject);
                        }
                        await PopupManager.ShowErrorAsync(
                            Translate("AR Not Supported"),
                            Translate("This procedure makes use of AR Functionality which is not supported on this device."));
                    }
                }
            }

            View.Hide();
        }
        
        public Task Stop()
        {
            if (m_destroyPositioner && Positioner is Component c)
            {
                Object.Destroy(c.gameObject);
            }
            Positioner = null;
            UnhookFromEvents();
            View?.Hide();
            if (GesturesView != null)
            {
                if (m_gesturesViewVisibility)
                {
                    GesturesView.Show();
                }
                else
                {
                    GesturesView.Hide();
                }
            }

            if (XRInitialized)
            {
                DeinitializeXRSystem();
                XRInitialized = false;
            }

            return Task.CompletedTask;
        }

        private async Task<bool> InitializeXRSystem()
        {
            return true;
            //#if WEAVR_PLAYER_XR
            //            Debug.Log("Initializing XR...");
            //            var operation = UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.InitializeLoader();

            //            if (UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader == null)
            //            {
            //                WeavrDebug.LogError(this, "Initializing XR Failed. Check Editor or Player log for details.");
            //            }
            //            else
            //            {
            //                Debug.Log("Starting XR...");
            //                UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.StartSubsystems();
            //            }
            //#else
            //            return false;
            //#endif
        }

        private Task DeinitializeXRSystem()
        {
#if WEAVR_PLAYER_XR
            Debug.Log("Stopping XR...");
            UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.StopSubsystems();
            UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.Log("XR stopped completely.");
#endif
            return Task.CompletedTask;
        }

        private void HookToEvents()
        {
            ARObject.Global.TargetChanged -= ARObject_TargetChanged;
            ARObject.Global.TargetChanged += ARObject_TargetChanged;
            View.SetARInteractivity(false);

            View.OnAREnabledChanged += View_OnAREnabledChanged;
            View.OnModeChanged += View_OnModeChanged;
            View.OnPlacementUnlockChanged += View_OnPlacementLockChanged;

            View.OnRelativePositionX += View_OnRelativePositionX;
            View.OnRelativePositionY += View_OnRelativePositionY;
            View.OnRelativePositionZ += View_OnRelativePositionZ;
            View.OnARResetPosition += View_OnARResetPosition;
            View.OnARSavePosition += View_OnARSavePosition;

            View.OnRelativeRotationX += View_OnRelativeRotationX;
            View.OnRelativeRotationY += View_OnRelativeRotationY;
            View.OnRelativeRotationZ += View_OnRelativeRotationZ;
            View.OnARResetRotation += View_OnARResetRotation;
            View.OnARSaveRotation += View_OnARSaveRotation;

            View.OnScaleChange += View_OnScaleChange;
            View.OnSaveScale += View_OnSaveScale;
            View.OnResetScale += View_OnResetScale;

            if (InputPanel != null)
            {
                InputPanel.Zoomed += InputPanel_Zoomed;
                InputPanel.Rotated += InputPanel_Rotated;
                InputPanel.Translated += InputPanel_Translated;
                InputPanel.Clicked += InputPanel_Clicked;
            }

            Positioner.MarkerAquired += Positioner_MarkerAquired;
            Positioner.ObjectSetOnSurface += Positioner_ObjectSetOnSurface;

            if (ARObject.Global.Target)
            {
                ARObject_TargetChanged(ARObject.Global.Target);
            }
        }

        private void UnhookFromEvents()
        {
            if (ARObject.IsGlobalAlive)
            {
                ARObject.Global.TargetChanged -= ARObject_TargetChanged;
            }

            if (View != null)
            {
                View.OnAREnabledChanged -= View_OnAREnabledChanged;
                View.OnModeChanged -= View_OnModeChanged;
                View.OnPlacementUnlockChanged -= View_OnPlacementLockChanged;

                View.OnRelativePositionX -= View_OnRelativePositionX;
                View.OnRelativePositionY -= View_OnRelativePositionY;
                View.OnRelativePositionZ -= View_OnRelativePositionZ;
                View.OnARResetPosition -= View_OnARResetPosition;
                View.OnARSavePosition -= View_OnARSavePosition;

                View.OnRelativeRotationX -= View_OnRelativeRotationX;
                View.OnRelativeRotationY -= View_OnRelativeRotationY;
                View.OnRelativeRotationZ -= View_OnRelativeRotationZ;
                View.OnARResetRotation -= View_OnARResetRotation;
                View.OnARSaveRotation -= View_OnARSaveRotation;

                View.OnScaleChange -= View_OnScaleChange;
                View.OnSaveScale -= View_OnSaveScale;
                View.OnResetScale -= View_OnResetScale;
            }

            if (InputPanel != null)
            {
                InputPanel.Zoomed -= InputPanel_Zoomed;
                InputPanel.Rotated -= InputPanel_Rotated;
                InputPanel.Translated -= InputPanel_Translated;
                InputPanel.Clicked -= InputPanel_Clicked;
            }

            if (Positioner != null)
            {
                Positioner.MarkerAquired -= Positioner_MarkerAquired;
                Positioner.ObjectSetOnSurface -= Positioner_ObjectSetOnSurface;
            }
        }

        private void ARObject_TargetChanged(GameObject target)
        {
            if (target)
            {
                View?.SetARInteractivity(true);
            }
            else
            {
                View?.EnableAR(false);
                View?.SetARInteractivity(false);
            }
        }

        private void Positioner_ObjectSetOnSurface(bool value)
        {
            TargetSet = value;
            View.TargetDetectionChanged(value);
            if (InputPanel != null)
            {
                InputPanel.Active = MovementIsActive;
            }

            PositionOffset = Positioner.PositionOffset;
            RotationOffset = Positioner.RotationOffset;
            Positioner.Scale = Scale;
            View.Scale = Scale;
        }

        private void Positioner_MarkerAquired(bool value)
        {
            TargetSet = value;
            View.TargetDetectionChanged(value);
            if (InputPanel != null)
            {
                InputPanel.Active = MovementIsActive;
            }

            PositionOffset = Positioner.PositionOffset;
            RotationOffset = Positioner.RotationOffset;
            Positioner.Scale = Scale;
        }

        private void InputPanel_Clicked(InputType inputType, Vector2 screenPosition)
        {
            if (View.CurrentMode == ARTargetMode.Surface && !TargetSet)
            {
                Positioner.PositionOnSurface(screenPosition);
            }
        }

        private void InputPanel_Translated(InputType inputType, Vector3 offset, Vector3 actual)
        {
            if (Positioner.TryMoveOnSurface(actual, isDeltaMove: false))
            {
                PositionOffset = Positioner.PositionOffset;
                return;
            }

            if (Positioner.ARCamera)
            {
                var camTransform = Positioner.ARCamera.transform;
                var y = PositionOffset.y;
                PositionOffset += camTransform.right * offset.x + Vector3.ProjectOnPlane(camTransform.forward, Vector3.up) * offset.y;
                PositionOffset = new Vector3(PositionOffset.x, y, PositionOffset.z);
            }
            else
            {
                PositionOffset += offset;
            }
            Positioner.PositionOffset = PositionOffset;
        }

        private void InputPanel_Rotated(InputType inputType, Vector3 offset, Vector3 actual)
        {
            offset.y = -offset.y;
            RotationOffset += Vector3.Scale(offset, Vector3.up);
            Positioner.RotationOffset = RotationOffset;
        }

        private void InputPanel_Zoomed(InputType inputType, float offset, Vector2 zoomCenter)
        {
            Scale += offset;
            Positioner.Scale = Scale;
            View.Scale = Scale;
        }

        private void View_OnPlacementLockChanged(bool isUnlocked)
        {
            MovementIsActive = isUnlocked;
            if (InputPanel != null)
            {
                InputPanel.Active = isUnlocked;
            }
        }

        private void View_OnModeChanged(ARTargetMode mode)
        {
            TargetSet = false;
            Positioner.CurrentARTargetMode = mode;
            if (InputPanel != null)
            {
                InputPanel.Active = mode != ARTargetMode.Marker;
            }
        }

        private void View_OnAREnabledChanged(bool value)
        {
            if (!StandardCamera)
            {
                StandardCamera = WeavrCamera.CurrentCamera && WeavrCamera.CurrentCamera.isActiveAndEnabled ?
                                        WeavrCamera.CurrentCamera : Camera.main;
            }

            TargetSet = false;
            Positioner.Active = value;
            if (value)
            {
                var options = ARObject.Global.Options;
                Positioner.ShowLineToSurface = options.useLineToSurface;
                Positioner.ShowWorldAxes = options.use3DAxes;
                Positioner.LineToSurfaceGradient = options.lineGradient;
                Positioner.ARTarget = ARObject.Global.Target;
            }

            if (StandardCamera)
            {
                if (value)
                {
                    _storedTransform = new StoreTransform()
                    {
                        position = StandardCamera.transform.localPosition,
                        rotation = StandardCamera.transform.localRotation,
                        localScale = StandardCamera.transform.localScale,
                    };
                }
                else
                {
                    if (_storedTransform != null)
                    {
                        StandardCamera.transform.localPosition = _storedTransform.position;
                        StandardCamera.transform.localRotation = _storedTransform.rotation;
                        StandardCamera.transform.localScale = _storedTransform.localScale;
                    }
                    _storedTransform = null;
                }

                StandardCamera.gameObject.SetActive(!value);
            }

            if (value)
            {
                Positioner.CurrentARTargetMode = View.CurrentMode;
                Positioner.ResetPosition();
                Positioner.ResetRotation();
                Positioner.ResetScale();
                View.Scale = Positioner.Scale;
            }

            if (value && View.CurrentMode != ARTargetMode.Marker)
            {

            }
            else
            {
                MovementIsActive = false;
            }
            if (InputPanel != null)
            {
                InputPanel.Active = !value;
            }

            OnAREnabled?.Invoke(value);
        }

        private void View_OnResetScale()
        {
            Positioner.ResetScale();
            Scale = Positioner.Scale;
            View.Scale = Positioner.Scale;
        }

        private void View_OnSaveScale()
        {
            Positioner.SaveScale();
        }

        private void View_OnScaleChange(float value)
        {
            Scale = value;
            Positioner.Scale = Scale;
        }

        private void View_OnRelativeRotationZ(float value)
        {
            RotationOffset = new Vector3(RotationOffset.x, RotationOffset.y, RotationOffset.z + value);
            Positioner.RotationOffset = RotationOffset;
        }

        private void View_OnRelativeRotationY(float value)
        {
            RotationOffset = new Vector3(RotationOffset.x, RotationOffset.y + value, RotationOffset.z);
            Positioner.RotationOffset = RotationOffset;
        }

        private void View_OnRelativeRotationX(float value)
        {
            RotationOffset = new Vector3(RotationOffset.x + value, RotationOffset.y, RotationOffset.z);
            Positioner.RotationOffset = RotationOffset;
        }

        private void View_OnRelativePositionZ(float value)
        {
            PositionOffset = new Vector3(PositionOffset.x, PositionOffset.y, PositionOffset.z + value);
            Positioner.PositionOffset = PositionOffset;
        }

        private void View_OnRelativePositionY(float value)
        {
            PositionOffset = new Vector3(PositionOffset.x, PositionOffset.y + value, PositionOffset.z);
            Positioner.PositionOffset = PositionOffset;
        }

        private void View_OnRelativePositionX(float value)
        {
            PositionOffset = new Vector3(PositionOffset.x + value, PositionOffset.y, PositionOffset.z);
            Positioner.PositionOffset = PositionOffset;
        }

        private void View_OnARSaveRotation()
        {
            Positioner.SaveRotation();
        }

        private void View_OnARResetRotation()
        {
            Positioner.ResetRotation();
            RotationOffset = Positioner.RotationOffset;
        }

        private void View_OnARSavePosition()
        {
            Positioner.SavePosition();
        }

        private void View_OnARResetPosition()
        {
            Positioner.ResetPosition();
            PositionOffset = Positioner.PositionOffset;
        }


        class StoreTransform
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 localScale;
        }
    }
}
