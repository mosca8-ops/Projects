using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TXT.WEAVR.TextEditting
{

    public interface ITextFormatter
    {
        event Action<int> OnCursorMoved;
        event Action<int> OnCursorInvalidMove;
        event Action<char> OnCharInserted;
        event Action<char> OnInvalidChar;

        event Action<string> OnTextChanged;

        bool AutoAdvanceCursor { get; set; }
        bool CircularCursor { get; set; }

        string FormatString { get; set; }
        int CursorPosition { get; set; }
        int CursorInFormattedText { get; }
        string Text { get; set; }
        string PureText { get; set; }
        bool TextIsValid { get; }
        bool CursorIsAtEnd { get; set; }

        bool IsValidForInsertion(char c, int cursorPosition);
        bool Insert(int index, char character);
        void Insert(char character);
        void Delete();
        void Delete(int index);
        void Append(char character);
        void Append(string str);
        void Insert(string str);
        void Clear();
    }

    [Serializable]
    public struct FormatChar
    {
        private static char k_invalidChar = (char)0;
        private static char k_invalidReplaceChar = 'X';

        public char symbol;
        public string allowedCharacters;
        public char replaceSymbol;

        public static FormatChar @default = new FormatChar(k_invalidReplaceChar, k_invalidReplaceChar) { isInvalid = true };
        public static FormatChar CreateFixed(char symbol) => new FormatChar(symbol, symbol) { isInvalid = true };

        private bool isInvalid;

        public FormatChar(char digit, string allowedCharacters)
        {
            this.symbol = digit;
            this.allowedCharacters = allowedCharacters;
            replaceSymbol = ' ';

            isInvalid = false;
        }

        public FormatChar(char digit, char replaceSymbol = ' ')
        {
            this.symbol = digit;
            allowedCharacters = null;
            this.replaceSymbol = replaceSymbol;

            isInvalid = false;
        }

        public bool Contains(char c)
        {
            if (isInvalid) { return false; }
            if (allowedCharacters == null) { return true; }
            for (int i = 0; i < allowedCharacters.Length; i++)
            {
                if (allowedCharacters[i] == c)
                {
                    return true;
                }
            }
            return false;
        }

        public char Format(char c)
        {
            if (isInvalid) { return replaceSymbol; }
            if (allowedCharacters == null) { return c; }
            for (int i = 0; i < allowedCharacters.Length; i++)
            {
                if (allowedCharacters[i] == c)
                {
                    return c;
                }
            }
            return replaceSymbol;
        }
    }

    public abstract class AbstractTextFormatter : ITextFormatter
    {
        protected string m_pureValue;
        protected string m_text;
        protected int m_cursorPosition;
        protected int m_formatCursorPosition;

        protected StringBuilder m_tempSB { get; private set; } = new StringBuilder();

        public bool AutoAdvanceCursor { get; set; }
        public bool CircularCursor { get; set; }

        public event Action<int> OnCursorMoved;
        public event Action<int> OnCursorInvalidMove;
        public event Action<char> OnCharInserted;
        public event Action<char> OnInvalidChar;

        public event Action<string> OnTextChanged;

        protected string m_formatString;
        public string FormatString
        {
            get => m_formatString;
            set
            {
                if (m_formatString != value)
                {
                    m_formatString = value;
                    m_pureValue = string.Empty;
                    Rebuild();
                }
            }
        }

        public int CursorPosition
        {
            get => m_cursorPosition;
            set
            {
                if (m_cursorPosition != value)
                {
                    if (ValidateCursorPosition(value, out int newPosition, out int newFormatPosition))
                    {
                        m_cursorPosition = newPosition;
                        m_formatCursorPosition = newFormatPosition;
                        OnCursorMoved?.Invoke(m_cursorPosition);
                    }
                    else
                    {
                        OnCursorInvalidMove?.Invoke(value);
                    }
                }
            }
        }

        public int CursorInFormattedText => m_formatCursorPosition;

        public string Text
        {
            get => m_text;
            set
            {
                if (m_text != value)
                {
                    PureText = DeformatText(value);
                }
            }
        }

        public string PureText
        {
            get => m_pureValue;
            set
            {
                if (m_pureValue != value)
                {
                    var prevText = m_text;
                    ApplyFormat(value);
                    if (prevText != m_text)
                    {
                        OnTextChanged?.Invoke(m_text);
                    }
                }
            }
        }

        public virtual bool TextIsValid => true;

        public virtual bool CursorIsAtEnd
        {
            get => m_cursorPosition >= m_pureValue.Length;
            set
            {
                if (value)
                {
                    if(ValidateCursorPosition(m_pureValue.Length, out int newPosition, out int newFormatPosition))
                    {
                        m_cursorPosition = newPosition;
                        m_formatCursorPosition = newFormatPosition;
                    }
                }
            }
        }

        private void SetCursorPositionSilent(int value)
        {
            if (m_cursorPosition != value && ValidateCursorPosition(value, out int newPosition, out int newFormatPosition))
            {
                m_cursorPosition = newPosition;
                m_formatCursorPosition = newFormatPosition;
                OnCursorMoved?.Invoke(m_cursorPosition);
            }
        }

        protected void TextChanged() => OnTextChanged?.Invoke(Text);
        protected void CharacterInserted(char c) => OnCharInserted?.Invoke(c);
        protected void CharacterInvalid(char c) => OnInvalidChar?.Invoke(c);
        protected void CursorMoved(int newPosition) => OnCursorMoved?.Invoke(newPosition);
        protected void CursorInvalid(int currentCursorPosition) => OnCursorInvalidMove?.Invoke(currentCursorPosition);

        protected abstract void ApplyFormat(string value);

        protected abstract bool ValidateCursorPosition(int proposedPosition, out int newPosition, out int newPositionInFormattedText);

        protected abstract void Rebuild();

        protected abstract string DeformatText(string text);

        public virtual void Clear() => PureText = string.Empty;

        public static bool HasCharacter(string s, char c)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == c)
                {
                    return true;
                }
            }
            return false;
        }

        protected abstract bool InsertChar(int index, char c);

        public virtual bool IsValidForInsertion(char c, int cursorPosition) => true;

        public bool Insert(int index, char character)
        {
            if (InsertChar(index, character))
            {
                OnCharInserted?.Invoke(character);
                return true;
            }
            else
            {
                OnInvalidChar?.Invoke(character);
                return false;
            }
        }

        public virtual void Insert(char character)
        {
            if (Insert(m_cursorPosition, character) && AutoAdvanceCursor)
            {
                TextChanged();
                if (AutoAdvanceCursor)
                {
                    SetCursorPositionSilent(CursorPosition + 1);
                }
            }
        }

        public void Delete()
        {
            Delete(m_cursorPosition);
        }

        public virtual void Delete(int index)
        {
            if (index < m_pureValue.Length)
            {
                PureText = PureText.Remove(index, 1);
            }
        }

        public virtual void Append(char character)
        {
            if (Insert(m_pureValue.Length, character) && AutoAdvanceCursor)
            {
                TextChanged();
                if (AutoAdvanceCursor)
                {
                    SetCursorPositionSilent(m_pureValue.Length);
                }
            }
        }

        public virtual void Append(string str)
        {
            PureText += str;
            if (AutoAdvanceCursor)
            {
                SetCursorPositionSilent(m_pureValue.Length);
            }
        }

        public virtual void Insert(string character)
        {
            if (character != null && character.Length > 0)
            {
                for (int i = 0; i < character.Length; i++)
                {
                    Insert(character[i]);
                }
            }
        }
    }
}
