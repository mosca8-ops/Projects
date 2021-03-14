using System;
using TXT.WEAVR;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.UI;
using UnityEngine;
using UnityEngine.UI;
#if WEAVR_VUFORIA
using Vuforia;
#endif

[Obsolete]
[AddComponentMenu("")]
public class ButtonsManager : MonoBehaviour
{

    public enum ObjectMovement { AlwaysDisabled, AlwaysEnabled, WithButton }

    [SerializeField]
    [Draggable]
    private Camera _camera;
    private CameraOrbit _cameraOrbit;

    [SerializeField]
    [Draggable]
    private GameObject _webRawImage;
    private WebCamTexture m_Camera = null;

    [SerializeField]
    [HiddenBy(nameof(_disableStepButtonInAr))]
    private GameObject _nextStepButton;
    [SerializeField]
    [HiddenBy(nameof(_disableStepButtonInAr))]
    private GameObject _prevStepButton;

    [Header("Object Movement Button")]
    [SerializeField]
    private ObjectMovement m_objectMovement = ObjectMovement.AlwaysDisabled;
    private bool _showMovementButton
    {
        get { return m_objectMovement == ObjectMovement.WithButton; }
    }
    [SerializeField]
    //[HiddenBy(nameof(_showMovementButton))]
    [ShowOnEnum(nameof(m_objectMovement), (int)ObjectMovement.WithButton)]
    private GameObject _lockButton;
    [SerializeField]
    //[HiddenBy(nameof(_showMovementButton))]
    [ShowOnEnum(nameof(m_objectMovement), (int)ObjectMovement.WithButton)]
    private GameObject _unlockButton;

    [Header("AR Button")]
    [SerializeField]
    private GameObject _arButton;
    [SerializeField]
    private GameObject _noArButton;
    [SerializeField]
    private GameObject _markerButton;
    [SerializeField]
    private bool _disableStepButtonInAr;

    //private Vector3 _startPosition, _startRotation, _startScale;

    private Vector3 _cameraPosition, _cameraRotation, _cameraScale;
    private float _cameraFieldOfView;
    private Vector3 _objectPosition, _objectRotation, _objectScale;
    private bool _isPrevStateAR = false;


    // Use this for initialization
    void Start()
    {

        //SetStartValues();

        if (m_objectMovement == ObjectMovement.AlwaysEnabled)
        {
            _cameraOrbit.enabled = true;

            _lockButton.SetActive(false);
            _unlockButton.SetActive(false);
        }
        else if (m_objectMovement == ObjectMovement.AlwaysDisabled)
        {
            _cameraOrbit.enabled = false;

            _lockButton.SetActive(false);
            _unlockButton.SetActive(false);
        }
        else if (m_objectMovement == ObjectMovement.WithButton)
        {
            _cameraOrbit.enabled = false;

            _lockButton.SetActive(true);
            _unlockButton.SetActive(false);
        }
    }

    //private void SetStartValues()
    //{
    //    if (_isInteractable != null)
    //    {
    //        _startPosition = _isInteractable.transform.position;
    //        _startRotation = _isInteractable.transform.eulerAngles;
    //        _startScale = _isInteractable.transform.localScale;
    //    }
    //}

    void Awake()
    {
        _cameraOrbit = _camera.GetComponent<CameraOrbit>();

        //ProcedureEngine.Instance.navigationManager.NavigationChanged -= NavigationManager_NavigationChanged;
        //ProcedureEngine.Instance.navigationManager.NavigationChanged += NavigationManager_NavigationChanged;
    }

    //private void NavigationManager_NavigationChanged(object sender, NavigationChangedArgs args)
    //{

    //    // When the step is changed 
    //    if (args.hasMoved)
    //    {
    //        // Actual Step is AR
    //        if (args.currentStep != null && args.currentStep.IsAr)
    //        {
    //            if (args.previousStep != null && args.previousStep.IsAr)
    //            {

    //            }
    //            else
    //            {

    //            }
    //            _arButton.SetActive(true);
    //            _noArButton.SetActive(false);
    //        }
    //        else
    //        {
    //            if (args.previousStep != null && args.previousStep.IsAr)
    //            {
    //                TransparencyController.Instance.TransparencyEnabled = false;
    //            }
    //            _arButton.SetActive(false);
    //            _noArButton.SetActive(false);
    //        }
    //    }
    //}


#if WEAVR_VUFORIA
    public void OnOffVuforiaCam(bool bval)
    {
        if (VuforiaBehaviour.Instance != null)
        {
            VuforiaBehaviour.Instance.enabled = bval;
        }
    }
#else
    public void OnOffCam(bool bval)
    {
        if (m_Camera == null)
        {
            if (WebCamTexture.devices.Length > 0)
            {
                m_Camera = new WebCamTexture();
                _webRawImage.GetComponent<RawImage>().texture = m_Camera;
            }
        }

        if (bval)
        {
            _webRawImage.SetActive(true);
            m_Camera?.Play();
        }
        else
        {
            _webRawImage.SetActive(false);
            m_Camera?.Stop();
        }
    }
#endif

    public void LockUnlockSwitch()
    {
        if (m_objectMovement == ObjectMovement.AlwaysEnabled
            || m_objectMovement == ObjectMovement.AlwaysDisabled)
        {
            return;
        }

        if (_lockButton.activeInHierarchy)
        {
            _cameraOrbit.enabled = true;

            _unlockButton.SetActive(true);
            _lockButton.SetActive(false);

            if (_markerButton != null)
            {
                _markerButton.SetActive(false);
            }
        }
        else
        {
            _cameraOrbit.enabled = false;

            _lockButton.SetActive(true);
            _unlockButton.SetActive(false);

            if (_markerButton != null)
            {
                _markerButton.SetActive(false);
            }
        }
    }

    public void ARGuiSwitch()
    {
        // At the moment of click the AR is enabled, 
        // We want to disable it, we need to put all back to normal and switch off videocamera
        if (TransparencyController.Instance.TransparencyEnabled)
        {
            BillboardManager.Instance.enabled = true;

            // Put back AR button
            _arButton.SetActive(true);
            _noArButton.SetActive(false);

            // Put back Lock/Unlock button
            if (m_objectMovement == ObjectMovement.AlwaysEnabled)
            {
                _lockButton.SetActive(false);
                _cameraOrbit.enabled = true;
                _unlockButton.SetActive(false);
            }
            else if (m_objectMovement == ObjectMovement.AlwaysDisabled)
            {
                _lockButton.SetActive(false);
                _cameraOrbit.enabled = false;
                _unlockButton.SetActive(false);
            }
            else if (m_objectMovement == ObjectMovement.WithButton)
            {
                _lockButton.SetActive(true);
                _cameraOrbit.enabled = true;
                _unlockButton.SetActive(false);
            }

            if (_markerButton != null)
            {
                _markerButton.SetActive(true);
            }

            if (_disableStepButtonInAr)
            {
                _prevStepButton.SetActive(true);
                _nextStepButton.SetActive(true);
            }

            // Set last transform of camera and object
            //if (_camera != null && _isPrevStateAR)
            //{
            //    //_isInteractable.gameObject.transform.position = _objectPosition;
            //    //_isInteractable.gameObject.transform.eulerAngles = _objectRotation;
            //    //_isInteractable.gameObject.transform.localScale = _objectScale;
            //    _camera.transform.localPosition = _cameraPosition;
            //    _camera.transform.localEulerAngles = _cameraRotation;
            //    _camera.transform.localScale = _cameraScale;
            //    _camera.fieldOfView = _cameraFieldOfView;

            //}

#if WEAVR_VUFORIA
            OnOffVuforiaCam(false);
#else
            OnOffCam(false);
#endif

            _isPrevStateAR = false;
        }
        // At the moment of click the AR is not enabled, we need to save all
        // We want to enable it, we need to save all and switch on videocamera
        else
        {
            // Save last transform of camera and object
            //if (_camera != null && !_isPrevStateAR)
            //{
            //    _cameraPosition = _camera.transform.localPosition;
            //    _cameraRotation = _camera.transform.localEulerAngles;
            //    _cameraScale = _camera.transform.localScale;
            //    _cameraFieldOfView = _camera.fieldOfView;
            //    //_objectPosition = _isInteractable.gameObject.transform.position;
            //    //_objectRotation = _isInteractable.gameObject.transform.eulerAngles;
            //    //_objectScale = _isInteractable.gameObject.transform.localScale;
            //}

            BillboardManager.Instance.enabled = false;


            _arButton.SetActive(false);
            _noArButton.SetActive(true);

#if WEAVR_VUFORIA
            _lockButton.SetActive(false);
            _unlockButton.SetActive(false);
#else
            if (m_objectMovement == ObjectMovement.AlwaysEnabled)
            {
                _lockButton.SetActive(false);
                _cameraOrbit.enabled = true;
                _unlockButton.SetActive(false);
            }
            else if (m_objectMovement == ObjectMovement.AlwaysDisabled)
            {
                _lockButton.SetActive(false);
                _cameraOrbit.enabled = false;
                _unlockButton.SetActive(false);
            }
            else if (m_objectMovement == ObjectMovement.WithButton)
            {
                _lockButton.SetActive(true);
                _cameraOrbit.enabled = true;
                _unlockButton.SetActive(false);
            }
#endif

            if (_markerButton != null)
            {
                _markerButton.SetActive(false);
            }

            if (_disableStepButtonInAr)
            {
                _prevStepButton.SetActive(true);
                _nextStepButton.SetActive(true);
            }

#if WEAVR_VUFORIA
            OnOffVuforiaCam(true);
#else
            OnOffCam(true);
#endif

            _isPrevStateAR = true;
        }

        TransparencyController.Instance.TransparencyEnabled = !TransparencyController.Instance.TransparencyEnabled;
    }

    //public void ResetObjTransform()
    //{
    //    if (_isInteractable != null)
    //    {
    //        _isInteractable.transform.position = _startPosition;
    //        _isInteractable.transform.eulerAngles = _startRotation;
    //        _isInteractable.transform.localScale = _startScale;
    //    }
    //}

    //public void PrintFov()
    //{
    //    GameObject.Find("FovText").GetComponent<Text>().text = "cam fov is: " + _camera.GetComponent<Camera>().fieldOfView;
    //    Debug.Log(_camera.GetComponent<Camera>().fieldOfView);
    //    var fov = CameraDevice.Instance.GetCameraFieldOfViewRads();
    //    Debug.Log(fov.x + "   " + fov.y);
    //}
}
