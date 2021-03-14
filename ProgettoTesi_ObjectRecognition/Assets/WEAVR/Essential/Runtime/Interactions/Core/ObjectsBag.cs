namespace TXT.WEAVR.Interaction
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class ObjectsBag : IDictionary<string, GameObject>
    {
        public delegate void OnSeletedChanged(GameObject previous, GameObject current);
        public delegate void OnActiveHandChanged(Hand previous, Hand current);

        private Dictionary<string, GameObject> m_bag;

        public GameObject Selected {
            get {
                return ActiveHand?.Selected;
            }
            set {
                if(ActiveHand != null && ActiveHand.Selected != value) {
                    ActiveHand.Selected = value;
                }
            }
        }

        public event OnActiveHandChanged ActiveHandChanged;

        private Hand[] m_hands;
        private Dictionary<object, Hand> m_handsReferences;
        private Hand m_activeHand;
        private Hand m_prevActiveHand;

        private bool m_isOneDefaultHand;

        public Hand ActiveHand {
            get { return m_activeHand; }
            private set {
                if(m_activeHand != value)
                {
                    m_prevActiveHand = m_activeHand;
                    m_activeHand = value;
                    ActiveHandChanged?.Invoke(m_prevActiveHand, m_activeHand);
                }
            }
        }

        public ObjectsBag() {
            m_bag = new Dictionary<string, GameObject>();
            m_handsReferences = new Dictionary<object, Hand>();

            m_hands = new Hand[0];
            RegisterHand(this);
            m_isOneDefaultHand = true;
            m_activeHand = m_hands[0];
        }

        public GameObject GetSelected(object reference)
        {
            return GetHand(reference)?.Selected;
        }

        public Hand GetOwningHand(GameObject gameObject)
        {
            for (int i = 0; i < m_hands.Length; i++)
            {
                if (m_hands[i].Selected == gameObject)
                {
                    return m_hands[i];
                }
            }
            return null;
        }

        public Hand GetHand(object reference)
        {
            Hand hand = null;
            m_handsReferences.TryGetValue(reference, out hand);
            return hand;
        }

        public Hand GetHand(int index)
        {
            return index >= 0 && index < m_hands.Length ? m_hands[index] : null;
        }

        public void RestoreActiveHand(object reference)
        {
            if (!m_isOneDefaultHand && reference != null && m_handsReferences.TryGetValue(reference, out Hand hand))
            {
                ActiveHand = m_prevActiveHand;
            }
        }

        public void SetActiveHand(object reference)
        {
            if (!m_isOneDefaultHand && reference != null && m_handsReferences.TryGetValue(reference, out Hand hand))
            {
                ActiveHand = hand;
            }
        }

        public bool IsInAnyHand(GameObject gameObject)
        {
            for (int i = 0; i < m_hands.Length; i++)
            {
                if(m_hands[i].Selected == gameObject)
                {
                    return true;
                }
            }
            return false;
        }

        public Hand RegisterHand(object reference)
        {
            Hand hand = null;
            if (m_isOneDefaultHand)
            {
                m_hands = new Hand[0];
                m_isOneDefaultHand = false;
            }
            if(!m_handsReferences.TryGetValue(reference, out hand))
            {
                hand = new Hand(this);
                List<Hand> hands = new List<Hand>(m_hands);
                hands.Add(hand);
                m_hands = hands.ToArray();
                m_handsReferences[reference] = hand;
            }
            return hand;
        }

        public class Hand
        {
            private GameObject m_selected;
            private ObjectsBag m_bag;

            public event OnSeletedChanged SelectedChanged;

            public GameObject Selected {
                get {
                    return m_selected;
                }
                set {
                    if (m_selected != value)
                    {
                        var previous = m_selected;
                        m_selected = value;
                        SelectedChanged?.Invoke(previous, value);
                    }
                }
            }

            internal Hand(ObjectsBag bag)
            {
                m_bag = bag;
            }

            public void MakeActive(bool enable)
            {
                if (enable)
                {
                    m_bag.ActiveHand = this;
                }
                else if(m_bag.ActiveHand == this)
                {
                    m_bag.ActiveHand = null;
                }
            }
        }

        #region [  IDICTIONARY IMPLEMENTATION  ]

        public GameObject this[string key] { get { return m_bag[key]; } set { m_bag[key] = value; } }

        public ICollection<string> Keys { get { return m_bag.Keys; } }

        public ICollection<GameObject> Values { get { return m_bag.Values; } }

        public int Count { get { return m_bag.Count; } }

        public bool IsReadOnly { get { return false; } }

        public void Add(string key, GameObject value) {
            m_bag.Add(key, value);
        }

        public void Add(KeyValuePair<string, GameObject> item) {
            m_bag.Add(item.Key, item.Value);
        }

        public void Clear() {
            m_bag.Clear();
            Selected = null;
        }

        public bool Contains(KeyValuePair<string, GameObject> item) {
            return m_bag.ContainsKey(item.Key);
        }

        public bool ContainsKey(string key) {
            return m_bag.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, GameObject>[] array, int arrayIndex) {
            // Not implemented
        }

        public IEnumerator<KeyValuePair<string, GameObject>> GetEnumerator() {
            return m_bag.GetEnumerator();
        }

        public bool Remove(string key) {
            GameObject toRemove = null;
            if(m_bag.TryGetValue(key, out toRemove) && toRemove == Selected) {
                Selected = null;
            }
            return m_bag.Remove(key);
        }

        public bool Remove(KeyValuePair<string, GameObject> item) {
            return Remove(item.Key);
        }

        public bool TryGetValue(string key, out GameObject value) {
            return m_bag.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return m_bag.GetEnumerator();
        }

        #endregion
    }
}
