using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{
    public struct LowPassFilter
    {
        private float[] m_values;
        private int m_nextIndex;

        public LowPassFilter(int capacity)
        {
            m_nextIndex = 0;
            m_values = new float[capacity];
            if(capacity <= 0)
            {
                throw new ArgumentException("Capacity should be non negative");
            }
        }

        public void Clear()
        {
            for (int i = 0; i < m_values.Length; i++)
            {
                m_values[i] = 0;
            }
            m_nextIndex = 0;
        }

        public float Filter(float value)
        {
            m_values[m_nextIndex % m_values.Length] = value;
            m_nextIndex++;
            return m_nextIndex < m_values.Length ? value : GetAverage();
        }

        public float GetAverage()
        {
            float avg = m_values[0];
            for (int i = 1; i < m_values.Length; i++)
            {
                avg += m_values[i];
            }
            return avg / m_values.Length;
        }
    }


    public struct LowPassVector2Filter
    {
        private Vector2[] m_values;
        private int m_nextIndex;

        public LowPassVector2Filter(int capacity)
        {
            m_nextIndex = 0;
            m_values = new Vector2[capacity];
            if (capacity <= 0)
            {
                throw new ArgumentException("Capacity should be non negative");
            }
        }

        public void Clear()
        {
            for (int i = 0; i < m_values.Length; i++)
            {
                m_values[i] = Vector2.zero;
            }
            m_nextIndex = 0;
        }

        public Vector2 Filter(Vector2 value)
        {
            m_values[m_nextIndex % m_values.Length] = value;
            m_nextIndex++;
            return m_nextIndex < m_values.Length ? value : GetAverage();
        }

        public Vector2 GetAverage()
        {
            Vector2 avg = m_values[0];
            for (int i = 1; i < m_values.Length; i++)
            {
                avg += m_values[i];
            }
            return avg / m_values.Length;
        }
    }

    public struct LowPassVector3Filter
    {
        private Vector3[] m_values;
        private int m_nextIndex;

        public LowPassVector3Filter(int capacity)
        {
            m_nextIndex = 0;
            m_values = new Vector3[capacity];
            if (capacity <= 0)
            {
                throw new ArgumentException("Capacity should be non negative");
            }
        }

        public void Clear()
        {
            for (int i = 0; i < m_values.Length; i++)
            {
                m_values[i] = Vector3.zero;
            }
            m_nextIndex = 0;
        }

        public Vector3 Filter(Vector3 value)
        {
            m_values[m_nextIndex % m_values.Length] = value;
            m_nextIndex++;
            return m_nextIndex < m_values.Length ? value : GetAverage();
        }

        public Vector3 GetAverage()
        {
            Vector3 avg = m_values[0];
            for (int i = 1; i < m_values.Length; i++)
            {
                avg += m_values[i];
            }
            return avg / m_values.Length;
        }
    }
}
