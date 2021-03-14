#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
using TXT.WEAVR;
using UnityEngine;

/// <summary>
/// Attacch this script to CANVAS object.
/// This script nees WeavrARUIManager set in the parent object.
/// </summary>
public class WeavrARUICanvas : MonoBehaviour
{

    [Tooltip("InputManager prefabs")]
    public FocusManager focusManager;

    private WeavrARUIManager controller = null;

    private void Start()
    {
        // add event to gazeManager
        if (focusManager != null)
        {
            focusManager.PointerSpecificFocusChanged += FocusManager_PointerSpecificFocusChanged;
        }
        controller = GetComponentInParent<WeavrARUIManager>();
    }

    private void FocusManager_PointerSpecificFocusChanged(IPointingSource pointer, GameObject oldFocusedObject, GameObject newFocusedObject)
    {
        if (oldFocusedObject != null)
        {
            oldFocusedObject.SendMessage("OnGazeExit", SendMessageOptions.DontRequireReceiver);
            OnGazeExit();
        }

        if (newFocusedObject != null && newFocusedObject.transform.root == gameObject.transform.root)
        {
            newFocusedObject.SendMessage("OnGazeEnter", SendMessageOptions.DontRequireReceiver);
            OnGazeEnter();
        }
    }

    void Update()
    {

    }

    /// <summary>
    /// When Gaze enter in collision with the current game object
    /// </summary>
    void OnGazeEnter()
    {
        controller.followCamera = false;
    }

    /// <summary>
    /// When the Gaze exit collision with the current game object
    /// </summary>
    void OnGazeExit()
    {
        // recenter the canvas
        controller.followCamera = true;
    }
}


#endif
