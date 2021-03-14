using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;

namespace TXT.WEAVR.UI
{

    public class RatioCounter : MonoBehaviour
    {
        public Toggle firstField;
        public Toggle secondField;
        public LongPressButton incrementButton;
        public LongPressButton decrementButton;
        public int minCount = 1;
        public int maxCount = 1000;

        private float m_scale;

        private Counter m_firstCounter;
        private Counter m_secondCounter;
        private Counter m_activeCounter;

        public float Scale { get { return m_scale; } set { RatioCalculation(value); } }

        public int ActiveCount
        {
            get => m_activeCounter.Count;
            set
            {
                if (m_activeCounter.Count != value)
                {
                    m_activeCounter.Count = Mathf.Clamp(value, minCount, maxCount);
                    ScaleCalculation();
                }
            }
        }

        private void SetActiveCounter(Counter value)
        {
            if (m_activeCounter != value)
            {
                DisableCounterButtons();
                m_activeCounter = value;
                if (m_activeCounter != null)
                {
                    EnableCounterButtons();
                }
            }
        }

        public event Action<float> onScaleChange;

        private Counter FirstCounter
        {
            get
            {
                if(m_firstCounter == null)
                {
                    m_firstCounter = new Counter()
                    {
                        Count = 1,
                        field = firstField,
                        text = firstField.GetComponentInChildren<TextMeshProUGUI>(),
                    };
                    firstField.onValueChanged.AddListener(v => SetCounter(v, m_firstCounter));
                }
                return m_firstCounter;
            }
        }

        private Counter SecondCounter
        {
            get
            {
                if(m_secondCounter == null)
                {
                    m_secondCounter = new Counter()
                    {
                        Count = 1,
                        field = secondField,
                        text = secondField.GetComponentInChildren<TextMeshProUGUI>(),
                    };
                    secondField.onValueChanged.AddListener(v => SetCounter(v, m_secondCounter));
                }
                return m_secondCounter;
            }
        }

        private void SetCounter(bool isOn, Counter counter)
        {
            if (isOn) { SetActiveCounter(counter); }
        }

        private void EnableCounterButtons()
        {
            incrementButton.Button.interactable = true;
            incrementButton.Button.onClick.AddListener(Increment);
            incrementButton.OnPress.AddListener(IncrementLongPress);
            decrementButton.Button.interactable = true;
            decrementButton.OnPress.AddListener(DecrementLongPress);
            decrementButton.Button.onClick.AddListener(Decrement);
        }

        private void DisableCounterButtons()
        {
            incrementButton.Button.onClick.RemoveListener(Increment);
            incrementButton.OnPress.RemoveListener(IncrementLongPress);
            incrementButton.Button.interactable = false;
            decrementButton.Button.onClick.RemoveListener(Decrement);
            decrementButton.OnPress.RemoveListener(DecrementLongPress);
            decrementButton.Button.interactable = false;
        }

        private void IncrementLongPress(float pressedDuration)
        {
            int increment = GetIncrementByTime(pressedDuration);
            ActiveCount += increment;
        }
        
        private void Increment()
        {
            ActiveCount++;
        }

        private void DecrementLongPress(float pressedDuration)
        {
            int increment = GetIncrementByTime(pressedDuration);
            ActiveCount -= increment;
        }

        private void Decrement()
        {
            ActiveCount--;
        }

        private int GetIncrementByTime(float time)
        {
            if(time < 1) { return 0; }
            if(time < 2) { return 1; }
            if(time < 3) { return 2; }
            if(time < 4) { return 5; }
            if(time < 5) { return 10; }
            if(time < 6) { return 20; }
            if(time < 8) { return 50; }
            if(time < 10) { return 100; }

            return 200;
        }

        private void ScaleCalculation()
        {
            m_scale = FirstCounter.Count / (float)SecondCounter.Count;
            onScaleChange?.Invoke(Scale);
        }

        public void RatioCalculation(float newScale)
        {
            float numerator = m_scale = newScale;
            float denominator = 1;

            while ((numerator - (int)numerator) >= Mathf.Epsilon
                        && numerator < maxCount
                        && denominator < maxCount)
            {
                denominator *= 10;
                numerator *= 10;
            }

            numerator = Mathf.Min(numerator, maxCount);
            denominator = Mathf.Min(denominator, maxCount);

            int gcd = GCD_Fast((int)numerator, (int)denominator);
            numerator /= gcd;
            denominator /= gcd;

            FirstCounter.Count = (int)numerator;
            SecondCounter.Count = (int)denominator;
        }

        private static int GCD_Fast(int a, int b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b) { a %= b; }
                else { b %= a; }
            }

            return a | b;
        }

        private class Counter
        {
            private int m_count;
            public int Count
            {
                get => m_count;
                set
                {
                    m_count = value;
                    if (text) { text.text = m_count.ToString(); }
                }
            }
            public Toggle field;
            public TextMeshProUGUI text;
        }

    }
}
