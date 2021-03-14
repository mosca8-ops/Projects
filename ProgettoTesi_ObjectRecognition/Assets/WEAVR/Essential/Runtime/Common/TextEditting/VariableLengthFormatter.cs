using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.TextEditting
{
    
    public class VariableLengthFormatter : AbstractTextFormatter
    {
        protected int m_startCursorPoint;
        private string m_pureFormattedValue;
        private string m_variablePureFormat;
        private string[] m_variableFormatPieces;

        protected override bool InsertChar(int index, char c)
        {
            PureText = PureText.Insert(index, new string(c, 1));
            return true;
        }

        protected override void ApplyFormat(string value)
        {
            m_pureValue = value;
            m_pureFormattedValue = string.Format(m_variablePureFormat, value);
            m_text = string.Format(m_formatString, value);
        }

        protected override string DeformatText(string text)
        {
            if (m_variableFormatPieces.All(p => text.Contains(p)))
            {
                for (int i = 0; i < m_variableFormatPieces.Length; i++)
                {
                    text = text.Replace(m_variableFormatPieces[i], string.Empty);
                }
            }
            return text;
        }

        protected override void Rebuild()
        {
            if (string.IsNullOrEmpty(m_formatString) || m_formatString.IndexOf("{0") >= m_formatString.IndexOf('}'))
            {
                m_formatString = "{0}";
                m_variablePureFormat = "{0}";
                m_startCursorPoint = 0;
                m_variableFormatPieces = new string[0];
            }
            else
            {
                int start = m_formatString.IndexOf("{0");
                int end = m_formatString.IndexOf('}');
                m_variablePureFormat = m_formatString.Substring(start, end - start);
                m_startCursorPoint = start;

                m_variableFormatPieces = new string[]
                {
                        m_formatString.Substring(0, start),
                        m_formatString.Substring(end + 1, m_formatString.Length - end - 1),
                };
            }
        }

        protected override bool ValidateCursorPosition(int value, out int newPosition, out int newPositionInFormattedText)
        {
            bool valid = true;
            if (CircularCursor && value > m_pureFormattedValue.Length)
            {
                newPosition = 0;
            }
            else if (CircularCursor && value < 0)
            {
                newPosition = m_pureFormattedValue.Length;
            }
            else
            {
                valid = 0 <= value && value <= m_pureFormattedValue.Length;
                newPosition = Mathf.Clamp(value, 0, m_pureFormattedValue.Length);
            }

            newPositionInFormattedText = m_startCursorPoint + newPosition;
            return valid;
        }
    }
}
