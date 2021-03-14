#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
using Microsoft.MixedReality.Toolkit.Experimental.Utilities;
using UnityEngine;

public class WorldAnchorObject : MonoBehaviour
{
    public WorldAnchorManager WorldAnchorManager;

    // Use this for initialization
    void Start()
    {
        WorldAnchorManager.AttachAnchor(gameObject);
    }

    public void AttachAnchor()
    {
        WorldAnchorManager.AttachAnchor(gameObject);
    }

    public void RemoveAnchor()
    {
        WorldAnchorManager.RemoveAnchor(gameObject);
    }
}
#endif
