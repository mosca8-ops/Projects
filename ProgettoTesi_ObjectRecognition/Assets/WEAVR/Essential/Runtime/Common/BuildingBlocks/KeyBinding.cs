using System;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [Serializable]
    public class KeyBinding
    {
        public enum State { NotPressed, Enter, Down, Up }

        [SerializeField]
        private KeyCode m_code;

        private State m_state;

        public KeyCode KeyCode {
            get { return m_code; }
            set {
                if (m_code != value)
                {
                    m_code = value;
                }
            }
        }

        public State KeyState {
            get { return m_state; }
            set {
                if (m_state != value)
                {
                    m_state = value;
                    StateChanged?.Invoke(m_code, m_state);
                    if (value == State.Up) { KeyUpEvent?.Invoke(); }
                    else if (value == State.Enter) { KeyEnterEvent?.Invoke(); }
                }
            }
        }

        public event Action<KeyCode, State> StateChanged;

        private event Action KeyUpEvent;
        private event Action KeyDownEvent;
        private event Action KeyEnterEvent;

        public KeyBinding(KeyCode code)
        {
            m_code = code;
            m_state = State.NotPressed;
            StateChanged = null;

            KeyUpEvent = null;
            KeyDownEvent = null;
            KeyEnterEvent = null;
        }

        public bool Update()
        {
            if (Input.GetKeyUp(m_code))
            {
                KeyState = State.Up;
            }
            else if (Input.GetKeyDown(m_code))
            {
                KeyState = KeyState == State.NotPressed ? State.Enter : State.Down;
            }
            //else if(KeyState == State.Down || KeyState == State.Enter)
            //{
            //    KeyState = State.Up;
            //}
            else if (!Input.GetKey(m_code))
            {
                KeyState = State.NotPressed;
            }

            if (KeyState == State.Down) { KeyDownEvent?.Invoke(); }
            return KeyState == State.Down || KeyState == State.Enter;
        }

        public bool KeyDown()
        {
            return KeyState == State.Down || KeyState == State.Enter;
        }

        public bool KeyUp()
        {
            return KeyState == State.Up;
        }

        public static void BindKeyUp(KeyBinding binding, Action onKeyUp)
        {
            binding.KeyUpEvent += onKeyUp;
        }

        public static void BindKeyDown(KeyBinding binding, Action onKeyDown)
        {
            binding.KeyDownEvent += onKeyDown;
        }

        public static void BindKeyEnter(KeyBinding binding, Action onKeyEnter)
        {
            binding.KeyEnterEvent += onKeyEnter;
        }
    }
}
