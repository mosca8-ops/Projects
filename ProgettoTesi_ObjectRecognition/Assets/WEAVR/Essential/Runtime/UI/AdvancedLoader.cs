using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("")]
public class AdvancedLoader : MonoBehaviour
{
    [Draggable]
    public Text Title;
    [Draggable]
    public Text ProgressPercentage;
    private AsyncOperation m_asyncOperation;

    public void SetLoader(string title, float progressPercentage, AsyncOperation asyncOperation)
    {
        if (Title.text != null)
        {
            Title.text = title;
        }
        m_asyncOperation = asyncOperation;
        if (ProgressPercentage != null)
        {
            if (progressPercentage > 0)
            {
                ProgressPercentage.text = Mathf.FloorToInt(progressPercentage*100).ToString() + "%";
            }
            else
            {
                ProgressPercentage.text = "";
            }
        }
    }

    public void UpdateProgressPercentage(float progressPercentage)
    {
        if (progressPercentage > 0)
        {
            ProgressPercentage.text = Mathf.FloorToInt(progressPercentage * 100).ToString() + "%";
        }
        else
        {
            ProgressPercentage.text = "";
        }
    }

    void Update()
    {
        if (m_asyncOperation != null)
        {
            if (m_asyncOperation.isDone)
            {
                m_asyncOperation = null;
            }
            else
            {
                UpdateProgressPercentage(m_asyncOperation.progress);
            }
        }
    }
}
