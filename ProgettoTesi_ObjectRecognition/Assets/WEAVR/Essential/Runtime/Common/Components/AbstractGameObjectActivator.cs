using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    public abstract class AbstractGameObjectActivator : MonoBehaviour
    {
        public abstract GameObject[] GameObjects { get; }

        public virtual bool ActiveAll
        {
            get => GameObjects.All(g => g.activeSelf);
            set
            {
                foreach(var o in GameObjects)
                {
                    o.SetActive(value);
                }
            }
        }

        public virtual GameObject OnlyActive
        {
            get => GameObjects.SingleOrDefault(g => g.activeSelf);
            set
            {
                if(value && GameObjects.Contains(value))
                {
                    foreach (var o in GameObjects)
                    {
                        o.SetActive(value == o);
                    }
                }
            }
        }

        public virtual int OnlyActiveIndex
        {
            get
            {
                int index = -1;
                for (int i = 0; i < GameObjects.Length; i++)
                {
                    if (GameObjects[i].activeSelf)
                    {
                        if(index > -1)
                        {
                            return -1;
                        }
                        index = i;
                    }
                }
                return -1;
            }
            set
            {
                if(0 <= value && value < GameObjects.Length)
                {
                    for (int i = 0; i < GameObjects.Length; i++)
                    {
                        GameObjects[i].SetActive(i == value);
                    }
                }
            }
        }

        private int m_defaultStartIndex = 0;
        public int DefaultStartIndex
        {
            get => m_defaultStartIndex;
            set
            {
                m_defaultStartIndex = Mathf.Clamp(value, 0,  DefaultEndIndex);
            }
        }

        private int m_defaultEndIndex = -1;
        public int DefaultEndIndex
        {
            get
            {
                if(m_defaultEndIndex < 0)
                {
                    m_defaultEndIndex = GameObjects.Length - 1;
                }
                return m_defaultEndIndex;
            }
            set
            {
                m_defaultEndIndex = Mathf.Clamp(value, m_defaultStartIndex, GameObjects.Length - 1);
            }
        }

        public virtual void ActivateNext() => ActivateNext(DefaultStartIndex, DefaultEndIndex);

        public virtual void ActivateNext(int startIndex) => ActivateNext(startIndex, DefaultEndIndex);

        public virtual void ActivateNext(int startIndex, int endIndex) => Activate(1, startIndex, endIndex);

        public virtual void ActivatePrevious() => ActivatePrevious(DefaultStartIndex, DefaultEndIndex);

        public virtual void ActivatePrevious(int startIndex) => ActivatePrevious(startIndex, DefaultEndIndex);

        public virtual void ActivatePrevious(int startIndex, int endIndex) => Activate(-1, startIndex, endIndex);

        public virtual void Activate(int increment, int startIndex, int endIndex)
        {
            int nextIndex = OnlyActiveIndex + increment;
            if(nextIndex > endIndex)
            {
                nextIndex = startIndex;
            }
            else if(nextIndex < startIndex)
            {
                nextIndex = endIndex;
            }
            OnlyActiveIndex = nextIndex;
        }
    }
}
