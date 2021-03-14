#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
using UnityEngine;
using UnityEngine.XR.WSA.Input;

public class WeavrARUIScroll : MonoBehaviour
{
    GestureRecognizer navigationGestureRecognizer;

    // Use this for initialization
    void Start()
    {
        navigationGestureRecognizer = new GestureRecognizer();

        //navigationGestureRecognizer.NavigationStartedEvent += ManipulationRecognizer_NavigationStartedEvent;
        //navigationGestureRecognizer.NavigationUpdatedEvent += ManipulationRecognizer_NavigationUpdatedEvent;
        //navigationGestureRecognizer.NavigationCompletedEvent += ManipulationRecognizer_NavigationCompletedEvent;
        //navigationGestureRecognizer.NavigationCanceledEvent += ManipulationRecognizer_NavigationCanceledvent;


    }


    // Update is called once per frame
    void Update()
    {

    }
}
#endif
