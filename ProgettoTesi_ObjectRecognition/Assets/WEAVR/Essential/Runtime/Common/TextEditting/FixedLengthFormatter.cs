using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TXT.WEAVR.TextEditting
{

    public class FixedLengthFormatter : AbstractTextFormatter
    {
        private StringBuilder m_pureValueSB;
        private StringBuilder m_fullTextSB;
        private string m_pureFormattedValue;
        private int m_fixedFormatInputSize;

        private Slot[] m_slots;
        private (int formatIndex, FormatChar formatSymbol)[] m_pureToFormatMap;

        private List<FormatChar> m_formatters;
        private List<Color> m_charsColors;

        public bool ShowFullFormat { get; set; } = true;
        public bool AllowCursorOverflow { get; set; } = true;
        public bool AllowInvalidCharInsertion { get; set; } = true;
        public bool BlockCursorOnInvalidChar { get; set; } = false;
        public char DefaultReplaceChar { get; set; } = ' ';
        public Color? InvalidCharColor { get; set; }

        public IEnumerable<FormatChar> Formatters
        {
            get => m_formatters;
            set
            {
                if(m_formatters != value)
                {
                    m_formatters = new List<FormatChar>(value);
                    //Rebuild();
                }
            }
        }

        private class Slot
        {
            public int pureIndex;
            public FormatChar formatChar;
            public Color? color;
            public bool placeholder;
            public int textIndex;
            public int colorIndex;
        }

        public FixedLengthFormatter(IEnumerable<FormatChar> formatChars, IEnumerable<Color> charsColors)
        {
            m_formatters = formatChars != null ? new List<FormatChar>(formatChars) : new List<FormatChar>();
            m_pureValueSB = new StringBuilder();
            m_fullTextSB = new StringBuilder();
            m_charsColors = new List<Color>(charsColors);
        }

        public override void Clear()
        {
            CursorPosition = 0;
            PureText = string.Empty;
        }

        public override bool IsValidForInsertion(char c, int cursorPosition)
        {
            return m_pureToFormatMap[cursorPosition].formatSymbol.Contains(c);
        }

        public override void Delete(int index)
        {
            if(index < m_pureValue.Length)
            {
                int formatIndex = m_pureToFormatMap[index].formatIndex;
                var slot = m_slots[formatIndex];

                char c = slot.formatChar.replaceSymbol;
                m_fullTextSB[slot.textIndex] = c;
                m_pureValueSB[index] = c;

                m_pureValue = m_pureValueSB.ToString();
                m_text = m_fullTextSB.ToString();

                TextChanged();
            }
        }

        public override bool TextIsValid
        {
            get
            {
                for (int i = 0; i < m_pureValue.Length; i++)
                {
                    if(!m_pureToFormatMap[i].formatSymbol.Contains(m_pureValue[i]) || m_pureValue[i] == DefaultReplaceChar)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        protected override bool InsertChar(int index, char c)
        {
            if (index >= m_pureValue.Length)
            {
                while (m_pureValueSB.Length < index)
                {
                    m_pureValueSB.Append(m_pureToFormatMap[m_pureValueSB.Length].formatSymbol.replaceSymbol);
                }
                m_pureValueSB.Append(c);
                m_pureValue = m_pureValueSB.ToString();
                return InsertChar(index, c);
            }
            int formatIndex = m_pureToFormatMap[index].formatIndex;
            var slot = m_slots[formatIndex];

            if (!slot.formatChar.Contains(c))
            {
                if (AllowInvalidCharInsertion)
                {
                    m_fullTextSB[slot.textIndex] = c;
                    //m_pureValueSB[index] = c;

                    if (InvalidCharColor.HasValue && slot.colorIndex > 0)
                    {
                        m_fullTextSB.Remove(slot.colorIndex, 6);
                        m_fullTextSB.Insert(slot.colorIndex, ColorUtility.ToHtmlStringRGBA(InvalidCharColor.Value));
                    }

                    //m_pureValue = m_pureValueSB.ToString();
                    m_text = m_fullTextSB.ToString();
                    TextChanged();
                }

                m_pureValueSB[index] = c;
                m_pureValue = m_pureValueSB.ToString();
                return false;
            }

            c = slot.formatChar.Format(c);
            m_fullTextSB[slot.textIndex] = c;
            m_pureValueSB[index] = c;

            if(AllowInvalidCharInsertion && InvalidCharColor.HasValue && slot.colorIndex > 0 && slot.color.HasValue)
            {
                m_fullTextSB.Remove(slot.colorIndex, 6);
                m_fullTextSB.Insert(slot.colorIndex, ColorUtility.ToHtmlStringRGBA(slot.color.Value));
            }

            m_pureValue = m_pureValueSB.ToString();
            m_text = m_fullTextSB.ToString();
            return true;
        }

        protected override void ApplyFormat(string value)
        {
            int index = 0;
            m_pureValue = value;
            m_pureValueSB.Clear();
            m_fullTextSB.Clear();

            for (int i = 0; i < m_slots.Length; i++)
            {
                var slot = m_slots[i];
                char c = slot.placeholder && !ShowFullFormat ? m_formatters[0].replaceSymbol : slot.formatChar.symbol;
                if (slot.placeholder && index < value.Length)
                {
                    c = slot.formatChar.Format(value[index]);
                    m_pureValueSB.Append(c);
                    index++;
                }
                if (slot.color.HasValue)
                {
                    m_fullTextSB.Append($"<color=#");
                    slot.colorIndex = m_fullTextSB.Length;
                    m_fullTextSB.Append(ColorUtility.ToHtmlStringRGBA(slot.color.Value)).Append('>');
                    slot.textIndex = m_fullTextSB.Length;
                    m_fullTextSB.Append(c).Append("</color>");
                }
                else
                {
                    slot.textIndex = m_fullTextSB.Length;
                    m_fullTextSB.Append(c);
                }
            }

            m_pureValue = m_pureValueSB.ToString();
            //m_pureFormattedValue = m_pureFormattedValueSB.ToString();
            m_text = m_fullTextSB.ToString();
        }

        protected override string DeformatText(string text)
        {
            m_tempSB.Clear();
            for (int i = 0, j = 0; i < m_formatString.Length && j < text.Length; i++)
            {
                if (m_slots[i].placeholder)
                {
                    if (m_slots[i].formatChar.Contains(text[j]))
                    {
                        m_tempSB.Append(text[j]);
                        j++;
                    }
                    else
                    {
                        m_fullTextSB[m_slots[i].textIndex] = m_slots[i].formatChar.replaceSymbol;
                        //m_tempSB.Append(m_slots[i].formatChar.replaceSymbol);
                        j++;
                    }
                    //m_tempSB.Append(m_slots[i].formatChar.Format(text[j]));

                }
                else if (text[j] == m_formatString[i])
                {
                    j++;
                    continue;
                }
            }
            return m_tempSB.ToString();
        }

        protected override void Rebuild()
        {
            m_fixedFormatInputSize = 0;
            m_slots = new Slot[m_formatString.Length];

            for (int i = 0; i < m_formatString.Length; i++)
            {
                int formatSymbolIndex = m_formatters.FindIndex(f => f.symbol == m_formatString[i]);
                if (formatSymbolIndex >= 0)
                {
                    m_slots[i] = new Slot()
                    {
                        pureIndex = m_fixedFormatInputSize,
                        color = m_charsColors.Count > i ? m_charsColors[i] : (Color?)null,
                        formatChar = m_formatters[formatSymbolIndex],
                        placeholder = true,
                        colorIndex = -1,
                    };
                    m_fixedFormatInputSize++;
                }
                else
                {
                    m_slots[i] = new Slot()
                    {
                        pureIndex = -1,
                        color = m_charsColors.Count > i ? m_charsColors[i] : (Color?)null,
                        formatChar = FormatChar.CreateFixed(m_formatString[i]),
                        placeholder = false,
                        colorIndex = -1,
                    };
                }
            }

            m_pureToFormatMap = new (int, FormatChar)[m_fixedFormatInputSize];

            int lastIndex = m_fixedFormatInputSize - 1;
            for (int i = m_slots.Length - 1; i >= 0; i--)
            {
                if (m_slots[i].pureIndex < 0)
                {
                    m_slots[i].pureIndex = lastIndex;
                }
                else
                {
                    lastIndex = m_slots[i].pureIndex;
                    m_pureToFormatMap[lastIndex] = (i, m_slots[i].formatChar);
                }
            }

            m_fullTextSB.Clear();
            for (int i = 0; i < m_slots.Length; i++)
            {
                var slot = m_slots[i];
                char c = slot.placeholder && !ShowFullFormat ? m_formatters[0].replaceSymbol : slot.formatChar.symbol;
                if (slot.color.HasValue)
                {
                    m_fullTextSB.Append($"<color=#");
                    slot.colorIndex = m_fullTextSB.Length;
                    m_fullTextSB.Append(ColorUtility.ToHtmlStringRGBA(slot.color.Value)).Append('>');
                    slot.textIndex = m_fullTextSB.Length;
                    m_fullTextSB.Append(c).Append("</color>");
                }
                else
                {
                    slot.textIndex = m_fullTextSB.Length;
                    m_fullTextSB.Append(c);
                }
            }

            if (!string.IsNullOrEmpty(m_pureValue))
            {
                ApplyFormat(m_pureValue);
            }

            if(m_formatCursorPosition == 0)
            {
                m_formatCursorPosition = m_pureToFormatMap[0].formatIndex;
            }
        }

        protected override bool ValidateCursorPosition(int value, out int newPosition, out int newPositionInFormattedText)
        {
            if(AllowInvalidCharInsertion && BlockCursorOnInvalidChar)
            {
                for (int i = 0; i < m_pureValue.Length; i++)
                {
                    if(m_pureValue[i] != DefaultReplaceChar && !m_pureToFormatMap[i].formatSymbol.Contains(m_pureValue[i]))
                    {
                        newPosition = i;
                        newPositionInFormattedText = m_pureToFormatMap[i].formatIndex;
                        return false;
                    }
                }
            }

            bool valid = true;

            if (!AllowCursorOverflow 
                && value > m_cursorPosition 
                && m_cursorPosition < m_pureValue.Length 
                && !m_pureToFormatMap[m_cursorPosition].formatSymbol.Contains(m_pureValue[m_cursorPosition]))
            {
                newPosition = m_cursorPosition;
                valid = false;
            }

            if (CircularCursor && value >= m_pureValue.Length && m_fixedFormatInputSize == m_pureValue.Length)
            {
                newPosition = 0;
            }
            else if (CircularCursor && value < 0)
            {
                newPosition = m_fixedFormatInputSize >= m_pureValue.Length ? m_pureValue.Length - 1 : m_pureValue.Length;
            }
            else
            {
                int effectiveLength = m_fixedFormatInputSize <= m_pureValue.Length ? m_pureValue.Length - 1 : m_pureValue.Length;
                valid &= 0 <= value && value <= effectiveLength;
                newPosition = Mathf.Clamp(value, 0, effectiveLength);
            }

            if (!AllowCursorOverflow && newPosition < m_pureValue.Length)
            {
                while (newPosition > 0)
                {
                    char c = m_pureValue[newPosition - 1];
                    if (c == DefaultReplaceChar || m_pureToFormatMap[newPosition - 1].formatSymbol.Contains(m_pureValue[newPosition - 1]))
                    {
                        break;
                    }
                    newPosition--;
                    valid |= CircularCursor;
                }
            }

            if (newPosition < 0)
            {
                newPosition = 0;
                valid = false;
                newPositionInFormattedText = m_formatCursorPosition;
            }
            else
            {
                newPositionInFormattedText = m_pureToFormatMap[newPosition].formatIndex;
            }
            return valid;
        }
    }
}