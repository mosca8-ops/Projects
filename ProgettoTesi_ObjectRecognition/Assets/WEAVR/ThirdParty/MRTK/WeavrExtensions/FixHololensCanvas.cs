#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
using UnityEngine;

[RequireComponent(typeof(Tagalong))]
public class FixHololensCanvas : MonoBehaviour
{
    private Tagalong TagalongComponent;
    private Billboard BillboardComponent;

    // Use this for initialization
    void Start()
    {
        TagalongComponent = gameObject.GetComponent<Tagalong>();
        BillboardComponent = gameObject.GetComponent<Billboard>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ToggleFixHoloensCanvas()
    {
        var enabled = !TagalongComponent.enabled;
        if (TagalongComponent != null)
        {
            TagalongComponent.enabled = enabled;
        }

        if (BillboardComponent != null)
        {
            BillboardComponent.enabled = enabled;
        }
    }
}
#endif
