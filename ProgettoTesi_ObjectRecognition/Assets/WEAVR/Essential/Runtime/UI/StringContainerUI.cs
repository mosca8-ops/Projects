using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{

    [AddComponentMenu("WEAVR/UI/String Container")]
    public class StringContainerUI : MonoBehaviour
    {
        [Draggable]
        public GameObject itemSample;
        [SerializeField]
        private OptionalInt m_capacity;

        private List<GameObject> m_list = new List<GameObject>();

        public int Count => m_list.Count;
        public bool IsEmpty => m_list.Count == 0;

        public void Clear()
        {
            foreach(var item in m_list)
            {
                Destroy(item);
            }
            m_list.Clear();
        }

        public void AddString(string value)
        {
            if(m_capacity.enabled && m_list.Count >= m_capacity)
            {
                RemoveAt(0);
            }

            var clone = Instantiate(itemSample);
            clone.SetActive(true);
            clone.GetComponentInChildren<Text>().text = value;
            m_list.Add(clone);
            clone.transform.SetParent(transform, false);

        }

        public void RemoveString(string value)
        {
            for (int i = 0; i < m_list.Count; i++)
            {
                if(m_list[i].GetComponentInChildren<Text>().text == value)
                {
                    RemoveAt(i);
                    return;
                }
            }
        }

        public void RemoveAt(int index)
        {
            if (0 <= index && index < m_list.Count)
            {
                Destroy(m_list[index]);
                m_list.RemoveAt(index);
            }
        }

        public void UpdateString(int index, string value)
        {
            if(0 <= index && index < m_list.Count)
            {
                m_list[index].GetComponentInChildren<Text>().text = value;
            }
        }
    }
}